using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    internal class IndoorPotMachine : ObjectMachine
    {
        private static bool isHarmonyPatchApplied = false;

        internal IndoorPotMachine(IndoorPot machine, Point accessPoint)
            : base(machine, accessPoint)
        {
        }

        public new IndoorPot Machine => (IndoorPot)base.Machine;

        public override bool IsCompatibleWithJunimo(JunimoType projectType) => projectType == JunimoType.IndoorPots;

        public override bool FillMachineFromChest(GameStorage storage, Func<Item, bool> isShinyTest)
        {
            throw new NotImplementedException(); // Guaranteed not to be called because of the state implementation
        }

        public override void FillMachineFromInventory(Inventory inventory)
        {
            throw new NotImplementedException(); // Guaranteed not to be called because of the state implementation
        }

        public override List<Item>? GetRecipeFromChest(GameStorage storage, Func<Item, bool> isShinyTest)
            => null;

        public override MachineState State => this.IsHarvestable() ? MachineState.AwaitingPickup : MachineState.Working;

        protected override IReadOnlyList<EstimatedProduct> EstimatedProducts
        {
            get
            {
                // While growing, foragables look like any other crop in HoeDirt, data wise.
                // Once full grown, the foragable is added to the pot's heldObject field and removed from HoeDirt. See Crop.newDay for logic. 
                if (this.Machine.heldObject.Value != null)
                {
                    var harvest = this.Machine.heldObject.Value;
                    return [this.HeldObjectToEstimatedProduct(harvest)];
                }

                // Harvest crop from HoeDirt
                if (this.Machine.hoeDirt.Value is HoeDirt dirt && dirt.crop is not null)
                {
                    Crop crop = dirt.crop;
                    var data = crop.GetData();

                    return [
                        // The quality is not known until harvest
                        new EstimatedProduct(data.HarvestItemId, null, (crop.programColored.Value ? crop.tintColor?.Value : null), data.HarvestMaxStack),

                        // A variety of things can spawn additional items:
                        //  if data.ExtraHarvestChance > 0, then there could be an *infinite* number of extra items with data.HarvestItemId and base quality.
                        //  if the crop is sunflowers, sunflower seeds can be generated
                        //  if the crop is wheat, hay can be generated
                        //  if the crop is fiber, mixed seeds can be generated.
                        //  probably some more I'm forgetting
                        //  mods could do anything.
                        // Given that level of mayhem, we're not going to try and replicate all the logic and just say that some random items
                        // can be generated in addition to the crop.  Note that the '5' number doesn't really matter since we didn't give an
                        // item id -- any null-qiid EstimatedProduct will demand that the chest have at least one fully empty slot in it.
                        new EstimatedProduct(null, null, null, 5)];
                }

                // Harvest from bush
                if (this.Machine.bush.Value is Bush bush)
                {
                    // Note: The only stock item that's possible to be harvested via this path is Tea Saplings.
                    string itemQiid = bush.GetShakeOffItem();
                    return [new EstimatedProduct(itemQiid, 0, null, 1)];
                }

                throw new InvalidOperationException(I("IndoorPotMachine.EstimatedProducts called when the pot wasn't ready to harvest"));
            }
        }

        public override List<Item> GetProducts()
        {
            return this.Harvest();
        }

        /// <summary>
        /// Calculate the harvest objects from the IndoorPot and add them to this.HarvestItems to be collected
        /// /// </summary>
        private List<Item> Harvest()
        {
            if (!isHarmonyPatchApplied)
            {
                Patcher.Apply();
                isHarmonyPatchApplied = true;
            }

            // Harvest foragables
            // While growing, foragables look like any other crop in HoeDirt, data wise.
            // Once full grown, the foragable is added to the pot's heldObject field and removed from HoeDirt. See Crop.newDay for logic. 
            if (this.Machine.heldObject.Value != null)
            {
                // Convert the object to its metadata
                var harvest = this.Machine.heldObject.Value;
                this.Machine.heldObject.Value = null;
                this.Machine.readyForHarvest.Value = false;
                return [harvest];
            }

            // Harvest crop from HoeDirt
            if (this.Machine.hoeDirt.Value is HoeDirt dirt && dirt.crop is not null)
            {
                var harvest = this.HarvestCrop(dirt);
                // Destroy HoeDirt.crop after harvest to prevent duplicate harvest if farmer tries to harvest after Junimo grabs first object
                if (!dirt.crop.RegrowsAfterHarvest())
                {
                    dirt.destroyCrop(this.Machine.Location.farmers.Any()); // Play animation if player is around
                }
                return harvest;
            }

            // Harvest from bush
            if (this.Machine.bush.Value is Bush bush)
            {
                // Note: The only stock item that's possible to be harvested via this path is Tea Saplings.
                return this.HarvestBush(bush);
            }

            throw new InvalidOperationException(I("IndoorPotMachine.Harvest called when the pot wasn't ready to harvest"));
        }


        /// <summary>
        /// Harvest HoeDirt.Crop and add the output to this.HarvestItems
        /// </summary>
        /// <param name="dirt">The HoeDirt of the IndoorPot</param>
        private List<Item> HarvestCrop(HoeDirt dirt)
        {
            Crop crop = dirt.crop;
            int xTile = (int)dirt.Tile.X;
            int yTile = (int)dirt.Tile.Y;

            return Patcher.InterceptHarvest(
                () => crop.harvest(xTile, yTile, dirt),
                this.Machine.Location);
        }

        /// <summary>
        /// Harvest Bush and add the output to this.HarvestItems
        /// </summary>
        /// <param name="bush">The bush of the IndoorPot</param> 
        private List<Item> HarvestBush(Bush bush)
        {
            // Call Bush.shake to harvest to support custom bushes
            // Custom Bush has a transpiler on Bush.shake, replacing the call to CreateObjectDebris with a custom method
            // As a result, Junimatic cannot call CreateObjectDebris directly as that would circumvent the call to the Custom Bush CreateObjectDebris
            // This Custom Bush method eventually calls the original CreateObjectDebris, which is where the Junimatic prefix would run, adding the items to HarvestObjects

            return Patcher.InterceptHarvest(
                () => bush.shake(this.Machine.TileLocation, doEvenIfStillShaking: false),
                this.Machine.Location);
        }

        /// <summary>
        /// Check if the plant growing in the pot is ready to be harvested
        /// </summary>
        /// <returns>True if pot is ready for harvest. Default: false</returns>
        private bool IsHarvestable()
        {
            // Check forage
            if (this.Machine.heldObject.Value != null) return true;

            // Check HoeDirt
            if (this.Machine.hoeDirt.Value is HoeDirt dirt && dirt.readyForHarvest()) return true;

            // Check Bush
            if (this.Machine.bush.Value is Bush bush && bush.tileSheetOffset.Value == 1 && bush.inBloom()) return true;

            return false;
        }
    }
}
