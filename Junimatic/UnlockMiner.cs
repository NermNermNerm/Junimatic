using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Powers;
using StardewValley.Locations;
using StardewValley.Monsters;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   Everything to do with unlocking the mining helper junimo
    /// </summary>
    public class UnlockMiner : ISimpleLog
    {
        private ModEntry mod = null!;

        private const string JunimoChrysalisQiid = "(O)Junimatic.JunimoChrysalis";
        private const string ReturnJunimoOrbEvent = "Junimatic.ReturnJunimoOrbEvent";
        private const string MiningJunimoDreamEvent = "Junimatic.MiningJunimoDreamEvent";
        private const string JunimoChrysalisToWizardQuest = "Junimatic.JunimoChrysalisToWizardQuest";
        private const string HasGottenJunimoChrysalisDrop = "Junimatic.HasGottenJunimoChrysalisDrop";

        public void Entry(ModEntry mod)
        {
            this.mod = mod;

            this.mod.Helper.Events.Content.AssetRequested += this.Content_AssetRequested;
            this.mod.Helper.Events.Player.Warped += this.Player_Warped;
            this.mod.Helper.Events.Player.InventoryChanged += this.Player_InventoryChanged;
        }

        public bool IsUnlocked => ModEntry.Config.EnableWithoutQuests || Game1.MasterPlayer.eventsSeen.Contains(MiningJunimoDreamEvent);

        private void Player_InventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            if (e.Added.Any(i => i.QualifiedItemId == JunimoChrysalisQiid))
            {
                if (!e.Player.IsMainPlayer)
                {
                    Game1.addHUDMessage(new HUDMessage(L("Give the strange orb to the host player - only the host can advance this quest.  (Put it in a chest for them)")) { noIcon = true });
                }
                else if (!this.IsUnlocked && !e.Player.questLog.Any(q => q.id.Value == JunimoChrysalisToWizardQuest))
                {
                    e.Player.addQuest(JunimoChrysalisToWizardQuest);
                    e.Player.modData[HasGottenJunimoChrysalisDrop] = true.ToString();
                }
                else
                {
                    this.LogWarning($"Player received a {JunimoChrysalisQiid} when they've already got or have completed the quest");
                }
            }
        }

        private bool IsJunimoChyrysalisFound(Farmer p) => p.modData.ContainsKey(HasGottenJunimoChrysalisDrop);

        private void Player_Warped(object? sender, WarpedEventArgs e)
        {
            if (e.NewLocation is MineShaft mine
                && e.Player.IsMainPlayer
                && !ModEntry.Config.EnableWithoutQuests
                && this.mod.UnlockPortalQuest.IsUnlocked
                && !this.IsJunimoChyrysalisFound(e.Player))
            {
                var bigSlime = mine.characters.OfType<BigSlime>().FirstOrDefault();
                if (bigSlime is not null)
                {
                    var o = ItemRegistry.Create<StardewValley.Object>(JunimoChrysalisQiid);
                    o.questItem.Value = true;
                    bigSlime.heldItem.Value = o;
                }
            }
        }

        private void Content_AssetRequested(object? sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(editor => this.EditObjectData(editor.AsDictionary<string, ObjectData>().Data));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/WizardHouse"))
            {
                e.Edit(editor => this.EditWizardHouseEvents(editor.AsDictionary<string, string>().Data));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/FarmHouse"))
            {
                e.Edit(editor => this.EditFarmHouseEvents(editor.AsDictionary<string, string>().Data));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Quests"))
            {
                e.Edit(editor => this.EditQuests(editor.AsDictionary<string, string>().Data));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Powers"))
            {
                e.Edit(asset =>
                {
                    var powers = asset.AsDictionary<string, PowersData>();
                    powers.Data[$"Junimatic.UnlockMiner"] = new PowersData()
                    {
                        DisplayName = L("Mining Junimo"),
                        Description = L("Junimos will work with mining-related machines like furnaces and geode crushers."),
                        TexturePath = Game1.objectSpriteSheetName,
                        TexturePosition = new Point(32, 192),
                        UnlockedCondition = IF($"PLAYER_HAS_SEEN_EVENT Current {UnlockMiner.MiningJunimoDreamEvent}"),
                        CustomFields = new() {
                            { "Spiderbuttons.SpecialPowerUtilities/Tab", this.mod.ModManifest.UniqueID },
                        }
                    };
                });
            }
        }

        private void EditObjectData(IDictionary<string, ObjectData> objects)
        {
            ModEntry.AddQuestItem(
                objects,
                JunimoChrysalisQiid,
                L("a strange faintly glowing orb"),
                L("It looks vaguely magical. It's quite hard and smooth."),
                1);
        }

        private void EditFarmHouseEvents(IDictionary<string, string> eventData)
        {
            eventData[IF($"{MiningJunimoDreamEvent}/H/sawEvent {ReturnJunimoOrbEvent}/time 600 620")]
                = SdvEvent($@"grandpas_theme
-2000 -1000
farmer 13 23 2
skippable
addTemporaryActor Grandpa 1 1 -100 -100 2 true
specificTemporarySprite grandpaSpirit
viewport -1000 -1000
pause 8000
speak Grandpa ""My dear boy...^My beloved grand-daughter...#$b#I am sorry to come to you like this, but I had to thank you for rescuing my dear Junimo friend.#$b#He protected me at a time when my darkest enemy was my own failing mind.#$b#In better days, he helped me with my smelters and other mine-related machines.  He will help you too; he really enjoys watching the glow of the fires!#$b#I rest much easier now knowing that my friend is safe.  I am so proud of you...""
playmusic none
pause 1000
end bed").Replace("\r", "").Replace("\n", "/");
        }

        private void EditWizardHouseEvents(IDictionary<string, string> eventData)
        {
            eventData[IF($"{ReturnJunimoOrbEvent}/H/i {JunimoChrysalisQiid}")] = SdvEvent(@$"WizardSong
-1000 -1000
farmer 8 24 0 Wizard 10 15 2 Junimo -2000 -2000 2
{ModEntry.SetJunimoColorEventCommand} OrangeRed
removeQuest {JunimoChrysalisToWizardQuest}
removeItem {JunimoChrysalisQiid} 2
skippable
showFrame Wizard 20
viewport 8 18 clamp
move farmer 0 -3 0
pause 2000
speak Wizard ""Ah... Come in.""
pause 800
animate Wizard false false 100 20 21 22 0
playSound dwop
pause 1000
stopAnimation Wizard
move Wizard -2 0 3 false
move Wizard 0 2 2
pause 1500
speak Wizard ""What have you brought for me this time?""
move farmer -1 0 3
move farmer 0 -4 0
faceDirection farmer 1
faceDirection Wizard 3
itemAboveHead (O)Junimatic.JunimoChrysalis
pause 500
faceDirection farmer 1
speak Wizard ""Hm..  Yes...#$b#Junimos are harmless, but they are not defenseless.  When this one was overwhelmed by the slime, it bound itself into a protective stasis and went into a sort of torpor.#$b#If we return this to the Junimo's world, they will be able to care for this one.""
pause 1000
faceDirection Wizard 1
speak Wizard ""I shall, using my immense powers, transport this one back to the Junimo realm!""
playMusic none
pause 800
playSound dwop
screenFlash .8
addObject 10 17 Junimatic.JunimoChrysalis
specificTemporarySprite junimoCage
pause 5000
playSound wand
screenFlash .8
removeObject 10 17
specificTemporarySprite junimoCageGone
playMusic WizardSong
pause 1000
faceDirection Wizard 3
speak Wizard ""There.  Done.""
emote farmer 8
pause 1000
speak Wizard ""Yes.  Done.  It's safely back to the Junimo realm and you should get about your business.""
move farmer 0 2  2
jump Wizard
playSound junimoMeep1
screenFlash .8
warp Junimo 10 17
specificTemporarySprite junimoCage
faceDirection farmer 1
faceDirection Wizard 1
pause 2000
emote Junimo 20
jump Junimo
pause 3000
playSound junimoMeep1
screenFlash .8
warp Junimo -3000 -3000
specificTemporarySprite junimoCageGone
faceDirection Wizard 2
faceDirection farmer 0
pause 1000
faceDirection farmer 1
faceDirection Wizard 1
pause 1000
faceDirection Wizard 2
speak Wizard ""That's it...#$b#Really...#$b#I think.""
move farmer 0 3 2 false
end warpOut
").Replace("\r", "").Replace("\n", "/");
        }

        private void EditQuests(IDictionary<string, string> data)
        {
            data[JunimoChrysalisToWizardQuest] = SdvQuest("Basic/The Strange Orb/Investigate the strange glowing thing you found inside a big slime./Bring the faintly glowing orb to the wizard's tower./null/-1/0/-1/false");
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
            => this.mod.WriteToLog(message, level, isOnceOnly);
    }
}
