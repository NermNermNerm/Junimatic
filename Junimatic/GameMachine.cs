using System;
using System.Collections.Generic;
using System.Linq;
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

        public abstract List<StardewValley.Object> HeldObject { get; }

        /// <summary>
        ///   Returns the HeldObject and removes it from the machines.
        /// </summary>
        public List<StardewValley.Object> RemoveHeldObject()
        {
            return this.TakeItemFromMachine();
        }

        /// <summary>
        /// Adds the item to storage
        /// <remarks>TakeItemFromMachine is not called here to reset the state of the machine as it would
        /// effect the full contents of the HeldObject list. The full contents of a list should not be modified while
        /// it is being iterated on. This method now defers to the calling method to handle resetting the machine state
        /// after finishing iterating through the list. E.G. RemoveHeldObject()</remarks>
        /// </summary>
        /// <param name="storage">The Chest to store the item</param>
        /// <param name="itemIndex">The index of the item in the HeldObject list to store</param>
        /// <returns>True if the item is successfully added to storage. Else false.</returns>
        public bool TryPutHeldObjectInStorage(GameStorage storage, int itemIndex)
        {
            if (this.HeldObject.Any() && storage.TryStore(this.HeldObject[itemIndex]))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected abstract List<StardewValley.Object> TakeItemFromMachine();

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
