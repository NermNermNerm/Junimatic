using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   Abstraction for indoor wells.  It creates a storage that appears to contain infinite watering cans.
    /// </summary>
    public class IndoorWellStorage
        : GameStorage
    {
        private StardewValley.Object IndoorWell => (StardewValley.Object)base.GameObject;

        internal IndoorWellStorage(StardewValley.Object indoorWell, Point accessPoint)
            : base(indoorWell, accessPoint)
        {
        }

        public override ProductCapacity CanHold(IReadOnlyList<EstimatedProduct> itemDescriptions) => ProductCapacity.Unusable;

        /// <inheritdoc/>
        public override bool TryStore(IEnumerable<StardewValley.Item> items) => false;

        /// <inheritdoc/>
        public override IInventory RawInventory
        {
            get
            {
                var i = new Inventory();
                var can = ItemRegistry.Create("(T)SteelWateringCan");
                i.Add(can);
                return i;
            }
        }

        public override string ToString()
        {
            return IF($"{this.IndoorWell.Name} at {this.IndoorWell.TileLocation}");
        }
    }
}
