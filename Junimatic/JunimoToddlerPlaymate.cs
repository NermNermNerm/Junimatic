using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;

namespace NermNermNerm.Junimatic
{
    public class JunimoToddlerPlaymate : JunimoPlaymateBase
    {
        private GameBall? gameBall = null;

        public JunimoToddlerPlaymate()
        {
            this.LogTrace($"Junimo toddler playmate cloned");
        }

        public JunimoToddlerPlaymate(Vector2 startingPoint, IReadOnlyList<Child> children)
            : base(startingPoint, children)
        {
            this.LogTrace($"Junimo toddler playmate created to play with {children[0].Name}");
        }

        public bool TryFindPlaceToPlay()
        {
            var firstChild = this.childrenToPlayWith.First();
            var playPoint = firstChild.controller?.endPoint ?? firstChild.Tile.ToPoint();
            this.LogTrace($"Starting a playdate with {firstChild.Name}");

            return this.TryGoToNewPlayArea(playPoint);
        }

        private bool TryGoToNewPlayArea(Point playPoint)
        {
            var firstChild = this.childrenToPlayWith.First();
            int numberOfArrivals = 0;
            void OnArrivedAtPlayPoint(Child? whoArrived) // If null, it's the Junimo
            {
                if (whoArrived is not null)
                {
                    this.ParkChild(whoArrived);
                }

                ++numberOfArrivals;
                if (numberOfArrivals == 1 + this.childrenToPlayWith.Count)
                {
                    this.PlayGame();
                }
            }

            // Note - TryGoTo has a side effect of setting the controller if it returns true.  This method
            //  *should* do nothing if it returns false.  We're not going to bother fussing over it because
            //  if this method returns false, this instance is going to get scrapped anyway.  This test gets
            //  run first for that reason and because the most likely cause of failure is if the hut is
            //  in a cordoned off room.
            bool canGo = this.TryGoTo(playPoint + new Point(0, 1), () => OnArrivedAtPlayPoint(null), this.GoHome);
            if (!canGo)
            {
                return false;
            }

            List<PathFindController> controllers = new List<PathFindController>();
            foreach (var child in this.childrenToPlayWith)
            {
                Point offset = child == firstChild ? new Point(-1, 0) : new Point(1, 0);
                var oldController = child.controller;
                child.controller = null;
                GoTo(child, playPoint + offset, () => OnArrivedAtPlayPoint(child));
                if (child.controller is null || child.controller.pathToEndPoint is null)
                {
                    child.controller = oldController;
                    return false;
                }

                child.controller.finalFacingDirection = 2 /* down */;
                controllers.Add(child.controller);
                child.controller = oldController;
            }

            // If we get here, then it should be possible for the children and the Junimo to reach the spot.
            for (int i = 0; i < this.childrenToPlayWith.Count; ++i)
            {
                var child = this.childrenToPlayWith[i];
                this.SetChildController(child, controllers[i]);
            }

            return true;
        }

        private void FindNewSpot()
        {
            this.LogTrace($"Finding a new place to play");
            var fh = (FarmHouse)this.currentLocation;
            // getRandomOpenPointInHouse returns the center of a 3x3 square of clear area
            var openPoint = fh.getRandomOpenPointInHouse(Game1.random, buffer: 1, tries: 100);
            if (openPoint == Point.Zero)
            {
                this.LogWarning($"Your house is very crowded - it's hard to find a place to play.");
                this.DoAfterDelay(this.FindNewSpot, 500);
                return;
            }

            if (!this.TryGoToNewPlayArea(openPoint))
            {
                this.LogInfo($"Either the child or the Junimo can't reach the next play point.");
                this.DoAfterDelay(this.FindNewSpot, 1000);
            }
        }


