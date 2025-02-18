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



        private readonly Child? childToPlayWith; // Null when in a multiplayer game

        private int gamesPlayed = 0;

        private bool noFarmersOnLastUpdate = false;

        private enum Activity { GoingToPlay, Playing, GoingHome };
        private Activity activity;

        public JunimoToddlerPlaymate()
        {
            this.LogTrace($"Junimo toddler playmate cloned");
        }

        public JunimoToddlerPlaymate(Vector2 startingPoint, Child child)
            : base(child.currentLocation, Color.Pink /* TODO */, new AnimatedSprite(@"Characters\Junimo", 0, 16, 16), startingPoint, 2, I("Junimo"))
        {
            this.Scale = 0.6f; // regular ones are .75
            this.childToPlayWith = child;
            this.LogTrace($"Junimo toddler playmate created to play with {child.Name}");
        }

        public bool TryGoToChild()
        {
            // (base.currentLocation as FarmHouse).getRandomOpenPointInHouse(r, 1, 200);
            if (this.childToPlayWith!.controller is not null)
            {
                // child's on the move - try again another time
                return false;
            }
            
            bool canGo = this.TryGoTo(this.childToPlayWith.TilePoint + new Point(-1,1), this.OnArrivedAtChild, this.GoHome);
            if (!canGo)
            {
                return false;
            }

            this.activity = Activity.GoingToPlay;
            return canGo;
        }

        private void OnArrivedAtChild()
        {
            this.gamesPlayed = 0;

            // TODO: Get the second toddler to show up.
            this.PlayGame();
        }

        private void PlayGame()
        {
            if (this.gamesPlayed == 20 || Game1.timeOfDay > 1200 + 730)
            {
                this.GoHome();
            }
            else
            {
                ++this.gamesPlayed;

                void JumpAround()
                {
                    this.jump();
                    this.childToPlayWith!.doEmote(heartEmote);
                    DoToddlerArmFlapAnimation(this.childToPlayWith);
                    this.DoAfterDelay(this.jump, 1500);
                    this.DoAfterDelay(() =>
                    {
                        this.PlayGame();
                    }, 3500);
                }
                void CircleRun()
                {
                    var junimoStartingTile = this.TilePoint;
                    int numAtDestination = 0;
                    void endGame()
                    {
                        ++numAtDestination;
                        if (numAtDestination == 2)
                        {
                            this.DoAfterDelay(this.PlayGame, 1000);
                        }
                    };
                    this.controller = null;
                    this.controller = new PathFindController(new Stack<Point>([
                            junimoStartingTile + new Point(0, 0),
                            junimoStartingTile + new Point(0, -1),
                            junimoStartingTile + new Point(2, -1),
                            junimoStartingTile + new Point(2, 0),
                        ]), this, this.currentLocation);
                    this.controller.endPoint = junimoStartingTile;
                    this.controller.finalFacingDirection = 0 /* up */;
                    this.controller.endBehaviorFunction = (_, _) => endGame();

                    var childStartingTile = this.childToPlayWith!.TilePoint;
                    this.childToPlayWith!.Speed = 5;
                    this.childToPlayWith!.controller = null;
                    this.childToPlayWith!.controller = new PathFindController(new Stack<Point>([
                            childStartingTile + new Point(0, 0),
                            childStartingTile + new Point(1, 0),
                            childStartingTile + new Point(1, 1),
                            childStartingTile + new Point(-1, 1),
                            childStartingTile + new Point(-1, 0),
                        ]), this.childToPlayWith, this.currentLocation);
                    this.childToPlayWith!.controller.finalFacingDirection = 2 /* down */;
                    this.childToPlayWith!.controller.endPoint = childStartingTile;
                    this.childToPlayWith!.controller.endBehaviorFunction = (_, _) => endGame();
                }

                Game1.random.Choose(JumpAround, CircleRun)();
            }
        }

        private bool IsTileBlocked(Point p)
        {
            return !this.currentLocation.hasTileAt(p.X, p.Y, I("Back"))
                || !this.currentLocation.CanItemBePlacedHere(p.ToVector2())
                || ((FarmHouse)this.currentLocation).isTileOnWall(p.X, p.Y)
                || this.currentLocation.getTileIndexAt(p.X, p.Y, I("Back"), I("indoor")) == 0;
        }


        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);

            if (!Game1.IsMasterGame)
            {
                return;
            }

            if (location.farmers.Any() || Game1.currentLocation == location)
            {
                if (this.noFarmersOnLastUpdate)
                {
                    this.noFarmersOnLastUpdate = false;
                    // Farmer just arrived - Reset play.

                    // I think this is guaranteed to be correct because the game puts children in the center of a 3x3 clear area.
                    this.Position = (this.Tile + new Vector2(0, 1)) * 64;
                    this.OnArrivedAtChild();
                }
            }
            else // Nobody here.
            {
                // If it's late or the junimo was going home anyway, just remove them from the scene.
                if (Game1.timeOfDay > 1200 + 700 || this.activity == Activity.GoingHome)
                {
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
                }
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
