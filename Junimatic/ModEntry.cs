using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Delegates;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.GarbageCans;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Quests;

namespace NermNermNerm.Junimatic
{
    public class ModEntry
        : Mod, ISimpleLog
    {
        public const string BigCraftablesSpritesPseudoPath = "Mods/NermNermNerm/Junimatic/Sprites";
        public const string OneTileSpritesPseudoPath = "Mods/NermNermNerm/Junimatic/1x1Sprites";

        public const string SetJunimoColorEventCommand = "junimatic.setJunimoColor";

        public Harmony Harmony = null!;

        public bool isCreated = false;

        public ModEntry()
        {
        }

        public override void Entry(IModHelper helper)
        {
            this.Harmony = new Harmony(this.ModManifest.UniqueID);

            this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;

            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.GameLoop_OneSecondUpdateTicked;
            this.Helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;

            this.Helper.Events.Player.InventoryChanged += this.Player_InventoryChanged;

            this.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            this.Helper.Events.GameLoop.DayEnding += this.OnDayEnding;

            this.Helper.Events.Player.Warped += this.Player_Warped;

            Event.RegisterPrecondition(ObjectIds.StartAnimalJunimoEventCriteria, (GameLocation location, string eventId, string[] args) =>
            {
                // TODO: Relocate.  Maybe we need a cutscene class
                var farmAnimals = Game1.getFarm().animals;
                bool hasEnoughAnimals = farmAnimals.Length >= 6;
                bool hasEnoughChickens = farmAnimals.Values.Count(a => a.type.Value.EndsWith(" Chicken")) >= 2;
                return hasEnoughAnimals && hasEnoughChickens;
            }
            );

            Event.RegisterCommand(SetJunimoColorEventCommand, this.SetJunimoColor);
        }

        private bool IsJunimoPortalDiscovered(Farmer p) => p.eventsSeen.Contains(ObjectIds.JunimoPortalDiscoveryEvent);
        private bool IsJunimoChyrysalisFound(Farmer p) => p.modData.ContainsKey(ObjectIds.HasGottenJunimoChrysalisDrop);

        private void Player_Warped(object? sender, WarpedEventArgs e)
        {
            if (e.NewLocation is MineShaft mine && e.Player.IsMainPlayer && this.IsJunimoPortalDiscovered(e.Player) && !this.IsJunimoChyrysalisFound(e.Player))
            {
                var x = mine.isTileClearForMineObjects(0, 0);

                var bigSlime = mine.characters.OfType<BigSlime>().FirstOrDefault();
                if (bigSlime is not null)
                {
                    var o = ItemRegistry.Create<StardewValley.Object>(ObjectIds.JunimoChrysalis);
                    o.questItem.Value = true;
                    bigSlime.heldItem.Value = o;
                }
            }
        }

        private void SetJunimoColor(Event @event, string[] split, EventContext context)
        {
            try
            {
                Color color = Color.Goldenrod;
                if (split.Length > 2)
                {
                    this.LogWarning($"{SetJunimoColorEventCommand} usage: [ <color> ]    where <color> is one of the constants in Microsoft.Xna.Framework.Color - e.g. 'Goldenrod'");
                    return;
                }

                if (split.Length == 2)
                {
                    var prop = typeof(Color).GetProperty(split[1], BindingFlags.Public | BindingFlags.Static);
                    if (prop is null)
                    {
                        this.LogWarning($"{SetJunimoColorEventCommand} was given '{split[1]}' as an argument, but that's not a valid Color constant.");
                        return;
                    }

                    color = (Color)prop.GetValue(null)!;
                }

                var junimo = @event.actors.OfType<Junimo>().FirstOrDefault();
                if (junimo is null)
                {
                    this.LogWarning($"{SetJunimoColorEventCommand} invoked when there wasn't a Junimo actor");
                    return;
                }

                var property = typeof(Junimo).GetField("color", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (property is null)
                {
                    this.LogError($"{SetJunimoColorEventCommand} can't set color because the game code is changed and the 'color' field is not there anymore.");
                    return;
                }

                ((NetColor)property.GetValue(junimo)!).Value = color;
            }
            finally
            {
                @event.CurrentCommand++;
            }
        }


        private void Player_InventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            if (e.Added.Any(i => i.ItemId == ObjectIds.OldJunimoPortal))
            {
                if (e.Player.IsMainPlayer)
                {
                    e.Player.addQuest(ObjectIds.OldJunimoPortalQuest);
                }
                else
                {
                    Game1.addHUDMessage(new HUDMessage("Give the strange little structure to the host player - only the host can advance this quest."));
                }
            }
            else if (e.Added.Any(i => i.ItemId == ObjectIds.JunimoChrysalis))
            {
                if (e.Player.IsMainPlayer)
                {
                    e.Player.addQuest(ObjectIds.JunimoChrysalisToWizardQuest);
                }
                else
                {
                    Game1.addHUDMessage(new HUDMessage("Give the strange orb to the host player - only the host can advance this quest."));
                }
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            this.LogInfo($"OnAssetRequested({e.NameWithoutLocale})");
            if (e.NameWithoutLocale.IsEquivalentTo(BigCraftablesSpritesPseudoPath))
            {
                e.LoadFromModFile<Texture2D>("assets/Sprites.png", AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(OneTileSpritesPseudoPath))
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
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/FarmHouse"))
            {
                e.Edit(editor =>
                {
                    ObjectIds.EditFarmHouseEvents(editor.AsDictionary<string, string>().Data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Forest"))
            {
                e.Edit(editor =>
                {
                    ObjectIds.EditForestEvents(editor.AsDictionary<string, string>().Data);
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
                    this.TestPlacePortal();
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


        public void TestPlacePortal()
        {
            // TODO: do only when it's raining and when it hasn't been placed already.

            var farm = Game1.getFarm();
            var existing = farm.objects.Values.FirstOrDefault(o => o.ItemId == ObjectIds.OldJunimoPortal);
            if (existing is not null)
            {
                this.LogInfoOnce($"{ObjectIds.OldJunimoPortal} is already placed at {existing.TileLocation.X},{existing.TileLocation.Y}");
                return;
            }

            var k = farm.Objects.Keys.First();

            List<Vector2> weedLocations = farm.objects.Pairs.Where(pair => pair.Value.ItemId == "784" /* weed*/).Select(pair => pair.Key).ToList();
            if (weedLocations.Count == 0)
            {
                this.LogWarning("No weeds on farm, can't place the old junimo portal");
                return;
            }

            var position = weedLocations[Game1.random.Next(weedLocations.Count)];
            var o = ItemRegistry.Create<StardewValley.Object>(ObjectIds.OldJunimoPortal);
            o.questItem.Value = true;
            o.Location = Game1.getFarm();
            o.TileLocation = position;
            this.LogInfoOnce($"{ObjectIds.OldJunimoPortal} placed at {position.X},{position.Y}");
            o.IsSpawnedObject = true;
            farm.objects[o.TileLocation] = o;
        }
    }
}
