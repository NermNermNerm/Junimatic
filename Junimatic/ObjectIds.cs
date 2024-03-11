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
        public const string JunimoPortalRecipe = "NermNermNerm.Junimatic.JunimoPortalRecipe";
        public const string OldJunimoPortal = "NermNermNerm.Junimatic.OldJunimoPortal";
        public const string JunimoPortalDiscoveryEvent = "NermNermNerm.Junimatic.OldJunimoPortalDiscovery";
        public const string OldJunimoPortalQuestId = "NermNermNerm.Junimatic.OldJunimoPortalQuestId";

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

        internal static void EditWizardHouseEvents(IDictionary<string, string> eventData)
        {
            // directions:  3 down
            eventData[$"{JunimoPortalDiscoveryEvent}/i (O){OldJunimoPortal}"] = $@"WizardSong/-1000 -1000/farmer 8 24 0 Wizard 10 15 2 Junimo -2000 -2000 2/
removeQuest {OldJunimoPortalQuestId}/
setSkipActions MarkCraftingRecipeKnown All {ObjectIds.JunimoPortalRecipe}#removeItem (O){OldJunimoPortal}/
skippable/
showFrame Wizard 20/
viewport 8 18 true/
move farmer 0 -3 0/
pause 2000/
speak Wizard ""Ah... Come in.""/
pause 800/
animate Wizard false false 100 20 21 22 0/
playSound dwop/
pause 1000/
stopAnimation Wizard/
move Wizard -2 0 3/
move Wizard 0 2 2/
pause 1500/
speak Wizard ""You have something for me?  Well, bring it to me!""/
move farmer -1 0 3/
move farmer 0 -4 0/
faceDirection farmer 1/
itemAboveHead (O){OldJunimoPortal}/ 
playSound dwop/
faceDirection farmer 1/
pause 1000/
faceDirection Wizard 3/
speak Wizard ""Ah I see why you brought it to me...#$b#I believe I recognize the magical traces, but let me consult my vast reference library to be certain...""/
move Wizard 0 -2 0/
faceDirection Wizard 2/
faceDirection farmer 0/
speak Wizard ""Come along then!""/
move Wizard 0 -10 0 farmer 0 -10 0/
move Wizard 1 0 1/
faceDirection Wizard 0/
emote Wizard 40/
pause 1000/
move Wizard -3 0 3/
faceDirection Wizard 0/
emote Wizard 40/
pause 1000/
faceDirection Wizard 2/
speak Wizard ""Yes.  I was right...#$b#As always.""/
move Wizard 2 0 1/
move Wizard 0 2 2/
faceDirection Wizard 3/
faceDirection farmer 1/
speak Wizard ""This is a sort of a crude portal, made long ago by your Grandfather to allow Junimos to easily travel between their world and ours.#$b#Your grandfather apparently made it.  It's an easy thing to do, even the greenest apprentice could do it.  Here, let me teach it to you.""/
removeItem (O){OldJunimoPortal}/
pause 500/
itemAboveHead/
playsound getNewSpecialItem/
addCraftingRecipe {ObjectIds.JunimoPortalRecipe}/
pause 3300/
message ""Learned how to craft a 'Junimo Portal'""/
playMusic none/
shake Wizard 1500/
speak Wizard ""Enticing a Junimo to *use* it, well, that's up to the Junimo...""/
end warpOut";


//            eventData[$"{JunimoPortalDiscoveryEvent}/i (O){OldJunimoPortal}"] = $@"WizardSong/-1000 -1000/farmer 8 24 0 Wizard 10 15 2 Junimo -2000 -2000 2/
//setSkipActions MarkCraftingRecipeKnown {ObjectIds.JunimoPortalRecipe}#removeItem (O){OldJunimoPortal}/
//skippable/
//showFrame Wizard 20/
//viewport 8 18 true/
//move farmer 0 -3 0/
//pause 2000/
//speak Wizard ""Ah... Come in.""/
//pause 800/
//animate Wizard false false 100 20 21 22 0/
//playSound dwop/
//pause 1000/
//stopAnimation Wizard/
//move Wizard -2 0 3 false/
//move Wizard 0 2 2/
//pause 1500/
//speak Wizard ""You have something for me?  Well, show me!""/
//pause 1000/
//itemAboveHead (O){OldJunimoPortal}/ 
//move Wizard 0 1 2/
//speak Wizard ""Ah I see...  Yes.  It is a passageway that the Junimo may use to easily move between this world and theirs.""/
//pause 1500/
//speak Wizard ""Here, I'd like to show you something.""/
//pause 500/
//faceDirection Wizard 1/
//playMusic none/
//pause 800/
//speak Wizard ""Behold!""/
//playMusic clubloop/
//pause 1000/
//showFrame Wizard 19/
//playSound wand/
//screenFlash .8/
//warp Junimo 10 17/
//specificTemporarySprite junimoCage/
//pause 3000/shake Junimo 800/playSound junimoMeep1/
//pause 1000/shake Junimo 800/playSound junimoMeep1/
//pause 1000/
//faceDirection Wizard 1 true/
//showFrame Wizard 4/
//pause 2000/
//shake Junimo 800/
//playSound junimoMeep1/
//pause 1000/
//speak Wizard ""You've seen one before, haven't you?""/
//move Wizard 0 -1 1/
//pause 1000/
//shake Junimo 800/
//playSound junimoMeep1/
//pause 1000/
//speak Wizard ""They call themselves the 'Junimos'...#$b#Mysterious spirits, these ones... For some reason, they refuse to speak with me.""/
//pause 1000/
//playSound dwop/
//faceDirection Wizard 2 true/
//showFrame Wizard 16/
//pause 500/
//playSound wand/
//screenFlash .8/
//warp Junimo -3000 -3000/
//specificTemporarySprite junimoCageGone/
//playMusic WizardSong/
//pause 1000/
//showFrame Wizard 0/
//pause 500/
//speak Wizard ""I'm not sure why they've moved into the community center, but you have no reason to fear them.""/
//pause 1000/
//move farmer 0 -1 0/
//emote farmer 48/
//pause 1000/
//speak Wizard ""Hmmm? You found a golden scroll written in an unknown language?#$b#Most interesting...""/
//move Wizard 0 1 2/
//speak Wizard ""Stay here. I'm going to see for myself. I'll return shortly.""/
//pause 1000/
//playSound shwip/
//faceDirection Wizard 3 true/
//pause 50/
//faceDirection Wizard 0 true/
//pause 50/
//faceDirection Wizard 1 true/
//pause 50/
//faceDirection Wizard 2 true/
//pause 50/
//showFrame Wizard 16/
//pause 500/
//playSound wand/
//warp Wizard -3000 -3000/
//specificTemporarySprite wizardWarp/
//pause 2000/
//faceDirection farmer 1/
//faceDirection farmer 3/
//faceDirection farmer 0/
//pause 2000/
//playSound dwop/
//faceDirection Wizard 0 true/
//faceDirection farmer 1 true/
//pause 50/
//faceDirection farmer 2/
//pause 1500/
//playSound doorClose/
//warp Wizard 8 24/
//faceDirection farmer 2 true/
//showFrame farmer 94/
//startJittering/
//move Wizard 0 -1 0/
//stopJittering/
//showFrame farmer 0/
//move Wizard 0 -2 0/
//speak Wizard ""I found the note...""/
//move Wizard -2 0 3/
//pause 800/
//speak Wizard ""The language is obscure, but I was able to decipher it:""/
//pause 1000/
//message ""We, the Junimo, are happy to aid you. In return, we ask for gifts of the valley. If you are one with the forest then you will see the true nature of this scroll.""/
//pause 500/
//move Wizard 0 -2 3/
//faceDirection farmer 3 true/
//move Wizard -3 0 2/
//pause 1000/
//showFrame Wizard 18/
//emote Wizard 40/
//speak Wizard ""Hmm... 'One with the forest'... What do they mean?""/
//pause 1000/
//speak Wizard ""...*sniff*...*sniff*...""/
//pause 1500/
//showFrame Wizard 0/
//jump Wizard/
//pause 800/
//speak Wizard ""Ah-hah!$h""/
//pause 800/
//faceDirection Wizard 1/
//speak Wizard ""Come here!$h""/
//pause 500/
//move farmer -2 0 0/
//move farmer 0 -1 3/
//move farmer -2 0 3/
//move Wizard -1 0 2/
//move farmer -1 0 2/
//pause 500/
//speak Wizard ""My cauldron is bubbling with ingredients from the forest.$h#$b#Baby fern, moss grub, caramel-top toadstool... Can you smell it?$h""/
//pause 500/
//showFrame Wizard 18/
//showFrame 96/
//pause 1000/
//speak Wizard ""Here. Drink up. Let the essence of the forest permeate your body.$h""/
//pause 800/
//emote farmer 28/
//showFrame Wizard 19/
//pause 800/
//showFrame farmer 90/
//pause 1000/
//farmerEat 184/
//pause 4000/
//playSound gulp/
//animate farmer false true 350 104 105/
//pause 4000/
//specificTemporarySprite farmerForestVision/
//pause 7000/
//pause 19500/
//globalFade .008/
//specificTemporarySprite junimoCageGone2/
//viewport -1000 -1000/
//playMusic none/
//pause 2000/
//playSound reward/
//pause 300/
//message ""You've gained the power of forest magic! Now you can decipher the true meaning of the Junimo scrolls.""/
//removeItem (O){OldJunimoPortal}/
//addCraftingRecipe {ObjectIds.JunimoPortalRecipe}/
//end warpOut";

        }
    }
}
