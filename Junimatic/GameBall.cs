using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace NermNermNerm.Junimatic
{
    public class GameBall : Critter
    {
        private const int BaseballBobberTileSheetIndex = 50;

        private readonly Action doWhenLands;
        private readonly GameLocation location;

        // 10 tiles/second = 640 pixels/second => (640/60) pixels/update-tick ~= 10 pixels/update-tick
        private const float MaxSingleDimensionSpeed = 10; // in pixels/update

        private int bounceNumber;

        public readonly Vector2 endingPosition;

        public GameBall(GameLocation farmHouse, Point startingTile, Point endingTile, Action doWhenLands)
        {
            this.position = startingTile.ToVector2() * 64F;
            this.startingPosition = startingTile.ToVector2() * 64F;
            this.sprite = new AnimatedSprite(@"TileSheets\bobbers", BaseballBobberTileSheetIndex, 16, 16);
            this.baseFrame = BaseballBobberTileSheetIndex;
            this.endingPosition = endingTile.ToVector2()*64F;
            this.doWhenLands = doWhenLands;
            this.bounceNumber = 1;
            this.location = farmHouse;
            this.gravityAffectedDY = - this.bounceNumber * 2f; // 7 will yield 1.5 tiles
        }

        public override bool update(GameTime time, GameLocation environment)
        {
            // As far as I can figure, returning true means delete the critter.
            // Critter.update returns true if the critter is off-screen, which shouldn't happen.
            bool result = base.update(time, environment);

            if (this.bounceNumber < 5 && this.yJumpOffset >= 0)
            {
                ++this.bounceNumber;
                this.gravityAffectedDY = -this.bounceNumber * 3f;
                this.location.playSound("dwop");
            }
            else if (this.bounceNumber == 5)
            {
                if (this.yJumpOffset >= 0)
                {
                    float deltaX = this.endingPosition.X - this.position.X;
                    float deltaY = this.endingPosition.Y - this.position.Y;
                    if (this.position == this.startingPosition)
                    {
                        float ticksToGetThere = Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)) / MaxSingleDimensionSpeed;
                        this.gravityAffectedDY = -ticksToGetThere / 8; // total delta-v is twice the initial gravityAffectedDY we need, v=(.5)*(.25)*time
                        this.location.playSound("dwop");
                    }
                    ++this.bounceNumber;
                }
            }
            else if (this.bounceNumber == 6)
            {
                float deltaX = this.endingPosition.X - this.position.X;
                float deltaY = this.endingPosition.Y - this.position.Y;
                if (this.position == this.endingPosition)
                {
                    if (this.yJumpOffset >= 0) // Ball is at or below floor-height
                    {
                        this.yJumpOffset = 0; // Since we don't guarantee this.gravityAffectedDY is a multiple of .25F, this could get a bit off-alignment.
                        ++this.bounceNumber; // not really bouncing anymore.
                        this.location.playSound("dwop");
                        this.doWhenLands();
                    }
                    // else there's still a bit more falling to do.
                }

                if (Math.Abs(deltaX) < MaxSingleDimensionSpeed && Math.Abs(deltaY) < MaxSingleDimensionSpeed)
                {
                    // We've arrived
                    this.position = this.endingPosition;
                }
                else
                {
                    this.position.X += Math.Sign(deltaX) * MaxSingleDimensionSpeed * Math.Min(1, (deltaY == 0 ? 1 : Math.Abs(deltaX / deltaY)));
                    this.position.Y += Math.Sign(deltaY) * MaxSingleDimensionSpeed * Math.Min(1, (deltaX == 0 ? 1 : Math.Abs(deltaY / deltaX)));
                }
            }

            return result;
        }

        public override void draw(SpriteBatch b)
        {
            // The base class' draw method draws the object two tiles above and one to the left of where the position says it should go, because
            // obviously that should be the default behavior...

            this.sprite.draw(
                b,
                Game1.GlobalToLocal(
                    Game1.viewport,
                    Utility.snapDrawPosition(this.position + new Vector2(0f, -20f + this.yJumpOffset + this.yOffset))),
                    (this.position.Y + 64f - 32f) / 10000f, 0, 0, Color.White, false, 4f);
            b.Draw(
                Game1.shadowTexture,
                Game1.GlobalToLocal(
                        Game1.viewport,
                        this.position + new Vector2(32f, 40f)),
                Game1.shadowTexture.Bounds,
                Color.White,
                0f,
                new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                3f + Math.Max(-3f, (this.yJumpOffset + this.yOffset) / 16f),
                SpriteEffects.None,
                (this.position.Y - 1f) / 10000f);
        }
    }
}
