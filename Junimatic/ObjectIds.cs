using System.Collections.Generic;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;

namespace NermNermNerm.Junimatic
{
    public static class ObjectIds
    {
        public const string JunimoPortal = "Junimatic.JunimoPortal";
        public const string JunimoPortalRecipe = "Junimatic.JunimoPortalRecipe";
        public const string OldJunimoPortal = "Junimatic.OldJunimoPortal";
        public const string JunimoChrysalis = "Junimatic.JunimoChrysalis";

        public const string JunimoPortalDiscoveryEvent = "Junimatic.JunimoPortalDiscoveryEvent";
        public const string MarnieSeesChickenJunimoEvent = "Junimatic.MarnieSeesChickenJunimoEvent";
        public const string LostJunimoDiscoveryEvent = "Junimatic.LostJunimoDiscoveryEvent";
        public const string DropPortalForJunimoEvent = "Junimatic.GivePortalForJunimoEvent";
        public const string AnimalJunimoDreamEvent = "Junimatic.AnimalJunimoDreamEvent";
        public const string ReturnJunimoOrbEvent = "Junimatic.ReturnJunimoOrbEvent";
        public const string MiningJunimoDreamEvent = "Junimatic.MiningJunimoDreamEvent";

        public const string OldJunimoPortalQuest = "Junimatic.OldJunimoPortalQuest";
        public const string CollectLostChickenQuest = "Junimatic.CollectLostChickenQuest";
        public const string RescueCindersnapJunimoQuest = "Junimatic.RescueCindersnapJunimoQuest";
        public const string JunimoChrysalisToWizardQuest = "Junimatic.JunimoChrysalisToWizardQuest";

        public const string StartAnimalJunimoEventCriteria = "Junimatic.StartAnimalJunimoEventCriteria";

        public const string HasGottenJunimoChrysalisDrop = "Junimatic.HasGottenJunimoChrysalisDrop";

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
            addQuestItem(
                JunimoChrysalis,
                "a strange faintly glowing orb", // TODO: 18n
                "It looks vaguely magical. It's quite hard and smooth.", // TODO: 18n
                1);
        }

        internal static void EditForestEvents(IDictionary<string,string> eventData)
        {
            eventData[$"{LostJunimoDiscoveryEvent}/sawEvent {MarnieSeesChickenJunimoEvent}/time 600 1700"]
                = $@"sadpiano/
-2000 -2000/
farmer 90 60 2 Junimo 95 72 3/
removeQuest {CollectLostChickenQuest}/
addQuest {RescueCindersnapJunimoQuest}/
{ModEntry.SetJunimoColorEventCommand} Gold/
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
playSound junimoMeep1/
emote Junimo 16/
speed Junimo 7/
advancedMove Junimo false 5 0/
pause 1500/
message ""Maybe a Junimo Portal would help it find its way home.""/
end/";
            eventData[$"{DropPortalForJunimoEvent}/sawEvent {LostJunimoDiscoveryEvent}/time 600 1700/i (BC){JunimoPortal}"]
    = $@"sadpiano/
-2000 -2000/
farmer 90 60 2 Junimo 89 72 3 Marnie 87 48 2/
removeQuest {RescueCindersnapJunimoQuest}/
skippable/
viewport 90 69 true/
{ModEntry.SetJunimoColorEventCommand} Gold/
animate Junimo false true 100 8 8 9 10 11 11 10 9/
move farmer 0 5 2/
pause 2000/
advancedMove Junimo false 1 0/
animate Junimo false true 100 8 8 9 10 11 11 10 9/
playSound junimoMeep1/
pause 1500/
faceDirection farmer 2/
pause 500/
emote Junimo 28/
pause 500/
advancedMove farmer false 0 2/
advancedMove Junimo false -2 0/
animate Junimo false true 100 8 8 9 10 11 11 10 9/
playSound junimoMeep1/
pause 2500/
advancedMove Junimo false 1 0/
animate Junimo false true 100 8 8 9 10 11 11 10 9/
playSound junimoMeep1/
pause 1000/
faceDirection farmer 2/
emote Junimo 16/
playSound junimoMeep1/
speed Junimo 7/
advancedMove Junimo false 6 0/
animate Junimo false true 100 8 8 9 10 11 11 10 9/
playSound junimoMeep1/
pause 2000/
temporaryAnimatedSprite ""Mods/NermNermNerm/Junimatic/Sprites"" 0 0 16 32 999999 1 0 90 68 false false 9999 0 1 0 0 0/
playsound dwop/
pause 2000/
advancedMove farmer false 0 -1/
pause 1000/
faceDirection farmer 2/
emote Junimo 4/
pause 1000/
advancedMove farmer false 0 -1/
pause 1000/
faceDirection farmer 2/
speed Junimo 1/
advancedMove Junimo false -3 0/
animate Junimo false true 100 8 8 9 10 11 11 10 9/
pause 2500/
playSound junimoMeep1/
emote Junimo 4/
speed Junimo 2/
advancedMove Junimo false -2 0/
animate Junimo false true 100 8 8 9 10 11 11 10 9/
pause 2500/
speed Junimo 7/
advancedMove Junimo false 0 -3/
pause 300/
playsound wand/
move Marnie 0 7 2/
faceDirection farmer 0/
move Marnie 0 7 2/
advancedMove Marnie false 1 0/
advancedMove farmer false 0 -3/
pause 2000/
faceDirection farmer 3/
speak Marnie ""That wasn't a chicken...  was it?$2#$b#Your grandfather had some little houses like that on his farm.  He said they were for his 'helpers'.  But as he declined, he was prone to say, well, a lot of stuff....$2#$b#I suppose I caught a glimpe of them from time to time, but, well...  I think I'm more comfortable not seeing them.$3#$b#You say they're good creatures?  Well...  Okay.  *Something* was sure keeping that farm in trim when your Grandad was declining.$2#$b#I suppose everybody needs a little magic in their lives from time to time.$0""/
pause 1000/
end fade/";
        }

