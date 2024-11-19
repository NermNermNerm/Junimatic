using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public record JunimoAssignment(
        JunimoType projectType,
        GameLocation location,
        StardewValley.Object hut,
        Point origin,
        GameInteractiveThing source,
        GameInteractiveThing target,
        List<Item>? itemsToRemoveFromChest)
    {
        public override string ToString()
        {
            string itemsList = this.itemsToRemoveFromChest is null ? I("<none>") : WorkFinder.ObjectListToLogString(this.itemsToRemoveFromChest ?? []);
            return IF($"{{ projectType={this.projectType} location={this.location.DisplayName} hut={this.hut.TileLocation} origin={this.origin} source={this.source} target={this.target} items={itemsList} }}");
        }
    }
}
