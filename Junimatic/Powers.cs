using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpecialPowerUtilities;
using StardewModdingAPI.Events;
using StardewValley.Characters;
using StardewValley.GameData.Powers;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class Powers
    {
        private ModEntry mod = null!; // Set in Entry

        public Powers()
        { }

        public void Entry(ModEntry mod)
        {
            this.mod = mod;
            mod.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var spuApi = this.mod.Helper.ModRegistry.GetApi<ISpecialPowerAPI>("Spiderbuttons.SpecialPowerUtilities");
            if (spuApi is not null)
            {
                if (spuApi.RegisterPowerCategory(this.mod.ModManifest.UniqueID, () => L("Junimatic"), ModEntry.OneTileSpritesPseudoPath, new Point(48,0), new Point(16,16)))
                {
                    this.mod.LogTrace($"Power category successfully registered with SpecialPowerUtilities.");
                }
                else
                {
                    this.mod.LogTrace($"The SpecialPowerUtilities mod is not installed.");
                }
            }
        }
    }
}
