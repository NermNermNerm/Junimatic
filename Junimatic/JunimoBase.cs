using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;

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

        public override void update(GameTime time, GameLocation location)
        {
            if (this.speed != 0)
            {
                this.speed = this.TravelingSpeed;  // Set the speed on each update because bumping into stuff will set it back to 3
            }

            this.netAnimationEvent.Poll();
            base.update(time, location);

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
                    location.characters.Remove(this);
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
