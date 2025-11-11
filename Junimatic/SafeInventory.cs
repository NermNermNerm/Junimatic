using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Inventories;

namespace NermNermNerm.Junimatic;

public class SafeInventory
    : IEnumerable<Item>
{
    private readonly IInventory rawInventory;

    public SafeInventory(IInventory inventory)
    {
        this.rawInventory = inventory;
    }

    public void Remove(Item item) => this.rawInventory.Remove(item);

    public void Reduce(Item item, int numToTake) => this.rawInventory.Reduce(item, numToTake);

    public IEnumerator<Item> GetEnumerator() => this.rawInventory.Where(i => i is not null).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.rawInventory.Where(i => i is not null).GetEnumerator();
}
