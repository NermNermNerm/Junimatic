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
    public class AutoGrabberStorage
        : GameStorage
    {
        private StardewValley.Object AutoPicker => (StardewValley.Object)base.GameObject;
        private Chest Chest => (Chest)(this.AutoPicker.heldObject.Value);

        internal AutoGrabberStorage(StardewValley.Object autoPicker, Point accessPoint)
            : base(autoPicker, accessPoint)
        {
        }

        public override ProductCapacity CanHold(IReadOnlyList<EstimatedProduct> itemDescriptions) => ProductCapacity.Unusable;

        /// <inheritdoc/>
        public override bool TryStore(IEnumerable<StardewValley.Item> items) => false;

        /// <inheritdoc/>
        public override IInventory RawInventory
        {
            get => this.Chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID);
        }

        public override string ToString()
        {
            return IF($"{this.AutoPicker.Name} at {this.AutoPicker.TileLocation}");
        }
    }
}
