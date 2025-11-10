using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.Pathfinding;

namespace NermNermNerm.Junimatic
{
    public class JunimoCrawlerPlaymate : JunimoPlaymateBase
    {
        public JunimoCrawlerPlaymate()
        {
            this.LogTrace($"Junimo crawler playmate cloned");
        }

        public JunimoCrawlerPlaymate(Vector2 startingPoint, Child childToPlayWith)
            : base(startingPoint, [childToPlayWith])
        {
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
                this.LogInfo($"The child(ren) and the Junimo got separated.  Picking a new place to play.");
                this.FindNewSpot();
            }
            else
            {
                Game1.random.Choose(JumpAround, JumpAround, JumpAround, CircleRun, CircleRun, CircleRun, CircleRun, this.FindNewSpot)();
            }
        }

        private void EndPlayDate()
        {
            // Todo, embellish this.
            this.GoHome();
        }

        protected override int TravelingSpeed => 5;
    }
}
