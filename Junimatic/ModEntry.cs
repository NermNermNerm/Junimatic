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

        public bool isCreated = true;

        public ModEntry()
        {
        }

        public override void Entry(IModHelper helper)
        {
            this.Harmony = new Harmony(this.ModManifest.UniqueID);

            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.GameLoop_OneSecondUpdateTicked;
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
