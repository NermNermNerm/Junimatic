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

#if false
        private class JunimoStatusMenu : StorageContainer
        {
            private readonly JunimoStatus owner;

            public JunimoStatusMenu(JunimoStatus owner)
                : base(FluffUpList(Game1.IsMasterGame ? owner.LoadShinyThings() : new List<Item>()),
                      NumShinySlots,
                      1, // number of rows of shiny stuff inventory
                      (i, p, o, c, ir) => ((JunimoStatusMenu)c).OnChangingShinyItems(i, p, o, c, ir),
                      Utility.highlightSmallObjects)
            {
                this.owner = owner;

                // This code controls where it goes - because reasons, it's not actually quite correct
                //int num = 64 * (capacity / rows);
                //ItemsToGrabMenu = new InventoryMenu(Game1.uiViewport.Width / 2 - num / 2, yPositionOnScreen + 64, playerInventory: false, inventory, null, capacity, rows);
                this.ItemsToGrabMenu.xPositionOnScreen = this.inventory.xPositionOnScreen;
                this.ItemsToGrabMenu.yPositionOnScreen = this.yPositionOnScreen + 192;
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

            public override void draw(SpriteBatch b)
            {
                // Mostly copied from StorageContainer
                b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);

                // ..except for this call to MenuWithInventory.draw
                // base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);
                if (this.trashCan != null)
                {
                    this.trashCan.draw(b);
                    b.Draw(Game1.mouseCursors, new Vector2(this.trashCan.bounds.X + 60, this.trashCan.bounds.Y + 40), new Rectangle(564 + Game1.player.trashCanLevel * 18, 129, 18, 10), Color.White, this.trashCanLidRotation, new Vector2(16f, 10f), 4f, SpriteEffects.None, 0.86f);
                }
                // Draw the box around the player's inventory
                Game1.drawDialogueBox(
                    x: this.xPositionOnScreen - IClickableMenu.borderWidth / 2,
                    y: this.yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 64,
                    width: this.width,
                    height: this.height - (IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 192),
                    speaker: false,
                    drawOnlyBox: true);
                this.okButton?.draw(b);
                this.inventory.draw(b, red: -1, green: -1, blue: -1); // Draw the actual inventory
                // end MenuWithInventory.draw


                Game1.drawDialogueBox(
                    x: this.ItemsToGrabMenu.xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder,
                    y: this.ItemsToGrabMenu.yPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder,
                    width: this.ItemsToGrabMenu.width + IClickableMenu.borderWidth * 2 + IClickableMenu.spaceToClearSideBorder * 2,
                    height: this.ItemsToGrabMenu.height + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth * 2,
                    speaker: false,
                    drawOnlyBox: true);
                this.ItemsToGrabMenu.draw(b);
                // this.poof?.draw(b, localPosition: true);
                if (!this.hoverText.Equals(""))
                {
                    drawHoverText(b, this.hoverText, Game1.smallFont);
                }

                base.heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16), 1f);
                this.drawMouse(b);
                string text = this.ItemsToGrabMenu.descriptionTitle;
                if (text != null && text.Length > 1)
                {
                    drawHoverText(b, this.ItemsToGrabMenu.descriptionTitle, Game1.smallFont, 32 + ((base.heldItem != null) ? 16 : (-21)), 32 + ((base.heldItem != null) ? 16 : (-21)));
                }
            }
        }
#endif
    }
}
