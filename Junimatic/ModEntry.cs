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

        public readonly UnlockPortal UnlockPortalQuest = new UnlockPortal();
        public readonly UnlockCropMachines CropMachineHelperQuest = new UnlockCropMachines();
        public readonly UnlockMiner UnlockMiner = new UnlockMiner();
        public readonly UnlockAnimal UnlockAnimal = new UnlockAnimal();
        public readonly UnlockForest UnlockForest = new UnlockForest();
        public readonly UnlockFishing UnlockFishing = new UnlockFishing();
        public readonly UnlockPots UnlockPots = new UnlockPots();
        public readonly JunimoStatus JunimoStatusDialog = new JunimoStatus();

        public readonly Powers Powers = new Powers();

        private readonly WorkFinder workFinder = new WorkFinder();
        private readonly Childsplay childsplay = new Childsplay();
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
            this.Powers.Entry(this);
            this.childsplay.Entry(this);

            this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;

            Event.RegisterCommand(SetJunimoColorEventCommand, this.SetJunimoColor);
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
