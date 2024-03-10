using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;

namespace NermNermNerm.Junimatic
{
    public static class ObjectIds
    {
        public const string JunimoPortal = "NermNermNerm.Junimatic.JunimoPortal";
        public const string OldJunimoPortal = "NermNermNerm.Junimatic.OldJunimoPortal";

        internal static void EditBigCraftableData(IDictionary<string, BigCraftableData> objects)
        {
            objects[JunimoPortal] = new BigCraftableData()
            {
                Name = JunimoPortal,
                SpriteIndex = 0,
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = true,
                Description = "A portal through which Junimos who want to help out on the farm can appear.  Place pathways next to these when placing them outdoors so the Junimos will know where to go.",
                DisplayName = "Junimo Portal",
                Texture = ModEntry.BigCraftablesSpritesPseudoPath,
            };
        }

        internal static void EditObjectData(IDictionary<string, ObjectData> objects)
        {
            void addQuestItem(string id, string displayName, string description, int spriteIndex)
            {
                objects[id] = new()
                {
                    Name = id,
                    DisplayName = displayName,
                    Description = description,
                    Type = "Quest",
                    Category = -999,
                    Price = 0,
                    Texture = ModEntry.OneTileSpritesPseudoPath,
                    SpriteIndex = spriteIndex,
                    ContextTags = new() { "not_giftable", "not_placeable", "prevent_loss_on_death" },
                };
            };
            addQuestItem(
                OldJunimoPortal,
                "a strange little structure", // TODO: 18n
                "At first it looked like a woody weed, but a closer look makes it like a little structure, and it smells sorta like the Wizard's forest-magic potion.", // TODO: 18n
                0);
        }
    }
}
