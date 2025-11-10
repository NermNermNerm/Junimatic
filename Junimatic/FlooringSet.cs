using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;


using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;


namespace NermNermNerm.Junimatic
{
    internal class FlooringSet
    {
        private readonly IReadOnlySet<string> validFloorIds;

        internal FlooringSet(IReadOnlySet<string> ids)
        {
            this.validFloorIds = ids;
        }

        internal bool IsTileWalkable(GameLocation location, Point tileToCheck)
        {
            string? floorId = getFlooringId(location, tileToCheck);
            return floorId is not null && this.validFloorIds.Contains(floorId);
        }

        /// <summary>
        ///   Gets an identifier for the floor-tile at the coordinate if the tile is indeed on a floor tile.
        /// </summary>
        /// <returns>
        ///   null if there's not a walkable tile at that spot, else an identifier for the type of floor it is.
        ///   The identifier isn't guaranteed to be an itemid or anything, just unique to the floor type.
        /// </returns>
        internal static string? getFlooringId(GameLocation l, Point point)
        {
            Vector2 tile = point.ToVector2();

            var objectAtLocation = l.getObjectAtTile(point.X, point.Y);

            // If there's an object that's not a rug, it's not walkable
            if (objectAtLocation is not null
                && !(objectAtLocation is Furniture f && f.furniture_type.Value == Furniture.rug))
            {
                return null;
            }

            if (l.getBuildingAt(point.ToVector2()) is not null)
            {
                return null;
            }

            l.terrainFeatures.TryGetValue(tile, out var terrainFeature);
            if (terrainFeature is Flooring flooring)
            {
                // *assumes that if flooring can be placed there, then it must be passable...  Not sure how safe an assumption that is...
                return flooring.whichFloor.Value;
            }
            else if (terrainFeature is null && !l.IsOutdoors && l.isTilePassable(tile) && l.isTilePlaceable(tile))
            {
                return I("#BARE_FLOOR#");
            }

            return null;
        }
    }
}
