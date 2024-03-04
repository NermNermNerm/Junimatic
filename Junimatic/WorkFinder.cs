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
    internal class WorkFinder
    {
        public enum JunimoType
        {
            Furnace,
            Animals,
            Kegs,
        };

        private readonly int[] numAvailableWorkers = { 1, 0, 0 };

        public record JunimoAssignment(
            GameLocation location,
            StardewValley.Object hut,
            StardewValley.Object source,
            StardewValley.Object target);

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
                        var project = this.FindProject(portal, JunimoType.Furnace);
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
        private static readonly Vector2[] placeableDirections = new Vector2[] {
            new Vector2(-1, -1), new Vector2(0, -1), new Vector2(1, -1),
            new Vector2(-1, 0), new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(-1, 1), new Vector2(0, 1), new Vector2(1, 1) };

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

        private bool CanMachineBeSuppliedWithChest(StardewValley.Object machine, Chest source)
        {
            var machineData = machine.GetMachineData();

            // Ensure it has the coal (aka all the 'AdditionalConsumedItems')
            if (!machineData.AdditionalConsumedItems.All(consumedItem => source.Items.Any(chestItem => chestItem.ItemId == consumedItem.ItemId && chestItem.Stack >= consumedItem.RequiredCount)))
            {
                return false;
            }

            foreach (var rule in machineData.OutputRules)
            {
                foreach (var trigger in rule.Triggers)
                {
                    int totalItems = source.Items.Where(i => i.ItemId == trigger.RequiredItemId).Sum(i => i.Stack);
                    if (totalItems >= trigger.RequiredCount)
                    {
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

        public JunimoAssignment? FindProject(StardewValley.Object portal, JunimoType projectType)
        {
            var location = portal.Location;
            // These lists are all in order of nearest to farthest from the portal
            var emptyMachines = new List<StardewValley.Object>();
            var fullMachines = new List<StardewValley.Object>();
            var knownChests = new List<Chest>();
            var tilesToInvestigate = new Queue<Vector2>();
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

            foreach (var tile in walkableDirections.Where(d => this.getFlooringId(location, portal.TileLocation + d) is not null))
            {
                tilesToInvestigate.Enqueue(tile);
            }

            while (tilesToInvestigate.TryDequeue(out var tile))
            {
                foreach (var direction in placeableDirections)
                {
                    var adjacentTile = tile + direction;
                    if (visitedTiles.Contains(adjacentTile))
                    {
                        continue;
                    }
                    visitedTiles.Add(adjacentTile);

                    var objectAtTile = location.getObjectAtTile((int)adjacentTile.X, (int)adjacentTile.Y);
                    if (objectAtTile is Chest chest)
                    {
                        // See if we can create a mission to carry from a full machine to this chest
                        foreach (var machineNeedingPickup in fullMachines)
                        {
                            if (this.IsChestGoodForStoring(chest, machineNeedingPickup.heldObject.Value))
                            {
                                return new JunimoAssignment(location, portal, machineNeedingPickup, chest);
                            }
                        }

                        // See if we can create a mission to carry from this chest to an idle machine
                        foreach (var machineNeedingDelivery in emptyMachines)
                        {
                            if (this.CanMachineBeSuppliedWithChest(machineNeedingDelivery, chest))
                            {
                                return new JunimoAssignment(location, portal, chest, machineNeedingDelivery);
                            }
                        }

                        knownChests.Add(chest);
                    }
                    else if (objectAtTile.MinutesUntilReady <= 0 && this.IsMachineCompatibleWithProjectType(objectAtTile, projectType))
                    {
                        if (objectAtTile.heldObject.Value is not null)
                        {
                            // Try and find a chest to tote it to
                            var targetChest = knownChests.FirstOrDefault(chest => this.IsChestGoodForStoring(chest, objectAtTile.heldObject.Value));
                            if (targetChest is not null)
                            {
                                return new JunimoAssignment(location, portal, objectAtTile, targetChest);
                            }
                        }
                        else
                        {
                            // Try and find a chest to supply it from
                            var sourceChest = knownChests.FirstOrDefault(chest => this.CanMachineBeSuppliedWithChest(objectAtTile, chest));
                            if (sourceChest is not null)
                            {
                                return new JunimoAssignment(location, portal, sourceChest, objectAtTile);
                            }
                        }
                    }
                    else if ((direction.X == 0 || direction.Y == 0) && this.getFlooringId(location, tile) is string floorId && validFloorTilesToWalk.Contains(floorId))
                    {
                        tilesToInvestigate.Enqueue(adjacentTile);
                    }
                }
            }


            // Couldn't find any work delivering to machines.
            var fullMachine = fullMachines.FirstOrDefault();
            if (fullMachine is not null)
            {
                var chestWithSpace = knownChests.FirstOrDefault(chest => chest.Items.HasEmptySlots());
                if (chestWithSpace is not null)
                {
                    return new JunimoAssignment(location, portal, fullMachine, chestWithSpace);
                }
            }

            return null;
        }
    }
}
