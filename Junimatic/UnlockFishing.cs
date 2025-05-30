using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Powers;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class UnlockFishing
    {
        private ModEntry mod = null!;

        private const string MeetLinusAt60Quest = "Junimatic.MeetLinusAt60";
        private const string MeetLinusAtTentQuest = "Junimatic.MeetLinus";
        private const string MeetLinusAtTentEvent = "Junimatic.MeetLinusAtTent";
        private const string LinusHadADreamMailKey = "Junimatic.LinusHadADream";
        private const string CatchIcePipsQuest = "Junimatic.CatchIcePips";
        private const string AddFishTankPropEventCommand = "Junimatic.AddFishTankProp";
        private const string SetExitLocationCommand = "Junimatic.SetExitLocation";
        private const string HasDoneIcePipsQuestModDataKey = "Junimatic.HasDoneIcePipsQuest";
        private const string IcePipsQuestStartedDayModDataKey = "Junimatic.IcePipsQuestStartedDay";
        private const string IcePipsQuestCompletedDayModDataKey = "Junimatic.IcePipsQuestCompletedDay";
        private const string OnIcePipsConversationKey = "Junimatic.OnIcePipsQuest";
        private const string AfterIcePipsConversationKey = "Junimatic.AfterIcePipsConversationKey";
        private const string IcePipQuestCountKey = "Junimatic.IcePipCount";

        private const string IcePipItemId = "161";

        public static Color JunimoColor => Color.Cyan;

        public void Entry(ModEntry mod)
        {
            this.mod = mod;

            mod.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
            mod.Helper.Events.Player.Warped += this.Player_Warped;
            Event.RegisterCommand(AddFishTankPropEventCommand, this.AddFishTankProp);
            Event.RegisterCommand(SetExitLocationCommand, this.SetExitLocation);
            mod.Helper.Events.GameLoop.OneSecondUpdateTicked += this.GameLoop_OneSecondUpdateTicked;
            mod.Helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;
            mod.Helper.Events.GameLoop.DayStarted += this.GameLoop_DayStarted;

            mod.Helper.ConsoleCommands.Add(
                "Junimatic.EnableFish",
                L("Enables the fishing Junimo - Enables the Junimo for handling fishing things with or without having done the quest."),
                this.ForceEnable);

            this.mod.LogTrace($"Applying harmony patch to MineShaft.getFish");
            var getFishMethod = typeof(MineShaft).GetMethod("getFish");
            this.mod.Harmony.Patch(getFishMethod, prefix: new HarmonyMethod(typeof(UnlockFishing), nameof(Prefix_GetFish)));
        }

        private static bool Prefix_GetFish(ref Item __result, string bait, MineShaft __instance, Farmer who)
        {
            // 'bait' won't tell us what specific bait we're dealing with, but it's a good way to tell the difference
            // if we're being called by the VisibleFish mod instead of actually fishing.
            if (bait != "(O)SpecificBait")
            {
                return true;
            }

            // To know the actual specific bait, you have to look at the farmer - the call doesn't get that level of detail.
            var farmersBait = (who?.CurrentItem as FishingRod)?.GetBait();
            if (farmersBait?.QualifiedItemId == "(O)SpecificBait"
                && farmersBait.preservedParentSheetIndex.Value == "161" // ice pip bait
                && __instance.getMineArea() == 40 // level 60
                && Game1.random.NextDouble() < .9) // 90% chance of working
            {
                // You always get silver quality with this code.  In the game code, the quality might be gold, but I don't think it's worth
                // it to go for that level of fidelity.
                __result = ItemRegistry.Create("(O)161", 1, quality: 1);
                return false;
            }

            return true;
        }

        private void ForceEnable(string cmd, string[] args)
        {
            if (Game1.MasterPlayer is null)
            {
                this.mod.LogError($"This command has to be run when the game is loaded");
                return;
            }

            Game1.MasterPlayer.modData[HasDoneIcePipsQuestModDataKey] = true.ToString();
        }

        private void GameLoop_DayStarted(object? sender, DayStartedEventArgs e)
        {
            // the Quest's objective isn't serialized - fix it up at start-of-day.
            var quest = Game1.MasterPlayer.questLog.FirstOrDefault(q => q.id.Value == CatchIcePipsQuest);
            if (quest is not null)
            {
                quest.modData.TryGetValue(IcePipQuestCountKey, out string? valueAsString);
                int count = 0;
                if (valueAsString is not null)
                {
                    int.TryParse(valueAsString, out count);
                }

                quest.currentObjective = LF($"{count} of 6 teleported");
            }

            // Add convo keys.  Note that all players in multiplayer get the conversation key because it's set here..
            // Linus talks about the dreams continuing
            if (Game1.MasterPlayer.modData.TryGetValue(IcePipsQuestStartedDayModDataKey, out string? dayString)
                && int.TryParse(dayString, out int dayInteger))
            {
                if (Game1.Date.TotalDays == dayInteger + 2)
                {
                    Game1.player.activeDialogueEvents[OnIcePipsConversationKey] = 30;
                }
                else if (Game1.IsMasterGame && Game1.Date.TotalDays > dayInteger + 2)
                {
                    // clean up - no longer needed.  Waiting until the next day so that all (active) players get the key.
                    Game1.MasterPlayer.modData.Remove(IcePipsQuestStartedDayModDataKey);
                }
            }

            // Demetrius talks about reading a paper about the ice pips thing
            if (Game1.MasterPlayer.modData.TryGetValue(IcePipsQuestCompletedDayModDataKey, out dayString)
                && int.TryParse(dayString, out dayInteger))
            {
                if (Game1.Date.TotalDays == dayInteger + 14)
                {
                    Game1.player.activeDialogueEvents[IcePipsQuestCompletedDayModDataKey] = 30;
                }
                else if (Game1.Date.TotalDays > dayInteger + 14)
                {
                    // clean up - no longer needed
                    Game1.MasterPlayer.modData.Remove(IcePipsQuestCompletedDayModDataKey);
                }
            }
        }

        public bool IsUnlocked => ModEntry.Config.EnableWithoutQuests || Game1.MasterPlayer.modData.ContainsKey(HasDoneIcePipsQuestModDataKey);

        private void GameLoop_DayEnding(object? sender, DayEndingEventArgs e)
        {
            if (Game1.IsMasterGame
                && !ModEntry.Config.EnableWithoutQuests
                && this.mod.UnlockPortalQuest.IsUnlocked
                && Game1.player.fishingLevel.Value >= 8
                && Game1.player.getFriendshipHeartLevelForNPC("Linus") >= 7
                && Game1.player.deepestMineLevel > 60
                && !Game1.player.hasOrWillReceiveMail(LinusHadADreamMailKey))
            {
                Game1.player.mailForTomorrow.Add(LinusHadADreamMailKey);
            }
        }

        private int secondsSinceFishWasFirstSeen = 0;

        private void GameLoop_OneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Game1.IsMasterGame || Game1.currentLocation?.Name != "UndergroundMine60")
                return;

            var f = Game1.currentLocation.furniture;
            var tank = Game1.currentLocation.furniture.OfType<FishTankFurniture>().FirstOrDefault();
            var iceFishInTank = tank?.heldItems.FirstOrDefault(f => f.ItemId == IcePipItemId);
            var quest = Game1.MasterPlayer.questLog.FirstOrDefault(q => q.id.Value == CatchIcePipsQuest);
            if (tank is null || iceFishInTank is null || quest is null)
            {
                this.secondsSinceFishWasFirstSeen = 0;
            }
            else if (this.secondsSinceFishWasFirstSeen > 5)
            {
                tank.heldItems.Remove(iceFishInTank);
                tank.UpdateFish();
                tank.refreshFishEvent.Fire();

                Game1.playSound("wand");
                tank.shakeTimer = 100;
                this.secondsSinceFishWasFirstSeen = 0;

                quest.modData.TryGetValue(IcePipQuestCountKey, out string? _countStr);
                int.TryParse(_countStr, out int count);
                ++count;
                if (count >= 6)
                {
                    quest.questComplete();
                    Game1.MasterPlayer.modData[HasDoneIcePipsQuestModDataKey] = true.ToString();
                    Game1.MasterPlayer.modData[IcePipsQuestCompletedDayModDataKey] = Game1.Date.TotalDays.ToString(CultureInfo.InvariantCulture);
                    Game1.MasterPlayer.modData.Remove(IcePipsQuestStartedDayModDataKey);
                    // Should really do this for all players.  Not sure how.
                    Game1.MasterPlayer.activeDialogueEvents.Remove(OnIcePipsConversationKey);

                    DelayedAction.functionAfterDelay(this.RemoveProps, 3000);
                }
                else
                {
                    quest.modData[IcePipQuestCountKey] = count.ToString(CultureInfo.InvariantCulture);
                    quest.currentObjective = LF($"{count} of 6 teleported");
                }
            }
            else
            {
                ++this.secondsSinceFishWasFirstSeen;
            }
        }

        private void RemoveProps()
        {
            var f = Game1.currentLocation.furniture.FirstOrDefault(f => f.ItemId == "DecorativeBarrel");
            if (f is not null)
            {
                Game1.playSound("Cowboy_monsterDie");
                MakePoof(f.TileLocation - new Vector2(.5f, 1f), 2f);
                Game1.currentLocation.furniture.Remove(f);
                DelayedAction.functionAfterDelay(this.RemoveProps, 750);
                return;
            }
            f = Game1.currentLocation.furniture.FirstOrDefault(f => f.ItemId == "2414"); // modern fishtank
            if (f is not null)
            {
                Game1.playSound("explosion");
                MakePoof(f.TileLocation - new Vector2(0f, 1f), 2f);
                Game1.currentLocation.furniture.Remove(f);
                DelayedAction.functionAfterDelay(this.RemoveProps, 750);
                return;
            }
            var junimo = Game1.currentLocation.characters.OfType<Junimo>().FirstOrDefault();
            if (junimo is null)
            {
                MakePoof(new Vector2(7,12), 1f);
                Game1.playSound("junimoMeep1");
                junimo = new EventJunimo(new Vector2(7, 12), new Vector2(3, 0), JunimoColor);
                Game1.currentLocation.characters.Add(junimo);
                DelayedAction.functionAfterDelay(() => junimo.doEmote(20), 2000);
                DelayedAction.functionAfterDelay(() => junimo.jump(), 3000);
                DelayedAction.functionAfterDelay(this.RemoveProps, 5000);
            }
            else
            {
                MakePoof(new Vector2(10, 12), 1f);
                Game1.currentLocation.characters.Remove(junimo);
                Game1.playSound("wand");
                Game1.DrawDialogue(new Dialogue(null, null, L("You've made a new Junimo friend that will help with traps and fishing-related machines")));
            }
        }

        public static void MakePoof(Vector2 tile, float scale)
        {

            Vector2 landingPos = tile * 64f;
            TemporaryAnimatedSprite? dustTas = new(
                textureName: Game1.mouseCursorsName,
                sourceRect: new Rectangle(464, 1792, 16, 16),
                animationInterval: 120f,
                animationLength: 5,
                numberOfLoops: 0,
                position: landingPos,
                flicker: false,
                flipped: Game1.random.NextDouble() < 0.5,
                layerDepth: 9999, // (landingPos.Y + 40f) / 10000f,
                alphaFade: 0.01f,
                color: Color.White,
                scale: Game1.pixelZoom * scale,
                scaleChange: 0.02f,
                rotation: 0f,
                rotationChange: 0f)
            {
                // light = true,
            };

            Game1.Multiplayer.broadcastSprites(Game1.currentLocation, dustTas);
        }

        private void AddFishTankProp(Event @event, string[] args, EventContext context)
        {
            try
            {
                var tank = new FishTankFurniture("2414", new Vector2(8, 10)) { AllowLocalRemoval = false };
                @event.props.Add(tank);

                // This code doesn't work - I'm pretty sure event prop fish tanks can't have stuff in them
                //  (well, they can, but the contents don't get drawn.)  The code is left here just because
                //  maybe someday it'll start to work and it doesn't seem to do any harm.
                var rock = ItemRegistry.Create(StardewValley.Object.stoneQID);
                var seaweed = ItemRegistry.Create("(O)152"); // seaweed
                tank.AddItem(rock);
                tank.AddItem(seaweed);

                @event.props.Add(new Furniture("DecorativeBarrel", new Vector2(7, 11)) { AllowLocalRemoval = false });
                @event.props.Add(new Furniture("DecorativeBarrel", new Vector2(10, 11)) { AllowLocalRemoval = false });
            }
            finally
            {
                @event.CurrentCommand++;
            }
        }

        private void SetExitLocation(Event @event, string[] args, EventContext context)
        {
            try
            {
                @event.setExitLocation("UndergroundMine60", 12, 10);
            }
            finally
            {
                @event.CurrentCommand++;
            }
        }

        private void Player_Warped(object? sender, WarpedEventArgs e)
        {
            if ( e.NewLocation.Name == "UndergroundMine60")
            {
                this.secondsSinceFishWasFirstSeen = 0;

                if (Game1.MasterPlayer.hasQuest(MeetLinusAt60Quest)
                    || Game1.MasterPlayer.hasQuest(CatchIcePipsQuest))
                {
                    var level60 = e.NewLocation;
                    if (level60.getObjectAtTile(7,12) is null)
                    {
                        var tank = new FishTankFurniture("2414", new Vector2(8, 12)) { AllowLocalRemoval = false };
                        e.NewLocation.furniture.Add(tank);
                        var rock = ItemRegistry.Create(StardewValley.Object.stoneQID);
                        var seaweed = ItemRegistry.Create("(O)152"); // seaweed
                        tank.AddItem(rock);
                        tank.AddItem(seaweed);
                        e.NewLocation.furniture.Add(new Furniture("DecorativeBarrel", new Vector2(7, 12)) { AllowLocalRemoval = false });
                        e.NewLocation.furniture.Add(new Furniture("DecorativeBarrel", new Vector2(10, 12)) { AllowLocalRemoval = false });
                    }

                    if (Game1.IsMasterGame && !Game1.MasterPlayer.hasQuest(CatchIcePipsQuest))
                    {
                        Game1.MasterPlayer.modData[IcePipsQuestStartedDayModDataKey] = Game1.Date.TotalDays.ToString(CultureInfo.InvariantCulture);
                        level60.currentEvent = new Event(this.GetIcePipEventText());
                        level60.checkForEvents();
                    }
                }
            }
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Mountain"))
            {
                e.Edit(editor => this.EditMountainEvents(editor.AsDictionary<string, string>().Data));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Quests"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> data = editor.AsDictionary<string, string>().Data;
                    data[MeetLinusAtTentQuest] = SdvQuest("Basic/Find Linus At His Tent/Linus said he had something he needed your help with./Go to Linus' tent before 10pm/null/-1/0/-1/false");
                    data[MeetLinusAt60Quest] = SdvQuest("Basic/Meet Linus At Level 60/Linus had something he wanted to show you at level 60 of the mines./Follow Linus to level 60/null/-1/0/-1/false");
                    data[CatchIcePipsQuest] = SdvQuest("Basic/Catch Six Ice Pips/Catch six ice pips and put them in the mysterious fish tank.//null/-1/0/-1/false");
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> data = editor.AsDictionary<string, string>().Data;
                    data[LinusHadADreamMailKey] = SdvMail($"@,^Please come visit me at my tent.  I've found something and I need your help to sort it out.^   -Linus%item quest {MeetLinusAtTentQuest}%%[#]Meet Linus at his tent");
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Linus"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> data = editor.AsDictionary<string, string>().Data;
                    data[OnIcePipsConversationKey] = L("I'm still having those dreams.  The other me...  who seems less and less like me each night, keeps checking that fish tank in his world.  He's disappointed and puzzled.#$b#I'm pretty puzzled too.  But I'm glad you're working on it.");
                    data[AfterIcePipsConversationKey] = L("Did you finish collecting those fish?  I'm betting you have.  I had one last dream, where the fish were released and the tank disappeared.#$b#You know, one of the reasons I adopted my, uh...  lifestyle is that I was, well, not so stable in the head.#$b#And sometimes angry.  That was the part that made me take what most would regard as a drastic lifestyle change.#$b#But I've never felt saner.  At least up until these dreams started happening.#$b#But in these visions there is nothing like anger, so, well, that's good.#$b#But I still hope they go away.");
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Demetrius"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> data = editor.AsDictionary<string, string>().Data;
                    data[AfterIcePipsConversationKey] = L("Hey I just read a paper written by one of my old college buddies on habitat restoration of an underground pool populated with Ghostfish and Ice Pips!$1#$b#I'm told we have such a cavern deep in the mines.  Perhaps you could take me to it one day.#$b#Funny, the paper didn't specify where he got the fish to repopulate from...$3");
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Powers"))
            {
                e.Edit(asset =>
                {
                    var powers = asset.AsDictionary<string, PowersData>();
                    powers.Data[$"Junimatic.UnlockFishing"] = new PowersData()
                    {
                        DisplayName = L("Fishing Junimo"),
                        Description = L("Junimos will work with fishing-related machines like crab pots and recycling machines."),
                        TexturePath = Game1.objectSpriteSheetName,
                        TexturePosition = new Point(224, 464),
                        UnlockedCondition = IF($"PLAYER_MOD_DATA Current {UnlockFishing.HasDoneIcePipsQuestModDataKey} {true.ToString()}"),
                        CustomFields = new() {
                            { "Spiderbuttons.SpecialPowerUtilities/Tab", this.mod.ModManifest.UniqueID },
                        }
                    };
                });
            }
        }

        private void EditMountainEvents(IDictionary<string, string> eventData)
        {
            eventData[I($"{MeetLinusAtTentEvent}/H/t 600 2200/n {LinusHadADreamMailKey}")] = SdvEvent($@"spring_day_ambient
20 9
farmer 5 9 1 Linus 25 9 1
removeQuest {MeetLinusAtTentQuest}
addQuest {MeetLinusAt60Quest}
skippable

move farmer 10 0 1 true
pause 1500
faceDirection Linus 3
emote Linus 16
pause 1500
move Linus -2 0 3
speak Linus ""@!  I'm so glad you came!""
speak Linus ""Please come to the mines with me.  There's something I have to show you!""
move Linus 0 5 2 true
move farmer 4 0 1 true
fade
viewport -1000 -1000
waitForAllStationary

changeLocation Mine
warp Linus 17 6
warp farmer 17 9
faceDirection farmer 0
viewport 18 12 clamp
fade unfade

pause 2000
speak Linus ""I had been having these recurring dreams...  In it, there was somebody like me, but wasn't me...  You know how dreams are.""
speak Linus ""Anyway, this other me was at a pool, deep in a cave.  It was filled with litter, but the whole community was pitching in to clean it up.""
speak Linus ""They cleaned it, but there were no fish in it anymore, which made the other me sad.""
speak Linus ""But then a fish tank appeared!  And then fish started appearing in the fish tank, and the other me started netting them out and releasing them into the pond!""
speak Linus ""As to what happened next, well, you'd best see for yourself.  Please take the elevator down to 60.""

move Linus 0 -2 0 true
move farmer 0 -2 0 true
{SetExitLocationCommand} UndergroundMine60 12 10 
end fade
");
        }

        private string GetIcePipEventText()
        {
            // TODO: i18n it the same way as other events if possible.
            return SdvEvent($@"continue
-1000 -1000
farmer 12 10 2 Linus 8 14 1
removeQuest {MeetLinusAt60Quest}
addQuest {CatchIcePipsQuest}
skippable
setskipactions addquest Junimatic.CatchIcePips
{AddFishTankPropEventCommand}

viewport 18 8 clamp

pause 1000

speak Linus ""It was just a dream, I thought, but the next night I dreamt the same thing, so I just had to come down here.""
speak Linus ""Look at this!  The fish tank from my dream!  Where did that come from?""
move farmer 0 4 2
faceDirection Linus 0
move farmer -3 0 3
faceDirection farmer 0
pause 1000
faceDirection Linus 1
faceDirection farmer 3
pause 500

speak Linus ""Well, the tank was empty, and I decided I'd try putting a fish in there, so I caught a ghostfish and put it in there.""
speak Linus ""AND IT DISAPPEARED!""
jump Linus
speak Linus ""Poof!""
speak Linus ""Gone!""
faceDirection Linus 0
faceDirection Linus 1
speak Linus ""So I put in another, and it disappeared too!""
speak Linus ""Is it going to the world in my dream?  I don't know, but after about half a dozen fish went in there they stopped disappearing.""
speak Linus ""...But the tank is still here...""
speak Linus ""I see some smaller fish darting around in the pool, but I haven't been able to land any of them.""
speak Linus ""You're the only one that can help since you've got the skill, the equipment and can handle yourself this deep in the mine.""
speak Linus ""Could you try and catch some of those little fish and put them in the tank and see what happens?""
pause 1000
move Linus 0 -1 0
move Linus 4 0 1
faceDirection farmer 1
faceDirection Linus 3
speak Linus ""I'm afraid I've got to leave you to it.  I know it could take a while to catch them and I...""
move Linus 0 -1 0
faceDirection Linus 2
speak Linus ""Well, I've got a lot to think about.""
move Linus 0 -4 0

end");
        }
    }
}
