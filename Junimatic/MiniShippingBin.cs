using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class MiniShippingBinStorage
        : GameStorage
    {
        private Chest chest => (Chest)base.GameObject;

        internal MiniShippingBinStorage(Chest item, Point accessPoint)
            : base(item, accessPoint)
        {
        }

        public override ProductCapacity CanHold(IReadOnlyList<EstimatedProduct> itemDescriptions)
            =>  ModEntry.Config.AllowShippingArtisan
                && itemDescriptions.All(i => i.qiid is not null && ItemIsArtisanGood(i.qiid))
                && ChestStorage.ChestCanHold(this.chest, itemDescriptions) != ProductCapacity.Unusable
                ? ProductCapacity.Preferred : ProductCapacity.Unusable;

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
        public override IInventory RawInventory
        {
            get => this.chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID);
        }

        public override string ToString()
        {
            return IF($"{this.chest.Name} at {this.chest.TileLocation}");
        }

        private static Dictionary<string, bool> isArtisanCache = new Dictionary<string, bool>();

        private static bool ItemIsArtisanGood(string qiid)
        {
            if (isArtisanCache.TryGetValue(qiid, out bool isArtisan))
            {
                return isArtisan;
            }

            var data = ItemRegistry.GetData(qiid);
            isArtisan = data is not null && data.Category == StardewValley.Object.artisanGoodsCategory;
            isArtisanCache[qiid] = isArtisan;
            return isArtisan;
        }
    }
}
