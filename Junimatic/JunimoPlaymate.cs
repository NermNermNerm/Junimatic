using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Pathfinding;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class JunimoPlaymate : NPC, ISimpleLog
    {
        private float alpha = 1f;
        private float alphaChange;
        private readonly NetColor color = new NetColor();
        private bool destroy;
        private readonly NetEvent1Field<int, NetInt> netAnimationEvent = new NetEvent1Field<int, NetInt>();
        private readonly Child? childToPlayWith;

        public const int VillagerDetectionRange = 50;

        public JunimoPlaymate()
        {
            this.Breather = false;
            this.speed = 3;
            this.forceUpdateTimer = 9999;
            this.ignoreMovementAnimation = true;
            this.farmerPassesThrough = true;
            this.Scale = 0.75f;
            this.willDestroyObjectsUnderfoot = false;
            this.collidesWithOtherCharacters.Value = false;
            this.SimpleNonVillagerNPC = true;
            this.childToPlayWith = null;

            this.alpha = 0;
            this.alphaChange = 0.05f;
            this.LogTrace($"Junimo playmate cloned");
        }

        public JunimoPlaymate(GameLocation location, Vector2 startingPoint, Point endTile) // Test API
            : base(new AnimatedSprite(@"Characters\Junimo", 0, 16, 16), startingPoint, 2, I("Junimo"))
        {
            this.color.Value = Color.Pink;
            this.currentLocation = location;
            this.Breather = false;
            this.forceUpdateTimer = 9999;
            this.ignoreMovementAnimation = true;
            this.farmerPassesThrough = true;
            this.Scale = 0.75f;
            this.willDestroyObjectsUnderfoot = false;
            this.collidesWithOtherCharacters.Value = false;
            this.SimpleNonVillagerNPC = true;

            this.alpha = 0;
            this.alphaChange = 0.05f;

            this.controller = new PathFindController(this, location, endTile, 0, this.JunimoReachedCrib);
            this.controller = new PathFindController(this, location, endTile, 0, this.JunimoReachedCrib);
        }


        public JunimoPlaymate(Vector2 startingPoint, Child child)
            : base(new AnimatedSprite(@"Characters\Junimo", 0, 16, 16), startingPoint, 2, I("Junimo"))
        {
            this.color.Value = Color.Pink; // TODO: Mix this up
            this.currentLocation = child.currentLocation;
            this.Breather = false;
            this.forceUpdateTimer = 9999;
            this.ignoreMovementAnimation = true;
            this.farmerPassesThrough = true;
            this.Scale = 0.75f;
            this.willDestroyObjectsUnderfoot = false;
            this.collidesWithOtherCharacters.Value = false;
            this.SimpleNonVillagerNPC = true;
            this.childToPlayWith = child;
            this.alpha = 0;
            this.alphaChange = 0.05f;
            this.LogTrace($"Junimo playmate created to play with {child.Name}");
            var playPoint = this.childToPlayWith!.Tile + new Vector2(0, 1);
            this.controller = new PathFindController(this, this.childToPlayWith.currentLocation, playPoint.ToPoint(), 0, this.JunimoReachedCrib);
        }

        public bool IsViable => this.controller?.pathToEndPoint is not null;

        public void JunimoReachedCrib(Character c, GameLocation l)
        {
        }


        public void JunimoReachedHut(Character c, GameLocation l)
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

        public override void update(GameTime time, GameLocation location)
        {
            if (Game1.IsMasterGame && this.controller is null)
            {
                this.LogTrace($"Junimo playmate can't get where it's going");
                location.characters.Remove(this);
                return;
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
            if (this.moveRight)
            {
                this.flip = false;
                if (this.Sprite.Animate(time, 16, 8, 50f))
                {
                    this.Sprite.currentFrame = 16;
                }
            }
            else if (this.moveLeft)
            {
                if (this.Sprite.Animate(time, 16, 8, 50f))
                {
                    this.Sprite.currentFrame = 16;
                }

                this.flip = true;
            }
            else if (this.moveUp)
            {
                if (this.Sprite.Animate(time, 32, 8, 50f))
                {
                    this.Sprite.currentFrame = 32;
                }
            }
            else if (this.moveDown)
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
