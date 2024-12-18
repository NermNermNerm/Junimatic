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
    ///   This is for the mini-shipping bin.  It always wants stuff and never produces anything.
    /// </summary>
    public class MiniShippingBinMachine
        : ObjectMachine
    {
        private const int MaxToteableStack = 5;

        private Chest chest => (Chest)base.GameObject;

        internal MiniShippingBinMachine(Chest item, Point accessPoint)
            : base(item, accessPoint)
        {
        }

        public override MachineState State => MachineState.Idle;

        public override bool FillMachineFromChest(GameStorage storage, Func<Item, bool> isShinyTest)
        {
            if (!ModEntry.Config.AllowShippingArtisan)
            {
                return false;
            }

            var shippable = storage.RawInventory.FirstOrDefault(
                i => i is not null
                  && i.Category == StardewValley.Object.artisanGoodsCategory
                  && i.Stack > 0
                  && !isShinyTest(i));
            if (shippable is null)
            {
                return false;
            }

            int numToTake = Math.Min(MaxToteableStack, shippable.Stack);
            if (!this.CanHold(shippable, numToTake))
            {
                return false;
            }

            Item duplicate = shippable.getOne();
            duplicate.Stack = numToTake;
            storage.RawInventory.Reduce(shippable, numToTake);
            this.chest.addItem(duplicate);
            return true;
        }

        public override List<Item>? GetRecipeFromChest(GameStorage storage, Func<Item, bool> isShinyTest)
        {
            if (!ModEntry.Config.AllowShippingArtisan)
            {
                return null;
            }

            var shippable = storage.RawInventory.FirstOrDefault(
                i => i is not null
                  && i.Category == StardewValley.Object.artisanGoodsCategory
                  && i.Stack > 0
                  && !isShinyTest(i));
            if (shippable is not null && this.CanHold(shippable, 5))
            {
                var duplicate = shippable.getOne();
                duplicate.Stack = Math.Min(shippable.Stack, MaxToteableStack);
                return new List<Item> { duplicate };
            }
            else
            {
                return null;
            }
        }

        public override void FillMachineFromInventory(Inventory inventory)
        {
            foreach (var item in inventory)
            {
                this.chest.addItem(item);
            }
            inventory.Clear();
        }

        public override bool IsCompatibleWithJunimo(JunimoType projectType) => true;

        private bool CanHold(Item item, int actualStack)
        {
            var rawInventory = this.chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID);

            int emptySlots = this.chest.GetActualCapacity() - rawInventory.CountItemStacks();
            return emptySlots > 0 || rawInventory.Any(i => i.canStackWith(item) && i.Stack + item.Stack < 1000);
        }

        public override string ToString()
        {
            return IF($"{this.chest.Name} at {this.chest.TileLocation}");
        }
    }
}
