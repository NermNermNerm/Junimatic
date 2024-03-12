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
        public const string StartAnimalJunimoEvent = "NermNermNerm.Junimatic.StartAnimalJunimoEvent";
        public const string MiddleAnimalJunimoEvent = "NermNermNerm.Junimatic.MiddleAnimalJunimoEvent";
        public const string FinalAnimalJunimoEvent = "NermNermNerm.Junimatic.FinalAnimalJunimoEvent";
        public const string RescueCindersnapJunimo = "NermNermNerm.Junimatic.RescueCindersnapJunimo";

        public const string OldJunimoPortalQuestId = "NermNermNerm.Junimatic.OldJunimoPortalQuestId";
        public const string CollectLostChickenQuest = "NermNermNerm.Junimatic.CollectLostChickenQuest";

        public const string StartAnimalJunimoEventCriteria = "NermNermNerm.Junimatic.StartAnimalJunimoEventCriteria";

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

        internal static void EditForestEvents(IDictionary<string,string> eventData)
        {
            eventData[$"{MiddleAnimalJunimoEvent}/sawEvent {StartAnimalJunimoEvent}/time 600 1700"]
                = $@"sadpiano/
-2000 -2000/
farmer 90 60 2 Junimo 95 72 3/
removeQuest {CollectLostChickenQuest}/
addQuest {RescueCindersnapJunimo}/
skippable/
viewport 90 69 true/
move farmer 0 3 2/
move farmer 0 3 2 true/
advancedMove Junimo false -5 0/
animate Junimo false true 100 8 8 9 10 11 11 10 9/
pause 1500/
emote farmer 16/
message ""That's not a chicken.""/
advancedMove Junimo false 2 0/
animate Junimo false true 50 8 8 9 10 11 11 10 9/
pause 1500/
emote Junimo 28/
pause 1500/
advancedMove Junimo false -1 0/
animate Junimo false true 50 8 8 9 10 11 11 10 9/
pause 1500/
emote Junimo 28/
pause 1500/
message ""I wonder if it's lost and can't find its way home...  It seems distressed.""/
advancedMove Junimo false 0 1/
animate Junimo false true 50 8 8 9 10 11 11 10 9/
emote Junimo 28/
pause 1500/
advancedMove Junimo false 0 -1/
pause 500/
jump Junimo/
emote Junimo 16/
speed Junimo 7/
advancedMove Junimo false 5 0/
pause 1500/
message ""Maybe a Junimo Portal would help it find its way home.""/
end/";
        }

        internal static void EditFarmEvents(IDictionary<string, string> eventData)
        {
            eventData[$"{StartAnimalJunimoEvent}/{StartAnimalJunimoEventCriteria}/sawEvent {JunimoPortalDiscoveryEvent}/time 600 930/weather sunny"]
                = $@"continue/
64 15/
farmer 64 15 2 Marnie 65 16 0/
pause 1000/
speak Marnie ""Hello @!  Are you missing any chickens?  I think one might have run off!""/
emote Marnie 16/
pause 1000/
faceDirection Marnie 2/
speak Marnie ""I saw it across the river to the south of my farm!  I went over to try and round it up, but it ran away from me!$2#$b#Maybe it would come to you if you went down there and called it!""/
addQuest {CollectLostChickenQuest}/
faceDirection Marnie 3/
pause 400/
faceDirection Marnie 0/
speak Marnie ""Oof! I need to be getting back! Anyway, I hope you can wrangle it back home!  Bye now!  Don't be a stranger!""/
pause 200/
globalFade/
viewport -1000 -1000/
end";
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
speak Wizard ""You have something to show me?  Well, bring it!""/
move farmer -1 0 3/
move farmer 0 -4 0/
faceDirection farmer 1/
itemAboveHead (O){OldJunimoPortal}/ 
playSound dwop/
faceDirection farmer 1/
pause 1000/
faceDirection Wizard 3/
speak Wizard ""Ah I see why you thought I should see this...#$b#I believe I recognize the magical traces, but let me consult my vast reference library to be certain...""/
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
speak Wizard ""This is a sort of a crude portal, made by your Grandfather to allow Junimos to easily travel between their world and ours.#$b#It's an easy thing to construct, even the greenest apprentice could do it.  Here, let me teach it to you.""/
removeItem (O){OldJunimoPortal}/
pause 500/
itemAboveHead/
playsound getNewSpecialItem/
addCraftingRecipe {ObjectIds.JunimoPortalRecipe}/
pause 3300/
message ""I learned how to craft a 'Junimo Portal'""/
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

        internal static void EditQuests(IDictionary<string, string> data)
        {
            data[OldJunimoPortalQuestId] = "Basic/The strange little structure/I found the remnants of what looks like a little buildling.  It smells like it has some Forest Magic in it./Bring the remnants of the strange little structure to the wizard./null/-1/0/-1/false";
            data[CollectLostChickenQuest] = "Basic/Chicken Round-Up/Marnie says there's a lost chicken in the forest south of her farm.  I don't think I've lost any chickens, but I should have a look anyway./Enter the Cindersnap Forest during the day./null/-1/0/-1/false";
            data[RescueCindersnapJunimo] = "Basic/Help the Junimo Go Home/I found a lost Junimo in the Cindersnap Forest - I need to help it get home./Enter the Cindersnap Forest during the day with a Junimo Portal in your inventory./null/-1/0/-1/false";
        }
    }
}
