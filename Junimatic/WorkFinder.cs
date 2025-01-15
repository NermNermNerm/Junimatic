using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TokenizableStrings;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class WorkFinder : ISimpleLog
    {
        private ModEntry mod = null!;
        private readonly Dictionary<GameLocation, IReadOnlyList<MachineNetwork>> cachedNetworks = new();
        private readonly HashSet<GameLocation> alreadyDisabledLocations = new HashSet<GameLocation>();

        private int timeOfDayAtLastCheck = -1;
        private int numActionsAtThisGameTime;

        private bool isDayStarted = false;
        private bool haveLookedForRaisins = false;

        public bool AreJunimosRaisinPowered { get; private set; }

        /// <summary>The number of Junimos that are being simulated out doing stuff.</summary>
        private readonly Dictionary<JunimoType, int> numAutomatedJunimos = Enum.GetValues<JunimoType>().ToDictionary(t => t, t => 0);

        private static readonly Point[] walkableDirections = [new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1)];
        private static readonly Point[] crabPotReachableDirections = [new Point(-2, 0), new Point(2, 0), new Point(0, -2), new Point(0, 2)];
        private static readonly Point[] reachableDirections = [
            new Point(-1, -1), new Point(0, -1), new Point(1, -1),
            new Point(-1, 0), /*new Point(0, 0),*/ new Point(1, 0),
            new Point(-1, 1), new Point(0, 1), new Point(1, 1)];

        public void Entry(ModEntry mod)
        {
            this.mod = mod;
            mod.Helper.Events.GameLoop.OneSecondUpdateTicked += this.GameLoop_OneSecondUpdateTicked;
            mod.Helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;
            mod.Helper.Events.GameLoop.DayStarted += this.GameLoop_DayStarted;
        }

        private void GameLoop_DayStarted(object? sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            this.LogTrace($"WorkFinder.OnDayStarted unleashed the junimos");
            this.isDayStarted = true;
            this.AreJunimosRaisinPowered = false;
            this.haveLookedForRaisins = false;
            this.alreadyDisabledLocations.Clear();
        }

        private void GameLoop_DayEnding(object? sender, StardewModdingAPI.Events.DayEndingEventArgs e)
        {
            this.isDayStarted = false;
            if (!Game1.IsMasterGame)
            {
                this.LogTrace($"WorkFinder.OnDayEnding - not doing anything because this is not the master game.");
                return;
            }

            foreach (var location in this.GetAllJunimoFriendlyLocations())
            {
                foreach (var junimo in location.characters.OfType<JunimoShuffler>().ToArray())
                {
                    junimo.OnDayEnding(location);
                }
            }
        }

        // 10 minutes in SDV takes 7.17 seconds of real time.  So our setting of 2 means
        //  that we assume that junimo actions take about 3-4 seconds to do.
        private const int numActionsPerTenMinutes = 2;
        private const int numActionsPerTenMinutesWithRaisins = 7;

        private void GameLoop_OneSecondUpdateTicked(object? sender, StardewModdingAPI.Events.OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                this.cachedNetworks.Clear();
                return;
            }

            if (Game1.isTimePaused || !Game1.IsMasterGame || !this.isDayStarted)
            {
                return;
            }

            // timeOfDay is the time as shown by the game's clock.  740 means 7:40AM. 1920 means 7:20PM.
            //   I don't see how to get a more granular time than that.  The ticks given in the event args
            //   proceed whether the game is moving along or not.
            // currentTime is simply how much playtime has happened - perhaps it just synchronizes games?
            //
            if (this.timeOfDayAtLastCheck != Game1.timeOfDay)
            {
                this.timeOfDayAtLastCheck = Game1.timeOfDay;
                this.numActionsAtThisGameTime = 0;
            }
            else
            {
                ++this.numActionsAtThisGameTime;
            }

            if (this.numActionsAtThisGameTime >= (this.AreJunimosRaisinPowered ? numActionsPerTenMinutesWithRaisins : numActionsPerTenMinutes))
            {
                return;
            }

            this.DoJunimos(true);
        }

        private void DoJunimos(bool isAutomationInterval)
        {
            var isShinyTest = this.mod.JunimoStatusDialog.GetIsShinyTest();
            var numAvailableJunimos = this.GetNumUnlockedJunimos();
            foreach (var junimoType in Enum.GetValues<JunimoType>())
            {
                if (isAutomationInterval)
                {
                    this.numAutomatedJunimos[junimoType] = 0;
                }
            }

            var allJunimoFriendlyLocations = this.GetAllJunimoFriendlyLocations();
            foreach (var location in allJunimoFriendlyLocations)
            {
                foreach (var animatedJunimo in location.characters.OfType<JunimoShuffler>())
                {
                    if (animatedJunimo.Assignment is null)
                    {
                        continue; // Should not happen - Assignment is only null when on a non-master multiplayer game, and we know we're on the master game.
                    }

                    if (numAvailableJunimos[animatedJunimo.Assignment.projectType] > 0) // Should always be true
                    {
                        numAvailableJunimos[animatedJunimo.Assignment.projectType] -= 1;
                    }
                }
            }

            // Try to employ junimos in visible locations first:
            HashSet<GameLocation> animatedLocations = new HashSet<GameLocation>(Game1.getOnlineFarmers().Select(f => f.currentLocation).Where(l => l is not null && allJunimoFriendlyLocations.Contains(l)));
            foreach (GameLocation location in animatedLocations)
            {
                this.cachedNetworks.Remove(location);

                if (this.IsLocationTemporarilyNotDoingJunimos(location))
                {
                    continue;
                }

                var map = new GameMap(location);
                bool startedRaisinProject = false;

                if (!this.haveLookedForRaisins)
                {
                    foreach (var portal in map.GetPortals())
                    {
                        var junimoType = Enum.GetValues<JunimoType>().Select(i => (JunimoType?)i).FirstOrDefault(i => numAvailableJunimos[i!.Value] > 0);
                        if (junimoType.HasValue)
                        {
                            if (numAvailableJunimos[junimoType.Value] > 0)
                            {
                                var raisinProject = this.FindRaisinProject(portal, junimoType.Value, isShinyTest);
                                if (raisinProject != null)
                                {
                                    numAvailableJunimos[junimoType.Value] -= 1;
                                    this.haveLookedForRaisins = true; // disable further raisin hunting
                                    this.LogTrace($"Starting Animated Junimo to grab a raisin: {raisinProject}");
                                    location.characters.Add(new JunimoShuffler(raisinProject, this));
                                    startedRaisinProject = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!startedRaisinProject)
                {
                    foreach (var portal in map.GetPortals())
                    {
                        foreach (var junimoType in Enum.GetValues<JunimoType>())
                        {
                            if (numAvailableJunimos[junimoType] > 0)
                            {
                                var project = this.FindProject(portal, junimoType, null, isShinyTest);
                                if (project is not null)
                                {
                                    this.LogTrace($"Starting Animated Junimo for {project}");
                                    location.characters.Add(new JunimoShuffler(project, this));
                                    numAvailableJunimos[junimoType] -= 1;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (isAutomationInterval)
            {
                foreach (GameLocation location in allJunimoFriendlyLocations.Where(l => !animatedLocations.Contains(l)))
                {
                    if (this.IsLocationTemporarilyNotDoingJunimos(location))
                    {
                        continue;
                    }

                    foreach (var junimoType in Enum.GetValues<JunimoType>())
                    {
                        if (numAvailableJunimos[junimoType] > 0)
                        {
                            if (this.TryDoAutomationsForLocation(location, junimoType, isShinyTest))
                            {
                                numAvailableJunimos[junimoType] -= 1;
                            }
                        }
                    }
                }
            }

            this.haveLookedForRaisins = true; // We always do the raisin check on the first tick of the day.
        }

        /// <summary>
        ///  This returns true if this is a farm structure that's being renovated by Robin.
        /// </summary>
        private bool IsLocationTemporarilyNotDoingJunimos(GameLocation location)
        {
            if (!IsFarmLocation(location))
            {
                // If it's not the farm, it won't be a temporary suspension; it'll be permanent.
                return false;
            }

            var scaryVillager = location.characters.FirstOrDefault(JunimoShuffler.IsScaryVillager);
            if (scaryVillager is null)
            {
                return false;
            }

            if (!this.alreadyDisabledLocations.Contains(location))
            {
                this.alreadyDisabledLocations.Add(location);

                if (location.Objects.Values.Any(o => o.QualifiedItemId == UnlockPortal.JunimoPortalQiid))
                {
                    this.LogInfo($"Junimos are not working at {location.Name} on day {Game1.Date.TotalDays}.  They are scared of {scaryVillager.Name}.");
                    if (location.ParentBuilding is null)
                    {
                        Game1.hudMessages.Add(new HUDMessage(LF($"Junimos are not working on the {location.DisplayName} today.  They are scared of {scaryVillager.displayName}."), HUDMessage.error_type));
                    }
                    else
                    {
                        string name = TokenParser.ParseText(location.ParentBuilding.GetData().Name);
                        Game1.hudMessages.Add(new HUDMessage(LF($"Junimos are not working in the {name} today.  They are scared of {scaryVillager.displayName}."), HUDMessage.error_type));
                    }
                }
            }

            return true;
        }

        /// <summary>Returns true for the main farm and any buildings within it.</summary>
        private static bool IsFarmLocation(GameLocation location) => location.IsFarm || (location.GetParentLocation() is not null && IsFarmLocation(location.GetParentLocation()));

        private List<GameLocation> GetAllJunimoFriendlyLocations()
        {
            List<GameLocation> allJunimoFriendlyLocations = Game1.getFarm().buildings
                        .Select(b => b.indoors.Value)
                        .Where(l => l is not null).Select(l => l!)
                        .ToList();
            allJunimoFriendlyLocations.AddRange(Game1.locations);
            return allJunimoFriendlyLocations;
        }

        private Dictionary<JunimoType, int> GetNumUnlockedJunimos()
        {
            var result = new Dictionary<JunimoType, int>
            {
                { JunimoType.Crops, this.mod.CropMachineHelperQuest.IsUnlocked ? 1 : 0 },
                { JunimoType.Mining, this.mod.UnlockMiner.IsUnlocked ? 1 : 0 },
                { JunimoType.Animals, this.mod.UnlockAnimal.IsUnlocked ? 1 : 0 },
                { JunimoType.Fishing, this.mod.UnlockFishing.IsUnlocked ? 1 : 0 },
                { JunimoType.Forestry, this.mod.UnlockForest.IsUnlocked ? 1 : 0 },
                { JunimoType.IndoorPots, this.mod.UnlockPots.IsUnlocked ? 1 : 0 },
            };
            return result;
        }


        private bool TryDoAutomationsForLocation(GameLocation location, JunimoType projectType, Func<Item, bool> isShinyTest)
        {
            if (!this.cachedNetworks.TryGetValue(location, out var networks))
            {
                networks = this.BuildNetwork(location);
                this.cachedNetworks.Add(location, networks);
            }

            foreach (var network in networks)
            {
                if (network.corners.Any(c => JunimoShuffler.IsVillagerNear(location, c, JunimoShuffler.VillagerDetectionRange)))
                {
                    location.Objects[network.hut.TileLocation] = (StardewValley.Object)ItemRegistry.Create(UnlockPortal.AbandonedJunimoPortalQiid);
                    this.cachedNetworks.Remove(location); // Invalidate the cache so it gets rebuilt next tick
                    this.LogInfo($"A Junimo encountered a villager at {location.Name}, became frightened, and abandoned the Junimo Hut it came from.  Junimos are afraid of villagers and won't work in areas villagers frequent.  If you don't like this rule, it can be turned off in the Junimatic mod settings.");
                    continue; // Continue to process other networks - possibly removing more.
                }

                if (!this.haveLookedForRaisins)
                {
                    foreach (var storage in network.Chests)
                    {
                        var raisinStack = storage.RawInventory.FirstOrDefault(i => i?.QualifiedItemId == "(O)Raisins" && !isShinyTest(i));
                        if (raisinStack is not null)
                        {
                            this.LogTrace($"Automated Raisin collection from {storage} at {location.Name}");
                            storage.RawInventory.Reduce(raisinStack, 1);
                            this.JunimosGotDailyRaisin();
                        }
                    }
                }

                var machines = network.Machines[projectType];

                // Try and load a machine
                foreach (var emptyMachine in machines.Where(m => m.IsIdle && m.IsCompatibleWithJunimo(projectType)))
                {
                    foreach (var chest in network.Chests)
                    {
                        if (emptyMachine.FillMachineFromChest(chest, isShinyTest))
                        {
                            this.LogTrace($"{Game1.timeOfDay} Automatic machine fill of {emptyMachine} on {location.Name} from {chest}");
                            return true;
                        }
                    }
                }

                // Try and empty a machine
                foreach (var fullMachine in machines)
                {
                    if (fullMachine.IsAwaitingPickup && fullMachine.IsCompatibleWithJunimo(projectType))
                    {
                        GameStorage? goodChest = null;
                        foreach (var c in network.Chests)
                        {
                            var usageValidity = fullMachine.CanHoldProducts(c);
                            if (usageValidity == ProductCapacity.Preferred)
                            {
                                goodChest = c;
                                break;
                            }
                            else if (usageValidity == ProductCapacity.CanHold)
                            {
                                goodChest = c;
                            }
                        }

                        if (goodChest is not null)
                        {
                            var wasHolding = fullMachine.GetProducts();
                            string wasHoldingLogText = ObjectListToLogString(wasHolding); // Calculate now - TryStore alters the list quantities.
                            if (goodChest.TryStore(wasHolding))
                            {
                                this.LogTrace($"{Game1.timeOfDay} Automatic machine empty of {fullMachine} in {location.Name} holding {wasHoldingLogText} into {goodChest}");
                            }
                            else
                            {
                                // This should be prevented by the code in the above foreach loop which should only mark a chest as the
                                //  'goodChest' if it has sufficient storage.
                                this.LogError($"{Game1.timeOfDay} Automatic machine empty failed!  Attempting to unload {fullMachine} holding {wasHoldingLogText} on {location.Name} into {goodChest}.");
                            }

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static string ObjectToLogString(Item item)
            => IF($"{item.DisplayName}[{item.Quality}]x{item.Stack}");

        public static string ObjectListToLogString(IEnumerable<Item> items)
            => string.Join(",", items.Select(ObjectToLogString));

        record class MachineNetwork(
            IReadOnlyDictionary<JunimoType, IReadOnlyList<GameMachine>> Machines,
            IReadOnlyList<GameStorage> Chests,
            StardewValley.Object hut,
            IReadOnlyList<Vector2> corners);

        private List<MachineNetwork> BuildNetwork(GameLocation location)
        {
            var watch = Stopwatch.StartNew();

            var map = new GameMap(location);
            var result = new List<MachineNetwork>();
            var portals = map.GetPortals();
            foreach (var portal in portals)
            {
                var network = TryBuildNetwork(map, portal);
                if (network is not null)
                {
                    result.Add(network);
                }
            }

            long elapsedMs = watch.ElapsedMilliseconds;
            if (elapsedMs > 0)
            {
                this.LogInfo($"WorkFinder.BuildNetwork for {location.Name} took {watch.ElapsedMilliseconds}ms");
            }
            return result;
        }

        private static MachineNetwork? TryBuildNetwork(GameMap map, StardewValley.Object portal)
        {
            var machines = new Dictionary<JunimoType, List<GameMachine>>(Enum.GetValues<JunimoType>().Select(e => new KeyValuePair<JunimoType, List<GameMachine>>(e, new List<GameMachine>())));
            var chests = new List<GameStorage>();
            var checkedForWorkTiles = new HashSet<Point>();
            var walkedTiles = new HashSet<Point>();
            map.GetStartingInfo(portal, out var startingPoints, out var walkableFloorTypes);

            foreach (var startingTile in startingPoints)
            {
                var tilesToInvestigate = new Queue<Point>();
                tilesToInvestigate.Enqueue(startingTile);

                while (tilesToInvestigate.TryDequeue(out var reachableTile))
                {
                    if (walkedTiles.Contains(reachableTile))
                    {
                        continue;
                    }

                    foreach (var direction in reachableDirections)
                    {
                        var adjacentTile = reachableTile + direction;
                        if (checkedForWorkTiles.Contains(adjacentTile))
                        {
                            continue;
                        }

                        map.GetThingAt(adjacentTile, reachableTile, walkableFloorTypes, out bool isWalkable, out var machine, out var storage);
                        if (storage is not null)
                        {
                            chests.Add(storage);
                            checkedForWorkTiles.Add(adjacentTile);
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
                            checkedForWorkTiles.Add(adjacentTile);
                        }
                        else if (isWalkable && (direction.X == 0 || direction.Y == 0)) // && is not on a diagonal
                        {
                            tilesToInvestigate.Enqueue(adjacentTile);
                        }
                        else if (!isWalkable)
                        {
                            checkedForWorkTiles.Add(adjacentTile);
                        }
                    }

                    if (map.Location.IsOutdoors)
                    {
                        foreach (var direction in crabPotReachableDirections)
                        {
                            var adjacentTile = reachableTile + direction;
                            if (checkedForWorkTiles.Contains(adjacentTile))
                            {
                                continue;
                            }

                            map.GetCrabPotAt(adjacentTile, reachableTile, out var machine);
                            if (machine is not null)
                            {
                                foreach (JunimoType junimoType in Enum.GetValues<JunimoType>())
                                {
                                    if (machine.IsCompatibleWithJunimo(junimoType))
                                    {
                                        machines[junimoType].Add(machine);
                                    }
                                }
                            }
                        }
                    }

                    checkedForWorkTiles.Add(reachableTile);
                    walkedTiles.Add(reachableTile);
                }
            }

            if (chests.Any() && machines.SelectMany(d => d.Value).Any())
            {
                // ^^ Note if there are any chests or any machines, walkedTiles will have to contain an item
                int minX = walkedTiles.Any() ? walkedTiles.Min(x => x.X) : 0;
                int maxX = walkedTiles.Any() ? walkedTiles.Max(x => x.X) : 0;
                int minY = walkedTiles.Any() ? walkedTiles.Min(x => x.Y) : 0;
                int maxY = walkedTiles.Any() ? walkedTiles.Max(x => x.Y) : 0;
                return new MachineNetwork(machines.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<GameMachine>)pair.Value), chests,
                    portal, [new Vector2(minX, minY), new Vector2(minX, maxY), new Vector2(maxX, minY), new Vector2(maxX, maxY)]);
            }
            else
            {
                return null;
            }
        }

        public JunimoAssignment? FindProject(StardewValley.Object portal, JunimoType projectType, JunimoShuffler? forJunimo)
            => this.FindProject(portal, projectType, forJunimo, this.mod.JunimoStatusDialog.GetIsShinyTest());

        public JunimoAssignment? FindProject(StardewValley.Object portal, JunimoType projectType, JunimoShuffler? forJunimo, Func<Item,bool> isShinyTest)
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
            if (forJunimo is not null)
            {
                startingPoints = new List<Point>() { forJunimo.Tile.ToPoint() };
            }

            HashSet<object> busyMachines = portal.Location.characters
                .OfType<JunimoShuffler>()
                .Where(j => j != forJunimo)
                .Select(junimo =>
                    junimo.Assignment?.source is GameMachine machine
                    ? machine.GameObject
                    : (junimo.Assignment?.target is GameMachine targetMachine
                        ? targetMachine.GameObject
                        : null))
                .Where(machine => machine is not null)
                .Select(machine => machine!)
                .ToHashSet();

            foreach (var startingTile in startingPoints)
            {
                var originTile = forJunimo?.Assignment?.origin ?? startingTile;
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
                                // TODO: Cache results of CanHoldProducts lookups.
                                if (machineNeedingPickup.CanHoldProducts(chest) == ProductCapacity.Preferred)
                                {
                                    return new JunimoAssignment(projectType, location, portal, originTile, machineNeedingPickup, chest, itemsToRemoveFromChest: null);
                                }
                            }

                            // See if we can create a mission to carry from this chest to an idle machine
                            foreach (var machineNeedingDelivery in emptyMachines)
                            {
                                var inputs = machineNeedingDelivery.GetRecipeFromChest(chest, isShinyTest);
                                if (inputs is not null)
                                {
                                    return new JunimoAssignment(projectType, location, portal, originTile, chest, machineNeedingDelivery, inputs);
                                }
                            }

                            knownChests.Add(chest);
                            visitedTiles.Add(adjacentTile);
                        }
                        else if (machine is not null && machine.IsCompatibleWithJunimo(projectType) && !busyMachines.Contains(machine.GameObject))
                        {
                            if (machine.IsAwaitingPickup)
                            {
                                // Try and find a chest to tote it to
                                var preferredChest = knownChests.FirstOrDefault(chest => chest.IsPreferredStorageForMachinesOutput(machine));
                                if (preferredChest is not null)
                                {
                                    return new JunimoAssignment(projectType, location, portal, originTile, machine, preferredChest, itemsToRemoveFromChest: null);
                                }

                                fullMachines.Add(machine);
                            }
                            else if (machine.IsIdle)
                            {
                                // Try and find a chest to supply it from
                                foreach (var sourceChest in knownChests)
                                {
                                    var inputs = machine.GetRecipeFromChest(sourceChest, isShinyTest);
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

                    if (location.IsOutdoors && projectType == JunimoType.Fishing)
                    {
                        foreach (var direction in crabPotReachableDirections)
                        {
                            var adjacentTile = tile + direction;
                            if (visitedTiles.Contains(adjacentTile))
                            {
                                continue;
                            }

                            map.GetCrabPotAt(adjacentTile, tile, out var machine);

                            if (machine is not null && machine.IsCompatibleWithJunimo(projectType) && !busyMachines.Contains(machine.GameObject))
                            {
                                if (machine.IsAwaitingPickup)
                                {
                                    // Try and find a chest to tote it to
                                    var targetChest = knownChests.FirstOrDefault(chest => chest.IsPreferredStorageForMachinesOutput(machine));
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
                                        var inputs = machine.GetRecipeFromChest(sourceChest, isShinyTest);
                                        if (inputs is not null)
                                        {
                                            return new JunimoAssignment(projectType, location, portal, originTile, sourceChest, machine, inputs);
                                        }
                                    }

                                    emptyMachines.Add(machine);
                                }
                                visitedTiles.Add(adjacentTile);
                            }
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
                    var chestWithSpace = knownChests.FirstOrDefault(chest => chest.IsPossibleStorageForMachinesOutput(fullMachine));
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


        public JunimoAssignment? FindRaisinProject(StardewValley.Object portal, JunimoType projectType, Func<Item, bool> isShinyTest)
        {
            // Yet another clone of BuildNetwork for the specialized purpose of finding a raisin to eat

            var location = portal.Location;
            // These lists are all in order of nearest to farthest from the portal
            var knownChests = new HashSet<GameStorage>();
            var visitedTiles = new HashSet<Point>();

            var map = new GameMap(location);

            map.GetStartingInfo(portal, out var startingPoints, out var walkableFloorTypes);

            foreach (var startingTile in startingPoints)
            {
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

                        if (chest is not null && !knownChests.Contains(chest))
                        {
                            var raisinStack = chest.RawInventory.FirstOrDefault(i => i?.QualifiedItemId == "(O)Raisins" && i.Stack > 0 && !isShinyTest(i));
                            if (raisinStack != null)
                            {
                                var targetHut = new JunimoHutMachine(portal, startingTile, this);
                                return new JunimoAssignment(projectType, location, portal, startingTile, chest, targetHut, [ItemRegistry.Create("(O)Raisins", 1, raisinStack.Quality)]);
                            }
                            else
                            {
                                knownChests.Add(chest);
                            }
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
            }

            return null;
        }

        public void JunimosGotDailyRaisin()
        {
            this.haveLookedForRaisins = true;
            this.AreJunimosRaisinPowered = true;

            foreach (var location in Game1.getOnlineFarmers().Select(f => f.currentLocation).Distinct())
            {
                foreach (var portal in new GameMap(location).GetPortals())
                {
                    portal.shakeTimer = 100;
                    var poofAction = () => MakePoof(new Vector2(portal.TileLocation.X, portal.TileLocation.Y));
                    DelayedAction.functionAfterDelay(poofAction, 500);
                    DelayedAction.functionAfterDelay(poofAction, 1500);
                    DelayedAction.functionAfterDelay(poofAction, 2500);
                }

                for (int i = 0; i < 15; i++)
                {
                    int delay = Math.Max(Game1.random.Next(11), Game1.random.Next(11))
                              + Math.Max(Game1.random.Next(11), Game1.random.Next(11))
                              + Game1.random.Next(11);
                    // delay is a number between 0 and 30 on a bell-curve that's pushed to the right of average.
                    DelayedAction.functionAfterDelay(() => location.playSound("junimoMeep1"), delay * 100);
                }
            }

            Game1.addHUDMessage(new HUDMessage(L("The Junimos munched a box of grapes this morning!"), HUDMessage.achievement_type));
        }

        private static void MakePoof(Vector2 tile)
        {
            var colors = new Color[4][] {
                [Color.SpringGreen, Color.LawnGreen, Color.LightGreen],
                [Color.DarkGreen, Color.ForestGreen, Color.Green],
                [Color.Orange, Color.DarkRed, Color.Red],
                [Color.White, Color.LightBlue, Color.LightGray]
            };

            var colorChoice = Game1.currentLocation.IsOutdoors ? colors[Game1.seasonIndex] : colors[0];

            Vector2 landingPos = tile * 64f;
            landingPos.Y -= 64;
            landingPos.X -= 16;
            float scale = 0.15f;
            TemporaryAnimatedSprite? dustTas = new(
                textureName: Game1.animationsName,
                sourceRect: new Rectangle(0, 256, 64, 64),
                animationInterval: 120f,
                animationLength: 8,
                numberOfLoops: 0,
                position: landingPos,
                flicker: false,
                flipped: Game1.random.NextDouble() < 0.5,
                layerDepth: (landingPos.Y + 150) / 10000f, // SDV uses a base value of y/10k for layerDepth. +150 is a fudge factor that seems to be above the hut, but below any trees or what have you in front of the hut.
                alphaFade: 0.01f,
                color: colorChoice[Game1.random.Next(colorChoice.Length)],
                scale: Game1.pixelZoom * scale,
                scaleChange: 0.02f,
                rotation: 0f,
                rotationChange: 0f);

            Game1.Multiplayer.broadcastSprites(Game1.currentLocation, dustTas);
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            this.mod.WriteToLog(message, level, isOnceOnly);
        }
    }
}
