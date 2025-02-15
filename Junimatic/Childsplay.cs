using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;

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

            // TODO: Comment this out prior to shipping  Or maybe #if debug?
            this.mod.Helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;
        }

        private void GameLoop_DayEnding(object? sender, DayEndingEventArgs e)
        {
            var farmhouse = (FarmHouse)Game1.getFarm().GetMainFarmHouse().GetIndoors();
            var toRemove = farmhouse.characters.OfType<JunimoBase>().Where(c => c is not JunimoShuffler).ToArray();
            farmhouse.characters.RemoveWhere(c => c is JunimoParent || c is JunimoCribPlaymate);
            // Q:  What happens to playmates when we leave the scene?
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
            var children = Game1.MasterPlayer.getChildren();
            var child = children[1 /* Game1.random.Next(children.Count) */];
            if (child.isInCrib())
            {
                this.StartCribVisit(child);
            }
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
                    var playmate = new JunimoCribPlaymate(tile.ToVector2() * 64, child);
                    if (playmate.IsViable)
                    {
                        farmhouse.characters.Add(playmate);
                        if (child.Age == Child.newborn)
                        {
                            DelayedAction.functionAfterDelay(() => {
                                var parentJunimo = new JunimoParent(farmhouse, tile.ToVector2() * 64);
                                farmhouse.characters.Add(parentJunimo);
                                playmate.Parent = parentJunimo;
                            }, 1000);
                        }

                        return;
                    }
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
