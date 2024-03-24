using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using StardewValley;

namespace NermNermNerm.Junimatic
{
    internal class GameMap
    {
        private readonly GameLocation location;

        public GameMap(GameLocation location)
        {
            this.location = location;
        }

        /// <summary>
        ///   Given a point, try and see what's there.  One or none of the output parameters will be set.
        ///   If none, it means that the point contains nothing and is impassible.
        /// </summary>
        /// <param name="tileToCheck">The location to test.</param>
        /// <param name="reachableTile">An adjacent point that a juniumo can reach.</param>
        /// <param name="validFloors">The set of floortiles that we can traverse.</param>
        /// <param name="isWalkable">True if the tile is empty, but a Junimo would be allowed to walk there.</param>
        /// <param name="machine">If the tile contains a machine that a junimo could interact with, this is set.</param>
        /// <param name="storage">If the tile contains a storage machine that the junimo could use, this is set.</param>
        public void GetThingAt(Point tileToCheck, Point reachableTile, FlooringSet validFloors, out bool isWalkable, out GameMachine? machine, out GameStorage? storage)
        {
            var item = this.location.getObjectAtTile(tileToCheck.X, tileToCheck.Y);
            if (item is not null)
            {
                isWalkable = false;
                machine = GameMachine.TryCreate(item, reachableTile);
                storage = GameStorage.TryCreate(item, reachableTile);
            }
            else
            {
                isWalkable = validFloors.IsTileWalkable(this.location, tileToCheck);
                machine = null;
                storage = null;
            }
        }

        private static readonly Point[] walkableDirections = [new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1)];

        public void GetStartingInfo(StardewValley.Object portal, out List<Point> adjacentTiles, out FlooringSet validFlooringTiles)
        {
            var floorIds = new HashSet<string>();
            var tilesWithFloors = new List<Point>();
            foreach (var direction in walkableDirections)
            {
                var targetPoint = direction + portal.TileLocation.ToPoint();
                string? floorIdAt = FlooringSet.getFlooringId(this.location, targetPoint);

                if (floorIdAt is not null)
                {
                    tilesWithFloors.Add(targetPoint);
                    floorIds.Add(floorIdAt);
                }
            }

            validFlooringTiles = new FlooringSet(floorIds);
            adjacentTiles = tilesWithFloors;
        }

        /// <summary>
        ///   Returns a list of all the points in the map's location that have a junimo portal on them.
        /// </summary>
        public IEnumerable<Point> GetPortalTiles()
        {
            return this.GetPortals().Select(o => o.TileLocation.ToPoint());
        }

        public IEnumerable<StardewValley.Object> GetPortals()
        {
            return this.location.objects.Values.Where(o => o.ItemId == UnlockPortal.JunimoPortal);
        }
    }
}
