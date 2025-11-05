using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   Provides the basic Junimo functionality that all the kinds of Junimos in this mod have.
    ///   Much of the code is copied from game code.
    /// </summary>
    public class JunimoBase : NPC, ISimpleLog
    {
        private float alpha = 1f;
        private float alphaChange;
        private readonly NetColor color = new NetColor();
        protected bool destroy;
        private readonly NetEvent1Field<int, NetInt> netAnimationEvent = new NetEvent1Field<int, NetInt>();
        private Action? pathBlockedAction = null;
        private readonly List<DelayedAction> delayedActions = new List<DelayedAction>();

        public JunimoBase()
        {
            this.Breather = false;
            this.speed = 3;
            this.forceUpdateTimer = 9999;
            this.ignoreMovementAnimation = true;
            this.farmerPassesThrough = true;
            this.Scale = 0.6f;
            this.willDestroyObjectsUnderfoot = false;
            this.collidesWithOtherCharacters.Value = false;
            this.SimpleNonVillagerNPC = true;

            this.alpha = 0;
            this.alphaChange = 0.05f;
        }

        public JunimoBase(GameLocation currentLocation, Color color, AnimatedSprite sprite, Vector2 position, int facingDir, string name, LocalizedContentManager? content = null)
            : base(sprite, position, facingDir, name, content)
        {
            // NOTES ON NAME!
            //  You wouldn't think it'd be a big deal...  but actually...
            //
            //  1. There's code in FarmHouse that deletes all "non-unique" NPC's, where "Unique" is
            //     based on having the same name.
            //  2. There's code in GameLocation.isCollidingPosition that makes it so that a tile is
            //     considered "blocked" if it has a property, "NPCBarrier" set on it...  UNLESS the
            //     NPC's name happens to contain "NPC".  Yep, really.

            this.color.Value = color;
            this.currentLocation = currentLocation;
            this.Breather = false;
            this.forceUpdateTimer = 9999;
            this.ignoreMovementAnimation = true;
            this.farmerPassesThrough = true;
            this.Scale = 0.75f; // regular ones are .75
            this.willDestroyObjectsUnderfoot = false;
            this.collidesWithOtherCharacters.Value = false;
            this.SimpleNonVillagerNPC = true;
            this.alpha = 0;
            this.alphaChange = 0.05f;
        }

        /// <summary>
        ///   Tries to plot a path to <paramref name="targetTile"/>.  If it cannot do so, it returns false and
        ///   does not call <paramref name="onCancel"/>.
        /// </summary>
        /// <param name="targetTile">The tile to go to.</param>
        /// <param name="onArrival">The action to call when the Junimo reaches the given point.</param>
        /// <param name="onCancel">This is called if the Junimo becomes unable to reach its assigned destination.</param>
        /// <returns>True if the course is plotted, false otherwise.</returns>
        public bool TryGoTo(Point targetTile, Action onArrival, Action? onCancel = null)
        {
            this.controller = null;
            this.pathBlockedAction = onCancel;
            this.controller = new PathFindController(this, this.currentLocation, targetTile, 0, (_, _) => {
                this.pathBlockedAction = null;
                this.controller = null;
                onArrival();
            });
            if (this.controller.pathToEndPoint is not null)
            {
                return true;
            }
            else
            {
                this.controller = null;
                this.pathBlockedAction = null;
                return false;
            }
        }

        /// <summary>
        ///   Goes to the assigned <paramref name="targetTile"/>.  If it cannot go there, <paramref name="onCancel"/> is called immediately.
        /// </summary>
        public void GoTo(Point targetTile, Action onArrival, Action? onCancel = null)
        {
            if (!this.TryGoTo(targetTile, onArrival, onCancel))
            {
                if (onCancel is not null)
                {
                    onCancel();
                }
            }
        }

        public static bool TryGoTo(NPC character, Point targetTile, Action onArrival)
        {
            character.controller = null;
            character.controller = new PathFindController(character, character.currentLocation, targetTile, 0, (_, _) => {
                character.controller = null;
                onArrival();
            });
            if (character.controller.pathToEndPoint is not null)
            {
                return true;
            }
            else
            {
                character.controller = null;
                return false;
            }
        }

        public static void GoTo(NPC character, Point targetTile, Action onArrival)
        {
            character.controller = null;
            character.controller = new PathFindController(character, character.currentLocation, targetTile, 0, (_, _) => {
                character.controller = null;
                onArrival();
            });
        }

        public void GoHome()
        {
            this.CancelAllDelayedActions();
            var gameMap = new GameMap(this.currentLocation);
            foreach (var portal in this.currentLocation.Objects.Values.Where(o => o.QualifiedItemId == UnlockPortal.JunimoPortalQiid).OrderBy(o => Math.Abs(o.TileLocation.X - this.Tile.X) + Math.Abs(o.TileLocation.Y - this.Tile.Y)))
            {
                gameMap.GetStartingInfo(portal, out var adjacentTiles, out _);
                foreach (var tile in adjacentTiles)
                {
                    if (this.TryGoTo(tile, this.FadeOutJunimo, () => { this.LogWarning($"Junimo could not reach its home"); this.FadeOutJunimo(); }))
                    {
                        return;
                    }
                }
            }

            this.LogWarning($"Junimo playmate could not find its way back to a hut!");
            this.FadeOutJunimo();
        }

        protected void DoAfterDelay(Action a, int delayInMs)
        {
            var newAction = DelayedAction.functionAfterDelay(() => { }, delayInMs);
            newAction.behavior = () =>
            {
                this.delayedActions.Remove(newAction);
                a();
            };

            this.delayedActions.Add(newAction);
        }

        protected void CancelAllDelayedActions()
        {
            foreach (var da in this.delayedActions)
            {
                Game1.delayedActions.Remove(da);
            }

            this.delayedActions.Clear();
        }

        public void Meep()
        {
            this.currentLocation.playSound("junimoMeep1");
        }

        public void FadeOutJunimo()
        {
            this.controller = null;
            this.destroy = true;
        }

        protected override void initNetFields()
        {
            base.initNetFields();
            base.NetFields
                .AddField(this.color, nameof(this.color))
                .AddField(this.netAnimationEvent, nameof(this.netAnimationEvent));
            this.netAnimationEvent.onEvent += this.doAnimationEvent;
        }

        protected virtual void doAnimationEvent(int animId)
        {
            switch (animId)
            {
                case 0:
                    this.Sprite.CurrentAnimation = null;
                    break;

                // 2-5 are unused, as best as I can figure.
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

                // These are set randomly in Update
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

        protected virtual int TravelingSpeed => 3;

        public override void update(GameTime time, GameLocation farmHouse)
        {
            if (this.speed != 0)
            {
                this.speed = this.TravelingSpeed;  // Set the speed on each update because bumping into stuff will set it back to 3
            }

            this.netAnimationEvent.Poll();
            base.update(time, farmHouse);

            if (this.controller is null && this.pathBlockedAction is not null)
            {
                // Clear pathBlockedAction before calling the action just in case that action involves calling GoTo again.
                var a = this.pathBlockedAction;
                this.pathBlockedAction = null;
                a();
            }

            this.forceUpdateTimer = 99999;

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
                if (this.destroy)
                {
                    farmHouse.characters.Remove(this);
                    return;
                }
            }

            if (Game1.IsMasterGame)
            {
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
                        case 5:
                            this.netAnimationEvent.Fire(8);
                            break;
                    }
                }
            }

            this.Sprite.CurrentAnimation = null;
            int frame;
            bool isStationary = !(this.moveRight || this.moveLeft || this.moveUp || this.moveDown);
            if (this.moveRight || (isStationary && this.FacingDirection == 1))
            {
                frame = 16;
                this.flip = false;
            }
            else if (this.moveLeft || (isStationary && this.FacingDirection == 3))
            {
                frame = 16;
                this.flip = true;
            }
            else if (this.moveUp || (isStationary && this.FacingDirection == 0))
            {
                frame = 32;
            }
            else // must be true: (this.moveDown || (isStationary && this.FacingDirection == 2))
            {
                frame = 0;
            }

            if (this.isMoving() || Game1.random.Next(4) == 0)
            {
                if (this.Sprite.Animate(time, frame, 8, 50f))
                {
                    // I don't seriously believe this statement does anything valuable, but the base game does it.
                    this.Sprite.currentFrame = frame;
                }
            }
        }

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
            }

            if (!Game1.eventUp)
            {
                this.DrawEmote(b);
            }
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            ModEntry.Instance.WriteToLog(message, level, isOnceOnly);
        }
    }
}
