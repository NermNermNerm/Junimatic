using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using StardewValley;
using StardewValley.Menus;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;


namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   Implements the UI that pops up when you interact with a Junimo Portal.  This is the UI-interaction part,
    ///   the backing control is in the <see cref="JunimoStatus"/> modlet.
    /// </summary>
    /// <remarks>
    ///   This is an adaptation of the game's <see cref="StorageContainer"/> class.  That class, despite it's general-purpose-sounding
    ///   name is only used for the Grange Display at the Stardew Valley Fair.  I suspect it was originally built with the idea
    ///   of being used in more than one place, but when it was tried to use in that way, it was found that it was chok-a-blok
    ///   full of hard-coding for that use and thus I'm probably not the first one to copy it.
    /// </remarks>
    public class JunimoStatusMenu : MenuWithInventory
    {
        private TemporaryAnimatedSprite? poof = null;
        private InventoryMenu ItemsToGrabMenu;
        private readonly JunimoStatus owner;

        public JunimoStatusMenu(JunimoStatus owner)
            : base(Utility.highlightSmallObjects, okButton: true, trashCan: true)
        {
            this.owner = owner;

            List<Item?> shinyItems = Game1.IsMasterGame ? new List<Item?>(owner.LoadShinyThings()) : new List<Item?>();
            while (shinyItems.Count < JunimoStatus.NumShinySlots)
            {
                shinyItems.Add(null);
            }

            this.ItemsToGrabMenu = new InventoryMenu(
                this.inventory.xPositionOnScreen,
                this.yPositionOnScreen + 192,
                playerInventory: false,
                shinyItems, null, JunimoStatus.NumShinySlots, 1);
            for (int i = 0; i < this.ItemsToGrabMenu.actualInventory.Count; i++)
            {
                if (i >= this.ItemsToGrabMenu.actualInventory.Count - this.ItemsToGrabMenu.capacity)
                {
                    this.ItemsToGrabMenu.inventory[i].downNeighborID = i + 53910;
                }
            }

            for (int j = 0; j < base.inventory.inventory.Count; j++)
            {
                base.inventory.inventory[j].myID = j + 53910;
                if (base.inventory.inventory[j].downNeighborID != -1)
                {
                    base.inventory.inventory[j].downNeighborID += 53910;
                }

                if (base.inventory.inventory[j].rightNeighborID != -1)
                {
                    base.inventory.inventory[j].rightNeighborID += 53910;
                }

                if (base.inventory.inventory[j].leftNeighborID != -1)
                {
                    base.inventory.inventory[j].leftNeighborID += 53910;
                }

                if (base.inventory.inventory[j].upNeighborID != -1)
                {
                    base.inventory.inventory[j].upNeighborID += 53910;
                }

                if (j < 12)
                {
                    base.inventory.inventory[j].upNeighborID = this.ItemsToGrabMenu.actualInventory.Count - this.ItemsToGrabMenu.capacity / this.ItemsToGrabMenu.rows;
                }
            }

            this.dropItemInvisibleButton.myID = -500;
            this.ItemsToGrabMenu.dropItemInvisibleButton.myID = -500;
            if (Game1.options.SnappyMenus)
            {
                this.populateClickableComponentList();
                this.setCurrentlySnappedComponentTo(53910);
                this.snapCursorToCurrentSnappedComponent();
            }
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            this.ItemsToGrabMenu = new InventoryMenu(
                this.inventory.xPositionOnScreen,
                this.yPositionOnScreen + 192,
                playerInventory: false,
                this.ItemsToGrabMenu.actualInventory,
                null, this.ItemsToGrabMenu.capacity, this.ItemsToGrabMenu.rows);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            Item item = base.heldItem;
            int num = item?.Stack ?? (-1);
            if (base.isWithinBounds(x, y))
            {
                base.receiveLeftClick(x, y, playSound: false);
            }

            bool flag = true;
            if (this.ItemsToGrabMenu.isWithinBounds(x, y))
            {
                base.heldItem = this.ItemsToGrabMenu.leftClick(x, y, base.heldItem, playSound: false);
                if ((base.heldItem != null && item == null) || (base.heldItem != null && item != null && !base.heldItem.Equals(item)))
                {
                    flag = this.itemChangeBehavior(base.heldItem, this.ItemsToGrabMenu.getInventoryPositionOfClick(x, y), item, onRemoval: true);

                    if (flag)
                    {
                        Game1.playSound("dwop");
                    }
                }

                if ((base.heldItem == null && item != null) || (base.heldItem != null && item != null && !base.heldItem.Equals(item)))
                {
                    Item? one = base.heldItem;
                    if (base.heldItem == null && this.ItemsToGrabMenu.getItemAt(x, y) != null && num < this.ItemsToGrabMenu.getItemAt(x, y).Stack)
                    {
                        one = item.getOne();
                        one.Stack = num;
                    }

                    flag = this.itemChangeBehavior(item, this.ItemsToGrabMenu.getInventoryPositionOfClick(x, y), one, onRemoval: true);

                    if (flag)
                    {
                        Game1.playSound("Ship");
                    }
                }

                Item? item2 = base.heldItem;
                if (item2 != null && item2.IsRecipe)
                {
                    string key = item2.Name.Substring(0, item2.Name.IndexOf("Recipe") - 1);
                    try
                    {
                        if (item2.Category == -7)
                        {
                            Game1.player.cookingRecipes.Add(key, 0);
                        }
                        else
                        {
                            Game1.player.craftingRecipes.Add(key, 0);
                        }

                        this.poof = new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 320, 64, 64), 50f, 8, 0, new Vector2(x - x % 64 + 16, y - y % 64 + 16), flicker: false, flipped: false);
                        Game1.playSound("newRecipe");
                    }
                    catch (Exception)
                    {
                    }

                    base.heldItem = null;
                }
                else if (Game1.oldKBState.IsKeyDown(Keys.LeftShift) && Game1.player.addItemToInventoryBool(base.heldItem))
                {
                    base.heldItem = null;
                    flag = this.itemChangeBehavior(base.heldItem, this.ItemsToGrabMenu.getInventoryPositionOfClick(x, y), item, onRemoval: true);

                    if (flag)
                    {
                        Game1.playSound("coin");
                    }
                }
            }

            if (this.okButton.containsPoint(x, y) && this.readyToClose())
            {
                Game1.playSound("bigDeSelect");
                Game1.exitActiveMenu();
            }

            if (this.trashCan.containsPoint(x, y) && base.heldItem != null && base.heldItem.canBeTrashed())
            {
                Utility.trashItem(base.heldItem);
                base.heldItem = null;
            }
        }


        private bool itemChangeBehavior(Item? i, int position, Item? old, bool onRemoval)
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
                            this.ItemsToGrabMenu.actualInventory[position].Stack = 1;

                            // This does nothing - when onRemove is false, 'old' is the item in the chest, so container.heldItem already == old
                            this.heldItem = old;
                            return false;
                        }

                        if (old != null)
                        {
                            // swaps what's in the hand and what's in the chest.
                            Utility.addItemToInventory(old, position, this.ItemsToGrabMenu.actualInventory);
                            this.heldItem = i;
                            return false;
                        }

                        // This is the case where you're adding to an empty slot
                        int allButOne = i.Stack - 1; // The stack size after putting it in the container - can be zero
                        Item reject = i.getOne(); // 'reject' is the part of the incoming stack that won't fit because we only take one item.
                        reject.Stack = allButOne; //   <- see that it's the right size
                        this.heldItem = reject; //  And now that's what's in-hand
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
                this.owner.OnPlayerChangedShinyList(this.ItemsToGrabMenu.actualInventory.Where(i => i is not null));
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            int num = ((base.heldItem != null) ? base.heldItem.Stack : 0);
            Item? item = base.heldItem;
            if (base.isWithinBounds(x, y))
            {
                base.receiveRightClick(x, y, playSound: true);
            }

            if (!this.ItemsToGrabMenu.isWithinBounds(x, y))
            {
                return;
            }

            base.heldItem = this.ItemsToGrabMenu.rightClick(x, y, base.heldItem, playSound: false);
            if ((base.heldItem != null && item == null) || (base.heldItem != null && item != null && !base.heldItem.Equals(item)) || (base.heldItem != null && item != null && base.heldItem.Equals(item) && base.heldItem.Stack != num))
            {
                this.itemChangeBehavior(base.heldItem, this.ItemsToGrabMenu.getInventoryPositionOfClick(x, y), item, onRemoval: true);
                Game1.playSound("dwop");
            }

            if ((base.heldItem == null && item != null) || (base.heldItem != null && item != null && !base.heldItem.Equals(item)))
            {
                this.itemChangeBehavior(item, this.ItemsToGrabMenu.getInventoryPositionOfClick(x, y), base.heldItem, onRemoval: false);
                Game1.playSound("Ship");
            }

            Item? item2 = base.heldItem;
            if (Game1.oldKBState.IsKeyDown(Keys.LeftShift) && Game1.player.addItemToInventoryBool(base.heldItem))
            {
                base.heldItem = null;
                Game1.playSound("coin");
                this.itemChangeBehavior(base.heldItem, this.ItemsToGrabMenu.getInventoryPositionOfClick(x, y), item, onRemoval: true);
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);
            if (this.poof != null && this.poof.update(time))
            {
                this.poof = null;
            }
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            this.ItemsToGrabMenu.hover(x, y, base.heldItem);
        }

        public void Reset(List<Item> newItems)
        {
            for (int i = 0; i < JunimoStatus.NumShinySlots; ++i)
            {
                this.ItemsToGrabMenu.actualInventory[i] = i < newItems.Count ? newItems[i] : null;
            }
        }



        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
            base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);

            string title = L("* Shiny Things *");
            var titleSize = Game1.dialogueFont.MeasureString(title);
            int titleWidth = (int)titleSize.X;
            int titleHeight = (int)titleSize.Y;
            int titleMargin = titleHeight / 4;

            // This is the box that contains the 'Shiny things' title and list of items.
            Game1.drawDialogueBox(
                this.ItemsToGrabMenu.xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder,
                this.ItemsToGrabMenu.yPositionOnScreen - (IClickableMenu.spaceToClearTopBorder + /*IClickableMenu.borderWidth*/ + titleHeight + titleMargin * 2),
                this.ItemsToGrabMenu.width + IClickableMenu.borderWidth * 2 + IClickableMenu.spaceToClearSideBorder * 2,
                this.ItemsToGrabMenu.height + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + titleHeight + titleMargin*2,
                speaker: false, drawOnlyBox: true);

            var titlePosition = new Vector2(
                this.ItemsToGrabMenu.xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder
                 +((this.ItemsToGrabMenu.width + IClickableMenu.borderWidth * 2 + IClickableMenu.spaceToClearSideBorder * 2) - titleWidth) / 2,
                this.ItemsToGrabMenu.yPositionOnScreen - titleMargin - titleHeight
                );
            b.DrawString(Game1.dialogueFont, title, titlePosition, Game1.textColor);

            int mouseX = Game1.getOldMouseX();
            int mouseY = Game1.getOldMouseY();

            this.ItemsToGrabMenu.draw(b);
            this.poof?.draw(b, localPosition: true);
            if (!this.hoverText.Equals(""))
            {
                IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont);
            }

            base.heldItem?.drawInMenu(b, new Vector2(mouseX + 16, mouseY + 16), 1f);
            this.drawMouse(b);
            string text = this.ItemsToGrabMenu.descriptionTitle;
            if (text != null && text.Length > 1)
            {
                IClickableMenu.drawHoverText(b, this.ItemsToGrabMenu.descriptionTitle, Game1.smallFont, 32 + ((base.heldItem != null) ? 16 : (-21)), 32 + ((base.heldItem != null) ? 16 : (-21)));
            }

            if (mouseX >= titlePosition.X && mouseX <= titlePosition.X + titleWidth
                && mouseY >= titlePosition.Y && mouseY <= titlePosition.Y + titleHeight)
            {
                IClickableMenu.drawHoverText(b, L("Junimos won't put items with equal or better quality\nthan these items into machines or shipping bins."), Game1.smallFont);
            }
        }
    }
}
