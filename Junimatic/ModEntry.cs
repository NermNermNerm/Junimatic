using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Objects;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class ModEntry
        : Mod, ISimpleLog
    {
        public const string BigCraftablesSpritesPseudoPath = "Mods/NermNermNerm/Junimatic/Sprites";
        public const string OneTileSpritesPseudoPath = "Mods/NermNermNerm/Junimatic/1x1Sprites";

        public const string SetJunimoColorEventCommand = "junimatic.setJunimoColor";

        public UnlockPortal UnlockPortalQuest = new UnlockPortal();
        public UnlockCropMachines CropMachineHelperQuest = new UnlockCropMachines();
        public UnlockMiner UnlockMiner = new UnlockMiner();
        public UnlockAnimal UnlockAnimal = new UnlockAnimal();
        public UnlockForest UnlockForest = new UnlockForest();
        public UnlockFishing UnlockFishing = new UnlockFishing();
        public UnlockPots UnlockPots = new UnlockPots();
        public JunimoStatus JunimoStatusDialog = new JunimoStatus();

        private readonly WorkFinder workFinder = new WorkFinder();
        public PetFindsThings PetFindsThings = new PetFindsThings();

        public static ModEntry Instance = null!;

        public static ModConfig Config = null!;
        public ModConfigMenu ConfigMenu = new ModConfigMenu();

        public Harmony Harmony = null!;

        public ModEntry() { }

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            Initialize(this);
            this.Helper.Events.Content.LocaleChanged += (_, _) => this.Helper.GameContent.InvalidateCache("Data/Objects");

            this.Harmony = new Harmony(this.ModManifest.UniqueID);

            Config = this.Helper.ReadConfig<ModConfig>();
            this.ConfigMenu.Entry(this);
            this.CropMachineHelperQuest.Entry(this);
            this.UnlockPortalQuest.Entry(this);
            this.UnlockMiner.Entry(this);
            this.UnlockAnimal.Entry(this);
            this.UnlockForest.Entry(this);
            this.workFinder.Entry(this);
            this.UnlockFishing.Entry(this);
            this.UnlockPots.Entry(this);
            this.PetFindsThings.Entry(this);
            this.JunimoStatusDialog.Entry(this);

            this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;

            //this.Helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;

            Event.RegisterCommand(SetJunimoColorEventCommand, this.SetJunimoColor);
        }

        private void Input_ButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button != SButton.O)
            {
                return;
            }

            // Testing the grape celebration

            foreach (var portal in new GameMap(Game1.currentLocation).GetPortals())
            {
                portal.shakeTimer = 3000;
                var poofAction = () => this.MakePoof(new Vector2(portal.TileLocation.X, portal.TileLocation.Y));
                DelayedAction.functionAfterDelay(poofAction, 500);
                DelayedAction.functionAfterDelay(poofAction, 1500);
                DelayedAction.functionAfterDelay(poofAction, 2500);
            }

            for (int i = 0; i < 15; i++)
            {
                int delay = Math.Max(Game1.random.Next(11), Game1.random.Next(11))
                          + Math.Max(Game1.random.Next(11), Game1.random.Next(11))
                          + Game1.random.Next(11);
                // delay is a number between 0 and 30 on a bell-curve that's pushed to the right of average.
                DelayedAction.functionAfterDelay(() => Game1.playSound("junimoMeep1"), delay * 100);
            }

            //Game1.playSound("junimoMeep1");
            //DelayedAction.functionAfterDelay(() => { }, 100 /*milliseconds*/);
            //Game1.playSound("yoba");
        }

        private void MakePoof(Vector2 tile)
        {
            var colors = new Color[4][] {
                [Color.SpringGreen, Color.LawnGreen, Color.LightGreen],
                [Color.DarkGreen, Color.ForestGreen, Color.Green],
                [Color.Orange, Color.DarkRed, Color.Red],
                [Color.White, Color.LightBlue, Color.LightGray]
            };

            var colorChoice = Game1.currentLocation.IsOutdoors ? colors[Game1.seasonIndex] : colors[0];

            Vector2 landingPos = tile * 64f;
            landingPos.Y -= 64;
            landingPos.X -= 16;
            float scale = 0.15f;
            TemporaryAnimatedSprite? dustTas = new(
                textureName: Game1.animationsName,
                sourceRect: new Rectangle(0, 256, 64, 64),
                animationInterval: 120f,
                animationLength: 8,
                numberOfLoops: 0,
                position: landingPos,
                flicker: false,
                flipped: Game1.random.NextDouble() < 0.5,
                layerDepth: (landingPos.Y+150) / 10000f, // SDV uses a base value of y/10k for layerDepth. +150 is a fudge factor that seems to be above the hut, but below any trees or what have you in front of the hut.
                alphaFade: 0.01f,
                color: colorChoice[Game1.random.Next(colorChoice.Length)],
                scale: Game1.pixelZoom * scale,
                scaleChange: 0.02f,
                rotation: 0f,
                rotationChange: 0f);

            Game1.Multiplayer.broadcastSprites(Game1.currentLocation, dustTas);
        }


        public bool IsRunningSve => this.Helper.ModRegistry.IsLoaded("FlashShifter.SVECode");

        public IExtraMachineConfigApi? ExtraMachineConfigApi => this.Helper.ModRegistry.GetApi<IExtraMachineConfigApi>("selph.ExtraMachineConfig");

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

                var property = typeof(Junimo).GetField(I("color"), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
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



        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(BigCraftablesSpritesPseudoPath))
            {
                e.LoadFromModFile<Texture2D>("assets/Sprites.png", AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(OneTileSpritesPseudoPath))
            {
                e.LoadFromModFile<Texture2D>("assets/1x1_Sprites.png", AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.StartsWith("Characters/Dialogue/"))
            {
                e.Edit(editor =>
                {
                    ConversationKeys.EditAssets(e.NameWithoutLocale, editor.AsDictionary<string, string>().Data);
                });
            }
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
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

        private static readonly Regex unQualifier = new Regex(@"^\([a-z]\)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        internal static void AddQuestItem(IDictionary<string, ObjectData> objects, string qiid, string displayName, string description, int spriteIndex)
        {
            string itemId = unQualifier.Replace(qiid, "");  // I don't think there is a more stylish way to unqualify a name
            objects[itemId] = new()
            {
                Name = itemId,
                DisplayName = displayName,
                Description = description,
                Type = I("Quest"),
                Category = -999,
                Price = 0,
                Texture = ModEntry.OneTileSpritesPseudoPath,
                SpriteIndex = spriteIndex,
                ContextTags = new() { I("not_giftable"), I("not_placeable"), I("prevent_loss_on_death") },
            };
        }
    }
}
