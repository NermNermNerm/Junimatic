using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.TerrainFeatures;

namespace NermNermNerm.Junimatic
{
    internal class FruitTreeMachine : GameMachine
    {
        private static bool isHarmonyPatchApplied = false;

        internal FruitTreeMachine(FruitTree machine, Point accessPoint)
            : base(machine, accessPoint)
        {
        }

        public FruitTree FruitTree => (FruitTree)base.GameObject;

        public override bool IsCompatibleWithJunimo(JunimoType projectType) => projectType == JunimoType.IndoorPots;

        public override bool FillMachineFromChest(GameStorage storage, Func<Item, bool> isShinyTest)
        {
            throw new NotImplementedException(); // State will keep this from getting called.
        }

        public override void FillMachineFromInventory(Inventory inventory)
        {
            throw new NotImplementedException(); // State will keep this from getting called.
        }

        public override List<Item>? GetRecipeFromChest(GameStorage storage, Func<Item, bool> isShinyTest)
        {
            throw new NotImplementedException(); // State will keep this from getting called.
        }

        public override MachineState State => this.FruitTree.fruit.Any() ? MachineState.AwaitingPickup : MachineState.Working;

        protected override IReadOnlyList<EstimatedProduct> EstimatedProducts
        {
            get => this.FruitTree.fruit.Select(i => new EstimatedProduct(i.QualifiedItemId, i.Quality, null, i.Stack)).ToList();
        }

        public override bool IsStillPresent => true;

        public override List<Item> GetProducts()
        {
            var result = this.FruitTree.fruit.ToList();
            this.FruitTree.fruit.Clear();
            return result;
        }
    }
}
