using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;

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
        ///   Returns true if the machine isn't doing anything and could be made busy by
        ///   supplying it with an input.
        /// </summary>
        public abstract bool IsIdle { get; }

        public abstract StardewValley.Object? HeldObject { get; }

        /// <summary>
        ///   Returns the HeldObject and removes it from the machines.
        /// </summary>
        public StardewValley.Object RemoveHeldObject()
        {
            return this.TakeItemFromMachine();
        }

        public bool TryPutHeldObjectInStorage(GameStorage storage)
        {
            if (this.HeldObject is not null && storage.TryStore(this.HeldObject))
            {
                _ = this.TakeItemFromMachine();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected abstract StardewValley.Object TakeItemFromMachine();

        /// <summary>
        ///   Looks at the recipes allowed by this machine and the contents of the chest.  If there's
        ///   enough stuff in the chest to allow it, it builds a list of the items needed but doesn't
        ///   actually remove the items from the chest.
        /// </summary>
        public abstract List<Item>? GetRecipeFromChest(GameStorage storage, Func<Item, bool> isShinyTest);

        /// <summary>
        ///   Tries to populate the given machine's input with the contents of the chest.  If it
        ///   succeeds, it returns true and the necessary items are removed.
        /// </summary>
        public abstract bool FillMachineFromChest(GameStorage storage, Func<Item,bool> isShinyTest);

        /// <summary>
        ///   Fills the machine from the supplied Junimo inventory.
        /// </summary>
        public abstract bool FillMachineFromInventory(Inventory inventory);

        private static Dictionary<string,bool> cachedCompatList = new Dictionary<string,bool>();

        /// <summary>
        ///   Returns true if the machine has a recipe that the Junimo can do.
        /// </summary>
        public abstract bool IsCompatibleWithJunimo(JunimoType projectType);
    }
}
