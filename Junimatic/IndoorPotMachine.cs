using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

            // Set the HarvestObjects list to the HarvestObjects list stored in the machine's modData if it exists
            if (this.Machine.modData.TryGetValue(IF($"{ModEntry.Instance.ModManifest.UniqueID}/HarvestObjects"), out string HarvestObjectsString))
            {
                if (JsonConvert.DeserializeObject<List<OutputObjectMetadata>>(HarvestObjectsString) is List<OutputObjectMetadata> HarvestObjectsMetadata)
                    this.HarvestObjects = HarvestObjectsMetadata;
            }
        }

        public new IndoorPot Machine => (IndoorPot)base.Machine;

        public override bool IsIdle => false; // Only supporting harvesting

        public override SObject? HeldObject => this.GetHeldObject();

        private readonly List<OutputObjectMetadata> HarvestObjects = [];
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
                // Destroy HoeDirt.crop after harvest to prevent duplicate harvest if farmer tries to harvest after Junimo grabs first object
                HoeDirt? dirt = this.Machine.hoeDirt?.Value;
                if (dirt?.crop is not null && !dirt.crop.RegrowsAfterHarvest())
                    dirt.destroyCrop(false);
            }
            // Return and remove the first object in HarvestObjects, if populated
            if (this.HarvestObjects.Any())
            {
                HarvestObject = this.HarvestObjects[0].ConvertToSObject();
                this.HarvestObjects.RemoveAt(0);

                // Serialize the updated list to modData
                // This fixes the issue where not all objects in HarvestObjects are retrieved as this class does not persist after this method is called.
                // I.E. This class is recreated for the machine each time the junimo is looking for work
                this.Machine.modData[IF($"{ModEntry.Instance.ModManifest.UniqueID}/HarvestObjects")] = JsonConvert.SerializeObject(this.HarvestObjects);
            }
            return HarvestObject;
            
        }

        /// <summary>
        /// Return the DummyObject if pot is ready to harvest
        /// </summary>
        /// <returns>The DummyObject. Default: null</returns>
        private SObject? GetHeldObject()
        {
            if (this.IsHarvestable() || this.HarvestObjects.Any()) return this.DummyObject;
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
                // Convert the object to its metadata
                this.HarvestObjects.Add(new OutputObjectMetadata(this.Machine.heldObject.Value));
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

            // Add the harvested objects metadata to HarvestObjects
            Patcher.Objects = new Action<Item>(i => {
                OutputObjectMetadata Object = new (i);
                // If matching object metadata is already in HarvestObject, increment the stack (i.e. group like items into a single entry)
                if (this.HarvestObjects.Find(x => x.ItemId == Object.ItemId && x.Quality == Object.Quality && x.TintColor == Object.TintColor) is OutputObjectMetadata StackObject)
                    StackObject.Stack += Object.Stack;
                // Otherwise, add the new object metadata to HarvestObjects
                else
                    this.HarvestObjects.Add(Object); 
                });

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
            Patcher.Objects = new Action<Item>(i => {
                OutputObjectMetadata Object = new (i);
                // If matching object metadata is already in HarvestObject, increment the stack (i.e. group like items into a single entry)
                if (this.HarvestObjects.Find(x => x.ItemId == Object.ItemId && x.Quality == Object.Quality && x.TintColor == Object.TintColor) is OutputObjectMetadata StackObject)
                    StackObject.Stack += Object.Stack;
                // Otherwise, add the new object metadata to HarvestObjects
                else
                    this.HarvestObjects.Add(Object); 
                });

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