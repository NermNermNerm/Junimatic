using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.TerrainFeatures;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   This represents all the game content related to enabling the Junimo that
    ///   works garden pots.
    /// </summary>
    public class UnlockPots : ISimpleLog
    {
        private ModEntry mod = null!;

        public UnlockPots() { }

        private const string MetLewisMopingPart1EventId = "Junimatic.MetLewisMoping1";
        private const string MetLewisMopingPart2EventId = "Junimatic.MetLewisMoping2";
        private const string EvelynExplainsEventId = "Junimatic.EvelynExplains";
        private const string PotJunimoThankYouEventId = "Junimatic.PotJunimoThankYou";

        private const string GiveLewisPlantQuestId = "Junimatic.GiveLewisPlant";

        private const string MightHaveBeenRoseObjectId = "Junimatic.MightabeenRose";
        private const string MightHaveBeenRoseObjectQiid = "(BC)Junimatic.MightabeenRose";
        private const string IndoorWellObjectId = "Junimatic.IndoorWell";
        private const string IndoorWellObjectQiid = "(BC)Junimatic.IndoorWell";

        private const string IndoorWellRecipeId = "Junimatic.IndoorWellRecipe";

        private const string LewisGotPlantConversationTopic = "Junimatic.LewisGotPlant";
        private const string SawLewisMopingConversationTopic = "Junimatic.SawLewisMoping";

        public void Entry(ModEntry mod)
        {
            this.mod = mod;
            mod.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
        }

        public bool IsUnlocked => ModEntry.Config.EnableWithoutQuests || Game1.MasterPlayer.eventsSeen.Contains(PotJunimoThankYouEventId);

        private void OnAssetRequested(object? sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit(editor =>
                {
                    var objects = editor.AsDictionary<string, BigCraftableData>().Data;
                    objects[MightHaveBeenRoseObjectId] = new BigCraftableData()
                    {
                        Name = MightHaveBeenRoseObjectId,
                        SpriteIndex = 1,
                        CanBePlacedIndoors = false,
                        CanBePlacedOutdoors = false,
                        Description = L("A house plant that a Junimo wants me to give to Lewis."),
                        DisplayName = L("Mightabeen Rose"),
                        Texture = ModEntry.BigCraftablesSpritesPseudoPath,
                    };
                    objects[IndoorWellObjectId] = new BigCraftableData()
                    {
                        Name = IndoorWellObjectId,
                        SpriteIndex = 2,
                        CanBePlacedIndoors = true,
                        CanBePlacedOutdoors = false,
                        Description = L("A source of water for Junimos watering crops.  (Not implemented yet)"),
                        DisplayName = L("Junimo Well"),
                        Texture = ModEntry.BigCraftablesSpritesPseudoPath,
                    };
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> recipes = editor.AsDictionary<string, string>().Data;

                    recipes[IndoorWellRecipeId] = IF($"{StardewValley.Object.stoneID} 20 {StardewValley.Object.woodID} 5 {330 /* clay*/} 5/Field/{IndoorWellObjectId}/true/None/");
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Quests"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, string>().Data;
                    data[GiveLewisPlantQuestId] = SdvQuest("ItemDelivery/Bring the plant to Lewis/Bring the Mightabeen Rose to Lewis/Evelyn might want to have a look at this plant before you give it to Lewis, drop by her house for her to check it out./Lewis (BC)Junimatic.MightabeenRose 1/-1/0/-1/false/For me?  How considerate!  I do love to garden!#$b#Mmm...  The flower's scent it...  it... brings back old memories... and some fresh ones too...#$b#Yes!  I do think I will enjoy this plant.  I sure hope I don't kill it!#$t Junimatic.LewisGotPlant 999");
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Farmhouse"))
            {
                e.Edit(editor =>
                {
                    (int modDeltaX, int modDeltaY) = this.mod.IsRunningSve ? (0, 7) : (0, 0);
                    var d = editor.AsDictionary<string, string>().Data;
                    d[IF($"{PotJunimoThankYouEventId}/H/t 600 620/ActiveDialogueEvent {LewisGotPlantConversationTopic}")] = SdvEvent($@"communityCenter
-2000 -2000
farmer {29 + modDeltaX} {14 + modDeltaY} 3 Junimo {26 + modDeltaX} {14 + modDeltaY} 1
{ModEntry.SetJunimoColorEventCommand} Orange
setSkipActions MarkCraftingRecipeKnown All {IndoorWellRecipeId}
skippable
changeLocation Woods
viewport {27 + modDeltaX} {12 + modDeltaY} true
animate Junimo true true 50 16 17 18 19 20 21 22 23
spriteText 4 ""Thank you for bringing my flower to Lewis...""
pause 3000
playSound junimoMeep1
animate Junimo true true 50 0 1 2 3 4 5 6 7
spriteText 4 ""Since you helped Lewis, I will help with your indoor plants...""
pause 1000
itemAboveHead
playsound getNewSpecialItem
addCraftingRecipe {IndoorWellRecipeId}
animate Junimo true true 100 28 29 30 31
pause 2000
fade
end bed
").Replace("\r", "").Replace("\n", "/");
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Woods"))
            {
                e.Edit(editor =>
                {
                    (int modDeltaX, int modDeltaY) = this.mod.IsRunningSve ? (0, 0) : (40, 15);

                    var d = editor.AsDictionary<string, string>().Data;
                    // 'e 900553' means seen Evelyn's plant-pot event
                    d[IF($"{MetLewisMopingPart1EventId}/H/f Lewis 8/e 900553/w sunny")] = SdvEvent($@"AbigailFlute
-1000 -1000
farmer {80 - modDeltaX} {29 - modDeltaY} 3 Lewis {69 - modDeltaX} {28 - modDeltaY} 0 Junimo -2000 -2000 2
setSkipActions addItem {MightHaveBeenRoseObjectQiid} 1#addQuest {GiveLewisPlantQuestId}
skippable
makeInvisible 68 22 4 9
viewport {67 - modDeltaX} {27 - modDeltaY} true

move farmer -9 0 3
pause 100
faceDirection Lewis 1

jump Lewis 3
speak Lewis ""Ah!  @!  What brings you to the forest today?#$r -1 0 -1 0 event_lewismopes1#Hunting mushrooms!#$r -1 0 event_lewismopes2#Catching exotic fish!#$r -1 0 event_lewismopes3#Just looking for quiet time in the woods""
pause 1000
speak Lewis ""Hm.  It's a good place for that.""
emote farmer 8
pause 1000
speak Lewis ""Me??""
faceDirection Lewis 2
pause 500
faceDirection Lewis 1
pause 500
speak Lewis ""Well, I guess I just like a good walk in the woods from time to time.  It cheers me up.""
pause 1000
emote farmer 8
speak Lewis ""Feeling down?  Me?  No no no.  Right as rain.""
faceDirection Lewis 2
pause 1500
faceDirection Lewis 1
pause 500
speak Lewis ""It was nice chatting with you!  I'd better get back to town!#$b#Being mayor is a 24x7 job!""
move Lewis 10 0 1
move farmer -1 0 3
addConversationTopic {SawLewisMopingConversationTopic} 90

end fade
").Replace("\r", "").Replace("\n", "/");

                d[IF($"{MetLewisMopingPart2EventId}/H/f Lewis 8/e 900553/w sunny")] = SdvEvent($@"AbigailFlute
-1000 -1000
farmer {80 - modDeltaX} {29 - modDeltaY} 3 Lewis {69 - modDeltaX} {28 - modDeltaY} 0 Junimo -2000 -2000 2
setSkipActions addItem {MightHaveBeenRoseObjectQiid} 1#addQuest {GiveLewisPlantQuestId}
skippable
makeInvisible 68 22 4 9
viewport {67 - modDeltaX} {27 - modDeltaY} true

move farmer -9 0 3
pause 100
faceDirection Lewis 1

jump Lewis 3
speak Lewis ""Ah!  @!  You caught me...""
emote farmer 8
pause 1000
speak Lewis ""...You caught me moping around feeling sorry for myself.$2""
faceDirection Lewis 2
pause 500
faceDirection Lewis 1
pause 500
speak Lewis ""Some people come to the woods to try and connect with themselves or something.""
speak Lewis ""Me, well, I come here to kick myself for things I should have done and said but didn't.$2""
faceDirection Lewis 2
pause 1500
faceDirection Lewis 1
pause 500
speak Lewis ""Being a bachelor at my age isn't the only way things could have turned out for me...$2""
speak Lewis ""I'm not unhappy...""
pause 1000
faceDirection Lewis 2
pause 1500
faceDirection Lewis 1
pause 500
speak Lewis ""9 days out of 10...  But sometimes...$2""
pause 500
emote farmer 60
speak Lewis ""I shouldn't trouble you.  I hope you'll forgive me for dumping this on you; thanks for lending an ear.""
pause 1000
faceDirection Lewis 2
pause 1500
faceDirection Lewis 1
pause 500
speak Lewis ""I should get back to town.  I'll see you around.""
move Lewis 10 0 1
move farmer -1 0 3

playSound junimoMeep1
screenFlash .8
warp Junimo {68 - modDeltaX} {29 - modDeltaY}
pause 100
jump farmer
pause 300
emote Junimo 28
playSound junimoMeep1
jump Junimo
pause 1000
playSound junimoMeep1
pause 1000
emote Junimo 8
pause 2000

message ""I think the Junimo wants to help...""

pause 500
jump Junimo 4
screenFlash .8
playSound wand
temporaryAnimatedSprite ""Mods/NermNermNerm/Junimatic/Sprites"" 16 0 16 32 999999 1 0 {69 - modDeltaX} {28 - modDeltaY} false false 9999 0 1 0 0 0
pause 1000
jump Junimo 4
playSound junimoMeep1
pause 500

message ""The Junimo wants me to give this plant to Lewis...""
animate farmer true false 100 58 59 60 61
removeSprite {69 - modDeltaX} {28 - modDeltaY}
playSound dwop
pause 1000
emote Junimo 20
pause 1000
addItem {MightHaveBeenRoseObjectQiid}
addQuest {GiveLewisPlantQuestId}

message ""Huh...  I wonder why a plant would be of help.  Junimos work in strange ways.""
end fade
").Replace("\r", "").Replace("\n", "/");
            });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/JoshHouse"))
            {
                e.Edit(editor =>
                {
                    var d = editor.AsDictionary<string, string>().Data;
                    d[IF($"{EvelynExplainsEventId}/H/i {MightHaveBeenRoseObjectQiid}")] = SdvEvent($@"50s
-1000 -1000
farmer 9 23 0 Evelyn 2 17 1 George 16 22 0
skippable
viewport 4 18 true
textAboveHead Evelyn ""Please come in!""
pause 1000
move farmer 0 -2 0
faceDirection farmer 3
pause 1000
itemAboveHead (BC)Junimatic.MightabeenRose
faceDirection farmer 3
move Evelyn 0 1 2
move Evelyn 1 0 1
speak Evelyn ""Oh now what have you got there?  Bring it over here where the light is better.""
pause 1000
move farmer 0 -2 0
move farmer -3 0 3
move farmer 0 -1 0
move farmer -1 0 3
temporaryAnimatedSprite ""Mods/NermNermNerm/Junimatic/Sprites"" 16 0 16 32 999999 1 0 4 17 false false 9999 0 1 0 0 0
pause 1000
shake Evelyn 100
emote Evelyn 40
pause 2000
move Evelyn 0 -1 0
move Evelyn 1 0 2
shake Evelyn 100
emote Evelyn 40
pause 1000
move Evelyn -1 0 3
move Evelyn 0 1 1
pause 1000
speak Evelyn ""I haven't seen one of these plants for a long, long time.#$b#My grandfather kept one in his study.  It was always a happy, beautiful plant.#$b#Whenever he fell into a funk, he'd go tend to it and emerge feeling better.$1#$b#When he passed, my mother tried to take care of it, but it just slowly withered away.  It missed him too I guess!""
speak Evelyn ""I researched it as a young woman, and it turns out these plants are rare, somewhat finicky, and notoriously hard to propagate.#$b#Some people say the smell of the flower helps them to remember that what might have been isn't necessarily better than the here and now.$1""
shake Evelyn 500
speak Evelyn ""But honestly, I don't feel anything at all from smelling it!  But you say you mean to give it as a gift?""
emote farmer 40
speak Evelyn ""For Lewis?""
pause 2000
speak Evelyn ""Oh...$2""
move Evelyn 0 -1 3
move Evelyn 2 0 2
speak Evelyn ""You know Lewis was quite the casanova in his youth.  He made time with *all* the girls.$1""
speak Evelyn ""There was one girl that was special to him, but she wasn't the sort who wanted to worry about what her man was up to when her back was turned...#$b#At the time, he played like he didn't care.  He was too cool to admit that there was a girl he couldn't get - and kept chasing other girls like it didn't mean anything to him.  I've often wondered if she was the reason he never married.""
faceDirection Evelyn 3
pause 2000
faceDirection Evelyn 2
speak Evelyn ""But just between you and me, I think it turned out for the best for both of them.$1""
end fade

").Replace("\r", "").Replace("\n", "/");
                });
            }
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
            => this.mod.WriteToLog(message, level, isOnceOnly);
    }
}
