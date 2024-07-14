using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    /// Store the required data to recreate the object/colored object
    /// Used for storing the object in Machine.modData for object persistence 
    /// </summary> 
    internal class OutputObjectMetadata
    {
        public string ItemId { get; }
        public int Stack { get; set; }
        public int Quality { get; }
        public Color? TintColor { get; }

        [JsonConstructor]
        public OutputObjectMetadata(string ItemId, int Stack, int Quality, Color? TintColor = null)
        {
            this.ItemId = ItemId;
            this.Stack = Stack;
            this.Quality = Quality;
            this.TintColor = TintColor;
        }

        public OutputObjectMetadata(Item Item)
        {
            this.ItemId = Item.QualifiedItemId;
            this.Stack = Item.Stack;
            this.Quality = Item.Quality;
            if (Item is ColoredObject ColoredObject)
                this.TintColor = ColoredObject.color.Value;
        }
        
        /// <summary>
        /// Create a StardewValley.Object or ColoredObject from the metadata
        /// </summary>
        /// <returns>The StardewValley.Object</returns>
        public SObject ConvertToSObject()
        {
            // Create ColoredObject if TintColor is populated
            if (this.TintColor is Color color)
            {
                return new ColoredObject(this.ItemId, this.Stack, color){ Quality = this.Quality };
            }
            // Otherwise, create StardewValley.Object
            return ItemRegistry.Create<SObject>(this.ItemId, this.Stack, this.Quality);
        }
    }
}