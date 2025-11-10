using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
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
            : base(location, Color.MediumOrchid/* TODO */, new AnimatedSprite(@"Characters\Junimo", 0, 16, 16), startingPoint, 2, I("NPC_Junimo_Parent"))
        {
            this.LogTrace($"Junimo parent created");
        }

        public bool TryGoToCrib()
        {
            var crib = ((FarmHouse)this.currentLocation).GetCribBounds()!.Value; // crib is in Tile coordinates
            var target = new Point(crib.X - 1, crib.Bottom /* neither +1 or -1 work in this spot */);
            return this.TryGoTo(target, () => { }, this.GoHome);
        }

        public void SetByCrib()
        {
            // Place the parent in the final location - happens if the player leaves the scene while play is ongoing.
            var crib = ((FarmHouse)(this.currentLocation)).GetCribBounds()!.Value; // crib is in Tile coordinates
            this.Position = new Vector2((crib.X - 1)*64, crib.Bottom*64-32 /* neither +1 or -1 work in this spot */);
            this.FacingDirection = 1;
            this.Sprite.faceDirection(1);
            this.moveUp = this.moveDown = this.moveLeft = this.moveRight = false;
            this.Sprite.ClearAnimation();
            this.controller = null;
        }

        public override void update(GameTime time, GameLocation farmHouse)
        {
            base.update(time, farmHouse);

            if (this.controller is null)
            {
                var crib = ((FarmHouse)farmHouse).GetCribBounds()!.Value; // crib is in Tile coordinates
                if (this.Tile.X == crib.X - 1 && (this.Tile.Y == crib.Bottom || this.Tile.Y == crib.Bottom - 1))
                {
                    float targetYPos = crib.Bottom * 64 - 32;
                    if (this.Position.Y > targetYPos)
                    {
                        this.Position = new Vector2(this.Position.X, this.Position.Y - 1);
                        if (this.Position.Y == targetYPos)
                        {
                            this.FacingDirection = 1;
                            this.Sprite?.faceDirection(1);
                        }
                    }
                }
            }
        }

        protected override int TravelingSpeed => 3; // Parents go at normal speed.
    }
}
