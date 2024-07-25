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
    ///   Abstraction for chests, autopickers and shipping containers.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   Now a "preferred" chest is one where the item would be best placed.
    ///   Setting aside shipping containers for the moment, a "preferred"
    ///   chest is one where there already is a collection of stuff.
    ///  </para>
    ///  <para>
    ///   Perhaps there should be a list of especially shiny things, perhaps
    ///   defined by a chest of a particular color in the network.  Anything
    ///   that's in that chest would not be put in the shipping bin or put
    ///   into a machine.  If you had a thing like that, then you could make
    ///   it so that the shipping bin chest was always "preferred" for
    ///   items not marked as shiny and never just "possible".
    ///  </para>
    /// </remarks>
    public class GameStorage
        : GameInteractiveThing
    {
        private StardewValley.Object item => (StardewValley.Object)base.GameObject;

        internal GameStorage(StardewValley.Object item, Point accessPoint)
            : base(item, accessPoint)
        {
        }

        internal static GameStorage? TryCreate(StardewValley.Object item, Point accessPoint)
        {
            if (item is Chest chest && (chest.SpecialChestType == Chest.SpecialChestTypes.None || chest.SpecialChestType == Chest.SpecialChestTypes.JunimoChest || chest.SpecialChestType == Chest.SpecialChestTypes.BigChest))
            {
                return new GameStorage(item, accessPoint);
            }
            else if (item.ItemId == "165") // auto-grabber
            {
                return new GameStorage(item, accessPoint);
            }
            else
            {
                return null;
            }
        }

        public bool IsPreferredStorageForMachinesOutput(StardewValley.Object objectToStore)
        {
            if (this.item is Chest chest)
            {
                var items = chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID);
                if (items.HasEmptySlots() || chest.GetActualCapacity() > items.Count)
                {
                    return items.Any(i => i is not null && i.ItemId == objectToStore.ItemId && i.Quality == objectToStore.Quality);
                }
                else
                {
                    int[] stacks = items.Where(i => i is not null && i.ItemId == objectToStore.ItemId && i.Quality == objectToStore.Quality).Select(i => i.Stack).ToArray();
                    return stacks.Sum() + objectToStore.Stack <= stacks.Length * 999;
                }
            }
            else
            {
                return false; // auto-grabbers are never used for storage.
            }
        }

        public bool IsPossibleStorageForMachinesOutput(StardewValley.Object item)
        {
            if (this.item is Chest chest)
            {
                return this.RawInventory.Count < chest.GetActualCapacity()
                    || this.RawInventory.Any(i => i.ItemId == item.ItemId && i.Quality == item.Quality && i.Stack + item.Stack <= 999);
            }
            else
            {
                return false; // auto-grabbers are never used for storage.
            }
        }

        /// <summary>
        ///  Attempts to store the given item in the chest.  Partial success is not
        ///  considered - either the whole stack goes or none.
        /// </summary>
        /// <remarks>
        ///  The current implementation assumes that there's only one item in the given Inventory
        ///  and does not do anything to prevent a partial success.
        /// </remarks>
        public bool TryStore(Inventory items)
        {
            if (this.item is Chest chest)
            {
                foreach (var item in items.ToList())
                {
                    if (chest.addItem(item) is not null)
                    {
                        return false;
                    }
                    // Remove item successfully added to chest
                    items.Remove(item);
                }
                items.Clear();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///  Attempts to store the given item in the chest.  Partial success is not
        ///  considered - either the whole stack goes or none.
        /// </summary>
        public bool TryStore(StardewValley.Object item)
        {
            if (this.item is Chest chest)
            {
                if (chest.addItem(item) is not null) 
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///   Given a list of items and quantities, <paramref name="shoppingList"/>, first see if the chest actually contains that much stuff
        ///   and then transfers the items from the chest into <paramref name="toteBag"/>.
        /// </summary>
        /// <returns>True if all the items in <paramref name="shoppingList"/> were transferred to <paramref name="toteBag"/>, false if
        /// the chest didn't contain all the things on the list</returns>
        public bool TryFulfillShoppingList(List<Item> shoppingList, Inventory toteBag)
        {
            // Ensure enough stuff exists
            var chestInventory = this.RawInventory;
            foreach (var item in shoppingList)
            {
                if (chestInventory.Where(i => i.ItemId == item.ItemId).Sum(i => i.Stack) < item.Stack)
                {
                    return false;
                }
            }

            // Pull the stuff out of the chest's inventory
            foreach (var item in shoppingList)
            {
                int leftToRemove = item.Stack;
                Item template = null!; // The while loop is guaranteed to be run once because leftToRemove will always be > 1
                while (leftToRemove > 0)
                {
                    var first = chestInventory.First(i => i is not null && i.Stack > 0 && i.ItemId == item.ItemId && i.Quality == item.Quality);

                    var forBag = first.getOne();
                    forBag.Stack = Math.Min(item.Stack, leftToRemove);
                    toteBag.Add(forBag);

                    if (first.Stack > item.Stack)
                    {
                        first.Stack -= leftToRemove;
                        leftToRemove = 0;
                    }
                    else
                    {
                        leftToRemove -= first.Stack;
                        chestInventory.Remove(first);
                    }
                    template = first;
                }
            }

            return true;
        }

        /// <summary>
        ///   Gets the contents of the chest.
        /// </summary>
        /// <remarks>
        ///   This is strictly for use by the <see cref="GameMachine"/> class, which
        ///   can remove items from this inventory.
        /// </remarks>
        internal IInventory RawInventory
        {
            get =>
                (this.item is Chest chest ? chest : (Chest)this.item.heldObject.Value)
                    .GetItemsForPlayer(Game1.player.UniqueMultiplayerID);
        }

        public override string ToString()
        {
            return IF($"{this.item.Name} at {this.item.TileLocation}");
        }

    }
}
