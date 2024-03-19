using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Machines;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using static StardewValley.Menus.CharacterCustomization;


namespace NermNermNerm.Junimatic
{
    public class WorkFinder : ISimpleLog
    {
        private ModEntry mod = null!;
        private readonly Dictionary<GameLocation, IReadOnlyList<MachineNetwork>> cachedNetworks = new();

        /// <summary>The number of Junimos that are being simulated out doing stuff.</summary>
        private readonly Dictionary<JunimoType, int> numAutomatedJumimos = Enum.GetValues<JunimoType>().ToDictionary(t => t, t => 0);

        /// <summary>The number of Junimos that are on-screen doing stuff.</summary>
        private readonly Dictionary<JunimoType, int> numAnimatedJunimos = Enum.GetValues<JunimoType>().ToDictionary(t => t, t => 0);

        private static readonly Point[] walkableDirections = [new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1)];
        private static readonly Point[] reachableDirections = [
            new Point(-1, -1), new Point(0, -1), new Point(1, -1),
            new Point(-1, 0), /*new Point(0, 0),*/ new Point(1, 0),
            new Point(-1, 1), new Point(0, 1), new Point(1, 1)];


        private const int TickIntervalBetweenChecksForVisibleActions = 10;
        private const int TickIntervalBetweenAutomatedActions = 20;

        public void Entry(ModEntry mod)
        {
            this.mod = mod;
            mod.Helper.Events.GameLoop.OneSecondUpdateTicked += this.GameLoop_OneSecondUpdateTicked;
            mod.Helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;
        }

