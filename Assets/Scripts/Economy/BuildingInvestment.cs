using System;
using System.Collections.Generic;

// Snapshot actual spending; asset changes must not rewrite a placed building's cost.
public static class BuildingInvestment
{
    public static bool TryCombine(ResourceAmount[] previous, ResourceAmount[] added, out ResourceAmount[] combined)
    {
        combined = Array.Empty<ResourceAmount>();
        var totals = new Dictionary<ResourceData,int>();
        foreach (var costs in new[] { previous, added })
        {
            if (costs == null) continue;
            foreach (var cost in costs)
            {
                if (cost.resource == null || cost.amount < 0) return false;
                totals.TryGetValue(cost.resource, out int stored);
                if (cost.amount > int.MaxValue - stored) return false;
                totals[cost.resource] = stored + cost.amount;
            }
        }
        var result = new List<ResourceAmount>();
        foreach (var cost in totals) result.Add(new ResourceAmount(cost.Key,cost.Value));
        combined = result.ToArray();
        return true;
    }

    public static bool TryRefund(ResourceAmount[] investment, int percent, out ResourceAmount[] refund)
    {
        refund = Array.Empty<ResourceAmount>();
        if (percent < 0 || percent > 100 || !TryCombine(investment, null, out var totals)) return false;
        for (int i = 0; i < totals.Length; i++)
            totals[i].amount = (int)Math.Round((long)totals[i].amount * percent / 100.0, MidpointRounding.ToEven);
        refund = totals;
        return true;
    }
}
