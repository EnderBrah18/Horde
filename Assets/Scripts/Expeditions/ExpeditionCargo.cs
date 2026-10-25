using System;
using System.Collections.Generic;

// Temporary load, measured in resource units. Deposited only on extraction.
public sealed class ExpeditionCargo
{
    readonly Dictionary<ResourceData, int> items = new Dictionary<ResourceData, int>();
    public IReadOnlyDictionary<ResourceData, int> Items => items;
    public int Capacity { get; }
    public int Count { get; private set; }
    public bool Delivered { get; private set; }

    public ExpeditionCargo(int capacity) { Capacity = Math.Max(1, capacity); }

    public int Collect(ResourceData resource, int requested)
    {
        if (Delivered || resource == null || requested <= 0) return 0;
        int accepted = Math.Min(requested, Capacity - Count);
        if (accepted == 0) return 0;
        items.TryGetValue(resource, out int stored);
        items[resource] = stored + accepted;
        Count += accepted;
        return accepted;
    }

    public bool TryDeliver(Wallet wallet)
    {
        if (Delivered || wallet == null) return false;
        var entries = new List<ResourceAmount>();
        foreach (var item in items) entries.Add(new ResourceAmount(item.Key, item.Value));
        // Set before notifications: an observer cannot trigger a second deposit.
        Delivered = true;
        if (!wallet.TryDeposit(entries)) { Delivered = false; return false; }
        items.Clear();
        Count = 0;
        return true;
    }
}
