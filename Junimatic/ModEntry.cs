using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.GarbageCans;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;

namespace NermNermNerm.Junimatic
{
    public class ModEntry
        : Mod, ISimpleLog
    {
        public const string BigCraftablesSpritesPseudoPath = "Mods/NermNermNerm/Junimatic/Sprites";
        public const string OneTileSpritesPseudoPath = "Mods/NermNermNerm/Junimatic/1x1Sprites";

        private JunimoPortalQuestController junimoPortalQuestController = null!;

        public Harmony Harmony = null!;

        public bool isCreated = false;

        public ModEntry()
        {
        }

        public override void Entry(IModHelper helper)
        {
            this.Harmony = new Harmony(this.ModManifest.UniqueID);
            this.junimoPortalQuestController = new JunimoPortalQuestController(this);

            this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;

            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.GameLoop_OneSecondUpdateTicked;
            this.Helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;

            this.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            this.Helper.Events.GameLoop.DayEnding += this.OnDayEnding;

            Event.RegisterPrecondition(ObjectIds.StartAnimalJunimoEventCriteria, (GameLocation location, string eventId, string[] args) =>
            {
                // TODO: Relocate.  Maybe we need a cutscene class
                var farmAnimals = Game1.getFarm().animals;
                bool hasEnoughAnimals = farmAnimals.Length >= 6;
                bool hasEnoughChickens = farmAnimals.Values.Count(a => a.type.Value.EndsWith(" Chicken")) >= 2;
                return hasEnoughAnimals && hasEnoughChickens;
            }
            );
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            this.junimoPortalQuestController.OnDayStarted();
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            this.junimoPortalQuestController.OnDayEnding();
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            this.LogInfo($"OnAssetRequested({e.NameWithoutLocale}");
            if (e.NameWithoutLocale.IsEquivalentTo(BigCraftablesSpritesPseudoPath))
            {
                e.LoadFromModFile<Texture2D>("assets/Sprites.png", AssetLoadPriority.Exclusive);
            }
            if (e.NameWithoutLocale.IsEquivalentTo(OneTileSpritesPseudoPath))
            {
                e.LoadFromModFile<Texture2D>("assets/1x1_Sprites.png", AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit(editor =>
                {
                    ObjectIds.EditBigCraftableData(editor.AsDictionary<string, BigCraftableData>().Data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(editor =>
                {
                    ObjectIds.EditObjectData(editor.AsDictionary<string, ObjectData>().Data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                // TODO: Make this recipe discovered
                e.Edit(editor =>
                {
                    IDictionary<string, string> recipes = editor.AsDictionary<string, string>().Data;
                    recipes[ObjectIds.JunimoPortalRecipe] = $"{StardewValley.Object.woodID} 20 {"92" /* sap*/} 30 {-777 /*wild seeds any*/} 5/Field/{ObjectIds.JunimoPortal}/true/None/";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/WizardHouse"))
            {
                e.Edit(editor =>
                {
                    ObjectIds.EditWizardHouseEvents(editor.AsDictionary<string, string>().Data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Farm"))
            {
                e.Edit(editor =>
                {
                    ObjectIds.EditFarmEvents(editor.AsDictionary<string, string>().Data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Quests"))
            {
                e.Edit(editor =>
                {
                    ObjectIds.EditQuests(editor.AsDictionary<string, string>().Data);
                });
            }
        }

        private void Input_ButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button.TryGetKeyboard(out Microsoft.Xna.Framework.Input.Keys k))
            {
                if (k == Microsoft.Xna.Framework.Input.Keys.Insert)
                {
                    var assignment = (new WorkFinder()).GlobalFindProjects().FirstOrDefault();
                    if (assignment is not null)
                    {
                        assignment.location.characters.Add(new JunimoShuffler(assignment));
                    }

                    //if (!this.isCreated)
                    //{
                    //    var farm = Game1.getFarm();
                    //    int x = 71;
                    //    int y = 17;
                    //    farm.characters.Add(new JunimoShuffler(farm, new Vector2(x, y) * 64f, Color.AliceBlue));
                    //    this.isCreated = true;
                    //}
                }

                if (k == Microsoft.Xna.Framework.Input.Keys.Home)
                {
                    this.junimoPortalQuestController.TestPlacePortal();
                }
            }
        }

        void ISimpleLog.WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            if (isOnceOnly)
            {
                this.Monitor.LogOnce(message, level);
            }
            else
            {
                this.Monitor.Log(message, level);
            }
        }

        private void GameLoop_OneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            // var farm = Game1.getFarm();
            //if (!this.isCreated)
            //{
            //    var farm = Game1.getFarm();
            //    int x = 71;
            //    int y = 17;
            //    farm.characters.Add(new JunimoShuffler(farm, new Vector2(x, y) * 64f, Color.AliceBlue));
            //    this.isCreated = true;
            //}
            //farm.getObjectAt(75 * 64, 15 * 64).heldObject.Value = null;

        }
    }
}
