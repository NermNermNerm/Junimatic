using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   This makes a 'machine' made out of a JunimoHut to give us a target for the raisin delivery.  It is only constructed during
    ///   creation of the raisin delivery mission, so it does not need to implement most stuff.
    /// </summary>
    internal class JunimoHutMachine
        : GameMachine
    {
        private readonly WorkFinder workFinder;

        public JunimoHutMachine(StardewValley.Object machine, Point accessPoint, WorkFinder workFinder)
            : base(machine, accessPoint)
        {
            this.workFinder = workFinder;
        }

        public override MachineState State => MachineState.Idle;

        public override void FillMachineFromInventory(Inventory inventory)
        {
            // 'inventory' should always be a single raisin
            inventory.Clear();
            this.workFinder.JunimosGotDailyRaisin();
        }

        protected override IReadOnlyList<EstimatedProduct> EstimatedProducts => throw new NotImplementedException();
        public override bool FillMachineFromChest(GameStorage storage, Func<Item, bool> isShinyTest) => throw new NotImplementedException();
        public override List<Item>? GetRecipeFromChest(GameStorage storage, Func<Item, bool> isShinyTest) => throw new NotImplementedException();
        public override List<Item> GetProducts() => throw new NotImplementedException();

        public override bool IsCompatibleWithJunimo(JunimoType projectType) => true; // Doesn't matter
        public override bool IsStillPresent => true; // If the user picks up the portal, the Junimo pretends like it's still there.

        public override string ToString()
        {
            var hut = (StardewValley.Object)(this.GameObject);
            return IF($"JunimoHut at {hut.TileLocation.X},{hut.TileLocation.Y}");
        }
    }
}
