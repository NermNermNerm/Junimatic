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
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    internal class IndoorPotMachine
        : ObjectMachine 
    {
        internal IndoorPotMachine(IndoorPot machine, Point accessPoint)
            : base(machine, accessPoint)
        {
            this.FakeFarmer = new Farmer() {
                currentLocation = machine.Location,
                // MARGO throws errors if this isn't set
                mostRecentlyGrabbedItem = new SObject()
            };

            this.DummyObject = ItemRegistry.Create<SObject>("770"); // Mixed Seeds dummy object to flag pot ready for harvest
        }

        public new IndoorPot Machine => (IndoorPot)base.Machine;

        public override bool IsIdle => false; // Only supporting harvesting

        public override SObject? HeldObject => this.GetHeldObject();

        private readonly List<SObject> HarvestObjects = [];
        private readonly Farmer FakeFarmer;
        private readonly SObject DummyObject;

        public override bool IsCompatibleWithJunimo(JunimoType projectType)
        {
            return projectType == JunimoType.Crops;
        }

        // 
        protected override SObject TakeItemFromMachine()
        {
            SObject HarvestObject = null!;
            // Harvest if HarvestObjects is not populated and pot is still ready to harvest
            if (!this.HarvestObjects.Any() && this.IsHarvestable())
            {
                this.Harvest();
            }
            // Return and remove the first object in HarvestObjects, if populated
            if (this.HarvestObjects.Any())
            {
                HarvestObject = this.HarvestObjects[0];
                this.HarvestObjects.RemoveAt(0);
            }
            return HarvestObject;
            
        }

        /// <summary>
        /// Return the DummyObject if pot is ready to harvest
        /// </summary>
        /// <returns>The DummyObject. Default: null</returns>
        private SObject? GetHeldObject()
        {
            if (this.IsHarvestable()) return this.DummyObject;
            return null;
        }

        /// <summary>
        /// Calculate the harvest objects from the IndoorPot and add them to this.HarvestItems to be collected
        /// /// </summary>
        private void Harvest()
        {
            // Harvest foragables
                // While growing, foragables look like any other crop in HoeDirt, data wise.
                // Once full grown, the foragable is added to the pot's heldObject field and removed from HoeDirt. See Crop.newDay for logic. 
            if (this.Machine.heldObject.Value != null)
            {
                this.HarvestObjects.Add(this.Machine.heldObject.Value);
                this.Machine.heldObject.Value = null;
                this.Machine.readyForHarvest.Value = false;
                return;
            }

            // Harvest crop from HoeDirt
            if (this.Machine.hoeDirt.Value is HoeDirt dirt && dirt.crop is not null)
            {
                this.HarvestCrop(dirt);
                return;
            }

            // Harvest from bush
            if (this.Machine.bush.Value is Bush bush)
            {
                this.HarvestBush(bush);
            }
            
        }

        private void HarvestCrop(HoeDirt dirt)
        {
            Crop crop = dirt.crop;
            int xTile = (int)dirt.Tile.X;
            int yTile = (int)dirt.Tile.Y;

            // Add the harvested objects to HarvestObjects
            Patcher.Objects = new Action<SObject>(i => { this.HarvestObjects.Add(i); });

            // Farmer Professions and foraging/farming levels not applied when Junimo harvests
            Patcher.Harvester = this.FakeFarmer;

            // Harvest crop
            crop.harvest(xTile, yTile, dirt);

            // Clear patch variables
            Patcher.Objects = null!;
            Patcher.Harvester = null!;
        }

        private void HarvestBush(Bush bush)
        {
            Vector2 tileLocation = this.Machine.TileLocation;

            // Add the harvested objects to HarvestObjects
            Patcher.Objects = new Action<SObject>(i => { this.HarvestObjects.Add(i); });

            // Farmer Professions and foraging/farming levels not applied when Junimo harvests
            Patcher.Harvester = this.FakeFarmer;

            // Harvest bush
            // Call Bush.shake to harvest to support custom bushes
                // Custom Bush has a transpiler on Bush.shake, replacing the call to CreateObjectDebris with a custom method
                // As a result, Junimatic cannot call CreateObjectDebris directly as that would circumvent the call to the Custom Bush CreateObjectDebris
                // This Custom Bush method eventually calls the original CreateObjectDebris, which is where the Junimatic prefix would run, adding the items to HarvestObjects
            bush.shake(tileLocation, doEvenIfStillShaking: false);

            // Clear patch variables
            Patcher.Objects = null!;
            Patcher.Harvester = null!;
        }

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