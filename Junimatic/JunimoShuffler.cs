using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.Pathfinding;

namespace NermNermNerm.Junimatic
{
    public class JunimoShuffler : NPC, ISimpleLog
    {
        private float alpha = 1f;
        private float alphaChange;
        private Vector2 motion = Vector2.Zero;
        private new Rectangle nextPosition;
        private readonly NetColor color = new NetColor();
        private bool destroy;
        private readonly NetEvent1Field<int, NetInt> netAnimationEvent = new NetEvent1Field<int, NetInt>();
        private readonly Inventory carrying = new Inventory();
        private JunimoAssignment assignment;
        private readonly WorkFinder workFinder;

        public JunimoShuffler(JunimoAssignment assignment, WorkFinder workFinder)
            : base(new AnimatedSprite("Characters\\Junimo", 0, 16, 16), assignment.origin.ToVector2()*64, 2, "Junimo")
        {
            this.color.Value = assignment.projectType switch { JunimoType.MiningProcessing => Color.Tan, _ => Color.Aqua };
            this.currentLocation = assignment.hut.Location;
            this.nextPosition = this.GetBoundingBox();
            this.Breather = false;
            this.speed = 3;
            this.forceUpdateTimer = 9999;
            this.ignoreMovementAnimation = true;
            this.farmerPassesThrough = true;
            this.Scale = 0.75f;
            this.willDestroyObjectsUnderfoot = false;
            this.collidesWithOtherCharacters.Value = false;

            this.assignment = assignment;
            this.controller = new PathFindController(this, assignment.hut.Location, assignment.source.AccessPoint, 0, this.junimoReachedSource);
            this.alpha = 0;
            this.alphaChange = 0.05f;
            this.workFinder = workFinder;
            this.LogTrace($"Junimo created {this.assignment}");
        }

        private void junimoReachedSource(Character c, GameLocation l)
        {
            this.LogTrace($"Junimo reached its source {this.assignment}");

            if (this.carrying.Count != 0) throw new InvalidOperationException("inventory should be empty here");

            if (this.assignment.source is GameStorage chest)
            {
                if (this.assignment.itemsToRemoveFromChest is null) throw new InvalidOperationException("Should have some items to fetch");

                // Ensure the chest still contains what we need
                this.assignment.itemsToRemoveFromChest.Reverse(); // <- tidy

                if (!chest.TryFulfillShoppingList(this.assignment.itemsToRemoveFromChest!, this.carrying))
                {
                    this.LogTrace($"Assigned chest didn't have needed items");
                    this.junimoQuitsInDisgust();
                    return;
                }

                l.playSound("pickUpItem"); // Maybe 'openChest' instead?
            }
            else if (this.assignment.source is GameMachine machine && machine.HeldObject is not null)
            {
                this.carrying.Add(machine.RemoveHeldObject());
                l.playSound("dwop"); // <- might get overriden by the furnace sound...  but if it's not a furnace...
            }
            else
            {
                this.junimoQuitsInDisgust();
                return;
            }

            // Head to the target
            this.controller = new PathFindController(this, base.currentLocation, this.assignment.target.AccessPoint, 0, this.junimoReachedTarget);
        }

        private void junimoQuitsInDisgust()
        {
            this.LogTrace($"Junimo quits {this.assignment}");
            foreach (Item item in this.carrying)
            {
                this.TurnIntoDebris(item);
            }
            this.carrying.Clear();
            this.doEmote(12);

            this.controller = new PathFindController(this, base.currentLocation, this.assignment.origin, 0, this.junimoReachedHut);
        }

        private void TurnIntoDebris(Item item)
        {
            // TODO: Is this the right way to do this?
            base.currentLocation.debris.Add(new Debris(item, this.Tile*64));
        }

