using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Inventories;

namespace NermNermNerm.Junimatic
{
    public class GameMachine
        : GameInteractiveThing
    {
        internal GameMachine(Object machine, Point accessPoint)
            : base(accessPoint)
        {
            this.Machine = machine;
        }

        public StardewValley.Object Machine { get; }

        internal static GameMachine? TryCreate(Object item, Point accessPoint)
            => item.GetMachineData() is null ? null : new GameMachine(item, accessPoint);

        public virtual bool IsIdle => this.Machine.heldObject.Value is null && this.Machine.MinutesUntilReady == 0;

        public virtual Object? HeldObject => this.Machine.MinutesUntilReady == 0 ? this.Machine.heldObject.Value : null;

        /// <summary>
        ///   Returns the HeldObject and removes it from the machines.
        /// </summary>
        public Object RemoveHeldObject()
        {
            var result = this.Machine.heldObject.Value;
            this.Machine.heldObject.Value = null;
            this.Machine.readyForHarvest.Value = false;
            if (this.Machine.ItemId == "21")
            {
                // Crystallarium - if we don't do this, the crystalarium just shuts down.
                this.Machine.PlaceInMachine(this.Machine.GetMachineData(), result, false, Game1.player, false, false);
            }
            return result;
        }

        public bool TryPutHeldObjectInStorage(GameStorage storage)
        {
            if (this.Machine.heldObject.Value is not null && storage.TryStore(this.Machine.heldObject.Value))
            {
                var oldGem = this.Machine.heldObject.Value;
                this.Machine.heldObject.Value = null;
                this.Machine.readyForHarvest.Value = false;
                if (this.Machine.ItemId == "21")
                {
                    this.Machine.PlaceInMachine(this.Machine.GetMachineData(), oldGem, false, Game1.player, false, false);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///   Looks at the recipes allowed by this machine and the contents of the chest.  If there's
        ///   enough stuff in the chest to allow it, it builds a list of the items needed but doesn't
        ///   actually remove the items from the chest.
        /// </summary>
        public virtual List<Item>? GetRecipeFromChest(GameStorage storage)
        {
            if (this.Machine.ItemId == "21") // Never feed crystalariums
            {
                return null;
            }

            var machineData = this.Machine.GetMachineData();
            var inputs = new List<Item>();

            var sourceInventory = storage.RawInventory;
            // Ensure it has the coal (aka all the 'AdditionalConsumedItems')
            if (machineData.AdditionalConsumedItems is not null)
            {
                if (!MachineDataUtility.HasAdditionalRequirements(sourceInventory, machineData.AdditionalConsumedItems, out _))
                {
                    return null;
                }

                inputs.AddRange(machineData.AdditionalConsumedItems.Select(i => ItemRegistry.Create(i.ItemId, i.RequiredCount)));
            }

            foreach (var rule in machineData.OutputRules)
            {
                foreach (var trigger in rule.Triggers)
                {
                    foreach (var item in sourceInventory)
                    {
                        if (MachineDataUtility.CanApplyOutput(this.Machine, rule, MachineOutputTrigger.ItemPlacedInMachine, item, Game1.MasterPlayer, this.Machine.Location, out var triggerRule, out _))
                        {
                            inputs.Add(ItemRegistry.Create(item.ItemId, trigger.RequiredCount));
                            return inputs;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///   Tries to populate the given machine's input with the contents of the chest.  If it
        ///   succeeds, it returns true and the necessary items are removed.
        /// </summary>
        public virtual bool FillMachineFromChest(GameStorage storage)
            => this.Machine.AttemptAutoLoad(storage.RawInventory, Game1.MasterPlayer);

        /// <summary>
        ///   Fills the machine from the supplied Junimo inventory.
        /// </summary>
        public virtual bool FillMachineFromInventory(Inventory inventory)
            => this.Machine.AttemptAutoLoad(inventory, Game1.MasterPlayer);

        private static Dictionary<string,bool> cachedCompatList = new Dictionary<string,bool>();

        /// <summary>
        ///   Returns true if the machine has a recipe that the Junimo can do.
        /// </summary>
        public bool IsCompatibleWithJunimo(JunimoType projectType)
        {
            string cacheKey = this.Machine.ItemId + ":" + projectType.ToString();
            if (cachedCompatList.TryGetValue(cacheKey, out bool result))
            {
                return result;
            }

            result = this.IsCompatibleWithJunimoNoCache(projectType);
            cachedCompatList[cacheKey] = result;
            return result;
        }

        private bool IsCompatibleWithJunimoNoCache(JunimoType projectType)
        {
            // The MachineData contains clues as to what the assignments should be, but it's definitely fuzzy.
            // The game's notion of "category" is largely a matter of taste and isn't really consistent, and
            // this mod's notion of categorization is also just a matter of taste.  By using categories, we're
            // just trying to make it so there's a fighting chance for getting it right for mods without having
            // to do special configuration.

            // Special cases.
            switch (this.Machine.ItemId)
            {
                case "12": // keg
                    return projectType == JunimoType.CropProcessing; // Otherwise it'll return true for Animals, because there's a recipe (forget which) that involves animal stuff.
                case "25": // seed maker
                    return projectType == JunimoType.CropProcessing; // There's no data at all in its MachineData.
                case "211":
                    return projectType == JunimoType.Forestry; // Else it gets to thinking that fishing would work.
                case "10": // bee house
                    return projectType == JunimoType.Animals; // no good data
                case "154": // worm bin
                    return projectType == JunimoType.Fishing; // The output item makes it perfectly clear, but it's the only thing where OutputItem would add value.
                case "231": // solar panel
                case "9": // lightning rod
                    return projectType == JunimoType.MiningProcessing; // no good data
                case "105":
                case "264":
                    return projectType == JunimoType.Forestry; // no good data
            }

            // TODO: Add configurable special cases.

            string[][] tags = [
                ["category_minerals", "category_gem", "bone_item"],
                ["egg_item", "large_egg_item", "slime_egg_item"],
                ["category_vegetable", "category_fruit", "keg_wine", "preserves_pickle", "preserves_jelly"],
                [], // there just aren't any tags for fish or wood stuff listed
                []]
                ;

            int[][] categories = [
                [StardewValley.Object.GemCategory, StardewValley.Object.mineralsCategory, StardewValley.Object.metalResources, StardewValley.Object.monsterLootCategory],
                [StardewValley.Object.EggCategory, StardewValley.Object.MilkCategory, StardewValley.Object.meatCategory, StardewValley.Object.sellAtPierresAndMarnies /* wool, duck feather, etc. */, StardewValley.Object.artisanGoodsCategory],
                [StardewValley.Object.VegetableCategory, StardewValley.Object.FruitsCategory, StardewValley.Object.SeedsCategory, StardewValley.Object.flowersCategory, StardewValley.Object.fertilizerCategory, StardewValley.Object.artisanGoodsCategory],
                [StardewValley.Object.junkCategory, StardewValley.Object.baitCategory],
                [StardewValley.Object.buildingResources]
                ];

            var machineData = this.Machine.GetMachineData();
            if (machineData.OutputRules is not null)
            {
                foreach (var rule in machineData.OutputRules)
                {
                    foreach (var trigger in rule.Triggers)
                    {
                        if (trigger.RequiredTags is not null && trigger.RequiredTags.Intersect(tags[(int)projectType]).Any())
                        {
                            return true;
                        }

                        if (trigger.RequiredItemId is not null)
                        {
                            var itemData = ItemRegistry.GetData(trigger.RequiredItemId);
                            if (categories[(int)projectType].Contains(itemData.Category))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            //if (this.machine.Name == "Furnace" && projectType == JunimoType.MiningProcessing)
            //{
            //    return true;
            //}
            //if ((this.machine.Name == "Keg" || this.machine.Name == "Cask" || this.machine.Name == "Preserves Jar") && projectType == JunimoType.CropProcessing)
            //{
            //    return true;
            //}
            //if ((this.machine.Name == "Mayonnaise Machine" || this.machine.Name == "Cheese Press" || this.machine.Name == "Loom") && projectType == JunimoType.Animals)
            //{
            //    return true;
            //}
            return false;
        }

        public override string ToString()
        {
            return $"{this.Machine.Name} at {this.Machine.TileLocation}";
        }
    }
}
