using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using StardewValley;
using StardewValley.BellsAndWhistles;
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

        public static string nullstr = I("null");
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            this.owner.LogInfo($"In receiveLeftClick, heldItem={base.heldItem?.Name ?? nullstr}");
            Item? item = base.heldItem;
            int num = item?.Stack ?? (-1);
            if (base.isWithinBounds(x, y))
            {
                this.owner.LogInfo($"In base.isWithinBounds case");
                base.receiveLeftClick(x, y, playSound: false);
            }

            if (this.ItemsToGrabMenu.isWithinBounds(x, y))
            {
                base.heldItem = this.ItemsToGrabMenu.leftClick(x, y, base.heldItem, playSound: false);
                this.owner.LogInfo($"  itemToGrabMenu.leftClick changed heldItem to: {base.heldItem?.Name ?? nullstr}");
                if ((base.heldItem != null && item == null) || (base.heldItem != null && item != null && !base.heldItem.Equals(item)))
                {
                    this.owner.OnPlayerChangedShinyList(this.ItemsToGrabMenu.actualInventory.Where(i => i is not null));
                    Game1.playSound("dwop");
                }

                if ((base.heldItem == null && item != null) || (base.heldItem != null && item != null && !base.heldItem.Equals(item)))
                {
                    Item? one = base.heldItem;
                    if (base.heldItem == null && this.ItemsToGrabMenu.getItemAt(x, y) != null && num < this.ItemsToGrabMenu.getItemAt(x, y).Stack)
                    {
                        one = item.getOne();
                        one.Stack = num;
                    }

                    this.owner.OnPlayerChangedShinyList(this.ItemsToGrabMenu.actualInventory.Where(i => i is not null));
                    Game1.playSound("Ship");
                }
                else if (Game1.oldKBState.IsKeyDown(Keys.LeftShift) && Game1.player.addItemToInventoryBool(base.heldItem))
                {
                    base.heldItem = null;
                    this.owner.OnPlayerChangedShinyList(this.ItemsToGrabMenu.actualInventory.Where(i => i is not null));
                    Game1.playSound("coin");
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

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            this.owner.LogInfo($"In receiveRightClick, heldItem={base.heldItem?.Name ?? nullstr}");
            base.receiveRightClick(x, y, playSound);
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

            string title = L("= $ Shiny things $ =");
            int titleWidth = SpriteText.getWidthOfString(title);
            int titleHeight = SpriteText.getHeightOfString(title);
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
            SpriteText.drawString(b, title, (int)titlePosition.X, (int)titlePosition.Y);

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
