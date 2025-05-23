using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Powers;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class UnlockAnimal : ISimpleLog
    {
        private ModEntry mod = null!;

        private const string MarnieSeesChickenJunimoEvent = "Junimatic.MarnieSeesChickenJunimoEvent";
        private const string LostJunimoDiscoveryEvent = "Junimatic.LostJunimoDiscoveryEvent";
        private const string DropPortalForJunimoEvent = "Junimatic.GivePortalForJunimoEvent";
        private const string AnimalJunimoDreamEvent = "Junimatic.AnimalJunimoDreamEvent";
        private const string CollectLostChickenQuest = "Junimatic.CollectLostChickenQuest";

        // Note:  The quest has cinder-*S*nap in it because it was originally misspelled that way.  Keeping it
        //  the same ensures compat between versions and this string isn't visible to the player.
        private const string RescueCindersapJunimoQuest = "Junimatic.RescueCindersnapJunimoQuest"; 
        private const string StartAnimalJunimoEventCriteria = "Junimatic.StartAnimalJunimoEventCriteria";

        public void Entry(ModEntry mod)
        {
            this.mod = mod;

            mod.Helper.Events.Content.AssetRequested += this.OnAssetRequested;

            Event.RegisterPrecondition(StartAnimalJunimoEventCriteria, (GameLocation location, string eventId, string[] args) =>
            {
                if (ModEntry.Config.EnableWithoutQuests)
                {
                    return false;
                }

                var farmAnimals = Game1.getFarm().animals;
                bool hasEnoughAnimals = farmAnimals.Length >= 6;
                bool hasEnoughChickens = farmAnimals.Values.Count(a => a.type.Value.EndsWith(I(" Chicken"))) >= 2;
                return hasEnoughAnimals && hasEnoughChickens;
            });
        }

        public bool IsUnlocked => ModEntry.Config.EnableWithoutQuests || Game1.MasterPlayer.eventsSeen.Contains(AnimalJunimoDreamEvent);

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Farm"))
            {
                e.Edit(editor => this.EditFarmEvents(editor.AsDictionary<string, string>().Data));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/FarmHouse"))
            {
                e.Edit(editor => this.EditFarmHouseEvents(editor.AsDictionary<string, string>().Data));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Forest"))
            {
                e.Edit(editor => this.EditForestEvents(editor.AsDictionary<string, string>().Data));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Quests"))
            {
                e.Edit(editor => this.EditQuests(editor.AsDictionary<string, string>().Data));
            }
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Powers"))
            {
                e.Edit(asset =>
                {
                    var powers = asset.AsDictionary<string, PowersData>();
                    powers.Data[$"Junimatic.UnlockAnimal"] = new PowersData()
                    {
                        DisplayName = L("Crops Junimo"),
                        Description = L("Junimos will work with animal-related machines like cheese presses, looms and mayonnaise machines."),
                        TexturePath = Game1.objectSpriteSheetName,
                        TexturePosition = new Point(288, 112),
                        UnlockedCondition = IF($"PLAYER_HAS_SEEN_EVENT Current {UnlockAnimal.AnimalJunimoDreamEvent}"),
                        CustomFields = new() {
                            { "Spiderbuttons.SpecialPowerUtilities/Tab", this.mod.ModManifest.UniqueID },
                        }
                    };
                });
            }
        }

        private void EditForestEvents(IDictionary<string,string> eventData)
        {
            eventData[IF($"{LostJunimoDiscoveryEvent}/H/sawEvent {MarnieSeesChickenJunimoEvent}/time 600 1900")]
                = SdvEvent($@"sadpiano
-2000 -2000
farmer 90 60 2 Junimo 95 72 3
removeQuest {CollectLostChickenQuest}
addQuest {RescueCindersapJunimoQuest}
{ModEntry.SetJunimoColorEventCommand} Gold
skippable
makeInvisible 89 60 3 15
viewport 90 69 clamp
move farmer 0 3 2
move farmer 0 3 2 true
advancedMove Junimo false -5 0
animate Junimo false true 100 8 8 9 10 11 11 10 9
pause 1500
emote farmer 16
message ""That's not a chicken.""
advancedMove Junimo false 2 0
animate Junimo false true 50 8 8 9 10 11 11 10 9
pause 1500
emote Junimo 28
pause 1500
advancedMove Junimo false -1 0
animate Junimo false true 50 8 8 9 10 11 11 10 9
pause 1500
emote Junimo 28
pause 1500
message ""I wonder if it's lost and can't find its way home...  It seems distressed.""
advancedMove Junimo false 0 1
animate Junimo false true 50 8 8 9 10 11 11 10 9
emote Junimo 28
pause 1500
advancedMove Junimo false 0 -1
pause 500
jump Junimo
playSound junimoMeep1
emote Junimo 16
speed Junimo 7
advancedMove Junimo false 5 0
pause 1500
message ""Maybe a Junimo Portal would help it find its way home.""
end/");
            eventData[IF($"{DropPortalForJunimoEvent}/H/sawEvent {LostJunimoDiscoveryEvent}/time 600 1900/i (BC){UnlockPortal.JunimoPortal}")]
    = SdvEvent($@"sadpiano
-2000 -2000
farmer 90 60 2 Junimo 89 72 3 Marnie 87 48 2
removeQuest {RescueCindersapJunimoQuest}
skippable
makeInvisible 86 48 6 27
viewport 90 69 clamp
{ModEntry.SetJunimoColorEventCommand} Gold
animate Junimo false true 100 8 8 9 10 11 11 10 9
move farmer 0 5 2
pause 2000
advancedMove Junimo false 1 0
animate Junimo false true 100 8 8 9 10 11 11 10 9
playSound junimoMeep1
pause 1500
faceDirection farmer 2
pause 500
emote Junimo 28
pause 500
advancedMove farmer false 0 2
advancedMove Junimo false -2 0
animate Junimo false true 100 8 8 9 10 11 11 10 9
playSound junimoMeep1
pause 2500
advancedMove Junimo false 1 0
animate Junimo false true 100 8 8 9 10 11 11 10 9
playSound junimoMeep1
pause 1000
faceDirection farmer 2
emote Junimo 16
playSound junimoMeep1
speed Junimo 7
advancedMove Junimo false 6 0
animate Junimo false true 100 8 8 9 10 11 11 10 9
playSound junimoMeep1
pause 2000
temporaryAnimatedSprite ""Mods/NermNermNerm/Junimatic/Sprites"" 0 0 16 32 999999 1 0 90 68 false false 9999 0 1 0 0 0
playsound dwop
pause 2000
advancedMove farmer false 0 -1
pause 1000
faceDirection farmer 2
emote Junimo 4
pause 1000
advancedMove farmer false 0 -1
pause 1000
faceDirection farmer 2
speed Junimo 1
advancedMove Junimo false -3 0
animate Junimo false true 100 8 8 9 10 11 11 10 9
pause 2500
playSound junimoMeep1
emote Junimo 4
speed Junimo 2
advancedMove Junimo false -2 0
animate Junimo false true 100 8 8 9 10 11 11 10 9
pause 2500
speed Junimo 7
advancedMove Junimo false 0 -3
pause 300
playsound wand
move Marnie 0 7 2
faceDirection farmer 0
move Marnie 0 7 2
advancedMove Marnie false 1 0
advancedMove farmer false 0 -3
pause 2000
faceDirection farmer 3
speak Marnie ""That wasn't a chicken...  was it?$2#$b#Your grandfather had some little houses like that on his farm.  He said they were for his 'helpers'.  But as he declined, he was prone to say, well, a lot of stuff....$2#$b#I suppose I caught a glimpse of them from time to time, but, well...  I think I'm more comfortable not seeing them.$3#$b#You say they're good creatures?  Well...  Okay.  *Something* was sure keeping that farm in trim when your Grandad was declining.$2#$b#I suppose everybody needs a little magic in their lives from time to time.$0""
pause 1000
end fade/");
        }

        private void EditFarmHouseEvents(IDictionary<string, string> eventData)
        {
            (int modDeltaX, int modDeltaY) = this.mod.IsRunningSve ? (0, 7) : (0, 0);

            eventData[IF($"{AnimalJunimoDreamEvent}/H/sawEvent {DropPortalForJunimoEvent}/time 600 620")]
                = SdvEvent($@"communityCenter
-2000 -2000
farmer {29+modDeltaX} {14+modDeltaY} 3 Junimo {26+modDeltaX} {14+modDeltaY} 1
{ModEntry.SetJunimoColorEventCommand} PapayaWhip
skippable
changeLocation Woods
viewport {27 + modDeltaX} {12 + modDeltaY} clamp
animate Junimo true true 50 16 17 18 19 20 21 22 23
spriteText 4 ""Thank you for helping our friend get home...""
pause 3000
playSound junimoMeep1
animate Junimo true true 50 0 1 2 3 4 5 6 7
spriteText 4 ""I can help you, like I helped your Grandfather...""
pause 3000
animate Junimo true true 100 28 29 30 31
spriteText 4 ""I like animals and the wonderful things you can make with their help...""
fade
end bed");
        }

        private void EditFarmEvents(IDictionary<string, string> eventData)
        {
            eventData[IF($"{MarnieSeesChickenJunimoEvent}/H/{StartAnimalJunimoEventCriteria}/sawEvent {UnlockPortal.JunimoPortalDiscoveryEvent}/time 600 930/weather sunny")]
                = SdvEvent($@"continue
64 15
farmer 64 15 2 Marnie 65 16 0
pause 1000
speak Marnie ""Hello @!  Are you missing any chickens?  I think one might have run off!""
emote Marnie 16
pause 1000
faceDirection Marnie 2
speak Marnie ""I saw it across the river to the south of my farm!  I went over to try and round it up, but it ran away from me!$2#$b#Maybe it would come to you if you went down there and called it!""
addQuest {CollectLostChickenQuest}
faceDirection Marnie 3
pause 400
faceDirection Marnie 0
speak Marnie ""Oof! I need to be getting back! Anyway, I hope you can wrangle it back home!  Bye now!  Don't be a stranger!""
pause 200
globalFade
viewport -1000 -1000
end");
        }

        private void EditQuests(IDictionary<string, string> data)
        {
            data[CollectLostChickenQuest] = SdvQuest("Basic/Chicken Round-Up/Marnie thinks one of your chickens has escaped and is in the forest south of her farm; you should investigate./Enter the Cindersap Forest during the day./null/-1/0/-1/false");
            data[RescueCindersapJunimoQuest] = SdvQuest("Basic/Help the Junimo Go Home/Help the Junimo in the Cindersap Forest get home./Enter the Cindersap Forest during the day with a Junimo Portal in your inventory./null/-1/0/-1/false");
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            this.mod.WriteToLog(message, level, isOnceOnly);
        }
    }
}