        internal static void EditFarmHouseEvents(IDictionary<string, string> eventData)
        {
            eventData[$"{AnimalJunimoDreamEvent}/sawEvent {DropPortalForJunimoEvent}/time 600 620"]
                = $@"communityCenter/
-2000 -2000/
farmer 29 14 3 Junimo 26 14 1/
{ModEntry.SetJunimoColorEventCommand} PapayaWhip/
skippable/
changeLocation Woods/
viewport 27 12 true/
animate Junimo true true 50 16 17 18 19 20 21 22 23/
spriteText 4 ""Thank you for helping our friend get home...""/
pause 3000/
playSound junimoMeep1/
animate Junimo true true 50 0 1 2 3 4 5 6 7/
spriteText 4 ""I can help you, like I helped your Grandfather...""/
pause 3000/
animate Junimo true true 100 28 29 30 31/
spriteText 4 ""I like animals and the wonderful things you can make with their help...""/
fade/
end bed";
            eventData[$"{MiningJunimoDreamEvent}/sawEvent {ReturnJunimoOrbEvent}/time 600 620"]
                = $@"grandpas_theme/
-2000 -1000/
farmer 13 23 2/
skippable/
fade/
addTemporaryActor Grandpa 1 1 -100 -100 2 true/
specificTemporarySprite grandpaSpirit/
viewport -1000 -1000 true/
pause 8000/
speak Grandpa ""My dear boy...^My beloved grand-daughter...#$b#I am sorry to come to you like this, but I had to thank you for rescuing my dear Junimo friend.#$b#He protected me at a time when my darkest enemy was my own failing mind.#$b#In better days, he helped me with my smelters and other mine-related machines.  He will help you too; he really enjoys watching the glow of the fires!#$b#I rest much easier now knowing that my friend is safe.  I am so proud of you...""/playmusic none/
pause 1000/
end bed";
        }

