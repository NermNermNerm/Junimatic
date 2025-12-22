using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   This is a prop used by JunimoCrawlerPlaymate that simulates the child being carried up by a balloon.
    /// </summary>
    public class Balloon : Critter
    {
        // 10 tiles/second = 640 pixels/second => (640/60) pixels/update-tick ~= 10 pixels/update-tick
        private const float MaxSingleDimensionSpeed = 10; // in pixels/update

        private float speed;
        private float scale;

        private readonly int floatHeightInPixels;
        private readonly float startingYPosition;

        private const float startingScale = 1f;
        private const float fullScale = 4f;
        private const float scaleGrowth = (Balloon.fullScale - Balloon.startingScale)  /* total to grow */ / 60f /* 1 seconds worth of ticks */;
        private const float acceleration = .02f; // pixel/tick^2
        private const float maxSpeed = .75f; // 1 pixels/tick = 60 pixels/second = 60/64 ~= 1 tiles/second
        private const float downSpeed = 0.5f; // Matches JunimoCrawlerPlatemate.floatDownJumpVelocity

        public Balloon(GameLocation farmHouse, int floatHeightInTiles, Vector2 childPosition)
        {
            this.position = childPosition;
            this.startingYPosition = childPosition.Y;
            this.startingPosition = childPosition;
            this.sprite = new AnimatedSprite(ModEntry.BalloonSpritesPseudoPath, 0, 16, 40);
            this.baseFrame = 0;
            this.speed = 0;
            this.scale = Balloon.startingScale;
            this.floatHeightInPixels = floatHeightInTiles * 64;
        }

        public bool IsGoingDown { get; set; }= false;

        public AnimatedSprite Sprite => this.sprite;

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

            if (this.IsGoingDown)
            {
                this.position.Y = this.position.Y + Balloon.downSpeed;
            }
            else
            {
                this.position.Y = this.position.Y + this.speed;
                this.speed = this.position.Y < this.startingPosition.Y - this.floatHeightInPixels
                    ? Math.Min(this.speed + Balloon.acceleration, Balloon.maxSpeed)
                    : Math.Max(this.speed - Balloon.acceleration, -Balloon.maxSpeed);
            }

            this.scale = Math.Min(this.scale + Balloon.scaleGrowth, Balloon.fullScale);

            return result;
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(this.sprite.Texture,
                Game1.GlobalToLocal(
                             Game1.viewport,
                             Utility.snapDrawPosition(this.position + new Vector2(32f, 20f))),
                this.sprite.SourceRect,
                Color.White,
                0f,
                new Vector2(this.sprite.SpriteWidth / 2f, this.sprite.SpriteHeight / 2f),
                this.scale,
                this.sprite.CurrentAnimation != null && this.sprite.CurrentAnimation[this.sprite.currentAnimationIndex].flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                (this.startingYPosition + 31f) / 10000f); // The baby is drawn at layer depth 32, so -1 makes it appear just behind the baby.

            // Not drawing a shadow...  too lazy.

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
