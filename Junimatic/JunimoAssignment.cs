using Microsoft.Xna.Framework;
using StardewValley;


namespace NermNermNerm.Junimatic
{
    public record JunimoAssignment(
        GameLocation location,
        StardewValley.Object hut,
        Vector2 origin,
        StardewValley.Object source,
        Vector2 sourceTile,
        StardewValley.Object target,
        Vector2 targetTile);
}
