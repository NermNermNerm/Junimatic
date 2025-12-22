using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public abstract class JunimoPlaymateBase : JunimoBase
    {
        private bool noFarmersOnLastUpdate = false;
        protected readonly int timeToGoHome;

        private readonly Dictionary<Child, Point> childParkedTiles = new();
        private readonly Dictionary<Child, PathFindController> childControllers = new();

        private readonly NetString namesOfChildrenToPlayWith = new NetString();

        protected enum Activity { GoingToPlay, Playing, GoingHome };
        protected Activity activity;

        protected JunimoPlaymateBase() {}

        protected JunimoPlaymateBase(Vector2 startingPoint, IReadOnlyList<Child> children)
            : base(children[0].currentLocation, JunimoPlaymateColor, new AnimatedSprite(@"Characters\Junimo", 0, 16, 16), startingPoint, 2, I("NPC_Junimo_ToddlerPlaymate"))
        {
            this.Scale = 0.6f; // regular ones are .75
            this.timeToGoHome = Math.Min(Game1.timeOfDay + 200, 1200 + 640); // Play for 2 hours or until 6:40pm.  Child.tenMinuteUpdate sends toddlers to bed at 7pm.
            this.namesOfChildrenToPlayWith.Value = string.Join('\n', children.Select(c => c.Name));
        }

        protected override void initNetFields()
        {
            base.initNetFields();
            this.NetFields.AddField(this.namesOfChildrenToPlayWith, nameof(this.namesOfChildrenToPlayWith));
        }

        private IReadOnlyList<Child>? calculatedChildrenToPlayWith = null;

        protected IReadOnlyList<Child> childrenToPlayWith
        {
            get
            {
                this.calculatedChildrenToPlayWith ??= this.namesOfChildrenToPlayWith.Value.Split('\n')
                        .Select(n => Game1.getCharacterFromName(n, mustBeVillager: false)).Cast<Child>().ToList();

                return this.calculatedChildrenToPlayWith;
            }
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


        /// <summary>
        ///   Called when the farmer leaves the farmhouse.  Implementations should remove all
        ///   game-related props.  This method is only called for the master player.

        /// </summary>
        protected virtual void OnFarmersLeftFarmhouse()
        {
            this.CancelAllDelayedActions();
        }

        /// <summary>
        ///   Called when to resume a playdate that was suspended because the farmers left the farmhouse.
        /// This method is only called for the master player.
        /// </summary>
        protected virtual void OnFarmerEnteredFarmhouse()
        { }

        /// <summary>
        ///    Called when a child or the Junimo can't reach their target.  This method is only called for the master player.
        /// </summary>
        protected virtual void OnCharacterIsStuck()
        {
            this.CancelAllDelayedActions();
        }

        /// <summary>
        ///   Called when a playdate was ongoing, the farmer left the farmhouse, and now it's past bedtime.
        ///   Also called when the player leaves the farmhouse when the Junimo was on its way home.
        ///   This method is only called for the master player.
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
                this.OnCharacterIsStuck();
            }
        }


        protected abstract void PlayGame();
    }
}
