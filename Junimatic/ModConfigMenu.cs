using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;

namespace NermNermNerm.Junimatic
{
    public class ModConfigMenu
    {
        private ModEntry mod = null!;

        public void Entry(ModEntry mod)
        {
            this.mod = mod;

            mod.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            IManifest ModManifest = this.mod.ModManifest;
            ModConfig Config = ModEntry.Config;

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod configs
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.mod.Helper.WriteConfig(Config)
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Unlocks"
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Unlock Junimo Portal",
                getValue: () => Config.UnlockPortal,
                setValue: value => Config.UnlockPortal = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Unlock Animal Junimos",
                getValue: () => Config.UnlockAnimal,
                setValue: value => Config.UnlockAnimal = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Unlock Crop Machine Junimos",
                getValue: () => Config.UnlockCropMachines,
                setValue: value => Config.UnlockCropMachines = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Unlock Fishing Junimos",
                getValue: () => Config.UnlockFishing,
                setValue: value => Config.UnlockFishing = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Unlock Tree Junimos",
                getValue: () => Config.UnlockForest,
                setValue: value => Config.UnlockForest = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Unlock Mining Junimos",
                getValue: () => Config.UnlockForest,
                setValue: value => Config.UnlockForest = value
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Locations"
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Allow Junimos Outside of Farm",
                getValue: () => Config.AllowAnyLocation,
                setValue: value => Config.AllowAnyLocation = value
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Miscellaneous"
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Allow Bait Maker in Networks",
                getValue: () => Config.AllowBaitMaker,
                setValue: value => Config.AllowBaitMaker = value
            );
        }
    }
}
