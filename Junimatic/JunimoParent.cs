using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   Creates a Junimo to escort a JunimoPlaymate to a crib with a newborn child - one that only
    ///   sleeps.  It's totally a slave to the JunimoPlaymate.
    /// </summary>
    public class JunimoParent : JunimoBase
    {
        public JunimoParent()
        {
            this.LogTrace($"Junimo parent cloned");
        }

        public JunimoParent(FarmHouse location, Vector2 startingPoint)
            : base(location, Color.MediumOrchid/* TODO */, new AnimatedSprite(@"Characters\Junimo", 0, 16, 16), startingPoint, 2, I("Junimo"))
        {
            var crib = location.GetCribBounds()!.Value; // crib is in Tile coordinates
            var target = new Vector2(crib.X-1, crib.Bottom /* neither +1 or -1 work in this spot */);
            
            this.controller = new PathFindController(this, location, target.ToPoint(), 0, null);
            this.LogTrace($"Junimo parent created");
        }

        public bool IsViable => this.controller?.pathToEndPoint is not null;

        public void GoHome(Point homeTile)
        {
            this.controller = new PathFindController(this, this.currentLocation, homeTile, 0, (_, _) => this.FadeOutJunimo());
        }

        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);

            if (this.controller is null)
            {
                var crib = ((FarmHouse)location).GetCribBounds()!.Value; // crib is in Tile coordinates
            }
        }

        protected override int TravelingSpeed => 3; // Parents go at normal speed.
    }
}