        internal static void EditFarmEvents(IDictionary<string, string> eventData)
        {
            eventData[$"{MarnieSeesChickenJunimoEvent}/{StartAnimalJunimoEventCriteria}/sawEvent {JunimoPortalDiscoveryEvent}/time 600 930/weather sunny"]
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
removeQuest {OldJunimoPortalQuest}/
addConversationTopic {ConversationKeys.JunimosLastTripToMine} 200
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
globalFade/
viewport -1000 -1000/
message ""Usage: After completing quests to get junimo helpers, you can place Junimo Portals either in buildings or outside.  If outside, place walkways between the hut and any chests or machines you want the Junimo to automate.  If in a building, you can place walkways or just leave a clear path.""/
end warpOut";
            eventData[$"{ReturnJunimoOrbEvent}/i (O){JunimoChrysalis}"] = @$"WizardSong/
-1000 -1000/
farmer 8 24 0 Wizard 10 15 2 Junimo -2000 -2000 2/
{ModEntry.SetJunimoColorEventCommand} OrangeRed/
removeQuest {JunimoChrysalisToWizardQuest}/
removeItem {JunimoChrysalis} 2/
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
move Wizard -2 0 3 false/
move Wizard 0 2 2/
pause 1500/
speak Wizard ""What have you brought for me this time?""/
move farmer -1 0 3/
move farmer 0 -4 0/
faceDirection farmer 1/
faceDirection Wizard 3/
itemAboveHead (O)Junimatic.JunimoChrysalis/
pause 500/
faceDirection farmer 1/
speak Wizard ""Hm..  Yes...#$b#Junimos are harmless, but they are not defenseless.  When this one was overwhelmed by the slime, it bound itself into a protective stasis and went into a sort of torpor.#$b#If we return this to the Junimo's world, they will be able to care for this one.""/
pause 1000/
faceDirection Wizard 1/
speak Wizard ""I shall, using my immense powers, transport this one back to the Junimo realm!""/
playMusic none/
pause 800/
playSound dwop/
screenFlash .8/
addObject 10 17 Junimatic.JunimoChrysalis/
specificTemporarySprite junimoCage/
pause 5000/
playSound wand/
screenFlash .8/
removeObject 10 17/
specificTemporarySprite junimoCageGone/
playMusic WizardSong/
pause 1000/
faceDirection Wizard 3/
speak Wizard ""There.  Done.""/
emote farmer 8/
pause 1000/
speak Wizard ""Yes.  Done.  It's safely back to the Junimo realm and you should get about your business.""/
move farmer 0 2  2/
jump Wizard/
playSound junimoMeep1/
screenFlash .8/
warp Junimo 10 17/
specificTemporarySprite junimoCage/
faceDirection farmer 1/
faceDirection Wizard 1/
pause 2000/
emote Junimo 20/
jump Junimo/
pause 3000/
playSound junimoMeep1/
screenFlash .8/
warp Junimo -3000 -3000/
specificTemporarySprite junimoCageGone/
faceDirection Wizard 2/
faceDirection farmer 0/
pause 1000/
faceDirection farmer 1/
faceDirection Wizard 1/
pause 1000/
faceDirection Wizard 2/
speak Wizard ""That's it...#$b#Really...#$b#I think.""/
move farmer 0 3 2 false/
end warpOut/
";
        }

        internal static void EditQuests(IDictionary<string, string> data)
        {
            data[OldJunimoPortalQuest] = "Basic/The strange little structure/I found the remnants of what looks like a little buildling.  It smells like it has some Forest Magic in it./Bring the remnants of the strange little structure to the wizard./null/-1/0/-1/false";
            data[JunimoChrysalisToWizardQuest] = "Basic/The strange orb/Investigate the strange glowing thing I found inside a big slime./Bring the faintly glowing orb to the wizard./null/-1/0/-1/false";
            data[CollectLostChickenQuest] = "Basic/Chicken Round-Up/Marnie says there's a lost chicken in the forest south of her farm.  I don't think I've lost any chickens, but I should have a look anyway./Enter the Cindersnap Forest during the day./null/-1/0/-1/false";
            data[RescueCindersnapJunimoQuest] = "Basic/Help the Junimo Go Home/I found a lost Junimo in the Cindersnap Forest - I need to help it get home./Enter the Cindersnap Forest during the day with a Junimo Portal in your inventory./null/-1/0/-1/false";
        }
    }
}