        private void junimoReachedTarget(Character c, GameLocation l)
        {
            this.LogTrace($"Junimo reached target {this.assignment}");

            if (this.assignment.target is GameStorage chest)
            {
                l.playSound("Ship");
                // TODO: Put what we're carrying into the chest or huck it overboard if we can't.
                if (!chest.TryStore(this.carrying))
                {
                    this.LogWarning($"Target {chest} did not have room for {this.carrying[0].Stack} {this.carrying[0].Name}");
                    this.junimoQuitsInDisgust();
                    return;
                }
            }
            else
            {
                bool isLoaded = ((GameMachine)this.assignment.target).FillMachineFromInventory(this.carrying);
                if (!isLoaded)
                {
                    this.LogTrace($"Junimo could not load {this.assignment} - perhaps a player loaded it?");
                    this.junimoQuitsInDisgust();
                    return;
                }

                l.playSound("dwop"); // <- might get overriden by the furnace sound...  but if it's not a furnace...
            }

            var newAssignment = (new WorkFinder()).FindProject(this.assignment.hut, this.assignment.projectType, this.Tile.ToPoint(), this.assignment.origin);
            if (newAssignment is not null)
            {
                this.assignment = newAssignment;
                this.controller = new PathFindController(this, this.assignment.hut.Location, this.assignment.source.AccessPoint, 0, this.junimoReachedSource);
            }
            else
            {
                this.controller = new PathFindController(this, base.currentLocation, this.assignment.origin, 0, this.junimoReachedHut);
            }
        }

        public void junimoReachedHut(Character c, GameLocation l)
        {
            this.LogTrace($"Junimo returned to its hut {this.assignment}");
            this.controller = null;
            this.motion.X = 0f;
            this.motion.Y = -1f;
            this.destroy = true;
        }

        protected override void initNetFields()
        {
            base.initNetFields();
            base.NetFields
                .AddField(this.color, "color")
                .AddField(this.netAnimationEvent, "netAnimationEvent");
            this.netAnimationEvent.onEvent += this.doAnimationEvent;
        }

        public override void ChooseAppearance(LocalizedContentManager content)
        {
            // This appears to be an intentional null-out of a lot of code in the base class.
        }

