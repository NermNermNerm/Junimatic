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
        private Action onChildReachedDestination = () => { };

        private static readonly MethodInfo? setStateMethod  = typeof(Child).GetMethod("setState", BindingFlags.Instance | BindingFlags.NonPublic);

        private Balloon? balloon = null;

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


        private void JumpAroundGame()
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

        private void SynchronizedCrawlGame()
        {
            int[] freeSpacePerDirection = [0,0];

            for (int i = 0; i < 2; ++i)
            {
                int maxFreeSpace = 0;
                for (; maxFreeSpace < 10; ++maxFreeSpace)
                {
                    int x = this.childToPlayWith.TilePoint.X + (maxFreeSpace + 1) * (i * 2 - 1);
                    int y = this.childToPlayWith.TilePoint.Y;
                    if (!this.isPositionOpen(x, y) || !this.isPositionOpen(x, y + 1))
                    {
                        break;
                    }
                }
                freeSpacePerDirection[i] = maxFreeSpace;
            }

            int direction = freeSpacePerDirection[0] > freeSpacePerDirection[1] ?  -1 : 1;
            int spacesToCrawl = freeSpacePerDirection.Max();

            void nextGame() => this.DoAfterDelay(this.PlayGame, 1000);

            if (spacesToCrawl == 0) nextGame();

            bool canGo = this.TryGoTo(this.childToPlayWith.TilePoint + new Point(spacesToCrawl * direction, 1), () =>
            {
                if (this.childCrawlDestination is null) // If this child is done crawling, then the game's over.
                {
                    this.DoAfterDelay(this.PlayGame, 1000);
                }
            } );

            if (!canGo) nextGame();

            this.childCrawlDestination = this.childToPlayWith.TilePoint + new Point(spacesToCrawl * direction, 0);
            this.onChildReachedDestination = () =>
            {
                if (this.controller is null) // Junimo reached its destionation already
                {
                    this.DoAfterDelay(this.PlayGame, 1000);
                }
            };
        }

        private void BalloonRideGame()
        {
            this.balloon = new Balloon(this.currentLocation, 3, this.childToPlayWith.Position, () => { });

            this.currentLocation.instantiateCrittersList(); // <- only does something if the critters list is non-existent.
            this.currentLocation.addCritter(this.balloon); // <- if the critters list doesn't exist, this will do nothing.

            this.DoAfterDelay(() =>
            {
                this.currentLocation.critters.Remove(this.balloon);
                // ModEntry.Instance.PlaymateMultiplayerSupport.BroadcastRemoveBall();
                this.balloon = null;
                this.PlayGame();
            }, 30000);

        }

        protected override void PlayGame()
        {
            if (Game1.timeOfDay >= this.timeToGoHome)
            {
                this.EndPlayDate();
                return;
            }

            if (this.childrenToPlayWith.Any(c => Math.Abs(c.Tile.X - this.Tile.X) > 1 || Math.Abs(c.Tile.Y - this.Tile.Y) > 1))
            {
                this.LogInfo($"The child(ren) and the Junimo got separated.");
                this.OnCharacterIsStuck();
            }
            else
            {
                Game1.random.Choose(/*this.JumpAroundGame, this.SynchronizedCrawlGame, */ this.BalloonRideGame)();
            }
        }

        protected override void OnFarmersLeftFarmhouse()
        {
            this.childCrawlDestination = null;
            base.OnFarmersLeftFarmhouse();
        }

        protected override void OnFarmerEnteredFarmhouse()
        {
            Point junimoSpot = this.childToPlayWith.TilePoint + new Point(0, 1);
            if (this.isPositionOpen(junimoSpot.X, junimoSpot.Y))
            {
                this.Position = new Vector2(junimoSpot.X * 64, junimoSpot.Y * 64);
                this.PlayGame();
            }
            else
            {
                this.GoHome();
            }
        }

        private void EndPlayDate()
        {
            this.BroadcastEmote(sleepEmote);
            this.DoAfterDelay(() => this.BroadcastEmote(this.childToPlayWith, happyEmote), 1500);
            this.DoAfterDelay(this.GoHome, 3000);
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
                    this.onChildReachedDestination();
                }
                else if (this.childCrawlDestination.Value.X > this.childToPlayWith.TilePoint.X // Destination is to the right
                         && this.childToPlayWith.getDirection() != 1) // But not moving that way
                {
                    this.SetChildStateCrawlingRight();
                }
                else if (this.childCrawlDestination.Value.X < this.childToPlayWith.TilePoint.X // Destination is to the left
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