        private void Input_ButtonPressed(object? sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.Insert)
            {
                this.DoJunimos(true);
            }
        }

        private void GameLoop_OneSecondUpdateTicked(object? sender, StardewModdingAPI.Events.OneSecondUpdateTickedEventArgs e)
        {
            bool isAutomationInterval = e.IsMultipleOf(TickIntervalBetweenAutomatedActions);
            bool isAnimatedInterval = e.IsMultipleOf(TickIntervalBetweenChecksForVisibleActions);

            if (isAutomationInterval || isAnimatedInterval)
            {
                // this.DoJunimos(isAutomationInterval);
            }
        }

        private void DoJunimos(bool isAutomationInterval)
        {
            var numAvailableJunimos = this.GetNumUnlockedJunimos();
            foreach (var junimoType in Enum.GetValues<JunimoType>())
            {
                numAvailableJunimos[junimoType] -= this.numAnimatedJunimos[junimoType];
                if (isAutomationInterval)
                {
                    this.numAutomatedJumimos[junimoType] = 0;
                }
                else
                {
                    numAvailableJunimos[junimoType] -= this.numAutomatedJumimos[junimoType];
                }
            }

            // Try to employ junimos in visible locations first:
            HashSet<GameLocation> animatedLocations = new HashSet<GameLocation>(Game1.getOnlineFarmers().Select(f => f.currentLocation));
            foreach (GameLocation location in animatedLocations)
            {
                foreach (var portal in new GameMap(location).GetPortals())
                {
                    foreach (var junimoType in Enum.GetValues<JunimoType>())
                    {
                        if (numAvailableJunimos[junimoType] > 0)
                        {
                            var project = this.FindProject(portal, junimoType, null, null);
                            if (project is not null)
                            {
                                this.LogTrace($"Starting Animated Junimo for {project}");
                                this.numAnimatedJunimos[junimoType] += 1;
                                location.characters.Add(new JunimoShuffler(project, this));
                            }
                        }
                    }
                }
            }

            if (isAutomationInterval)
            {
                foreach (GameLocation location in Game1.locations.Where(l => !animatedLocations.Contains(l)))
                {
                    foreach (var junimoType in Enum.GetValues<JunimoType>())
                    {
                        if (numAvailableJunimos[junimoType] > 0)
                        {
                            if (this.TryDoAutomationsForLocation(location, junimoType))
                            {
                                numAvailableJunimos[junimoType] -= 1;
                            }
                        }
                    }
                }
            }
        }

        private Dictionary<JunimoType, int> GetNumUnlockedJunimos()
        {
            var result = new Dictionary<JunimoType, int>();
            result.Add(JunimoType.CropProcessing, this.mod.CropMachineHelperQuest.IsUnlocked() ? 1 : 0);

            // TODO: Fix after refactor to classes
            result.Add(JunimoType.MiningProcessing, Game1.MasterPlayer.eventsSeen.Contains(ObjectIds.MiningJunimoDreamEvent) ? 1 : 0);
            result.Add(JunimoType.Animals, Game1.MasterPlayer.eventsSeen.Contains(ObjectIds.AnimalJunimoDreamEvent) ? 1 : 0);
            return result;
        }

        private bool TryDoAutomationsForLocation(GameLocation location, JunimoType projectType)
        {
            if (!this.cachedNetworks.TryGetValue(location, out var networks))
            {
                networks = this.BuildNetwork(location);
                this.cachedNetworks.Add(location, networks);
            }

            foreach (var network in networks)
            {
                var machines = network.Machines[projectType];

                // Try and load a machine
                foreach (var emptyMachine in machines.Where(m => m.IsIdle && m.IsCompatibleWithJunimo(projectType)))
                {
                    foreach (var chest in network.Chests)
                    {
                        if (emptyMachine.FillMachineFromChest(chest))
                        {
                            this.LogInfo($"Automatic machine fill of {emptyMachine} on {location.Name} from {chest}");
                            return true;
                        }
                    }
                }

                // Try and empty a machine
                foreach (var fullMachine in machines)
                {
                    if (fullMachine.HeldObject is not null && fullMachine.IsCompatibleWithJunimo(projectType))
                    {
                        var goodChest = network.Chests.FirstOrDefault(c => c.IsPreferredStorageForMachinesOutput(fullMachine.HeldObject));
                        if (goodChest is null)
                        {
                            goodChest = network.Chests.FirstOrDefault(c => c.IsPossibleStorageForMachinesOutput(fullMachine.HeldObject));
                        }

                        if (goodChest is not null)
                        {
                            string wasHolding = fullMachine.HeldObject.Name;
                            if (fullMachine.TryPutHeldObjectInStorage(goodChest))
                            {
                                this.LogInfo($"Automatic machine empty of {fullMachine} holding {wasHolding} on {location.Name} into {goodChest}");
                            }
                            else
                            {
                                this.LogError($"FAILED: Automatic machine empty of {fullMachine} holding {fullMachine.HeldObject.Name} on {location.Name} into {goodChest}");
                            }

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        record class MachineNetwork(
            IReadOnlyDictionary<JunimoType,IReadOnlyList<GameMachine>> Machines,
            IReadOnlyList<GameStorage> Chests);

        private List<MachineNetwork> BuildNetwork(GameLocation location)
        {
            var map = new GameMap(location);
            var result = new List<MachineNetwork>();
            var portals = map.GetPortals();
            foreach (var portal in portals)
            {
                var machines = new Dictionary<JunimoType, List<GameMachine>>(Enum.GetValues<JunimoType>().Select(e => new KeyValuePair<JunimoType, List<GameMachine>>(e, new List<GameMachine>())));
                var chests = new List<GameStorage>();
                var visitedTiles = new HashSet<Point>();

                map.GetStartingInfo(portal, out var startingPoints, out var walkableFloorTypes);

                foreach (var startingTile in startingPoints)
                {
                    var tilesToInvestigate = new Queue<Point>();
                    tilesToInvestigate.Enqueue(startingTile);

                    while (tilesToInvestigate.TryDequeue(out var reachableTile))
                    {
                        if (visitedTiles.Contains(reachableTile))
                        {
                            continue;
                        }

                        foreach (var direction in reachableDirections)
                        {
                            var adjacentTile = reachableTile + direction;
                            if (visitedTiles.Contains(adjacentTile))
                            {
                                continue;
                            }

                            map.GetThingAt(adjacentTile, reachableTile, walkableFloorTypes, out bool isWalkable, out var machine, out var storage);
                            isWalkable &= direction.X == 0 || direction.Y == 0; // only really walkable if it's not on a diagonal from where we are.
                            if (storage is not null)
                            {
                                chests.Add(storage);
                                visitedTiles.Add(adjacentTile);
                            }
                            else if (machine is not null)
                            {
                                foreach (JunimoType junimoType in Enum.GetValues<JunimoType>())
                                {
                                    if (machine.IsCompatibleWithJunimo(junimoType))
                                    {
                                        machines[junimoType].Add(machine);
                                    }
                                }
                                visitedTiles.Add(adjacentTile);
                            }
                            else if (isWalkable)
                            {
                                tilesToInvestigate.Enqueue(adjacentTile);
                            }
                            else
                            {
                                visitedTiles.Add(adjacentTile);
                            }
                        }

                        visitedTiles.Add(reachableTile);
                    }

                    result.Add(new MachineNetwork(machines.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<GameMachine>)pair.Value), chests));
                }
            }

            return result;
        }

        public JunimoAssignment? FindProject(StardewValley.Object portal, JunimoType projectType, Point? junimoLocation, Point? oldOrigin)
        {
            // This duplicates the logic in BuildNetwork, except that it's trying to find the closest path and
            // it stops as soon as it cooks up something to do.

            var location = portal.Location;
            // These lists are all in order of nearest to farthest from the portal
            var emptyMachines = new List<GameMachine>();
            var fullMachines = new List<GameMachine>();
            var knownChests = new List<GameStorage>();
            var visitedTiles = new HashSet<Point>();

            var map = new GameMap(location);

            map.GetStartingInfo(portal, out var startingPoints, out var walkableFloorTypes);
            if (junimoLocation.HasValue)
            {
                startingPoints = new List<Point>() { junimoLocation.Value };
            }

            foreach (var startingTile in startingPoints)
            {
                var originTile = oldOrigin ?? startingTile;
                var tilesToInvestigate = new Queue<Point>();
                tilesToInvestigate.Enqueue(startingTile);

                while (tilesToInvestigate.TryDequeue(out var tile))
                {
                    if (visitedTiles.Contains(tile))
                    {
                        continue;
                    }

                    foreach (var direction in walkableDirections)
                    {
                        var adjacentTile = tile + direction;
                        if (visitedTiles.Contains(adjacentTile))
                        {
                            continue;
                        }

                        map.GetThingAt(adjacentTile, tile, walkableFloorTypes, out bool isWalkable, out var machine, out var chest);

                        if (chest is not null)
                        {
                            // See if we can create a mission to carry from a full machine to this chest
                            foreach (var machineNeedingPickup in fullMachines)
                            {
                                if (chest.IsPreferredStorageForMachinesOutput(machineNeedingPickup.HeldObject!))
                                {
                                    return new JunimoAssignment(projectType, location, portal, originTile, machineNeedingPickup, chest, itemsToRemoveFromChest: null);
                                }
                            }

                            // See if we can create a mission to carry from this chest to an idle machine
                            foreach (var machineNeedingDelivery in emptyMachines)
                            {
                                var inputs = machineNeedingDelivery.GetRecipeFromChest(chest);
                                if (inputs is not null)
                                {
                                    return new JunimoAssignment(projectType, location, portal, originTile, chest, machineNeedingDelivery, inputs);
                                }
                            }

                            knownChests.Add(chest);
                            visitedTiles.Add(adjacentTile);
                        }
                        else if (machine is not null && machine.IsCompatibleWithJunimo(projectType))
                        {
                            if (machine.HeldObject is not null)
                            {
                                // Try and find a chest to tote it to
                                var targetChest = knownChests.FirstOrDefault(chest => chest.IsPreferredStorageForMachinesOutput(machine.HeldObject));
                                if (targetChest is not null)
                                {
                                    return new JunimoAssignment(projectType, location, portal, originTile, machine, targetChest, itemsToRemoveFromChest: null);
                                }

                                fullMachines.Add(machine);
                            }
                            else if (machine.IsIdle)
                            {
                                // Try and find a chest to supply it from
                                foreach (var sourceChest in knownChests)
                                {
                                    var inputs = machine.GetRecipeFromChest(sourceChest);
                                    if (inputs is not null)
                                    {
                                        return new JunimoAssignment(projectType, location, portal, originTile, sourceChest, machine, inputs);
                                    }
                                }

                                emptyMachines.Add(machine);
                            }
                            visitedTiles.Add(adjacentTile);
                        }
                        else if (isWalkable)
                        {
                            tilesToInvestigate.Enqueue(adjacentTile);
                        }
                        else
                        {
                            visitedTiles.Add(adjacentTile);
                        }
                    }

                    visitedTiles.Add(tile);
                }

                // Couldn't find any work delivering to machines or pulling from machines where there was an existing stack.
                // The last type of work we're willing to take on is to pull from a machine and place the item in the closest
                // chest with room to spare for a new stack.
                var fullMachine = fullMachines.FirstOrDefault();
                if (fullMachine is not null)
                {
                    var chestWithSpace = knownChests.FirstOrDefault(chest => chest.IsPossibleStorageForMachinesOutput(fullMachine.HeldObject!));
                    if (chestWithSpace is not null)
                    {
                        return new JunimoAssignment(projectType, location, portal, originTile, fullMachine, chestWithSpace, itemsToRemoveFromChest: null);
                    }
                }

                // Maybesomeday:  Store a list of all the tiles we walked, and walk the whole list again looking for chests and
                // machines on diagonals.
            }

            return null;
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            this.mod.WriteToLog(message, level, isOnceOnly);
        }

        public void JunimoWentHome(JunimoType projectType)
        {
            if (this.numAnimatedJunimos[projectType] == 0)
            {
                this.LogError($"Animated {projectType} junimo returned, but we have no record of him leaving!");
            }
            else
            {
                this.LogTrace($"Animated {projectType} junimo returned to the pool");
                this.numAnimatedJunimos[projectType] -= 1;
            }
        }
    }
}
