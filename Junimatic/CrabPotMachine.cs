using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Inventories;
using StardewValley.Objects;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    internal class CrabPotMachine
        : ObjectMachine
    {
        internal CrabPotMachine(CrabPot machine, Point accessPoint)
            : base(machine, accessPoint)
        {
        }

        public new CrabPot Machine => (CrabPot)base.Machine;

        public override MachineState State
        {
            get
            {
                if (this.Machine.heldObject.Value is not null)
                {
                    return MachineState.AwaitingPickup;
                }
                else if (this.Machine.bait.Value is null)
                {
                    return MachineState.Idle;
                }
                else
                {
                    return MachineState.Working;
                }
            }
        }

        public override List<Item>? GetRecipeFromChest(GameStorage storage, Func<Item, bool> isShinyTest)
        {
            // TODO? Crab pots have the notion of an owner and the owner may or may not have the profession that
            //  makes it so that the traps don't need bait.  This ignores that.  Perhaps it's actually by-design
            //  since we're not, granting farming XP and so forth when a Junimo does the trapping.  Perhaps the
            //  Junimo can be said to not have the ability, and so the owner doesn't matter at all.

            foreach (var item in storage.RawInventory)
            {
                if (item.TypeDefinitionId == I(ItemRegistry.type_object) && item.Category == StardewValley.Object.baitCategory && item.Stack > 0)
                {
                    return new List<Item> { ItemRegistry.Create(item.QualifiedItemId) };
                }
            }

            return null;
        }

        public override List<StardewValley.Item> GetProducts()
        {
            var oldHeldObject = this.Machine.heldObject.Value;
            this.Machine.heldObject.Value = null;
            this.Machine.readyForHarvest.Value = false;
            this.Machine.tileIndexToShow = 710;
            this.Machine.bait.Value = null;
            this.Machine.lidFlapping = true;
            this.Machine.lidFlapTimer = 60f;
            this.Machine.shake = Vector2.Zero;
            this.Machine.shakeTimer = 0f;
            return [oldHeldObject];
        }

        public override bool FillMachineFromChest(GameStorage storage, Func<Item, bool> isShinyTest)
        {
            if (!base.FillMachineFromChest(storage, isShinyTest))
            {
                return false;
            }

            // The game code seems to be busted.  It does properly fill the machine, but it doesn't
            // remove the items.
            var inventoryStack = storage.RawInventory.First(i => i.QualifiedItemId == this.Machine.bait.Value.QualifiedItemId);
            if (inventoryStack.Stack > 1)
            {
                --inventoryStack.Stack;
            }
            else
            {
                storage.RawInventory.Remove(inventoryStack);
            }
            return true;
        }

        public override void FillMachineFromInventory(Inventory inventory)
        {
            base.FillMachineFromInventory(inventory);

            // The base game's crab pot implementation of FillMachineFromInventory can leave null slots in the inventory,
            // which I'd like to avoid.  Consider moving this up to the base implementation of this method - can't see how
            // it could do any harm.
            inventory.RemoveEmptySlots();
        }
    }
}
