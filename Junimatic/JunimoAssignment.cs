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
        Point origin,
        GameInteractiveThing source,
        GameInteractiveThing target,
        List<Item>? itemsToRemoveFromChest);
}
