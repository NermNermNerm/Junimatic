using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace NermNermNerm.Junimatic
{
    public class Balloon : Critter
    {
        // 10 tiles/second = 640 pixels/second => (640/60) pixels/update-tick ~= 10 pixels/update-tick
        private const float MaxSingleDimensionSpeed = 10; // in pixels/update

        private readonly Action doWhenLands;
        private readonly GameLocation location;
        private float speed;
        private float scale;

        private const float startingScale = 1f;
        private const float fullScale = 4f;
        private const float scaleGrowth = (Balloon.fullScale - Balloon.startingScale)  /* total to grow */ / 120f /* 2 seconds worth of ticks */;
        private const float acceleration = .02f; // pixel/tick^2
        private const float maxSpeed = .75f; // 1 pixels/tick = 60 pixels/second = 60/64 ~= 1 tiles/second

        private int floatHeightInPixels;


        public Balloon(GameLocation farmHouse, int floatHeightInTiles, Vector2 childPosition, Action doWhenLands)
        {
            this.position = childPosition;
            this.startingPosition = childPosition;
            this.sprite = new AnimatedSprite(ModEntry.BalloonSpritesPseudoPath, 0, 13, 36);
            this.baseFrame = 0;
            this.speed = 0;
            this.scale = Balloon.startingScale;
            this.doWhenLands = doWhenLands;
            this.location = farmHouse;
            this.floatHeightInPixels = floatHeightInTiles * 64;
        }

        public override bool update(GameTime time, GameLocation environment)
        {
            // As far as I can figure, returning true means delete the critter.
            // Critter.update returns true if the critter is off-screen, which shouldn't happen.
            bool result = base.update(time, environment);

            // Don't change anything if time is paused by a menu in a single-player game
            if (Game1.activeClickableMenu is not null && !Game1.IsMultiplayer)
            {
                return result;
            }

            this.position.Y = this.position.Y + this.speed;

            if (this.position.Y < this.startingPosition.Y - this.floatHeightInPixels)
            {
                this.speed = Math.Min(this.speed + Balloon.acceleration, Balloon.maxSpeed);
            }
            else
            {
                this.speed = Math.Max(this.speed - Balloon.acceleration, - Balloon.maxSpeed);
            }

            this.scale = Math.Min(this.scale + Balloon.scaleGrowth, Balloon.fullScale);

            return result;
        }

        public override void draw(SpriteBatch b)
        {
            // The base class' draw method draws the object two tiles above and one to the left of where the position says it should go, because
            // obviously that should be the default behavior...

            // this.sprite.draw(
            //     b,
            //     Game1.GlobalToLocal(
            //         Game1.viewport,
            //         Utility.snapDrawPosition(this.position + new Vector2(0f, -20f + this.yJumpOffset + this.yOffset))),
            //     (this.position.Y + 64f - 32f) / 10000f, 0, 0, Color.White, false, 4f,
            //     rotation: this.rotation,
            //     characterSourceRectOffset: true);

            b.Draw(this.sprite.Texture,
                Game1.GlobalToLocal(
                             Game1.viewport,
                             Utility.snapDrawPosition(this.position + new Vector2(32f, 20f))),
                new Rectangle?(new Rectangle(this.sprite.sourceRect.X, this.sprite.sourceRect.Y, this.sprite.sourceRect.Width, this.sprite.sourceRect.Height)),
                Color.White,
                0f,
                new Vector2(this.sprite.SpriteWidth / 2f, this.sprite.SpriteHeight / 2f),
                this.scale,
                this.sprite.CurrentAnimation != null && this.sprite.CurrentAnimation[this.sprite.currentAnimationIndex].flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                (this.position.Y + 64f - 32f) / 10000f);
            //
            // b.Draw(
            //     Game1.shadowTexture,
            //     Game1.GlobalToLocal(
            //             Game1.viewport,
            //             this.position + new Vector2(32f, 40f)),
            //     Game1.shadowTexture.Bounds,
            //     Color.White,
            //     0f,
            //     new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
            //     3f + Math.Max(-3f, (this.yJumpOffset + this.yOffset) / 16f),
            //     SpriteEffects.None,
            //     (this.position.Y - 1f) / 10000f);
        }
    }
}
