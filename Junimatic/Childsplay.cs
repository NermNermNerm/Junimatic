using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using xTile.Tiles;

namespace NermNermNerm.Junimatic
{
    internal class Childsplay : ISimpleLog
    {
        private ModEntry mod = null!;

        public void Entry(ModEntry mod)
        {
            this.mod = mod;

            this.mod.Helper.Events.GameLoop.OneSecondUpdateTicked += this.GameLoop_OneSecondUpdateTicked;
            this.mod.Helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;

            this.mod.Helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;
        }

        private void GameLoop_DayEnding(object? sender, DayEndingEventArgs e)
        {
            // TODO: Ensure there aren't any Junimo Playmates still around
        }

        private void Input_ButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button != SButton.O)
            {
                return;
            }

            this.LaunchJunimoPlaymate();
        }



        private void GameLoop_OneSecondUpdateTicked(object? sender, StardewModdingAPI.Events.OneSecondUpdateTickedEventArgs e)
        {
            if (Game1.IsMasterGame
                && Game1.getAllFarmers().Any(f => f.currentLocation is FarmHouse)
                && Game1.MasterPlayer.getChildrenCount() > 0
                && this.IsPlaytime
                && !this.IsAnimationOngoing
                && this.HasFrequencyLimitPassed
                && this.ShouldStartAnimation)
            {
                this.LaunchJunimoPlaymate();
            }
        }

        private void LaunchJunimoPlaymate()
        {
            var farmHouse = (FarmHouse)Game1.currentLocation;
            var junimo = new JunimoPlaymate(farmHouse, new Vector2(17, 15) * 64, new Point(43,23));

            farmHouse.characters.Add(junimo);


            //var children = Game1.MasterPlayer.getChildren();
            //var child = children[1 /* Game1.random.Next(children.Count) */];
            //if (child.isInCrib())
            //{
            //    if (child.isSleeping.Value)
            //    {
            //        this.StartCribVisit(child);
            //    }
            //}
        }

        private void StartCribVisit(Child child)
        {
            var farmhouse = (FarmHouse)Game1.getFarm().GetMainFarmHouse().GetIndoors();
            var cribBounds = farmhouse.GetCribBounds();
            if (cribBounds is null) // Shouldn't happen; but safety first.
            {
                this.LogError($"Tried to start a Junimo Playmate to go to the crib, but the crib can't be found?!");
                return;
            }

            var gameMap = new GameMap(farmhouse);
            foreach (var portal in farmhouse.Objects.Values.Where(o => o.QualifiedItemId == UnlockPortal.JunimoPortalQiid))
            {
                gameMap.GetStartingInfo(portal, out var adjacentTiles, out _);
                foreach (var tile in adjacentTiles)
                {
                    var playmate = new JunimoPlaymate(tile.ToVector2() * 64, child);
                    farmhouse.characters.Add(playmate);
                    playmate.SetupController();
                    if (playmate.IsViable)
                    {
                        return;
                    }
                    farmhouse.characters.Remove(playmate); // Didn't work out.
                }
            }
        }



        /// <summary>
        ///   True if there's a Junimo hopping about right now.
        /// </summary>
        private bool IsAnimationOngoing => false;

        /// <summary>
        ///   True if the Junimo playmate hasn't been seen in a while.
        /// </summary>
        private bool HasFrequencyLimitPassed => false;

        /// <summary>
        ///   True if this seems like a good time to start the Junimo animation.  This doesn't check other factors like
        ///   whether there's already one going, it just looks at random chance and maybe how long the player has been
        ///   in the house.
        /// </summary>
        private bool ShouldStartAnimation => false;

        private bool IsPlaytime => Game1.timeOfDay < 1200 + 700; // Kids go to bed at 8

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
            => this.mod.WriteToLog(message, level, isOnceOnly);
    }
}
