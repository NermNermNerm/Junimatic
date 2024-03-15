using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Characters;

namespace NermNermNerm.Junimatic
{
    public class EventJunimo : Junimo
    {
        // Random bright colors.
        private static readonly Color[] colors = new Color[] { Color.AliceBlue, Color.Chartreuse, Color.Cornsilk, Color.DarkSeaGreen, Color.ForestGreen, Color.Fuchsia, Color.OrangeRed, Color.HotPink, Color.Violet };

        private Vector2 starting;
        private Vector2 targetVector;
        private double? firstUpdateTime;
        private double startDelay;
        private const double timeToGetToTarget = 1000;
        private const double maxStartingDelay = 1000;

        public EventJunimo(Vector2 starting, Vector2 targetVector)
            : base(starting*64f + new Vector2(0, 5), -1, temporary: true)
        {
            this.SetColor(colors[Game1.random.Next(colors.Length)]);

            this.starting = this.Position;
            this.startDelay = new Random().NextDouble() * maxStartingDelay;
            this.targetVector = targetVector*64f;
        }

        private void SetColor(Color color)
        {
            var colorField = typeof(Junimo).GetField("color", BindingFlags.NonPublic | BindingFlags.Instance);
            ((NetColor)colorField!.GetValue(this)!).Value = color;
        }

        public override void update(GameTime gameTime, GameLocation location)
        {
            if (this.firstUpdateTime is null)
            {
                this.firstUpdateTime = gameTime.TotalGameTime.TotalMilliseconds;
            }
            else
            {
                double msSinceStart = gameTime.TotalGameTime.TotalMilliseconds - this.firstUpdateTime.Value - this.startDelay;
                float progressAsFraction = (float)Math.Max(0, Math.Min(1, msSinceStart/timeToGetToTarget));
                this.Position = this.starting + this.targetVector * progressAsFraction;
                Debug.WriteLine($"Position= {this.Position}");
            }

            base.update(gameTime, location);
        }

        public void GoBack()
        {
            this.firstUpdateTime = null;
            this.starting = this.Position;
            this.targetVector = new Vector2(0,0) - this.targetVector;
        }
    }
}
