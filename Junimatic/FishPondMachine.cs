using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Inventories;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class FishPondMachine
        : BuildingMachine
    {
        public FishPondMachine(FishPond fishPond, Point accessPoint)
            : base(fishPond, accessPoint)
        { }

        public new FishPond Building => (FishPond)base.Building;

        public override MachineState State
            => this.Building.output.Value is null ? MachineState.Working : MachineState.AwaitingPickup;

        public override bool FillMachineFromChest(GameStorage storage, Func<Item,bool> isShinyTest)
        {
            // State never being Idle should prevent this from ever being called
            throw new NotImplementedException();
        }

        public override void FillMachineFromInventory(Inventory inventory)
        {
            // State never being Idle should prevent this from ever being called
            throw new NotImplementedException();
        }

        public override List<Item>? GetRecipeFromChest(GameStorage storage, Func<Item, bool> isShinyTest)
        {
            // State never being Idle should prevent this from ever being called
            throw new NotImplementedException();
        }

        public override bool IsCompatibleWithJunimo(JunimoType projectType)
        {
            return projectType == JunimoType.Fishing;
        }
        public override List<StardewValley.Item> GetProducts()
        {
            var oldValue = this.Building.output.Value;
            this.Building.output.Value = null;
            return [oldValue];
        }

        public override ProductCapacity CanHoldProducts(GameStorage storage)
        {
            var heldObject = this.Building.output.Value as StardewValley.Object;
            if (heldObject is null)
            {
                throw new InvalidOperationException(I("CanHoldProducts should only be called if the State is AwaitingPickup"));
            }

            if (storage.CanAddToExistingStack(heldObject))
            {
                return ProductCapacity.CanHoldAndHasMainProduct;
            }
            else if (storage.IsPossibleStorageFor(heldObject))
            {
                return ProductCapacity.CanHold;
            }
            else
            {
                return ProductCapacity.NoSpace;
            }
        }
    }
}
