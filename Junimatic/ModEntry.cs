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
        public const string SpritesPseudoPath = "Mods/NermNermNerm/Junimatic/Sprites";

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
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(SpritesPseudoPath))
            {
                e.LoadFromModFile<Texture2D>("assets/Sprites.png", AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit(editor =>
                {
                    ObjectIds.EditAssets(editor.AsDictionary<string, BigCraftableData>().Data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                // TODO: Make this recipe discovered
                e.Edit(editor =>
                {
                    IDictionary<string, string> recipes = editor.AsDictionary<string, string>().Data;
                    recipes[ObjectIds.JunimoPortal] = $"{StardewValley.Object.woodID} 20 {"92" /* sap*/} 30 {-777 /*wild seeds any*/} 5/Field/{ObjectIds.JunimoPortal}/true/Mining 0/";
                });
            }
        }

        private void Input_ButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button.TryGetKeyboard(out Microsoft.Xna.Framework.Input.Keys k))
            {
                if (k == Microsoft.Xna.Framework.Input.Keys.D9)
                {
                    if (!this.isCreated)
                    {
                        var farm = Game1.getFarm();
                        int x = 71;
                        int y = 17;
                        farm.characters.Add(new JunimoShuffler(farm, new Vector2(x, y) * 64f, Color.AliceBlue));
                        this.isCreated = true;
                    }
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
            //if (!this.isCreated)
            //{
            //    var farm = Game1.getFarm();
            //    int x = 71;
            //    int y = 17;
            //    farm.characters.Add(new JunimoShuffler(farm, new Vector2(x, y) * 64f, Color.AliceBlue));
            //    this.isCreated = true;
            //}
        }
    }
}
