using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class JunimoToddlerPlaymate : JunimoBase
    {
        // Junimo runs to toddler - fix toddler in-place while junimo is going
        // Upon arrival, see if current location is 3x2 clear.  If not, run game1
        //
        // Game1:
        //   Run to another place that's 3x2-clear
        // Game2:
        //   Jump a few times
        // Game3:
        //   Run around in circles twice, finish with jumps, beeps and emotes.
        // Game4:
        //   Ball appears above Junimos head, shakes and shoots off to a random
        //   spot in the house that is pathable, both junimo and child run to the
        //   spot - their speeds change randomly as they go.  When one reaches the
        //   ball, it animates up and poofs.  Jumping and emotes follow,
        //   then they run game1.

        private readonly IReadOnlyList<Child> childrenToPlayWith = new List<Child>(); // Null when in a multiplayer game
        private bool noFarmersOnLastUpdate = false;
        private readonly int timeToGoHome;

        private readonly Dictionary<Child, Point> childParkedTiles = new();
        private readonly Dictionary<Child, PathFindController> childControllers = new();

        private enum Activity { GoingToPlay, Playing, GoingHome };
        private Activity activity;
        private GameBall? gameBall;

        public JunimoToddlerPlaymate()
        {
            this.LogTrace($"Junimo toddler playmate cloned");
        }

        public JunimoToddlerPlaymate(Vector2 startingPoint, IReadOnlyList<Child> children)
            : base(children[0].currentLocation, Color.Pink /* TODO */, new AnimatedSprite(@"Characters\Junimo", 0, 16, 16), startingPoint, 2, I("NPC_Junimo_ToddlerPlaymate"))
        {
            this.Scale = 0.6f; // regular ones are .75
            this.childrenToPlayWith = children;
            this.timeToGoHome = Math.Min(Game1.timeOfDay + 200, 1200 + 640); // Play for 2 hours or until 6:40pm.  Child.tenMinuteUpdate sends toddlers to bed at 7pm.
            this.LogTrace($"Junimo toddler playmate created to play with {children[0].Name}");
        }

        public bool TryGoToChild()
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
                    this.childControllers.Remove(whoArrived);
                    this.childParkedTiles[whoArrived] = whoArrived.TilePoint;
                }

                ++numberOfArrivals;
                if (numberOfArrivals == 1 + this.childrenToPlayWith.Count)
                {
                    this.PlayGame();
                }
            }

            // Note - TryGoTo has a side-effect of setting the controller if it returns true.  This method
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

            // If we get here, then it's a go
            this.childParkedTiles.Clear();
            for (int i = 0; i < this.childrenToPlayWith.Count; ++i)
            {
                var child = this.childrenToPlayWith[i];
                child.controller = controllers[i];
                this.childControllers[child] = controllers[i];
            }

            return true;
        }

        public override void GoHome()
        {
            this.LogTrace($"Playdate ending");
            this.activity = Activity.GoingHome;

            this.doEmote(sleepEmote);
            this.DoAfterDelay(() => this.childrenToPlayWith[0].doEmote(happyEmote), 1500);
            if (this.childrenToPlayWith.Count > 1)
            {
                this.DoAfterDelay(() => this.childrenToPlayWith[1].doEmote(sadEmote), 2250);
            }

            this.DoAfterDelay(() =>
            {
                base.GoHome();
            }, 3000);
        }

        private void PlayGame()
        {
            if (Game1.timeOfDay >= this.timeToGoHome)
            {
                this.GoHome();
            }
            else
            {
                void JumpAround()
                {
                    this.LogTrace($"Playing jump-around game");
                    this.FixChildControllers();

                    foreach (var child in this.childrenToPlayWith)
                    {
                        this.childParkedTiles[child] = child.TilePoint;
                    }

                    this.childrenToPlayWith[0].doEmote(heartEmote);
                    DoToddlerArmFlapAnimation(this.childrenToPlayWith[0]);
                    if (this.childrenToPlayWith.Count > 1)
                    {
                        this.DoAfterDelay(() =>
                        {
                            this.childrenToPlayWith[1].doEmote(happyEmote);
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
                    };
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
                        ]), this, this.currentLocation);
                    this.controller.endPoint = junimoStartingTile;
                    this.controller.finalFacingDirection = 0 /* up */;
                    this.controller.endBehaviorFunction = (_, _) => endGame();

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

                        child.controller = new PathFindController(path, child, this.currentLocation);
                        child.controller.finalFacingDirection = 2 /* down */;
                        child.controller.endPoint = childStartingTile;
                        child.controller.endBehaviorFunction = (_, _) =>
                        {
                            this.childControllers.Remove(child);
                            this.childParkedTiles[child] = child.TilePoint;
                            endGame();
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
                        if (openPoint != Point.Zero && (Math.Abs(openPoint.X - this.TilePoint.X)) > 15 || Math.Abs(openPoint.Y - this.TilePoint.Y) > 15)
                        {
                            this.controller = null; // Should already be null, but just to be sure...
                            var controller = new PathFindController(this, this.currentLocation, openPoint, 0);
                            if (controller.pathToEndPoint is not null)
                            {
                                this.FixChildControllers(); // Parks the children in their current position while the ball is bouncing
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

        /// <summary>
        ///   Make sure children's controllers don't get reset from where they are now.
        /// </summary>
        private void FixChildControllers()
        {
            this.childControllers.Clear();
            this.childParkedTiles.Clear();
            foreach (var c in this.childrenToPlayWith)
            {
                if (c.controller is null)
                {
                    this.childParkedTiles[c] = c.TilePoint;
                }
                else
                {
                    this.childControllers[c] = c.controller;
                }
            }
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

        public override void update(GameTime time, GameLocation location)
        {
            var oldController = this.controller; // this.controller might be set to null by base.update
            base.update(time, location);

            if (!Game1.IsMasterGame || this.destroy)
            {
                return;
            }

            // Logic for when the farmhouse becomes or is no longer empty.  Beware the short-circuit returns embedded herein.
            if (location.farmers.Any() || Game1.currentLocation == location)
            {
                if (this.noFarmersOnLastUpdate)
                {
                    this.noFarmersOnLastUpdate = false;
                    // Farmer just arrived - Reset play.

                    this.FindNewSpot();
                    return;
                }
            }
            else // Nobody here.
            {
                if (this.gameBall is not null)
                {
                    this.currentLocation.critters.Remove(this.gameBall);
                    this.gameBall = null;
                }

                // If it's late or the junimo was going home anyway, just remove them from the scene.
                if (Game1.timeOfDay > 1200 + 700 || this.activity == Activity.GoingHome)
                {
                    this.CancelAllDelayedActions();
                    location.characters.Remove(this);
                    return;
                }

                // Else right after the player leaves...
                if (!this.noFarmersOnLastUpdate)
                {
                    this.noFarmersOnLastUpdate = true;

                    // Reset to the play-starting position
                    this.controller = null;
                    this.Speed = this.TravelingSpeed;

                    // And put a stop to any planned activity
                    this.CancelAllDelayedActions();
                    return;
                }
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

                    this.doEmote(winningChild is null ? exclamationEmote : angryEmote);
                    foreach (var c in this.childrenToPlayWith)
                    {
                        c.doEmote(c == winningChild ? exclamationEmote : angryEmote);
                    }

                    this.DoAfterDelay(this.FindNewSpot, 1500);
                }
            }

            foreach (var c in this.childrenToPlayWith)
            {
                if (this.childParkedTiles.TryGetValue(c, out var tileParkedAt) && c.controller is not null && c.TilePoint != c.controller.endPoint)
                {
                    this.LogWarning($"{c.Name} tried to run off - clearing the controller."); // TODO Verbose
                    c.controller = null;
                    c.Position = tileParkedAt.ToVector2() * 64F;
                }
            }

            bool needsReset = false;
            foreach (var c in this.childrenToPlayWith)
            {
                if (this.childControllers.TryGetValue(c, out var assignedController) && c.controller != assignedController)
                {
                    if (c.controller is null)
                    {
                        if (c.Tile.ToPoint() != assignedController.endPoint)
                        {
                            this.LogInfo($"{c.Name} can't reach the next spot to play - resetting.");
                            needsReset = true;
                        }
                    }
                    else
                    {
                        this.LogWarning($"Something outside of Junimatic changed {c.Name}'s destination - putting it back to the play destination.");
                        c.controller = assignedController;
                    }
                }
            }

            if (this.controller is null && oldController is not null && oldController.endPoint != this.Tile.ToPoint())
            {
                this.LogInfo($"The junimo playmate could not reach its destination - resetting.");
                needsReset = true;
            }

            if (needsReset)
            {
                this.CancelAllDelayedActions();
                this.FindNewSpot();
            }
        }

        private static void DoToddlerArmFlapAnimation(Child c)
        {
            c.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
            {
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
            });
        }

        protected override int TravelingSpeed => 5;
    }
}
