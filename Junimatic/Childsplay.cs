using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.Locations;

namespace NermNermNerm.Junimatic
{
    internal class Childsplay : ISimpleLog
    {
        private ModEntry mod = null!;

        public void Entry(ModEntry mod)
        {
            this.mod = mod;

            this.mod.Helper.Events.GameLoop.TimeChanged += this.GameLoop_TimeChanged;
            this.mod.Helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;
            this.mod.Helper.Events.Player.Warped += this.Player_Warped;

            // TODO: Comment this out prior to shipping  Or maybe #if debug?
            this.mod.Helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;
        }

        private void Player_Warped(object? sender, WarpedEventArgs e)
        {

            if (e.NewLocation is FarmHouse farmHouse
                && Game1.IsMasterGame
                && this.IsPlaytime
                && Game1.MasterPlayer.getChildren().Any(c => c.Age != Child.crawler) // No games for crawlers right now.
                && !this.IsPlaydateHappening
                && Game1.random.Next(3) == 0)
            {
                // Consider doing a thing where we just plunk all the kids in an open spot so things get started faster.
                this.LaunchJunimoPlaymate();
            }
        }

        private void GameLoop_TimeChanged(object? sender, TimeChangedEventArgs e)
        {
            if (Game1.IsMasterGame
                && this.IsPlaytime
                && Game1.getAllFarmers().Any(f => f.currentLocation is FarmHouse)
                && Game1.MasterPlayer.getChildren().Any(c => c.Age != Child.crawler) // No games for crawlers right now.
                && !this.IsPlaydateHappening
                && Game1.random.Next(3) == 0) // 1:3 chance of happening
            {
                this.LaunchJunimoPlaymate();
            }
        }

        private void GameLoop_DayEnding(object? sender, DayEndingEventArgs e)
        {
            var farmhouse = (FarmHouse)Game1.getFarm().GetMainFarmHouse().GetIndoors();
            farmhouse.characters.RemoveWhere(c => c is JunimoParent || c is JunimoCribPlaymate || c is JunimoToddlerPlaymate);
            farmhouse.critters.RemoveAll(c => c is GameBall);
        }

        private void Input_ButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.O && e.IsDown(SButton.LeftShift))
            {
                if (!Game1.IsMasterGame)
                {
                    this.LogWarning($"Only the master player can start a Junimo playdate");
                }
                else if (Game1.timeOfDay >= 1200 + 700)
                {
                    this.LogWarning($"Can't start playdates after children's bedtime.");
                }
                else if (!Game1.MasterPlayer.getChildren().Any(c => c.Age != Child.crawler))
                {
                    this.LogWarning($"No children are available for playdates - (Crawler playdates are not implemented yet, alas).");
                }
                else if (this.IsPlaydateHappening)
                {
                    this.LogWarning($"Can only do one playdate at a time.");
                }
                else
                {
                    this.LaunchJunimoPlaymate();
                }
            }
        }

        private void LaunchJunimoPlaymate()
        {
            var children = Game1.MasterPlayer.getChildren();
            var child = Game1.random.Choose(children.Where(c => c.Age != Child.crawler).ToArray());
            this.StartPlayDate(child);
        }

        private void StartPlayDate(Child child)
        {
            var farmhouse = (FarmHouse)Game1.getFarm().GetMainFarmHouse().GetIndoors();
            var cribBounds = farmhouse.GetCribBounds();

            if (farmhouse.characters.Any(c => c is JunimoCribPlaymate || c is JunimoToddlerPlaymate))
            {
                this.LogInfo($"Can't start playdate because there's one going on already.");
                return;
            }

            if (cribBounds is null && child.Age != Child.toddler) // Shouldn't happen; but safety first.
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
                    if (child.Age == Child.toddler)
                    {
                        var toddlers = Game1.MasterPlayer.getChildren().Where(c => c.Age == Child.toddler).ToList();
                        var playmate = new JunimoToddlerPlaymate(tile.ToVector2() * 64, toddlers);
                        if (playmate.TryGoToChild())
                        {
                            farmhouse.characters.Add(playmate);
                            return;
                        }
                    }
                    else
                    {
                        var playmate = new JunimoCribPlaymate(tile.ToVector2() * 64, child);
                        if (playmate.TryGoToCrib())
                        {
                            farmhouse.characters.Add(playmate);
                            return;
                        }
                    }
                }
            }
        }



        /// <summary>
        ///   True if there's a Junimo hopping about right now.
        /// </summary>
        private bool IsPlaydateHappening => Game1.getFarm().GetMainFarmHouse().GetIndoors().characters.Any(c => c is JunimoCribPlaymate || c is JunimoToddlerPlaymate);


        private bool IsPlaytime => Game1.timeOfDay < 1200 + 600 // Kids go to bed at 7
                                   && Game1.timeOfDay > 630; // Don't start playdates right at 6, since the player is likely to leave quickly.

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
            => this.mod.WriteToLog(message, level, isOnceOnly);
    }
}