        protected override void PlayGame()
        {
            if (Game1.timeOfDay >= this.timeToGoHome)
            {
                this.EndPlayDate();
            }
            else
            {
                void JumpAround()
                {
                    this.LogTrace($"Playing jump-around game");
                    this.FixChildControllers();

                    foreach (var child in this.childrenToPlayWith)
                    {
                        this.ParkChild(child);
                    }

                    this.BroadcastEmote( this.childrenToPlayWith[0], heartEmote);
                    DoToddlerArmFlapAnimation(this.childrenToPlayWith[0]);
                    if (this.childrenToPlayWith.Count > 1)
                    {
                        this.DoAfterDelay(() =>
                        {
                            this.BroadcastEmote(this.childrenToPlayWith[1], happyEmote);
                            this.childrenToPlayWith[1].jump(6);
                        }, 500);
                    }

                    this.jump();
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
                        child.Speed = 3; // Normally the child cruises at 5.  Setting this seems sketch.  Perhaps it should be unset at the end?
                        child.controller = null;
                        var path = new Stack<Point>(child == this.childrenToPlayWith.First()
                            ? [
                                childStartingTile,
                                childStartingTile + new Point(0, 1),
                                childStartingTile + new Point(2, 1),
                                childStartingTile + new Point(2, 0), // starts upper left corner
                            ]
                            : [
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
                void BallHunt()
                {
                    // 5 tries to find a place
                    this.LogTrace($"Playing ball-hunt game");
                    for (int i = 0; i < 5 && this.gameBall is null; ++i)
                    {
                        var fh = (FarmHouse)this.currentLocation;
                        // getRandomOpenPointInHouse returns the center of a 3x3 square of clear area
                        var openPoint = fh.getRandomOpenPointInHouse(Game1.random, buffer: 0, tries: 20);
                        if (openPoint != Point.Zero && (Math.Abs(openPoint.X - this.TilePoint.X) > 15 || Math.Abs(openPoint.Y - this.TilePoint.Y) > 15))
                        {
                            this.controller = null; // Should already be null, but just to be sure...
                            var controller = new PathFindController(this, this.currentLocation, openPoint, 0);
                            if (controller.pathToEndPoint is not null)
                            {
                                this.LogTrace($"Starting ball game, ball will land at {openPoint}");
                                this.FixChildControllers(); // Parks the children in their current position while the ball is bouncing
                                var startingTile = this.Tile.ToPoint() + new Point(1, 0);
                                ModEntry.Instance.PlaymateMultiplayerSupport.BroadcastBall(startingTile, openPoint);
                                this.gameBall = new GameBall(this.currentLocation, this.Tile.ToPoint() + new Point(1, 0), openPoint, () =>
                                {
                                    this.controller = controller; // Send the Junimo on its way.
                                    foreach (var child in this.childrenToPlayWith)
                                    {
                                        child.Speed = Game1.random.Next(3) + 2;
                                        // Consider adding some kind of dither where the players run a few tiles in random directions before
                                        // bee-lining it to the ball.
                                        GoTo(child, openPoint, () => { });
                                    }
                                    this.FixChildControllers();
                                });
                                this.currentLocation.instantiateCrittersList(); // <- only does something if the critters list is non-existent.
                                this.currentLocation.addCritter(this.gameBall); // <- if the critters list doesn't exist, this will do nothing.
                            }
                        }
                    }

                    if (this.gameBall is null)
                    {
                        this.LogInfo($"The house is pretty crowded - can't play chase-the-ball");
                        this.DoAfterDelay(this.PlayGame, 5000);
                    }
                }

                if (this.childrenToPlayWith.Any(c => Math.Abs(c.Tile.X - this.Tile.X) > 1 || Math.Abs(c.Tile.Y - this.Tile.Y) > 1))
                {
                    this.LogInfo($"The child(ren) and the Junimo got separated.  Picking a new place to play.");
                    this.FindNewSpot();
                }
                else
                {
                    Game1.random.Choose(JumpAround, JumpAround, JumpAround, CircleRun, CircleRun, CircleRun, CircleRun, BallHunt, BallHunt, this.FindNewSpot)();
                }
            }
        }

        private void EndPlayDate()
        {
            this.LogTrace($"Playdate ending");
            this.activity = Activity.GoingHome;

            this.BroadcastEmote(sleepEmote);
            this.DoAfterDelay(() => this.BroadcastEmote(this.childrenToPlayWith[0], happyEmote), 1500);
            if (this.childrenToPlayWith.Count > 1)
            {
                this.DoAfterDelay(() => this.BroadcastEmote(this.childrenToPlayWith[1], sadEmote), 2250);
            }

            this.DoAfterDelay(this.GoHome, 3000);
        }

        protected override void OnCharacterIsStuck()
        {
            base.OnCharacterIsStuck();
            this.FindNewSpot();
        }

        protected override void OnFarmerEnteredFarmhouse()
        {
            this.FindNewSpot();
        }

        protected override void OnFarmersLeftFarmhouse()
        {
            if (this.gameBall is not null)
            {
                this.currentLocation.critters.Remove(this.gameBall);
                ModEntry.Instance.PlaymateMultiplayerSupport.BroadcastRemoveBall();
                this.gameBall = null;
            }
        }

        public override void update(GameTime time, GameLocation farmHouse)
        {
            base.update(time, farmHouse);

            if (!Game1.IsMasterGame || this.destroy)
            {
                return;
            }

            if (this.gameBall is not null && this.gameBall.IsLanded)
            {
                float roughDistance(Vector2 v1, Vector2 v2) => Math.Max(Math.Abs(v1.X - v2.X), Math.Abs(v1.Y - v2.Y));

                float junimoDistance = roughDistance(this.Position, this.gameBall.position);
                Child? winningChild = null;
                float winningDistance = junimoDistance;
                foreach (var c in this.childrenToPlayWith)
                {
                    float distance = roughDistance(c.Position, this.gameBall.position);
                    if (distance < winningDistance)
                    {
                        winningChild = c;
                        winningDistance = distance;
                    }
                }

                if (winningDistance <= 64)
                {
                    UnlockFishing.MakePoof(this.gameBall.position / 64, 1F);
                    this.currentLocation.playSound("dwoop");
                    this.currentLocation.critters.Remove(this.gameBall);
                    this.gameBall = null;
                    ModEntry.Instance.PlaymateMultiplayerSupport.BroadcastRemoveBall();

                    this.BroadcastEmote(winningChild is null ? exclamationEmote : angryEmote);
                    foreach (var c in this.childrenToPlayWith)
                    {
                        this.BroadcastEmote(c, c == winningChild ? exclamationEmote : angryEmote);
                    }

                    this.DoAfterDelay(this.FindNewSpot, 1500);
                }
            }
        }

        private static void DoToddlerArmFlapAnimation(Child c)
        {
            c.Sprite.setCurrentAnimation([
                new(16, 120, 0, secondaryArm: false, flip: false),
                new(17, 120, 0, secondaryArm: false, flip: false),
                new(18, 120, 0, secondaryArm: false, flip: false),
                new(19, 120, 0, secondaryArm: false, flip: false),
                new(18, 120, 0, secondaryArm: false, flip: false),
                new(17, 120, 0, secondaryArm: false, flip: false),
                new(16, 120, 0, secondaryArm: false, flip: false),
                new(0, 300, 0, secondaryArm: false, flip: false),
                new(16, 100, 0, secondaryArm: false, flip: false),
                new(17, 100, 0, secondaryArm: false, flip: false),
                new(18, 100, 0, secondaryArm: false, flip: false),
                new(19, 100, 0, secondaryArm: false, flip: false),
                new(18, 300, 0, secondaryArm: false, flip: false),
                new(17, 100, 0, secondaryArm: false, flip: false),
                new(16, 100, 0, secondaryArm: false, flip: false),
                new(0, 300, 0, secondaryArm: false, flip: false),
                new(16, 120, 0, secondaryArm: false, flip: false),
                new(17, 180, 0, secondaryArm: false, flip: false),
                new(16, 120, 0, secondaryArm: false, flip: false),
                new(0, 800, 0, secondaryArm: false, flip: false)
            ]);
        }

        protected override int TravelingSpeed => 5;
    }
}
