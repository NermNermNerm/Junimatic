using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.Inventories;
using StardewValley.Objects;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   This class is aimed at supporting the Mill building, but the Mill building, which used to have
    ///   a dedicated class behind it, has been deprecated and the base <see cref="Building"/> class
    ///   has all the logic built into it.  So in theory, this class will may actually work for
    ///   modded buildings that work like mills.
    /// </summary>
    public class MillMachine
        : BuildingMachine
    {
        private static Dictionary<(Guid,JunimoType), bool> isCompatCache = new Dictionary<(Guid, JunimoType), bool>();
        private static List<StardewValley.Item> allObjects = new List<StardewValley.Item>();

        public const int MaxToteableStackSize = MiniShippingBinMachine.MaxToteableStackSize;

        public MillMachine(Building mill, Point accessPoint)
            : base(mill, accessPoint)
        {
            // The Mill class is only used to preserve data from old save files. All mills were converted into plain Building
            // instances based on the rules in Data/Buildings. The input and output items are now stored in Building.buildingChests
            // with the 'Input' and 'Output' keys respectively
        }

        public static bool IsAutomatable(Building building) =>
                building.GetData().ItemConversions?.Any() == true
             && building.GetBuildingChest(I("Input")) is not null
             && building.GetBuildingChest(I("Input")) is not null;

        private Chest InputChest => this.Building.GetBuildingChest(I("Input"));
        private Chest OutputChest => this.Building.GetBuildingChest(I("Output"));

        public override MachineState State
        {
            get
            {
                // I don't think this is ever going to hit -- the constructor is gated on `IsAutomatable`, which checks this condition,
                // and it seems like there shouldn't be a way for it to change that state...  That said, SDV isn't all that clean in its handling
                // of null, and having a null check here in `State` will be sufficient to protect the rest of the code.
                if (!IsAutomatable(this.Building))
                {
                    return MachineState.Working;
                }

                var outputContents = this.OutputChest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID);
                if (outputContents.Any(i => i is not null && i.Stack > 0))
                {
                    return MachineState.AwaitingPickup;
                }

                var bd = this.Building.GetData();

                // It'd be nice if we could return 'Working' if the machine's input chests are actually full, as that would
                // make it so that we don't constantly check to see if it can be filled during automation sweeps.  However,
                // that's almost never going to be the case in normal gameplay, so it's not really worth bothering.
                return MachineState.Idle;
            }
        }

        private bool TryFill(SafeInventory inventory, Func<Item, bool> isShinyTest)
        {
            var possibleInputs = inventory.Where(item => item is not null && !isShinyTest(item) && this.Building.IsValidObjectForChest(item, this.InputChest)).ToArray();
            if (!possibleInputs.Any())
            {
                return false;
            }

            Utility.consolidateStacks(this.InputChest.Items);
            this.InputChest.clearNulls();

            List<Item> itemsToRemove = new List<Item>();
            bool didAnything = false;
            foreach (var item in possibleInputs)
            {
                BuildingItemConversion itemConversionForItem = this.Building.GetItemConversionForItem(item, this.InputChest);
                int numberOfItemThatCanBeAddedToThisInventoryList = Utility.GetNumberOfItemThatCanBeAddedToThisInventoryList(item, this.InputChest.Items, 36);
                if (item.Stack > itemConversionForItem.RequiredCount && numberOfItemThatCanBeAddedToThisInventoryList < itemConversionForItem.RequiredCount)
                {
                    // This item would work, but the building's chest can't take anymore
                    continue;
                }

                int num = Math.Min(numberOfItemThatCanBeAddedToThisInventoryList, item.Stack) / itemConversionForItem.RequiredCount * itemConversionForItem.RequiredCount;
                num = Math.Min(num, MaxToteableStackSize);
                if (num == 0)
                {
                    continue;
                }

                Item one = item.getOne();
                if (item.ConsumeStack(num) == null)
                {
                    itemsToRemove.Add(item);
                }

                one.Stack = num;
                Utility.addItemToThisInventoryList(one, this.InputChest.Items, 36);
                didAnything = true;
            }

            itemsToRemove.ForEach(item => inventory.Remove(item));
            return didAnything;
        }

        /// <inheritdoc/>
        public override bool FillMachineFromChest(GameStorage storage, Func<Item,bool> isShinyTest)
        {
            return this.TryFill(storage.SafeInventory, isShinyTest);
        }

        /// <inheritdoc/>
        public override void FillMachineFromInventory(Inventory inventory)
        {
            this.TryFill(new SafeInventory(inventory), item => false);
        }

        /// <inheritdoc/>
        public override List<Item>? GetRecipeFromChest(GameStorage storage, Func<Item, bool> isShinyTest)
        {
            var possibleInputs = storage.SafeInventory.Where(item => !isShinyTest(item) && this.Building.IsValidObjectForChest(item, this.InputChest)).ToArray();
            if (!possibleInputs.Any())
            {
                return null;
            }

            Utility.consolidateStacks(this.InputChest.Items);
            this.InputChest.clearNulls();

            List<Item> recipe = new List<Item>();
            foreach (var item in possibleInputs)
            {
                BuildingItemConversion itemConversionForItem = this.Building.GetItemConversionForItem(item, this.InputChest);
                int numberOfItemThatCanBeAddedToThisInventoryList = Utility.GetNumberOfItemThatCanBeAddedToThisInventoryList(item, this.InputChest.Items, 36);
                if (item.Stack > itemConversionForItem.RequiredCount && numberOfItemThatCanBeAddedToThisInventoryList < itemConversionForItem.RequiredCount)
                {
                    // This item would work, but the chest can't take anymore
                    continue;
                }

                int num = Math.Min(numberOfItemThatCanBeAddedToThisInventoryList, item.Stack) / itemConversionForItem.RequiredCount * itemConversionForItem.RequiredCount;
                num = Math.Min(MaxToteableStackSize, num);
                if (num == 0)
                {
                    continue;
                }

                Item one = item.getOne();
                one.Stack = Math.Min(num, MaxToteableStackSize);
                recipe.Add(one);
                break; // <- we only will fulfill one conversion possibility per trip for animated junimos.
            }

            return recipe;
        }

        /// <inheritdoc/>
        public override bool IsCompatibleWithJunimo(JunimoType projectType)
        {
            if (isCompatCache.TryGetValue((this.Building.id.Value,projectType), out bool result)) { return result; }

            var itemConversions = this.Building.GetData().ItemConversions;

            if (allObjects.Count == 0)
            {
                foreach (string id in Game1.objectData.Keys)
                {
                    allObjects.Add(ItemRegistry.Create(id));
                }
            }

            foreach (var item in allObjects)
            {
                if (this.Building.GetItemConversionForItem(item, this.InputChest) is not null)
                {
                    if (ObjectMachine.IsCategoryCompatibleWithProject(projectType, item.Category))
                    {
                        result = true;
                        break;
                    }

                    if (ObjectMachine.HasMatchingTag(projectType, item.GetContextTags()))
                    {
                        result = true;
                        break;
                    }
                }
            }

            // Perhaps run this if !result?  This doesn't help at all in the case of stock Mills because Wheat Flour, Rice and Sugar
            // don't have any categories or tags that pin down what Junimo should be messing with it.  (At least none that I can see).
            //
            //foreach (var conversion in itemConversions)
            //{
            //    foreach (var item in conversion.ProducedItems)
            //    {
            //        // item.ItemId could be an Id or an "item query"...  The proper way to do this would
            //        // probably involve GameStateQuery or something...  We'll be winging it.
            //        var md = ItemRegistry.ResolveMetadata(item.ItemId);
            //        if (md != null) {
            //            var itemData = ItemRegistry.GetData(item.ItemId);
            //            if (itemData.HasCategory() && ObjectMachine.IsCategoryCompatibleWithProject(projectType, itemData.Category))
            //            {
            //                result = true;
            //                break;
            //            }

            //            ObjectData rawData = (ObjectData)itemData.RawData;
            //            if (rawData?.ContextTags is not null && ObjectMachine.HasMatchingTag(projectType, rawData.ContextTags))
            //            {
            //                result = true;
            //                break;
            //            }
            //        }
            //    }
            //}

            isCompatCache[(this.Building.id.Value,projectType)] = result;
            return result;
        }
        public override List<Item> GetProducts()
        {
            var firstItem = this.OutputChest.Items.FirstOrDefault(i => i is not null && i.Stack > 0);
            if (firstItem is null)
            {
                return [];
            }
            else if (firstItem.Stack > MaxToteableStackSize)
            {
                var splitItem = firstItem.getOne();
                splitItem.Stack = MaxToteableStackSize;
                firstItem.Stack -= MaxToteableStackSize;
                return [splitItem];
            }
            {
                this.OutputChest.Items.Remove(firstItem);
                return [firstItem];
            }
        }

        protected override IReadOnlyList<EstimatedProduct> EstimatedProducts
        {
            get
            {
                var firstItem = this.OutputChest.Items.FirstOrDefault(i => i is not null && i.Stack > 0);
                if (firstItem is null)
                {
                    return [];
                }
                else
                {
                    return [this.HeldObjectToEstimatedProduct(firstItem)];
                }
            }
        }
    }
}
