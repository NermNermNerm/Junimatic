using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;

namespace NermNermNerm.Junimatic
{
    public class GameMachine
        : GameInteractiveThing
    {
        private readonly Object machine;

        internal GameMachine(Object machine, Point accessPoint)
            : base(accessPoint)
        {
            this.machine = machine;
        }

        internal static GameMachine? TryCreate(Object item, Point accessPoint)
        {
            // TODO: Use MachineData to figure this out.  Also do IsCompatibleWithJunimo
            return new string[] { "Furnace", "Keg", "Cask", "Preserves Jar", "Mayonnaise Machine", "Loom", "Cheese Press" }.Any(s => s == item.Name) ? new GameMachine(item, accessPoint) : null;
        }

        public virtual bool IsIdle => this.machine.heldObject.Value is null && this.machine.MinutesUntilReady == 0;

        public virtual Object? HeldObject => this.machine.MinutesUntilReady == 0 ? this.machine.heldObject.Value : null;

        /// <summary>
        ///   Returns the HeldObject and removes it from the machines.
        /// </summary>
        public Object RemoveHeldObject()
        {
            var result = this.machine.heldObject.Value;
            this.machine.heldObject.Value = null;
            this.machine.readyForHarvest.Value = false;
            return result;
        }

        public bool TryPutHeldObjectInStorage(GameStorage storage)
        {
            if (this.machine.heldObject.Value is not null && storage.TryStore(this.machine.heldObject.Value))
            {
                this.machine.heldObject.Value = null;
                this.machine.readyForHarvest.Value = false;
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
            var machineData = this.machine.GetMachineData();
            var inputs = new List<Item>();

            // Ensure it has the coal (aka all the 'AdditionalConsumedItems')
            if (machineData.AdditionalConsumedItems is not null)
            {
                if (!machineData.AdditionalConsumedItems.All(consumedItem => storage.RawInventory.Any(chestItem => chestItem.QualifiedItemId == consumedItem.ItemId && chestItem.Stack >= consumedItem.RequiredCount)))
                {
                    return null;
                }
                inputs.AddRange(machineData.AdditionalConsumedItems.Select(i => ItemRegistry.Create(i.ItemId, i.RequiredCount)));
            }

            foreach (var rule in machineData.OutputRules)
            {
                foreach (var trigger in rule.Triggers)
                {
                    var sourceInventory = storage.RawInventory;
                    var possibleItem = sourceInventory
                        .FirstOrDefault(i => trigger.RequiredItemId is not null && i.QualifiedItemId == trigger.RequiredItemId
                                 || trigger.RequiredTags is not null && trigger.RequiredTags.Any(tag => i.HasContextTag(tag)));
                    if (possibleItem is not null && (possibleItem.Stack >= trigger.RequiredCount || sourceInventory.Where(i => i.itemId == possibleItem.itemId).Sum(i => i.Stack) > trigger.RequiredCount))
                    {
                        inputs.Add(ItemRegistry.Create(possibleItem.ItemId, trigger.RequiredCount));
                        return inputs;
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
            => this.machine.AttemptAutoLoad(storage.RawInventory, Game1.MasterPlayer);

        /// <summary>
        ///   Fills the machine from the supplied Junimo inventory.
        /// </summary>
        public virtual bool FillMachineFromInventory(Inventory inventory)
            => this.machine.AttemptAutoLoad(inventory, Game1.MasterPlayer);

        /// <summary>
        ///   Returns true if the machine has a recipe that the Junimo can do.
        /// </summary>
        public bool IsCompatibleWithJunimo(JunimoType projectType)
        {
            // TODO: Make generic.
            if (this.machine.Name == "Furnace" && projectType == JunimoType.MiningProcessing)
            {
                return true;
            }
            if ((this.machine.Name == "Keg" || this.machine.Name == "Cask" || this.machine.Name == "Preserves Jar") && projectType == JunimoType.CropProcessing)
            {
                return true;
            }
            if ((this.machine.Name == "Mayonnaise Machine" || this.machine.Name == "Cheese Press" || this.machine.Name == "Loom") && projectType == JunimoType.Animals)
            {
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"{this.machine.Name} at {this.machine.TileLocation}";
        }
    }
}
