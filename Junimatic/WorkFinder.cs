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

        private static readonly Vector2[] walkableDirections = [new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1)];
        private static readonly Vector2[] placeableDirections = walkableDirections; // <- TODO: Add fancier stuff for chests only reachable at a diagonal
                                                                                    //new Vector2[] {
                                                                                    //new Vector2(-1, -1), new Vector2(0, -1), new Vector2(1, -1),
                                                                                    //new Vector2(-1, 0), new Vector2(1, 0),
                                                                                    //new Vector2(-1, 1), new Vector2(0, 1), new Vector2(1, 1) };


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
                var portals = location.objects.Values.Where(o => o.ItemId == ObjectIds.JunimoPortal);
                foreach (var portal in portals)
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
                foreach (var emptyMachine in machines.Select(mandp => mandp.Machine).Where(m => m.heldObject.Value is null && m.MinutesUntilReady <= 0 && this.IsMachineCompatibleWithProjectType(m, projectType)))
                {
                    foreach (var chest in network.Chests.Select(candp => candp.Machine))
                    {
                        if (emptyMachine.AttemptAutoLoad(chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID), Game1.MasterPlayer))
                        {
                            this.LogInfo($"Automatic machine fill of {emptyMachine.Name} at {emptyMachine.TileLocation} on {location.Name} from chest at {chest.TileLocation}");
                            return true;
                        }
                    }
                }

                // Try and empty a machine
                foreach (var fullMachine in machines.Select(mandp => mandp.Machine).Where(m => m.heldObject.Value is not null && this.IsMachineCompatibleWithProjectType(m, projectType)))
                {
                    var goodChest = network.Chests.Select(candp => candp.Machine).FirstOrDefault(c => this.IsChestGoodForStoring(c, fullMachine.heldObject.Value));
                    if (goodChest is null)
                    {
                        goodChest = network.Chests.Select(candp => candp.Machine).FirstOrDefault(chest => chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID).Count < chest.GetActualCapacity());
                    }

                    if (goodChest is not null)
                    {
                        if (goodChest.addItem(fullMachine.heldObject.Value) is not null)
                        {
                            this.LogError($"FAILED: Automatic machine empty of {fullMachine.Name} at {fullMachine.TileLocation} holding {fullMachine.heldObject.Value.Name} on {location.Name} into chest at {goodChest.TileLocation}");
                        }
                        else
                        {
                            this.LogInfo($"Automatic machine empty of {fullMachine.Name} at {fullMachine.TileLocation} holding {fullMachine.heldObject.Value.Name} on {location.Name} into chest at {goodChest.TileLocation}");
                        }

                        fullMachine.heldObject.Value = null;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///   Gets an identifier for the floor-tile at the coordinate if the tile is indeed on a floor tile.
        /// </summary>
        /// <returns>
        ///   null if there's not a walkable tile at that spot, else an identifier for the type of floor it is.
        ///   The identifier isn't guaranteed to be an itemid or anything, just unique to the floor type.
        /// </returns>
        private string? getFlooringId(StardewValley.GameLocation l, Vector2 tile)
        {
            l.terrainFeatures.TryGetValue(tile, out var terrainFeatures);
            return (terrainFeatures as Flooring)?.whichFloor.Value;

            // TODO: If empty flooring is valid in this location, check and see if the
            //   tile is just empty and passable, then return a magic identifier.
        }

        private bool IsMachineCompatibleWithProjectType(StardewValley.Object machine, JunimoType projectType)
        {
            if (machine.Name == "Furnace" && projectType == JunimoType.MiningProcessing)
            {
                return true;
            }
            if ((machine.Name == "Keg" || machine.Name == "Cask" || machine.Name == "Preserves Jar") && projectType == JunimoType.CropProcessing)
            {
                return true;
            }
            if ((machine.Name == "Mayonaise Machine" || machine.Name == "Cheese Press") && projectType == JunimoType.Animals)
            {
                return true;
            }

            return false;
        }

        private bool CanMachineBeSuppliedWithChest(StardewValley.Object machine, Chest source, out List<Item> inputs)
        {
            var machineData = machine.GetMachineData();
            inputs = new List<Item>();

            // Ensure it has the coal (aka all the 'AdditionalConsumedItems')
            if (machineData.AdditionalConsumedItems is not null)
            {
                if (!machineData.AdditionalConsumedItems.All(consumedItem => source.GetItemsForPlayer(Game1.player.UniqueMultiplayerID).Any(chestItem => chestItem.QualifiedItemId == consumedItem.ItemId && chestItem.Stack >= consumedItem.RequiredCount)))
                {
                    return false;
                }
                inputs.AddRange(machineData.AdditionalConsumedItems.Select(i => ItemRegistry.Create(i.ItemId, i.RequiredCount)));
            }

            foreach (var rule in machineData.OutputRules)
            {
                foreach (var trigger in rule.Triggers)
                {
                    var sourceInventory = source.GetItemsForPlayer(Game1.player.UniqueMultiplayerID);
                    var possibleItem = sourceInventory
                        .FirstOrDefault(i => (trigger.RequiredItemId is not null && i.QualifiedItemId == trigger.RequiredItemId)
                                 || (trigger.RequiredTags is not null && trigger.RequiredTags.Any(tag => i.HasContextTag(tag))));
                    if (possibleItem is not null && (possibleItem.Stack >= trigger.RequiredCount || sourceInventory.Where(i => i.itemId == possibleItem.itemId).Sum(i => i.Stack) > trigger.RequiredCount))
                    {
                        inputs.Add(ItemRegistry.Create(possibleItem.ItemId, trigger.RequiredCount));
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///   Returns true if the chest should be used to store a given item.
        /// </summary>
        /// <returns>True if the chest has stacks of an item and room to store more.</returns>
        private bool IsChestGoodForStoring(Chest chest, StardewValley.Object objectToStore)
        {
            var items = chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID);
            if (items.HasEmptySlots())
            {
                return items.Any(i => i.ItemId == objectToStore.ItemId);
            }
            else
            {
                return items.Any(i => i.ItemId == objectToStore.ItemId && i.Stack < 999 /* maximum stack size */);
            }
        }


        record ObjectAndReachablePosition<T>(Vector2 Location, T Machine);

        // TODO: Get rid of the reachable position if the idea that rebuilding the whole network for animated junimos is an okay idea pans out.

        record class MachineNetwork(
            IReadOnlyDictionary<JunimoType,IReadOnlyList<ObjectAndReachablePosition<StardewValley.Object>>> Machines,
            IReadOnlyList<ObjectAndReachablePosition<Chest>> Chests,
            ObjectAndReachablePosition<StardewValley.Object> Portal);

        private List<MachineNetwork> BuildNetwork(GameLocation location)
        {
            var result = new List<MachineNetwork>();
            var portals = location.objects.Values.Where(o => o.ItemId == ObjectIds.JunimoPortal);
            foreach (var portal in portals)
            {
                // These lists are all in order of nearest to farthest from the portal
                var machines = new Dictionary<JunimoType, List<ObjectAndReachablePosition<StardewValley.Object>>>() {
                    { JunimoType.MiningProcessing, new List<ObjectAndReachablePosition<StardewValley.Object>>()},
                    { JunimoType.Animals, new List<ObjectAndReachablePosition<StardewValley.Object>>()},
                    { JunimoType.CropProcessing, new List<ObjectAndReachablePosition<StardewValley.Object>>()},
                };
                var chests = new List<ObjectAndReachablePosition<Chest>>();
                var visitedTiles = new HashSet<Vector2>();

                var x = walkableDirections
                    .Select(d => portal.TileLocation + d)
                    .Select(tile => this.getFlooringId(location, tile));

                var validFloorTilesToWalk = new HashSet<string>(
                    walkableDirections
                    .Select(d => portal.TileLocation + d)
                    .Select(tile => this.getFlooringId(location, tile))
                    .Where(id => id is not null)
                    .Select(s => s!) /* compiler does not understand the above where clause guarantees a non-null result. */);

                var startingPoints = walkableDirections
                        .Select(direction => portal.TileLocation + direction)
                        .Where(tile => this.getFlooringId(location, tile) is not null)
                        .ToArray();

                foreach (var startingTile in startingPoints)
                {
                    var tilesToInvestigate = new Queue<Vector2>();
                    tilesToInvestigate.Enqueue(startingTile);

                    while (tilesToInvestigate.TryDequeue(out var tile))
                    {
                        if (visitedTiles.Contains(tile))
                        {
                            continue;
                        }

                        foreach (var direction in placeableDirections)
                        {
                            var adjacentTile = tile + direction;
                            if (visitedTiles.Contains(adjacentTile))
                            {
                                continue;
                            }

                            var objectAtTile = location.getObjectAtTile((int)adjacentTile.X, (int)adjacentTile.Y);
                            if (objectAtTile is Chest chest)
                            {
                                chests.Add(new ObjectAndReachablePosition<Chest>(tile, chest));
                                visitedTiles.Add(adjacentTile);
                            }
                            else if (objectAtTile is not null)
                            {
                                foreach (JunimoType junimoType in Enum.GetValues<JunimoType>())
                                {
                                    if (this.IsMachineCompatibleWithProjectType(objectAtTile, junimoType))
                                    {
                                        machines[junimoType].Add(new ObjectAndReachablePosition<StardewValley.Object>(tile, objectAtTile));
                                    }
                                }
                                visitedTiles.Add(adjacentTile);
                            }
                            else if (objectAtTile is null && (direction.X == 0 || direction.Y == 0) && this.getFlooringId(location, adjacentTile) is string floorId && validFloorTilesToWalk.Contains(floorId))
                            {
                                tilesToInvestigate.Enqueue(adjacentTile);
                            }
                        }

                        visitedTiles.Add(tile);
                    }

                    result.Add(new MachineNetwork(machines.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<ObjectAndReachablePosition<StardewValley.Object>>)pair.Value), chests, new ObjectAndReachablePosition<StardewValley.Object>(startingTile, portal)));
                }
            }

            return result;
        }

        public JunimoAssignment? FindProject(StardewValley.Object portal, JunimoType projectType, Vector2? junimoLocation, Vector2? oldOrigin)
        {
            var location = portal.Location;
            // These lists are all in order of nearest to farthest from the portal
            var emptyMachines = new List<(Vector2 location, StardewValley.Object machine)>();
            var fullMachines = new List<(Vector2 location, StardewValley.Object machine)>();
            var knownChests = new List<(Vector2 location, Chest chest)>();
            var visitedTiles = new HashSet<Vector2>();

            var x = walkableDirections
                .Select(d => portal.TileLocation + d)
                .Select(tile => this.getFlooringId(location, tile));

            var validFloorTilesToWalk = new HashSet<string>(
                walkableDirections
                .Select(d => portal.TileLocation + d)
                .Select(tile => this.getFlooringId(location, tile))
                .Where(id => id is not null)
                .Select(s=>s!) /* compiler does not understand the above where clause guarantees a non-null result. */);

            var startingPoints = junimoLocation.HasValue
                ? new Vector2[] { junimoLocation.Value }
                : walkableDirections
                    .Select(direction => portal.TileLocation + direction)
                    .Where(tile => this.getFlooringId(location, tile) is not null)
                    .ToArray();

            foreach (var startingTile in startingPoints)
            {
                var originTile = oldOrigin ?? startingTile;
                var tilesToInvestigate = new Queue<Vector2>();
                tilesToInvestigate.Enqueue(startingTile);

                while (tilesToInvestigate.TryDequeue(out var tile))
                {
                    if (visitedTiles.Contains(tile))
                    {
                        continue;
                    }

                    foreach (var direction in placeableDirections)
                    {
                        var adjacentTile = tile + direction;
                        if (visitedTiles.Contains(adjacentTile))
                        {
                            continue;
                        }

                        var objectAtTile = location.getObjectAtTile((int)adjacentTile.X, (int)adjacentTile.Y);
                        if (objectAtTile is Chest chest)
                        {
                            // See if we can create a mission to carry from a full machine to this chest
                            foreach (var machineNeedingPickup in fullMachines)
                            {
                                if (this.IsChestGoodForStoring(chest, machineNeedingPickup.machine.heldObject.Value))
                                {
                                    return new JunimoAssignment(projectType, location, portal, originTile, machineNeedingPickup.machine, machineNeedingPickup.location, chest, tile, itemsToRemoveFromChest: null);
                                }
                            }

                            // See if we can create a mission to carry from this chest to an idle machine
                            foreach (var machineNeedingDelivery in emptyMachines)
                            {
                                if (this.CanMachineBeSuppliedWithChest(machineNeedingDelivery.machine, chest, out List<Item> inputs))
                                {
                                    return new JunimoAssignment(projectType, location, portal, originTile, chest, tile, machineNeedingDelivery.machine, machineNeedingDelivery.location, inputs);
                                }
                            }

                            knownChests.Add((tile, chest));
                            visitedTiles.Add(adjacentTile);
                        }
                        else if (objectAtTile is not null && objectAtTile.MinutesUntilReady <= 0 && this.IsMachineCompatibleWithProjectType(objectAtTile, projectType))
                        {
                            if (objectAtTile.heldObject.Value is not null)
                            {
                                // Try and find a chest to tote it to
                                var targetChest = knownChests.FirstOrDefault(chestAndTile => this.IsChestGoodForStoring(chestAndTile.chest, objectAtTile.heldObject.Value));
                                if (targetChest.chest is not null)
                                {
                                    return new JunimoAssignment(projectType, location, portal, originTile, objectAtTile, tile, targetChest.chest, targetChest.location, itemsToRemoveFromChest: null);
                                }

                                fullMachines.Add((tile, objectAtTile));
                            }
                            else
                            {
                                // Try and find a chest to supply it from
                                foreach (var sourceChest in knownChests)
                                {
                                    if (this.CanMachineBeSuppliedWithChest(objectAtTile, sourceChest.chest, out List<Item> inputs))
                                    {
                                        return new JunimoAssignment(projectType, location, portal, originTile, sourceChest.chest, sourceChest.location, objectAtTile, tile, inputs);
                                    }
                                }

                                emptyMachines.Add((tile, objectAtTile));
                            }
                            visitedTiles.Add(adjacentTile);
                        }
                        else if (objectAtTile is null && (direction.X == 0 || direction.Y == 0) && this.getFlooringId(location, adjacentTile) is string floorId && validFloorTilesToWalk.Contains(floorId))
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

                // Couldn't find any work delivering to machines.
                var fullMachine = fullMachines.FirstOrDefault();
                if (fullMachine.machine is not null)
                {
                    var chestWithSpace = knownChests.FirstOrDefault(chest => chest.chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID).Count < chest.chest.GetActualCapacity());
                    if (chestWithSpace.chest is not null)
                    {
                        return new JunimoAssignment(projectType, location, portal, originTile, fullMachine.machine, fullMachine.location, chestWithSpace.chest, chestWithSpace.location, itemsToRemoveFromChest: null);
                    }
                }
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
