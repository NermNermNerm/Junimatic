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

        private const string MetLewisMopingEventId = "Junimatic.MetLewisMoping";
        private const string MetPotJunimoEventId = "Junimatic.MetPotJunimo";
        private const string EvelynExplainsEventId = "Junimatic.EvelynExplains";

        private const string BringPotToWoodsQuestId = "Junimatic.BringPotToWoods";
        private const string GiveLewisPlantQuestId = "Junimatic.GiveLewisPlant";

        private const string MightHaveBeenRoseObjectId = "Junimatic.MightabeenRose";
        private const string MightHaveBeenRoseObjectQiid = "(BC)Junimatic.MightabeenRose";

        private const string LewisGotPlantConversationTopic = "Junimatic.LewisGotPlant";

        public void Entry(ModEntry mod)
        {
            this.mod = mod;
            mod.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
        }

        // Suggestions - 'add mail flag in completing quest'
        public bool IsUnlocked => ModEntry.Config.EnableWithoutQuests /* TODO */;

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
                        Description = L("A house plant that the Junimos want me to bring to Lewis."),
                        DisplayName = L("Mightabeen Rose"),
                        Texture = ModEntry.BigCraftablesSpritesPseudoPath,
                    };
                });
            }
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Quests"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, string>().Data;
                    data[BringPotToWoodsQuestId] = SdvQuest("Basic/Bring a pot to the Secret Woods/Bring a plant pot to the secret woods//null/-1/0/-1/false");
                    data[GiveLewisPlantQuestId] = SdvQuest("ItemDelivery/Bring the plant to Lewis/Bring the Mightabeen Rose to Lewis/Evelyn might want to have a look at this plant before you give it to Lewis, drop by her house for her to check it out./Lewis (BC)Junimatic.MightabeenRose 1/-1/0/-1/false/For me?  How considerate!  I do love to garden!#$b#Mmm...  The flower's scent it...  it... brings back old memories... and some fresh ones too...#$b#Yes!  I do think I will enjoy this plant.  I sure hope I don't kill it!#$t Junimatic.LewisGotPlant 999");
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Woods"))
            {
                (int modDeltaX, int modDeltaY) = this.mod.IsRunningSve ? (40, 15) : (0, 0);

                e.Edit(editor =>
                {
                    // TODO: Adjust coordinates when not running SVE!
                    var d = editor.AsDictionary<string, string>().Data;
                    // 'e 900553' means seen Evelyn's plant-pot event
                    d[IF($"{MetLewisMopingEventId}/H/f Lewis 8/e 900553/w sunny")] = SdvEvent($@"AbigailFlute
-1000 -1000
farmer 80 29 3 Lewis 69 28 0 Junimo -2000 -2000 2
setSkipActions addItem {MightHaveBeenRoseObjectQiid} 1#addQuest {GiveLewisPlantQuestId}
skippable
makeInvisible 68 22 4 9
viewport 67 27 true

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
warp Junimo 68 29
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
temporaryAnimatedSprite ""Mods/NermNermNerm/Junimatic/Sprites"" 16 0 16 32 999999 1 0 69 28 false false 9999 0 1 0 0 0
pause 1000
jump Junimo 4
playSound junimoMeep1
pause 500

message ""The Junimo wants me to give this plant to Lewis...""
animate farmer true false 100 58 59 60 61
removeSprite 69 28
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
speak Evelyn ""I haven't seen one of these plants for a long, long time.#$b#My grandfather kept one in his study.  I was always a happy, beautiful plant.#$b#Whenever he fell into a funk, he'd go tend to it and emerge feeling better.$1#$b#When he passed, my mother tried to take care of it, but it just slowly withered away.  It missed him too I guess!""
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
        {
            this.mod.WriteToLog(message, level, isOnceOnly);
        }
    }
}
