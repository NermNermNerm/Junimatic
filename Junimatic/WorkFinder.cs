using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Machines;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using static StardewValley.Menus.CharacterCustomization;


namespace NermNermNerm.Junimatic
{

    public class WorkFinder
    {
        public enum JunimoType
        {
            Furnace,
            Animals,
            Kegs,
        };

        private readonly int[] numAvailableWorkers = { 1, 0, 0 };

        public IEnumerable<JunimoAssignment> GlobalFindProjects()
        {
            // Short circuit if we're out of Junimos.
            if (this.numAvailableWorkers.Any(i => i != 0))
            {
                List<JunimoAssignment> result = new List<JunimoAssignment>();
                foreach (var location in Game1.locations)
                {
                    var portals = location.objects.Values.Where(o => o.ItemId == ObjectIds.JunimoPortal);
                    foreach (var portal in portals)
                    {
                        var project = this.FindProject(portal, JunimoType.Furnace, null);
                        if (project is not null)
                        {
                            yield return project;

                            // Short circuit if we're out of Junimos.
                            if (this.numAvailableWorkers.All(i => i == 0))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }


        private static readonly Vector2[] walkableDirections = new Vector2[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1) };
        private static readonly Vector2[] placeableDirections = walkableDirections; // <- TODO: Add fancier stuff for chests only reachable at a diagonal
            //new Vector2[] {
            //new Vector2(-1, -1), new Vector2(0, -1), new Vector2(1, -1),
            //new Vector2(-1, 0), new Vector2(1, 0),
            //new Vector2(-1, 1), new Vector2(0, 1), new Vector2(1, 1) };

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
            if (machine.Name == "Furnace" && projectType == JunimoType.Furnace)
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
            if (!machineData.AdditionalConsumedItems.All(consumedItem => source.Items.Any(chestItem => chestItem.QualifiedItemId == consumedItem.ItemId && chestItem.Stack >= consumedItem.RequiredCount)))
            {
                return false;
            }
            inputs.AddRange(machineData.AdditionalConsumedItems.Select(i => ItemRegistry.Create(i.ItemId, i.RequiredCount)));

            foreach (var rule in machineData.OutputRules)
            {
                foreach (var trigger in rule.Triggers)
                {
                    int totalItems = source.Items.Where(i => i.QualifiedItemId == trigger.RequiredItemId).Sum(i => i.Stack);
                    if (totalItems >= trigger.RequiredCount)
                    {
                        inputs.Add(ItemRegistry.Create(trigger.RequiredItemId, trigger.RequiredCount));
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
            if (chest.Items.HasEmptySlots())
            {
                return chest.Items.Any(i => i.ItemId == objectToStore.ItemId);
            }
            else
            {
                return chest.Items.Any(i => i.ItemId == objectToStore.ItemId && i.Stack < 999 /* maximum stack size */);
            }
        }

        public JunimoAssignment? FindProject(StardewValley.Object portal, JunimoType projectType, Vector2? junimoLocation)
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

            var staringPoints = junimoLocation.HasValue
                ? new Vector2[] { junimoLocation.Value }
                : walkableDirections
                    .Select(direction => portal.TileLocation + direction)
                    .Where(tile => this.getFlooringId(location, tile) is not null)
                    .ToArray();

            foreach (var startingTile in staringPoints)
            {
                var tilesToInvestigate = new Queue<Vector2>();
                tilesToInvestigate.Enqueue(startingTile);

                while (tilesToInvestigate.TryDequeue(out var tile))
                {
                    System.Diagnostics.Debug.WriteLine($"Walking {tile}");

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
                            System.Diagnostics.Debug.WriteLine($"Looking at chest at {adjacentTile}");
                            // See if we can create a mission to carry from a full machine to this chest
                            foreach (var machineNeedingPickup in fullMachines)
                            {
                                if (this.IsChestGoodForStoring(chest, machineNeedingPickup.machine.heldObject.Value))
                                {
                                    return new JunimoAssignment(location, portal, startingTile, machineNeedingPickup.machine, machineNeedingPickup.location, chest, tile, itemsToRemoveFromChest: null);
                                }
                            }

                            // See if we can create a mission to carry from this chest to an idle machine
                            foreach (var machineNeedingDelivery in emptyMachines)
                            {
                                if (this.CanMachineBeSuppliedWithChest(machineNeedingDelivery.machine, chest, out List<Item> inputs))
                                {
                                    return new JunimoAssignment(location, portal, startingTile, chest, tile, machineNeedingDelivery.machine, machineNeedingDelivery.location, inputs);
                                }
                            }

                            knownChests.Add((tile, chest));
                            visitedTiles.Add(adjacentTile);
                        }
                        else if (objectAtTile is not null && objectAtTile.MinutesUntilReady <= 0 && this.IsMachineCompatibleWithProjectType(objectAtTile, projectType))
                        {
                            System.Diagnostics.Debug.WriteLine($"Looking at machine at {adjacentTile}");
                            if (objectAtTile.heldObject.Value is not null)
                            {
                                // Try and find a chest to tote it to
                                var targetChest = knownChests.FirstOrDefault(chestAndTile => this.IsChestGoodForStoring(chestAndTile.chest, objectAtTile.heldObject.Value));
                                if (targetChest.chest is not null)
                                {
                                    return new JunimoAssignment(location, portal, startingTile, objectAtTile, tile, targetChest.chest, targetChest.location, itemsToRemoveFromChest: null);
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
                                        return new JunimoAssignment(location, portal, startingTile, sourceChest.chest, sourceChest.location, objectAtTile, tile, inputs);
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
                    }

                    visitedTiles.Add(tile);
                }

                // Couldn't find any work delivering to machines.
                var fullMachine = fullMachines.FirstOrDefault();
                if (fullMachine.machine is not null)
                {
                    var chestWithSpace = knownChests.FirstOrDefault(chest => chest.chest.Items.Count < chest.chest.GetActualCapacity());
                    if (chestWithSpace.chest is not null)
                    {
                        return new JunimoAssignment(location, portal, startingTile, fullMachine.machine, fullMachine.location, chestWithSpace.chest, chestWithSpace.location, itemsToRemoveFromChest: null);
                    }
                }
            }

            return null;
        }
    }
}
