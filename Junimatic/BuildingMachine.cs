using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace NermNermNerm.Junimatic
{
    public abstract class BuildingMachine
        : GameMachine
    {
        protected BuildingMachine(Building building, Point accessPoint)
            : base(building, accessPoint)
        {
        }

        /// <inheritdoc/>
        /// <remarks>
        ///   Might there be mods that would allow buildings to be moved while the player is present?
        ///   Seems like a rare enough thing to not worry about...
        /// </remarks>
        public override bool IsStillPresent => true;

        public static BuildingMachine? TryCreate(Building building, Point accessPoint)
        {
            if (building is FishPond fishPond)
            {
                return new FishPondMachine(fishPond, accessPoint);
            }
            else if (building is ShippingBin shippingBin)
            {
                return new ShippingBinMachine(shippingBin, accessPoint);
            }
            else
            {
                return null;
            }
        }

        protected Building Building => (Building)base.GameObject;

        public override string ToString()
        {
            return IF($"{this.Building.buildingType} at {this.Building.tileX.Value},{this.Building.tileY.Value}");
        }
    }
}
