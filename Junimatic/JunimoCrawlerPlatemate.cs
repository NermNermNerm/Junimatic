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
    public class JunimoCrawlerPlaymate : JunimoPlaymateBase
    {
        public JunimoCrawlerPlaymate()
        {
            this.LogTrace($"Junimo crawler playmate cloned");
        }

        public JunimoCrawlerPlaymate(Vector2 startingPoint, Child childToPlayWith)
            : base(startingPoint, [childToPlayWith])
        {
        }

        protected override void PlayGame()
        {
            if (Game1.timeOfDay >= this.timeToGoHome)
            {
                this.GoHome();
            }
            else
            {

                void BalloonGame()
                {
                }
            }
        }

        private void EndPlayDate()
        {
            // Todo, embellish this.
            this.GoHome();
        }

        protected override int TravelingSpeed => 5;
    }
}
