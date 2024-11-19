using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace NermNermNerm.Junimatic
{
    /// <summary>
    ///   An estimation of the production for a machine.
    /// </summary>
    /// <param name="qiid">
    ///   The qualified item id the machine is expecting to produce.  If null, it means
    ///   it doesn't know what will be produced.  This will cause <see cref="GameMachine.CanHoldProducts"/>
    ///   to ensure that there's a completely empty slot in the chest for its product.
    /// </param>
    /// <param name="quality">
    ///   If not null, the quality of the item that will be produced.  This parameter is meaningless
    ///   if <paramref name="qiid"/> is null.
    /// </param>
    /// <param name="maxQuantity">
    ///   The maximum number of items that will be produced.
    /// </param>
    /// <param name="color">
    ///   If the product is tinted and the color is known, this is the color (tint) that it will stack with.
    ///   There is no way to specify that the object is known not to be colored.  However, when this object
    ///   is compared with an uncolored item, this will be ignored
    /// </param>
    public record EstimatedProduct(string? qiid, int? quality, Color? color, int maxQuantity = 1)
    {
        public bool CanStackWith(StardewValley.Item item)
        {
            if (item.QualifiedItemId != this.qiid)
            {
                return false;
            }

            if (this.quality is null || item.Quality != this.quality.Value)
            {
                return false;
            }

            if (item is ColoredObject colored)
            {
                if (this.color is null || this.color.Value != colored.color.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
