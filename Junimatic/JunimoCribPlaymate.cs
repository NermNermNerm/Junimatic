using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.Pathfinding;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public class JunimoCribPlaymate : JunimoBase
    {
        private readonly Child? childToPlayWith; // Null when in a multiplayer game

        private int gamesPlayed = 0;
        private bool isWaitingOnParent = false;
        private bool isCatchingUp = false;

        private bool noFarmersOnLastUpdate = false;
        private readonly List<DelayedAction> delayedActions = new List<DelayedAction>();

        private enum Activity { GoingToPlay, Playing, GoingHome };
        private Activity activity;

        public JunimoCribPlaymate()
        {
            this.LogTrace($"Junimo playmate cloned");
        }

        public JunimoCribPlaymate(Vector2 startingPoint, Child child)
            : base(child.currentLocation, Color.Pink /* TODO */, new AnimatedSprite(@"Characters\Junimo", 0, 16, 16), startingPoint, 2, I("Junimo"))
        {
            Child
            this.Scale = 0.6f; // regular ones are .75
            this.childToPlayWith = child;
            this.controller = new PathFindController(this, this.childToPlayWith.currentLocation, this.PlayStartPoint.ToPoint(), 0, this.OnArrivedAtCrib);
            this.LogTrace($"Junimo playmate created to play with {child.Name}");
            this.activity = Activity.GoingToPlay;
        }

        private Vector2 PlayStartPoint => this.childToPlayWith!.Tile + new Vector2(0, 2); // The crib has some funny z-ordering, going a bit farther away from it.

        public JunimoParent? Parent { get; set; } // When escorted, the parent pops out of the hut a second or so after the child

        public bool IsViable => this.controller?.pathToEndPoint is not null;

        private void OnArrivedAtCrib(Character c, GameLocation l)
        {
            this.gamesPlayed = 0;
            this.activity = Activity.Playing;
            this.DoCribGame();
            this.FacingDirection = 0;
            if (this.childToPlayWith!.Age == Child.baby)
            {
                this.DoCribBabyResponses();
            }
        }

        private const int NumAwakeCribBabyGamesToPlay = 20;

        private void DoAfterDelay(Action a, int delayInMs)
        {
            this.delayedActions.Add(DelayedAction.functionAfterDelay(a, delayInMs));
        }

        private void CancelAllDelayedActions()
        {
            foreach (var da in this.delayedActions)
            {
                Game1.delayedActions.Remove(da);
            }

            this.delayedActions.Clear();
        }

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
                        this.DoAfterDelay(advance, 500); // Hang around on the side for half a second, then go back
                    }
                    else if (this.childToPlayWith!.Position.X >= startingPos)
                    {
                        this.childToPlayWith!.Position = new Vector2(startingPos, this.childToPlayWith!.Position.Y);
                        // don't do anything more.
                    }
                    else
                    {
                        this.DoAfterDelay(advance, 1000/64); // Take ~1 second to track from one side to the other.
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
                    this.DoAfterDelay(() => this.childToPlayWith!.jump(2 + Game1.random.Next(1)), i);
                }
                return 2000;
            };

            int DoNothing()
            {
                return 1000;
            };

            int millisecondsToDelay = Game1.random.Choose(MoveAroundCrib, DoEmote, JumpUpAndDown, DoNothing)();
            ++this.gamesPlayed;
            this.DoAfterDelay(this.DoCribBabyResponses, millisecondsToDelay);
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
                    this.FacingDirection = 0;
                    this.doEmote(20);
                    return 3000;
                };

                int CribGameJump()
                {
                    this.FacingDirection = 0;
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
                this.DoAfterDelay(this.DoCribGame, millisecondsToDelay);
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

            if (location.farmers.Any() || Game1.currentLocation == location)
            {
                if (this.noFarmersOnLastUpdate)
                {
                    // Farmer just arrived
                    this.OnArrivedAtCrib(this, location);

                    this.noFarmersOnLastUpdate = false;
                }
            }
            else // Nobody here.
            {
                // If it's late or the junimo was going home anyway, just remove them from the scene.
                if (Game1.timeOfDay > 1200 + 700 || this.activity == Activity.GoingHome)
                {
                    if (this.Parent is not null)
                    {
                        location.characters.Remove(this.Parent);
                    }
                    location.characters.Remove(this);
                    return;
                }

                // Else right after the player leaves...
                if (!this.noFarmersOnLastUpdate)
                {
                    this.noFarmersOnLastUpdate = true;

                    // Reset to the play-starting position
                    this.Position = this.PlayStartPoint*64;
                    this.controller = null;
                    this.Speed = this.TravelingSpeed;
                    this.Parent?.SetByCrib();

                    // And put a stop to any planned activity
                    this.CancelAllDelayedActions();
                }
            }

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
            }
        }

        protected override int TravelingSpeed => this.activity == Activity.GoingHome && !this.isCatchingUp ? 2 : 5;
    }
}
