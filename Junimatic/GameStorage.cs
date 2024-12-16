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
    public abstract class GameStorage
        : GameInteractiveThing
    {
        internal GameStorage(StardewValley.Object item, Point accessPoint)
            : base(item, accessPoint)
        {
        }

        internal static GameStorage? TryCreate(StardewValley.Object item, Point accessPoint)
        {
            if (item.ItemId == "216")  // Mini-Fridge
            {
                return null;
            }
            else if (item is Chest chest && (chest.SpecialChestType == Chest.SpecialChestTypes.None || chest.SpecialChestType == Chest.SpecialChestTypes.JunimoChest || chest.SpecialChestType == Chest.SpecialChestTypes.BigChest))
            {
                return new ChestStorage(chest, accessPoint);
            }
            else if (item.ItemId == "165") // auto-grabber
            {
                return new AutoGrabberStorage(item, accessPoint);
            }
            else if (item.QualifiedItemId == UnlockPots.IndoorWellObjectQiid)
            {
                return new IndoorWellStorage(item, accessPoint);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///   Test to see if the chest can hold the output of a machine.
        /// </summary>
        /// <param name="itemDescriptions">
        ///   A collection of items which will be added to the chest.
        /// </param>
        /// <returns>
        ///   <list type="bullet">
        ///     <item>
        ///      <see cref="ProductCapacity.Preferred"/> if the chest could hold the described items and the chest
        ///      already has an item that is the same as the first entry in <paramref name="itemDescriptions"/>.
        ///     </item>
        ///     <item>
        ///      <see cref="ProductCapacity.CanHold"/> if the chest could hold the described items but doesn't
        ///      already have matching items.
        ///     </item>
        ///     <item>
        ///      <see cref="ProductCapacity.Unusable"/> if the chest hasn't got enough space to hold the items.
        ///     </item>
        ///   </list>
        /// </returns>
        public abstract ProductCapacity CanHold(IReadOnlyList<EstimatedProduct> itemDescriptions);


        /// <summary>
        ///   DELETE ME!  I'm just here to enable running without the optimization to better check the functioning of the fullish
        ///   chest scenarios.
        /// </summary>
        private static bool skipOptimizedCheck = false;

        /// <summary>
        ///   Test to see if the chest can hold the output of a machine.
        /// </summary>
        /// <param name="itemDescriptions">
        ///   A collection of items which will be added to the chest.
        /// </param>
        /// <returns>
        ///   <list type="bullet">
        ///     <item>
        ///      <see cref="ProductCapacity.Preferred"/> if the chest could hold the described items and the chest
        ///      already has an item that is the same as the first entry in <paramref name="itemDescriptions"/>.
        ///     </item>
        ///     <item>
        ///      <see cref="ProductCapacity.CanHold"/> if the chest could hold the described items but doesn't
        ///      already have matching items.
        ///     </item>
        ///     <item>
        ///      <see cref="ProductCapacity.Unusable"/> if the chest hasn't got enough space to hold the items.
        ///     </item>
        ///   </list>
        /// </returns>
        protected static ProductCapacity ChestCanHold(Chest chest, IReadOnlyList<EstimatedProduct> itemDescriptions)
        {
            if (itemDescriptions.Count == 0)
            {
                throw new InvalidOperationException();
            }

            var rawInventory = chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID);

            // This routine gets called an awful lot - so we want to make it as quick as possible, which means as
            // few iterations of the items list as we can do.
            int emptySlots = chest.GetActualCapacity() - rawInventory.CountItemStacks();

            // Most of the time chests will have enough open slots to contain all our output.  This shortcut enables
            //  us to loop through the items just once (to determine if it's a preferred chest or not).
            if (!skipOptimizedCheck && emptySlots >= itemDescriptions.Count)
            {
                // Note that ColoredObject.color is not taken into account in this comparison.  We've established that we have
                //  open slots in this storage, so even if we don't have a matching item, we have room for it.  By not looking
                //  at color, the advantage is that if we have a chest with, say, tulips in it, it'll be preferred over chests
                //  with other flowers in them even if the exact color of tulip isn't represented in this chest.
                string? matchItem = itemDescriptions.First()?.qiid;
                return matchItem is not null && rawInventory.Any(i => i is not null && i.QualifiedItemId == matchItem)
                    ? ProductCapacity.Preferred : ProductCapacity.CanHold;
            }

            // The other extremely likely case we'll face is where itemDescriptions only has one item in it.  We *could*
            //  create a data structure to ensure that we only loop through the chest contents once, but the overhead of
            //  creating that data structure would likely not be worth it most of the time, so we're probably better off
            //  with a simpler implementation.

            bool? isPreferred = null;
            foreach (var item in itemDescriptions)
            {
                if (item.qiid is null)
                {
                    if (emptySlots == 0)
                    {
                        return ProductCapacity.Unusable;
                    }
                    --emptySlots;
                }
                else if (item.quality is null)
                {
                    if (Enumerable
                        .Range(0, 4)
                        .Select(q => item with { quality = q })
                        .All(q => rawInventory.Any(i => q.CanStackWith(i) && i.Stack + item.maxQuantity < 1000)))
                    {
                        // there are stacks of every quality level that can hold whatever comes.
                        // Note that this test is not smart enough to detect if it can spread the output across 2 almost complete stacks.
                        isPreferred ??= true;
                    }
                    else
                    {
                        if (emptySlots == 0)
                        {
                            return ProductCapacity.Unusable;
                        }
                        --emptySlots;

                        isPreferred ??= rawInventory.Any(i => i?.QualifiedItemId == item.qiid);
                    }
                }
                else
                {
                    // Else we have a complete specification - see if there's are exactly matching stacks with sufficient capacity between them.
                    var existingSlots = rawInventory.Where(item.CanStackWith).ToList();
                    if (existingSlots.Count > 0 && existingSlots.Sum(i => i.Stack) + item.maxQuantity <= 999 * existingSlots.Count)
                    {
                        isPreferred ??= true;
                    }
                    else
                    {
                        if (emptySlots == 0)
                        {
                            return ProductCapacity.Unusable;
                        }
                        --emptySlots;

                        isPreferred ??= rawInventory.Any(i => i?.QualifiedItemId == item.qiid);
                    }
                }
            }

            return isPreferred == true ? ProductCapacity.Preferred : ProductCapacity.CanHold;
        }


        public bool IsPreferredStorageForMachinesOutput(GameMachine machine)
            => machine.CanHoldProducts(this) == ProductCapacity.Preferred;

        public bool IsPossibleStorageForMachinesOutput(GameMachine machine)
            => machine.CanHoldProducts(this) != ProductCapacity.Unusable;

        /// <summary>
        ///  Attempts to store the given item in the chest.  Partial success is not
        ///  considered - either the whole stack goes or none.
        /// </summary>
        /// <remarks>
        ///  The current implementation assumes that there's only one item in the given Inventory
        ///  and does not do anything to prevent a partial success.
        /// </remarks>
        public abstract bool TryStore(IEnumerable<StardewValley.Item> items);

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
        public abstract IInventory RawInventory { get; }
    }
}
