using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Inventories;
using StardewValley.Objects;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   This is for the big shipping box (the building).  It always wants stuff and never produces anything.
    /// </summary>
    public class ShippingBinMachine
        : BuildingMachine
    {
        private const int MaxToteableStack = 5;

        private ShippingBin chest => (ShippingBin)base.GameObject;

        internal ShippingBinMachine(ShippingBin item, Point accessPoint)
            : base(item, accessPoint)
        {
        }

        public override MachineState State => MachineState.Idle;

        protected override IReadOnlyList<EstimatedProduct> EstimatedProducts => throw new NotImplementedException();

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
            Item duplicate = shippable.getOne();
            duplicate.Stack = numToTake;
            storage.RawInventory.Reduce(shippable, numToTake);

            var farm = Game1.getFarm();
            farm.getShippingBin(Game1.MasterPlayer).Add(duplicate);
            farm.lastItemShipped = duplicate;
            farm.playSound("Ship");

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
            if (shippable is not null)
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
            var farm = Game1.getFarm();
            foreach (var item in inventory)
            {
                farm.getShippingBin(Game1.MasterPlayer).Add(item);
                farm.lastItemShipped = item;
            }
            inventory.Clear();
        }

        public override bool IsCompatibleWithJunimo(JunimoType projectType) => true;

        public override List<Item> GetProducts() => throw new NotImplementedException(); // never has products

        public override string ToString()
        {
            return IF($"ShippingBin at {this.chest.tileX.Value},{this.chest.tileY.Value}");
        }
    }
}
