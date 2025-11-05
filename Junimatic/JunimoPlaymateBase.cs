using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public abstract class JunimoPlaymateBase : JunimoBase
    {
        protected readonly IReadOnlyList<Child> childrenToPlayWith = new List<Child>();
        private bool noFarmersOnLastUpdate = false;
        protected readonly int timeToGoHome;

        private readonly Dictionary<Child, Point> childParkedTiles = new();
        private readonly Dictionary<Child, PathFindController> childControllers = new();

        protected enum Activity { GoingToPlay, Playing, GoingHome };
        protected Activity activity;

        protected JunimoPlaymateBase() {}

        protected JunimoPlaymateBase(Vector2 startingPoint, IReadOnlyList<Child> children)
            : base(children[0].currentLocation, JunimoPlaymateColor, new AnimatedSprite(@"Characters\Junimo", 0, 16, 16), startingPoint, 2, I("NPC_Junimo_ToddlerPlaymate"))
        {
            this.Scale = 0.6f; // regular ones are .75
            this.childrenToPlayWith = children;
            this.timeToGoHome = Math.Min(Game1.timeOfDay + 200, 1200 + 640); // Play for 2 hours or until 6:40pm.  Child.tenMinuteUpdate sends toddlers to bed at 7pm.
        }

        private static readonly Color JunimoPlaymateColor = Color.Pink; // Should this be configurable somehow?

        protected void BroadcastEmote(int emoteId)
        {
            this.doEmote(emoteId);
            ModEntry.Instance.PlaymateMultiplayerSupport.BroadcastEmote(this, emoteId);
        }

        protected void BroadcastEmote(Character emoter, int emoteId)
        {
            emoter.doEmote(emoteId);
            ModEntry.Instance.PlaymateMultiplayerSupport.BroadcastEmote(emoter, emoteId);
        }

        public bool TryGoToChild()
        {
            var firstChild = this.childrenToPlayWith.First();
            var playPoint = firstChild.controller?.endPoint ?? firstChild.Tile.ToPoint();
            this.LogTrace($"Starting a playdate with {firstChild.Name}");

            return this.TryGoToNewPlayArea(playPoint);
        }

        protected void ParkChild(Child child)
        {
            this.childControllers.Remove(child);
            this.childParkedTiles[child] = child.TilePoint;
        }

        protected void SetChildController(Child child, PathFindController? controller = null)
        {
            if (controller is not null)
            {
                child.controller = controller;
            }
            this.childControllers[child] = child.controller;
            this.childParkedTiles.Remove(child);
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
            this.childParkedTiles.Clear();
            for (int i = 0; i < this.childrenToPlayWith.Count; ++i)
            {
                var child = this.childrenToPlayWith[i];
                this.SetChildController(child, controllers[i]);
            }

            return true;
        }


        /// <summary>
        ///   Make sure children's controllers don't get reset from where they are now.
        /// </summary>
        protected void FixChildControllers()
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


        protected void FindNewSpot()
        {
            if (this is JunimoCribPlaymate)
            {
                this.LogError($"FindNewSpot should not be called for the crib playmate.");
                return;
            }

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

        /// <summary>
        ///   Called when the farmer leaves the farmhouse.  Implementations should remove all
        ///   game-related props.
        /// </summary>
        protected virtual void OnFarmersLeftFarmhouse() { }

        /// <summary>
        ///   Called when to resume a playdate that was suspended because the farmers left the farmhouse.
        /// </summary>
        protected virtual void OnFarmerEnteredFarmhouse()
        {
            this.FindNewSpot();
        }

        /// <summary>
        ///    Called when a child or the Junimo can't reach their target.
        /// </summary>
        protected virtual void OnCharacterIsStuck(NPC stuckCharacter)
        {
            this.CancelAllDelayedActions();
            this.FindNewSpot();
        }

        /// <summary>
        ///   Called when a playdate was ongoing, the farmer left the farmhouse, and now it's past bedtime.
        ///   Also called when the player leaves the farmhouse when the Junimo was on its way home.
        /// </summary>
        protected virtual void OnPastBedtime() {}

        public override void update(GameTime time, GameLocation farmHouse)
        {
            var oldController = this.controller; // this.controller might be set to null by base.update
            base.update(time, farmHouse);

            if (!Game1.IsMasterGame || this.destroy)
            {
                return;
            }

            // Logic for when the farmhouse becomes or is no longer empty.
            if (farmHouse.farmers.Any() || Game1.currentLocation is FarmHouse)
            {
                if (this.noFarmersOnLastUpdate)
                {
                    this.noFarmersOnLastUpdate = false;
                    // Farmer just arrived - Reset play.
                    this.OnFarmerEnteredFarmhouse();
                }
            }
            else // Nobody here.
            {
                // If it's late or the junimo was going home anyway, just remove them from the scene.
                if (Game1.timeOfDay > 1200 + 700 || this.activity == Activity.GoingHome)
                {
                    this.OnPastBedtime();
                    this.CancelAllDelayedActions();
                    farmHouse.characters.Remove(this);
                }
                // Else if it's the first update after the player leaves the farmhouse
                else if (!this.noFarmersOnLastUpdate)
                {
                    this.noFarmersOnLastUpdate = true;

                    // Stop anything that's going on
                    this.controller = null;
                    this.Speed = this.TravelingSpeed;
                    this.childParkedTiles.Clear();
                    this.childControllers.Clear();

                    // And put a stop to any planned activity
                    this.CancelAllDelayedActions();

                    this.OnFarmersLeftFarmhouse();
                }
            }

            foreach (var c in this.childrenToPlayWith)
            {
                if (this.childParkedTiles.TryGetValue(c, out var tileParkedAt) && c.controller is not null && c.TilePoint != c.controller.endPoint)
                {
                    this.LogTrace($"{c.Name} tried to run off - clearing the controller.");
                    c.controller = null;
                    c.Position = tileParkedAt.ToVector2() * 64F;
                }
            }

            NPC? stuckCharacter = null;
            foreach (var c in this.childrenToPlayWith)
            {
                if (this.childControllers.TryGetValue(c, out var assignedController) && c.controller != assignedController)
                {
                    if (c.controller is not null)
                    {
                        this.LogTrace($"Something outside of Junimatic changed {c.Name}'s destination - putting it back to the play destination.");
                        c.controller = assignedController;
                    }
                    else if (/* c.controller is null && */ c.Tile.ToPoint() != assignedController.endPoint)
                    {
                        this.LogInfo($"{c.Name} can't reach the next spot to play - resetting.");
                        stuckCharacter = c;
                    }
                }
            }

            if (this.controller is null && oldController is not null && oldController.endPoint != this.Tile.ToPoint())
            {
                this.LogInfo($"The junimo playmate could not reach its destination - resetting.");
                stuckCharacter = this;
            }

            if (stuckCharacter is not null)
            {
                this.OnCharacterIsStuck(stuckCharacter);
            }
        }


        protected abstract void PlayGame();
    }
}
