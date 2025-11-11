using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   Abstraction for chests.
    /// </summary>
    public class ChestStorage
        : GameStorage
    {
        private Chest chest => (Chest)base.GameObject;

        internal ChestStorage(Chest item, Point accessPoint)
            : base(item, accessPoint)
        {
        }

        public override ProductCapacity CanHold(IReadOnlyList<EstimatedProduct> itemDescriptions)
            => ChestStorage.ChestCanHold(this.chest, itemDescriptions);

        /// <inheritdoc/>
        public override bool TryStore(IEnumerable<StardewValley.Item> items)
        {
            foreach (var item in items)
            {
                if (this.chest.addItem(item) is not null)
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        protected override IInventory RawInventory
        {
            get => this.chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID);
        }

        public override string ToString()
        {
            return IF($"{this.chest.Name} at {this.chest.TileLocation}");
        }
    }
}
