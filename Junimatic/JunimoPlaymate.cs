using System;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.Pathfinding;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class JunimoPlaymate : JunimoBase
    {
        private readonly Child? childToPlayWith; // Null when in a multiplayer game

        private int gamesPlayed = 0;
        private bool isWaitingOnParent = false;
        private bool isCatchingUp = false;
        private enum Activity { GoingToPlay, Playing, GoingHome };
        private Activity activity;

        public JunimoPlaymate()
        {
            this.LogTrace($"Junimo playmate cloned");
        }

        public JunimoPlaymate(Vector2 startingPoint, Child child)
            : base(child.currentLocation, Color.Pink /* TODO */, new AnimatedSprite(@"Characters\Junimo", 0, 16, 16), startingPoint, 2, I("Junimo"))
        {
            this.Scale = 0.6f; // regular ones are .75
            this.childToPlayWith = child;
            var playPoint = this.childToPlayWith!.Tile + new Vector2(0, 2); // The crib has some funny z-ordering, going a bit farther away from it.
            this.controller = new PathFindController(this, this.childToPlayWith.currentLocation, playPoint.ToPoint(), 0, this.OnArrivedAtCrib);
            this.LogTrace($"Junimo playmate created to play with {child.Name}");
            this.activity = Activity.GoingToPlay;
        }

        public JunimoParent? Parent { get; set; } // When escorted, the parent pops out of the hut a second or so after the child

        public bool IsViable => this.controller?.pathToEndPoint is not null;

        private void OnArrivedAtCrib(Character c, GameLocation l)
        {
            this.gamesPlayed = 0;
            this.activity = Activity.Playing;
            this.DoCribGame();
            if (this.childToPlayWith!.Age == Child.baby)
            {
                this.DoCribBabyResponses();
            }
        }

        private const int NumAwakeCribBabyGamesToPlay = 20;

        private void DoCribBabyResponses()
        {
            if (this.gamesPlayed >= NumAwakeCribBabyGamesToPlay-1 || Game1.timeOfDay >= 1200 + 740)
            {
                // Baby knocks off when the Junimo is almost done or bedtime is near.
                return;
            }

            int MoveAroundCrib()
            {
                float startingPos = this.childToPlayWith!.Position.X;
                Vector2 movementInterval = new Vector2(-1, 0);
                Action advance = () => { };
                advance = () =>
                {
                    this.childToPlayWith!.Position += movementInterval;
                    if (this.childToPlayWith!.Position.X <= startingPos - 64)
                    {
                        movementInterval = new Vector2(1, 0);
                        DelayedAction.functionAfterDelay(advance, 500); // Hang around on the side for half a second, then go back
                    }
                    else if (this.childToPlayWith!.Position.X >= startingPos)
                    {
                        this.childToPlayWith!.Position = new Vector2(startingPos, this.childToPlayWith!.Position.Y);
                        // don't do anything more.
                    }
                    else
                    {
                        DelayedAction.functionAfterDelay(advance, 1000/64); // Take ~1 second to track from one side to the other.
                    }
                };
                advance();

                return 2500;
            };

            int DoEmote()
            {
                this.childToPlayWith!.doEmote(32);
                return 3000;
            };

            int JumpUpAndDown()
            {
                this.childToPlayWith!.jump(2 + Game1.random.Next(1)); // 8 is the normal jump height
                for (int i = 300; i <= 1800; i += 300)
                {
                    DelayedAction.functionAfterDelay(() => this.childToPlayWith!.jump(2 + Game1.random.Next(1)), i);
                }
                return 2000;
            };

            int DoNothing()
            {
                return 1000;
            };

            int millisecondsToDelay = Game1.random.Choose(MoveAroundCrib, DoEmote, JumpUpAndDown, DoNothing)();
            ++this.gamesPlayed;
            DelayedAction.functionAfterDelay(this.DoCribBabyResponses, millisecondsToDelay);
        }

        private void DoCribGame()
        {
            if (this.gamesPlayed >= NumAwakeCribBabyGamesToPlay // Junimo gets tired and goes home
                || Game1.timeOfDay > 1200+740) // Babies sleep at 8, so knock off before 7:40
            {
                this.GoHome();
            }
            else
            {
                int CribGameSwitchSide()
                {
                    // Move to the right or left side of the crib
                    var startPoint = this.childToPlayWith!.Tile + new Vector2(0, 2);
                    var waypoints = new Vector2[] { new(1, 0), new(1, -1), new(-1, -1), new(-1, 0), new(0, 0) };
                    int current = 0;
                    Action advance = () => { };
                    advance = () =>
                    {
                        if (current < waypoints.Length)
                        {
                            this.controller = null;
                            this.controller = new PathFindController(this, this.childToPlayWith.currentLocation, (startPoint + waypoints[current]).ToPoint(), 0, (_, _) => advance());
                            ++current;
                        }
                        else
                        {
                            this.controller = null;
                        }
                    };
                    advance();

                    return 3000;
                };

                int CribGameEmote()
                {
                    this.doEmote(20);
                    return 3000;
                };

                int CribGameJump()
                {
                    this.jump();
                    return 1000;
                };

                int CribGameMeep()
                {
                    this.Meep();
                    return 1000;
                };

                int millisecondsToDelay = Game1.random.Choose(CribGameSwitchSide, CribGameEmote, CribGameMeep, CribGameJump)();
                ++this.gamesPlayed;
                DelayedAction.functionAfterDelay(this.DoCribGame, millisecondsToDelay);
            }
        }

        private void GoHome()
        {
            this.activity = Activity.GoingHome;

            var gameMap = new GameMap(this.currentLocation);
            foreach (var portal in this.currentLocation.Objects.Values.Where(o => o.QualifiedItemId == UnlockPortal.JunimoPortalQiid).OrderBy(o => Math.Abs(o.TileLocation.X - this.Tile.X) + Math.Abs(o.TileLocation.Y - this.Tile.Y)))
            {
                gameMap.GetStartingInfo(portal, out var adjacentTiles, out _);
                foreach (var tile in adjacentTiles)
                {
                    this.controller = null;
                    this.controller = new PathFindController(this, this.currentLocation, tile, 0, (_,_) => this.FadeOutJunimo());
                    if (this.IsViable)
                    {
                        this.Parent?.GoHome(tile);
                        return;
                    }
                }
            }

            this.LogWarning($"Junimo playmate could not find its way back to a hut!");
            this.FadeOutJunimo();
        }

        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);

            if (this.Parent is not null)
            {
                float distanceToParent = Math.Max(Math.Abs(this.Position.X - this.Parent.Position.X), Math.Abs(this.Position.Y - this.Parent.Position.Y));
                bool isTooFarFromParent = distanceToParent > 64 * 5;
                bool isCloseEnoughToParent = distanceToParent < 64 + 32;
                if (this.activity == Activity.GoingToPlay)
                {
                    if (this.isWaitingOnParent)
                    {
                        if (this.yJumpOffset == 0)
                        {
                            if (isCloseEnoughToParent)
                            {
                                this.isWaitingOnParent = false;
                                this.speed = this.TravelingSpeed;
                                this.Meep();

                            }
                            else
                            {
                                this.jump(5);
                            }
                        }
                    }
                    else if (isTooFarFromParent && !this.isWaitingOnParent)
                    {
                        this.isWaitingOnParent = true;
                        this.speed = 0;
                        this.jump(4);
                    }
                }
                else if (this.activity == Activity.GoingHome)
                {
                    if (isTooFarFromParent)
                    {
                        this.isCatchingUp = true;
                        if ((int)distanceToParent % 64 == 0)
                        {
                            this.Meep();
                        }
                    }
                    else if (this.isCatchingUp && isCloseEnoughToParent)
                    {
                        this.isCatchingUp = false;
                    }
                }

                // Going to crib:
                //
                // If far away from parent
                //   set isWaitingOnParent
                // else if isWaitingOnParent
                //   if is close to parent
                //     clear isWaitingOnParent
                //   else not jumping
                //     jump or meep
                //
                // Returning (parent will be ahead)
                // If close to parent && !isWaitingOnParent
                //   set isWaitingOnParent
                // Else if isWaitingOnParent
                //   Do nothing
                // Else if far from parent
                //   clear isWaitingOnParent (so the child moves fast enough to catch up)
                //   parent.emote !  or meep1/meep
                //   child.jump
            }

        }

        protected override int TravelingSpeed => this.activity == Activity.GoingHome && !this.isCatchingUp ? 2 : 5;
    }
}
