using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Menus;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework.Graphics;
using StardewValley.TerrainFeatures;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;
using System;

namespace NermNermNerm.Junimatic
{
    public class JunimoStatus : ModLet
    {
        public const int NumShinySlots = 12;

        public const string UpdateListMessageId = "Junimatic.ShinyList";
        public const string AskForListMessageId = "Junimatic.PleaseSendList";
        private const string ShinyThingsCustomDataName = "Junimatic.ShinyThings";


        public override void Entry(ModEntry mod)
        {
            base.Entry(mod);

            this.Helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;
            this.Helper.Events.Multiplayer.ModMessageReceived += this.Multiplayer_ModMessageReceived;
        }

        private void SendState(string serializedShinyList)
        {
            this.Helper.Multiplayer.SendMessage(serializedShinyList, UpdateListMessageId, [this.Mod.ModManifest.UniqueID]);
        }

        private void SendAskForList()
        {
            this.Helper.Multiplayer.SendMessage("", AskForListMessageId, [this.Mod.ModManifest.UniqueID], [Game1.MasterPlayer.UniqueMultiplayerID]);
        }

        private void Multiplayer_ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            switch (e.Type)
            {
                case UpdateListMessageId:
                    string serializedShinyList = e.ReadAs<string>();
                    if (Game1.activeClickableMenu is JunimoStatusMenu menu)
                    {
                        menu.Reset(this.DeserializeShinyList(serializedShinyList));
                    }
                    if (Game1.IsMasterGame)
                    {
                        this.SaveRawShinyList(serializedShinyList);
                    }
                    break;
                case AskForListMessageId:
                    if (Game1.IsMasterGame) // This should always be true since we only send this message to the master game.
                    {
                        this.SendState(this.GetRawShinyList());
                    }
                    break;
                default:
                    break;
            };
        }

        private void Input_ButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.MouseRight && Game1.activeClickableMenu is null)
            {
                Vector2 tile = this.Helper.Input.GetCursorPosition().GrabTile;
                var obj = Game1.player?.currentLocation?.getObjectAtTile((int)tile.X, (int)tile.Y);
                if (obj?.ItemId == UnlockPortal.JunimoPortal)
                {
                    this.ShowDialog();
                }
            }
        }

        private void ShowDialog()
        {
            Game1.activeClickableMenu = new JunimoStatusMenu(this);
            if (!Game1.IsMasterGame)
            {
                this.SendAskForList();
            }
        }

        private string GetRawShinyList() => Game1.CustomData.TryGetValue(ShinyThingsCustomDataName, out string? serialized) ? serialized : "";

        private void SaveRawShinyList(string? serialized)
        {
            if (string.IsNullOrEmpty(serialized))
            {
                Game1.CustomData.Remove(ShinyThingsCustomDataName);
            }
            else
            {
                Game1.CustomData[ShinyThingsCustomDataName] = serialized;
            }
        }

        public List<Item> LoadShinyThings()
        {
            Game1.CustomData.TryGetValue("Junimatic.ShinyThings", out string? serialized);
            return this.DeserializeShinyList(serialized ?? "");
        }

        private List<Item> DeserializeShinyList(string serialized)
        {
            var itemPattern = new Regex(@"^(?<qiid>.*):(?<quality>\d+)$");
            List<Item> result = new();
            foreach (string s in serialized.Split(",", System.StringSplitOptions.RemoveEmptyEntries))
            {
                var match = itemPattern.Match(s);
                Item? item = null;
                if (match.Success)
                {
                    int quality = int.Parse(match.Groups[I("quality")].Value);
                    string qiid = match.Groups[I("qiid")].Value;
                    item = ItemRegistry.Create(qiid, 1, quality, true);
                }
                if (item is null)
                {
                    this.LogWarning($"One of the Junimatic Shiny Things was either not stored correctly or is an item that belongs to a mod that has been removed: '{s}'");
                }
                else
                {
                    result.Add(item);
                }
            }
            return result;
        }

        private string SerializeShinyList(IEnumerable<Item> shinyThings)
        {
            StringBuilder serialized = new StringBuilder();
            foreach (var item in shinyThings)
            {
                if (serialized.Length > 0)
                {
                    serialized.Append(",");
                }
                serialized.Append($"{item.QualifiedItemId}:{item.Quality}");
            }
            return serialized.ToString();
        }

        public void OnPlayerChangedShinyList(IEnumerable<Item> shinyThings)
        {
            string serialized = this.SerializeShinyList(shinyThings);
            this.SendState(serialized);
            if (Game1.IsMasterGame)
            {
                this.SaveRawShinyList(serialized);
            }
        }

        internal Func<Item,bool> GetIsShinyTest()
        {
            // The generated function gets called possibly a bunch of times, so the goal is to make it as efficient as we can make it.
            var hashSets = Enumerable.Range(0, 5).Select(_ => new HashSet<string>()).ToArray();
            foreach (var item in this.LoadShinyThings())
            {
                for (int i = item.Quality; i < 5; ++i)
                {
                    hashSets[i].Add(item.QualifiedItemId);
                }
            }

            return item => item is not null && hashSets[item.Quality].Contains(item.QualifiedItemId);
        }
    }
}
