using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;

namespace NermNermNerm.Junimatic
{

    public abstract class GameMachine
        : GameInteractiveThing
    {
        internal GameMachine(object gameObject, Point accessPoint)
            : base(gameObject, accessPoint)
        {
        }

        /// <summary>
        ///   Returns the current state of the machine (either idle, working or awaiting pickup).
        /// </summary>
        public abstract MachineState State { get; }

        /// <summary>
        ///   Ensures that the machine is still where it was - that is, it wasn't picked up by the player.
        /// </summary>
        public abstract bool IsStillPresent { get; }

        public bool IsAwaitingPickup => this.State == MachineState.AwaitingPickup;
        public bool IsIdle => this.State == MachineState.Idle;

        /// <summary>
        ///   Tests to see if the given chest can hold the outputs from the machine.
        /// </summary>
        public ProductCapacity CanHoldProducts(GameStorage storage)
        {
            return storage.CanHold(this.EstimatedProducts);
        }

        protected EstimatedProduct HeldObjectToEstimatedProduct(Item item)
        {
            return new EstimatedProduct(item.QualifiedItemId, item.Quality, (item as ColoredObject)?.color.Value, maxQuantity: 1);
        }

        /// <summary>
        ///   Returns an estimate (possibly a perfectly accurate one) of the result of a call to <see cref="GetProducts"/>.
        ///   It is meant for use with machines that 
        /// </summary>
        protected abstract IReadOnlyList<EstimatedProduct> EstimatedProducts { get; }


        /// <summary>
        ///   Extracts all the produce from the machine and resets the machine.
        /// </summary>
        /// <remarks>
        ///   This call is only valid for machines where <see cref="State"/> is <see cref="MachineState.AwaitingPickup"/>,
        ///   and most machines will go into the <see cref="MachineState.Idle"/> after this is complete.  The exceptions
        ///   would be machines like Crystalariums and Bait Makers where it will go into the <see cref="MachineState.Working"/> state.
        /// </remarks>
        public abstract List<StardewValley.Item> GetProducts();

        /// <summary>
        ///   Looks at the recipes allowed by this machine and the contents of the chest.  If there's
        ///   enough stuff in the chest to allow it, it builds a list of the items needed but doesn't
        ///   actually remove the items from the chest.  It will not return any recipe where any
        ///   item passes <paramref name="isShinyTest"/>.
        /// </summary>
        public abstract List<Item>? GetRecipeFromChest(GameStorage storage, Func<Item, bool> isShinyTest);

        /// <summary>
        ///   Tries to populate the given machine's input with the contents of the chest.  If it
        ///   succeeds, it returns true and the necessary items are removed.  It should not use any
        ///   items to fill the chest that pass <paramref name="isShinyTest"/>.
        /// </summary>
        public abstract bool FillMachineFromChest(GameStorage storage, Func<Item,bool> isShinyTest);

        /// <summary>
        ///   Fills the machine from the supplied Junimo inventory.  Callers should ensure that the
        ///   machine is in the <see cref="MachineState.Idle"/> state before calling and ensure that
        ///   the inventory contains all the items needed for a recipe.
        /// </summary>
        public abstract void FillMachineFromInventory(Inventory inventory);

        private static Dictionary<string,bool> cachedCompatList = new Dictionary<string,bool>();

        /// <summary>
        ///   Returns true if the machine has a recipe that the Junimo can do.
        /// </summary>
        public abstract bool IsCompatibleWithJunimo(JunimoType projectType);
    }
}
