using System;
using System.Collections.Generic;

// Estado da partida: cada ResourceData identifica um saldo independente.
public class Wallet
{
    readonly Dictionary<ResourceData, int> balances = new Dictionary<ResourceData, int>();
    public IReadOnlyDictionary<ResourceData, int> Balances => balances;
    public event Action<ResourceData, int> Changed;

    public int Wood { get => Get(DefaultResources.Wood); set => Set(DefaultResources.Wood, value); }
    public int Crystals { get => Get(DefaultResources.Crystals); set => Set(DefaultResources.Crystals, value); }

    public int Get(ResourceData resource) => resource != null && balances.TryGetValue(resource, out int amount) ? amount : 0;

    public void Set(ResourceData resource, int amount)
    {
        if (resource == null) throw new ArgumentNullException(nameof(resource));
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Get(resource) == amount) return;
        balances[resource] = amount;
        Changed?.Invoke(resource, amount);
    }

    public void Add(ResourceData resource, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Set(resource, checked(Get(resource) + amount));
    }

    public bool TryDeposit(IReadOnlyList<ResourceAmount> entries)
    {
        if (!TryTotal(entries, out var total)) return false;
        foreach (var entry in total)
            if (entry.Value > int.MaxValue - Get(entry.Key)) return false;
        // Commit the entire cargo before any observer can inspect the wallet.
        foreach (var entry in total) balances[entry.Key] = Get(entry.Key) + entry.Value;
        foreach (var entry in total)
            if (entry.Value > 0) Changed?.Invoke(entry.Key, Get(entry.Key));
        return true;
    }

    public bool CanAfford(ResourceData resource, int amount) => resource != null && amount >= 0 && Get(resource) >= amount;
    public bool Spend(ResourceData resource, int amount) => Spend(new[] { new ResourceAmount(resource, amount) });
    public bool CanAfford(int wood, int crystals) => CanAfford(BaseCosts(wood, crystals));
    public bool Spend(int wood, int crystals) => Spend(BaseCosts(wood, crystals));

    static ResourceAmount[] BaseCosts(int wood, int crystals) => new[]
    {
        new ResourceAmount(DefaultResources.Wood, wood), new ResourceAmount(DefaultResources.Crystals, crystals)
    };

    public bool CanAfford(IReadOnlyList<ResourceAmount> costs)
    {
        return TryTotal(costs, out var total) && CanAffordTotal(total);
    }

    public bool Spend(IReadOnlyList<ResourceAmount> costs)
    {
        if (!TryTotal(costs, out var total) || !CanAffordTotal(total)) return false;
        // Commit every balance before notifying observers (HUD, quests, etc.).
        foreach (var entry in total) balances[entry.Key] = Get(entry.Key) - entry.Value;
        foreach (var entry in total)
            if (entry.Value > 0) Changed?.Invoke(entry.Key, Get(entry.Key));
        return true;
    }

    bool CanAffordTotal(Dictionary<ResourceData, int> total)
    {
        foreach (var entry in total)
            if (Get(entry.Key) < entry.Value) return false;
        return true;
    }

    static bool TryTotal(IReadOnlyList<ResourceAmount> costs, out Dictionary<ResourceData, int> total)
    {
        total = new Dictionary<ResourceData, int>();
        if (costs == null) return true;
        foreach (var cost in costs)
        {
            if (cost.resource == null || cost.amount < 0) return false;
            total.TryGetValue(cost.resource, out int existing);
            if (cost.amount > int.MaxValue - existing) return false;
            total[cost.resource] = existing + cost.amount;
        }
        return true;
    }
}
