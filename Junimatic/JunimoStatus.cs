using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Menus;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class JunimoStatus : ModLet
    {
        public const int NumShinySlots = 10;

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
            if (e.Button == SButton.MouseRight)
            {
                Vector2 tile = this.Helper.Input.GetCursorPosition().GrabTile;
                var obj = Game1.player?.currentLocation?.getObjectAtTile((int)tile.X, (int)tile.Y);
                if (obj?.ItemId == UnlockPortal.JunimoPortal)
                {
                    this.LogInfoOnce($"Showing the dialog");
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

        private List<Item> LoadShinyThings()
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

        private void OnPlayerChangedShinyList(IEnumerable<Item> shinyThings)
        {
            string serialized = this.SerializeShinyList(shinyThings);
            this.SendState(serialized);
            if (Game1.IsMasterGame)
            {
                this.SaveRawShinyList(serialized);
            }
        }

        private class JunimoStatusMenu : StorageContainer
        {
            private readonly JunimoStatus owner;

            public JunimoStatusMenu(JunimoStatus owner)
                : base(FluffUpList(Game1.IsMasterGame ? owner.LoadShinyThings() : new List<Item>()),
                      NumShinySlots,
                      2,
                      (i, p, o, c, ir) => ((JunimoStatusMenu)c).OnChangingShinyItems(i, p, o, c, ir),
                      Utility.highlightSmallObjects)
            {
                this.owner = owner;
            }

            public void Reset(List<Item> newItems)
            {
                for (int i = 0; i < NumShinySlots; ++i)
                {
                    this.ItemsToGrabMenu.actualInventory[i] = i < newItems.Count ? newItems[i] : null;
                }
            }

            private static List<Item?> FluffUpList(List<Item> plainList)
            {
                List<Item?> result = new(plainList);
                while (result.Count < NumShinySlots)
                {
                    result.Add(null);
                }
                return result;
            }


            private bool OnChangingShinyItems(Item? i, int position, Item? old, StorageContainer container, bool onRemoval)
            {
                // The arguments to this thing are pretty much impossible to name well.
                //
                // If the user has an item that they've selected:
                //  'onRemoval' is false, 'i' is the incoming item and 'old' is null.
                //
                // If nothing is selected and the user clicks on an item in the box:
                //  'onRemoval' is true, 'i' is the item in the chest and 'old' is null.
                //
                // If the user has something selected and clicks on an item in the box, two events are generated:
                //  #1 - 'onRemoval' is true, 'i' is the item in the chest and 'old' is the item the user has selected.
                //  #2 - 'onRemoval' is false, 'i' is the incoming item, 'old' is the item in the chest.
                //
                // Apparently, 'i' is never null.  I'm leaving guards against it in-place.
                //
                // The return value indicates whether a sound should be played.
                //
                // container.heldItem is the item that is currently being "dragged" in the dialog.

                this.owner.LogInfo($"i={(i is null ? "null" : IF($"{i.Name}:{i.Quality}#{i.Stack}"))} old={(old is null ? "null" : IF($"{old.Name}:{old.Quality}#{old.Stack}"))} onRemoval={onRemoval}");
                try
                {
                    if (!onRemoval && i is not null)
                    {
                        if (i.Stack > 1 || (i.Stack == 1 && old != null && old.Stack == 1 && i.canStackWith(old)))
                        {
                            // This case covers the first event of the swap operation and the add operations where we have more than one item held

                            if (old != null && old.canStackWith(i)) // something's held and it's the same kind of thing as what's in the chest
                            {
                                // This does nothing - the items in the actual inventory are always stack size 1.
                                container.ItemsToGrabMenu.actualInventory[position].Stack = 1;

                                // This does nothing - when onRemove is false, 'old' is the item in the chest, so container.heldItem already == old
                                container.heldItem = old;
                                return false;
                            }

                            if (old != null)
                            {
                                // swaps what's in the hand and what's in the chest.
                                Utility.addItemToInventory(old, position, container.ItemsToGrabMenu.actualInventory);
                                container.heldItem = i;
                                return false;
                            }

                            // This is the case where you're adding to an empty slot
                            int allButOne = i.Stack - 1; // The stack size after putting it in the container - can be zero
                            Item reject = i.getOne(); // 'reject' is the part of the incoming stack that won't fit because we only take one item.
                            reject.Stack = allButOne; //   <- see that it's the right size
                            container.heldItem = reject; //  And now that's what's in-hand
                            i.Stack = 1; // 
                        }
                    }
                    else if (old != null && old.Stack > 1 && !old.Equals(i))
                    {
                        return false;
                    }
                    return true;
                }
                finally
                {
                    this.owner.OnPlayerChangedShinyList(container.ItemsToGrabMenu.actualInventory.Where(i => i is not null));
                }
            }
        }
    }
}
