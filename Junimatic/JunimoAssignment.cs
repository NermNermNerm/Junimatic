using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;


namespace NermNermNerm.Junimatic
{
    public record JunimoAssignment(
        JunimoType projectType,
        GameLocation location,
        StardewValley.Object hut,
        Vector2 origin,
        StardewValley.Object source,
        Vector2 sourceTile,
        StardewValley.Object target,
        Vector2 targetTile,
        List<Item>? itemsToRemoveFromChest);
}
