using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;

using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class JunimoCrawlerPlaymate : JunimoPlaymateBase
    {
        private Point? childCrawlDestination;

        private static readonly MethodInfo? setStateMethod  = typeof(Child).GetMethod("setState", BindingFlags.Instance | BindingFlags.NonPublic);

        public JunimoCrawlerPlaymate()
        {
            this.LogTrace($"Junimo crawler playmate cloned");
        }

        public JunimoCrawlerPlaymate(Vector2 startingPoint, Child childToPlayWith)
            : base(startingPoint, [childToPlayWith])
        {
        }

        private Child childToPlayWith => this.childrenToPlayWith.First();

        public bool TryGoToChild()
        {
            if (JunimoCrawlerPlaymate.setStateMethod is null)
            {
                this.LogWarningOnce($"Can't do crawler playdates, this version of sdv 1.6 doesn't support the setState method");
                return false;
            }

            if (this.childToPlayWith.isMoving())
            {
                // Maybe skip this check and just park the child where he's at?
                // this.LogTrace($"Not doing playdate because baby is on the move");
                // return false;
            }

            // See if the tile below the child is free
            var junimosPoint = this.childToPlayWith.TilePoint + new Point(0,1);

            if (!this.isPositionOpen(junimosPoint.X, junimosPoint.Y + 1))
            {
                this.LogTrace($"Not doing playdate because the tile below the crawler is not free");
                return false;
            }

            return this.TryGoTo(junimosPoint, this.PlayGame);
        }

        private bool isPositionOpen(int tileX, int tileY)
        {
            var farmHouse = (FarmHouse)this.childToPlayWith.currentLocation;
            var tile = new Vector2(tileX, tileY);

            return farmHouse.hasTileAt(tileX, tileY, I("Back"))
                   && farmHouse.CanItemBePlacedHere(tile, collisionMask: CollisionMask.Furniture | CollisionMask.Objects | CollisionMask.TerrainFeatures)
                   && !farmHouse.isTileOnWall(tileX, tileY)
                   && farmHouse.getTileIndexAt(tileX, tileY, I("Back"), I("indoor")) != 0;
        }


        protected override void PlayGame()
        {
            if (Game1.timeOfDay >= this.timeToGoHome)
            {
                this.EndPlayDate();
                return;
            }

            void JumpAround()
            {
                this.LogTrace($"Playing jump-around game");
                this.FixChildControllers();

                foreach (var child in this.childrenToPlayWith)
                {
                    this.ParkChild(child);
                }

                this.BroadcastEmote(this.childrenToPlayWith[0], heartEmote);

                this.DoAfterDelay(this.Meep, 500);
                this.DoAfterDelay(this.jump, 1500);
                this.DoAfterDelay(this.PlayGame, 3500);
            }

            // Some privates that might help
            //
            //         child.Sprite.SpriteHeight = 16 /*0x10*/;
            //         child.Sprite.setCurrentAnimation(this.getRandomCrawlerAnimation(1));
            // getRandomCrawlerAnimation(0) means the animation where it seems to have something between its legs
            //                          (1) means just sitting on the floor
            //
            //  setState(n)
            //           0=up, 1=>right, 2=>down, 3=>left
            //           5=sitting with toys
            //           6=sitting without toys
            //
            // performToss() look for code that swaps into the arms-flapping animation.
            //
            // resetForPlayerEntry() will clear old animations and reset for whatever the 'state' is.

            void CircleRun()
            {
                this.LogTrace($"Playing circle-run game");
                var junimoStartingTile = this.TilePoint;
                int numAtDestination = 0;

                void endGame()
                {
                    ++numAtDestination;
                    if (numAtDestination == 1 + this.childrenToPlayWith.Count)
                    {
                        this.DoAfterDelay(this.PlayGame, 1000);
                    }
                }

                this.controller = null;
                // Junimo starts in lower center of area
                // NOTE: Paths are in reverse order!
                this.Meep();
                this.controller = new PathFindController(new Stack<Point>([
                    junimoStartingTile,
                    junimoStartingTile + new Point(1, 0),
                    junimoStartingTile + new Point(1, -1),
                    junimoStartingTile + new Point(-1, -1),
                    junimoStartingTile + new Point(-1, 0),
                ]), this, this.currentLocation)
                {
                    endPoint = junimoStartingTile,
                    finalFacingDirection = 0,
                    endBehaviorFunction = (_, _) => endGame()
                };

                foreach (var child in this.childrenToPlayWith)
                {
                    var childStartingTile = child.TilePoint;
                    child.Speed =
                        3; // Normally the child cruises at 5.  Setting this seems sketch.  Perhaps it should be unset at the end?
                    child.controller = null;
                    var path = new Stack<Point>(child == this.childrenToPlayWith.First()
                        ?
                        [
                            childStartingTile,
                            childStartingTile + new Point(0, 1),
                            childStartingTile + new Point(2, 1),
                            childStartingTile + new Point(2, 0), // starts upper left corner
                        ]
                        :
                        [
                            childStartingTile,
                            childStartingTile + new Point(-2, 0),
                            childStartingTile + new Point(-2, 1),
                            childStartingTile + new Point(0, 1), // starts upper right corner
                        ]);

                    child.controller = new PathFindController(path, child, this.currentLocation)
                    {
                        finalFacingDirection = 2,
                        endPoint = childStartingTile,
                        endBehaviorFunction = (_, _) =>
                        {
                            this.ParkChild(child);
                            endGame();
                        }
                    };
                }

                this.FixChildControllers();
            }

            if (this.childrenToPlayWith.Any(c => Math.Abs(c.Tile.X - this.Tile.X) > 1 || Math.Abs(c.Tile.Y - this.Tile.Y) > 1))
            {
                this.LogInfo($"The child(ren) and the Junimo got separated.");
                this.OnCharacterIsStuck();
            }
            else
            {
                Game1.random.Choose(JumpAround)();
            }
        }

        private void EndPlayDate()
        {
            // Todo, embellish this.
            this.GoHome();
        }

        public override void update(GameTime time, GameLocation farmHouse)
        {
            base.update(time, farmHouse);

            if (this.childCrawlDestination.HasValue)
            {
                // Ensure the child's still going that right direction
                if (this.childCrawlDestination.Value.X == this.childToPlayWith.TilePoint.X)
                {
                    // Reached the promised land!
                    this.SetChildStateSitting();
                    this.childCrawlDestination = null;
                }
                else if (this.childCrawlDestination.Value.X < this.childToPlayWith.TilePoint.X // Destination is to the right
                         && this.childToPlayWith.getDirection() != 1) // But not moving that way
                {
                    this.SetChildStateCrawlingRight();
                }
                else if (this.childCrawlDestination.Value.X > this.childToPlayWith.TilePoint.X // Destination is to the left
                         && this.childToPlayWith.getDirection() != 3) // But not moving that way
                {
                    this.SetChildStateCrawlingLeft();
                }
            }
            else
            {
                if (this.childToPlayWith.isMoving())
                {
                    this.SetChildStateSitting();
                }
            }
        }

        private void SetChildStateSitting()
        {
            // 4 => sitting with the toy in his lap, 5 without it.
            this.SetChildState(Random.Shared.Next(1) + 4);
        }

        private void SetChildStateCrawlingRight() => this.SetChildState(1);
        private void SetChildStateCrawlingLeft() => this.SetChildState(3);

        private void SetChildState(int state)
        {
            JunimoCrawlerPlaymate.setStateMethod!.Invoke(this.childToPlayWith, [state]);
        }

        protected override int TravelingSpeed => 5;
    }
}
