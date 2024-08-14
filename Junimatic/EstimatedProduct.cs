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
    public record EstimatedProduct(string? qiid, int? quality, int maxQuantity = 1);
}