        protected virtual void doAnimationEvent(int animId)
        {
            switch (animId)
            {
                case 0:
                    this.Sprite.CurrentAnimation = null;
                    break;
                case 2:
                    this.Sprite.currentFrame = 0;
                    break;
                case 3:
                    this.Sprite.currentFrame = 1;
                    break;
                case 4:
                    this.Sprite.currentFrame = 2;
                    break;
                case 5:
                    this.Sprite.currentFrame = 44;
                    break;
                case 6:
                    this.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                    {
                        new FarmerSprite.AnimationFrame(12, 200),
                        new FarmerSprite.AnimationFrame(13, 200),
                        new FarmerSprite.AnimationFrame(14, 200),
                        new FarmerSprite.AnimationFrame(15, 200)
                    });
                    break;
                case 7:
                    this.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                    {
                        new FarmerSprite.AnimationFrame(44, 200),
                        new FarmerSprite.AnimationFrame(45, 200),
                        new FarmerSprite.AnimationFrame(46, 200),
                        new FarmerSprite.AnimationFrame(47, 200)
                    });
                    break;
                case 8:
                    this.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                    {
                        new FarmerSprite.AnimationFrame(28, 100),
                        new FarmerSprite.AnimationFrame(29, 100),
                        new FarmerSprite.AnimationFrame(30, 100),
                        new FarmerSprite.AnimationFrame(31, 100)
                    });
                    break;
                case 1:
                    break;
            }
        }

        public override bool shouldCollideWithBuildingLayer(GameLocation location)
        {
            return true;
        }

        public void setMoving(int xSpeed, int ySpeed)
        {
            this.motion.X = xSpeed;
            this.motion.Y = ySpeed;
        }

        public void setMoving(Vector2 motion)
        {
            this.motion = motion;
        }

        public override void Halt()
        {
            base.Halt();
            this.motion = Vector2.Zero;
        }

        public override bool canTalk() => false;

        public override void faceDirection(int direction) { }

        protected override void updateSlaveAnimation(GameTime time) { }

        public override void update(GameTime time, GameLocation location)
        {
            if (this.controller is null && Game1.IsMasterGame && !this.destroy)
            {
                this.workFinder.LogInfo("Junimo returned due to players leaving scene");
                if (this.carrying.Count > 0)
                {
                    this.junimoReachedTarget(this, location);
                }

                this.workFinder.JunimoWentHome(this.assignment.projectType);
                location.characters.Remove(this);
                return;
            }

            this.netAnimationEvent.Poll();
            base.update(time, location);

            this.forceUpdateTimer = 99999;
            if (this.eventActor)
            {
                return;
            }

            if (this.destroy)
            {
                this.alphaChange = -0.05f;
            }

            this.alpha += this.alphaChange;
            if (this.alpha > 1f)
            {
                this.alpha = 1f;
            }
            else if (this.alpha < 0f)
            {
                this.alpha = 0f;
                if (this.destroy && Game1.IsMasterGame)
                {
                    location.characters.Remove(this);
                    this.workFinder.JunimoWentHome(this.assignment.projectType);
                    return;
                }
            }

            if (Game1.IsMasterGame)
            {
                if (this.alpha > 0f && this.controller == null)
                {
                    if ((this.addedSpeed > 0f || base.speed > 3 || this.isCharging) && Game1.IsMasterGame)
                    {
                        this.destroy = true;
                    }

                    this.nextPosition = this.GetBoundingBox();
                    this.nextPosition.X += (int)this.motion.X;
                    bool flag = false;
                    if (!location.isCollidingPosition(this.nextPosition, Game1.viewport, this))
                    {
                        this.position.X += (int)this.motion.X;
                        flag = true;
                    }

                    this.nextPosition.X -= (int)this.motion.X;
                    this.nextPosition.Y += (int)this.motion.Y;
                    if (!location.isCollidingPosition(this.nextPosition, Game1.viewport, this))
                    {
                        this.position.Y += (int)this.motion.Y;
                        flag = true;
                    }

                    if (!this.motion.Equals(Vector2.Zero) && flag && Game1.random.NextDouble() < 0.005)
                    {
                        // TODO: Figure out if this matters - Game1.multiplayer is internal - there's probably a public variant somewhere.
                        //Game1.multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(Game1.random.Choose(10, 11), base.Position, color.Value)
                        //{
                        //    motion = this.motion / 4f,
                        //    alphaFade = 0.01f,
                        //    layerDepth = 0.8f,
                        //    scale = 0.75f,
                        //    alpha = 0.75f
                        //});
                    }

                    if (Game1.random.NextDouble() < 0.002)
                    {
                        switch (Game1.random.Next(6))
                        {
                            case 0:
                                this.netAnimationEvent.Fire(6);
                                break;
                            case 1:
                                this.netAnimationEvent.Fire(7);
                                break;
                            case 2:
                                this.netAnimationEvent.Fire(0);
                                break;
                            case 3:
                                this.jumpWithoutSound();
                                this.yJumpVelocity /= 2f;
                                this.netAnimationEvent.Fire(0);
                                break;
                            case 4:
                                {
                                    //JunimoHut junimoHut2 = home;
                                    //if (junimoHut2 != null && !junimoHut2.noHarvest)
                                    //{
                                    //    pathfindToNewCrop();
                                    //}

                                    break;
                                }
                            case 5:
                                this.netAnimationEvent.Fire(8);
                                break;
                        }
                    }
                }
            }

            bool flag2 = this.moveRight;
            bool flag3 = this.moveLeft;
            bool flag4 = this.moveUp;
            bool flag5 = this.moveDown;
            if (Game1.IsMasterGame)
            {
                if (this.controller == null && this.motion.Equals(Vector2.Zero))
                {
                    return;
                }

                flag2 |= Math.Abs(this.motion.X) > Math.Abs(this.motion.Y) && this.motion.X > 0f;
                flag3 |= Math.Abs(this.motion.X) > Math.Abs(this.motion.Y) && this.motion.X < 0f;
                flag4 |= Math.Abs(this.motion.Y) > Math.Abs(this.motion.X) && this.motion.Y < 0f;
                flag5 |= Math.Abs(this.motion.Y) > Math.Abs( this.motion.X) && this.motion.Y > 0f;
            }
            else
            {
                flag3 = this.IsRemoteMoving() && this.FacingDirection == 3;
                flag2 = this.IsRemoteMoving() && this.FacingDirection == 1;
                flag4 = this.IsRemoteMoving() && this.FacingDirection == 0;
                flag5 = this.IsRemoteMoving() && this.FacingDirection == 2;
                if (!flag2 && !flag3 && !flag4 && !flag5)
                {
                    return;
                }
            }

            this.Sprite.CurrentAnimation = null;
            if (flag2)
            {
                this.flip = false;
                if (this.Sprite.Animate(time, 16, 8, 50f))
                {
                    this.Sprite.currentFrame = 16;
                }
            }
            else if (flag3)
            {
                if (this.Sprite.Animate(time, 16, 8, 50f))
                {
                    this.Sprite.currentFrame = 16;
                }

                this.flip = true;
            }
            else if (flag4)
            {
                if (this.Sprite.Animate(time, 32, 8, 50f))
                {
                    this.Sprite.currentFrame = 32;
                }
            }
            else if (flag5)
            {
                this.Sprite.Animate(time, 0, 8, 50f);
            }
        }

        private static readonly int[] yBounceBasedOnFrame = new int[] { 12, 10, 8, 6, 4, 4, 8, 10 };
        private static readonly int[] xBounceBasedOnFrame = new int[] { 1, 3, 1, -1, -3, -1, 1, 0  };
        public override void draw(SpriteBatch b, float alpha = 1f)
        {
            if (this.alpha > 0f)
            {
                float num = (float)base.StandingPixel.Y / 10000f;
                b.Draw(
                    this.Sprite.Texture,
                    this.getLocalPosition(Game1.viewport)
                        + new Vector2(
                            this.Sprite.SpriteWidth * 4 / 2,
                            (float)this.Sprite.SpriteHeight * 3f / 4f * 4f / (float)Math.Pow(this.Sprite.SpriteHeight / 16, 2.0) + (float)this.yJumpOffset - 8f)  // Apparently yJumpOffset is always 0.
                        + ((this.shakeTimer > 0) // Apparently shakeTimer is always 0.
                            ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2))
                            : Vector2.Zero),
                    this.Sprite.SourceRect,
                    this.color.Value * this.alpha, this.rotation,
                    new Vector2(this.Sprite.SpriteWidth * 4 / 2,
                    (float)(this.Sprite.SpriteHeight * 4) * 3f / 4f) / 4f,
                    Math.Max(0.2f, this.Scale) * 4f, this.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    Math.Max(0f, this.drawOnTop ? 0.991f : num));
                if (!this.swimming.Value)
                {
                    b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, base.Position + new Vector2((float)(this.Sprite.SpriteWidth * 4) / 2f, 44f)), Game1.shadowTexture.Bounds, this.color.Value * this.alpha, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), (4f + (float)this.yJumpOffset / 40f) * this.Scale, SpriteEffects.None, Math.Max(0f, num) - 1E-06f);
                }

                float xOffset = 0;
                foreach (var carried in this.carrying)
                {
                    // This makes it vary between 0% and 5% bigger, independent of the animation frame because...  Well, I don't know if it's good or bad.
                    //  It also probably ought to affect bounce, if we were trying for some kind of specific effect, but we aren't, so it doesn't.
                    float scaleFactor = (float)((Math.Cos(Game1.currentGameTime.TotalGameTime.Milliseconds * Math.PI / 512.0) + 1.0) * 0.05f);

                    var bounce = new Vector2(xBounceBasedOnFrame[this.Sprite.CurrentFrame & 7], yBounceBasedOnFrame[this.Sprite.CurrentFrame & 7]);
                    var itemOffset = new Vector2(xOffset - 2.5f * this.carrying.Count, 0);
                    xOffset += 5f;
                    ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(carried.QualifiedItemId);
                    b.Draw(
                        dataOrErrorItem.GetTexture(),
                        Game1.GlobalToLocal(Game1.viewport, base.Position + new Vector2(8f, -64f * (float)this.Scale + 4f + (float)this.yJumpOffset) + bounce + itemOffset),
                        dataOrErrorItem.GetSourceRect(0, carried.ParentSheetIndex),
                        Color.White * this.alpha,
                        0f,
                        Vector2.Zero,
                        4f * (float)this.Scale*(1 + scaleFactor),
                        SpriteEffects.None,
                        base.Position.Y / 10000f + 0.0001f);
                }
            }
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            this.workFinder.WriteToLog(message, level, isOnceOnly);
        }
    }
}
