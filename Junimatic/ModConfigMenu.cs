using StardewModdingAPI;
using StardewModdingAPI.Events;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

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

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
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

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => L("Junimos work anywhere"),
                getValue: () => Config.AllowAllLocations,
                setValue: value => Config.AllowAllLocations = value,
                tooltip: () => L("Normally Junimos only work where there are no villagers or monsters, as they are shy.  Turning this on allows Junimo portals to work anywhere.")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => L("No Questing"),
                getValue: () => Config.EnableWithoutQuests,
                setValue: value => Config.EnableWithoutQuests = value,
                tooltip: () => L("Normally quests are required to unlock the Junimos; setting this just enables all the junimos.  To get started, craft a 'Junimo Portal' and place it down.")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => L("Allow Shipping"),
                getValue: () => Config.AllowShippingArtisan,
                setValue: value => Config.AllowShippingArtisan = value,
                tooltip: () => L("If this is checked, Junimos will take artisan goods to mini-shipping bins.")
            );
        }
    }
}
