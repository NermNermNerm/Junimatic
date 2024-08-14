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
        public override List<Item> GetProducts()
        {
            var oldValue = this.Building.output.Value;
            this.Building.output.Value = null;
            return [oldValue];
        }

        protected override IReadOnlyList<EstimatedProduct> EstimatedProducts => [this.HeldObjectToEstimatedProduct(this.Building.output.Value)];
    }
}
