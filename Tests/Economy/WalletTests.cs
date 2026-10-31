using System;

// Test harness outside Assets: asset identity and Inspector attributes need no Unity runtime.
namespace UnityEngine { public sealed class MinAttribute : Attribute { public MinAttribute(float value) {} } }
public sealed class ResourceData { public string id; }
public static class DefaultResources
{
    public static readonly ResourceData Wood = new ResourceData { id = "wood" };
    public static readonly ResourceData Crystals = new ResourceData { id = "mana_crystal" };
}

static class WalletTests
{
    static int checks;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
    }
    static void Throws<T>(Action action, string name) where T : Exception
    {
        try { action(); } catch (T) { checks++; return; }
        throw new Exception("FAIL: " + name);
    }
    static void Main()
    {
        var wood = DefaultResources.Wood;
        var crystal = DefaultResources.Crystals;
        var food = new ResourceData { id = "food" };
        var baseCost = new[] { new ResourceAmount(wood,5) };
        var upgradeCost = new[] { new ResourceAmount(wood,12), new ResourceAmount(crystal,2) };
        Check(BuildingInvestment.TryCombine(baseCost,upgradeCost,out var paid), "combine original cost and upgrade");
        Check(Array.Find(paid,e=>e.resource==wood).amount==17 && Array.Find(paid,e=>e.resource==crystal).amount==2,
            "investment includes all resources actually paid");
        Check(BuildingInvestment.TryRefund(paid,100,out var full) && Array.Find(full,e=>e.resource==wood).amount==17,
            "same-day full refund includes base cost");
        Check(BuildingInvestment.TryRefund(paid,70,out var used) && Array.Find(used,e=>e.resource==wood).amount==12 &&
            Array.Find(used,e=>e.resource==crystal).amount==1, "used upgraded building receives seventy percent of total");
        Check(BuildingInvestment.TryCombine(paid,new[]{new ResourceAmount(food,3)},out var secondUpgrade) &&
            Array.Find(secondUpgrade,e=>e.resource==wood).amount==17 && Array.Find(secondUpgrade,e=>e.resource==food).amount==3,
            "multiple upgrades preserve prior investment");
        baseCost[0].amount=99;
        Check(Array.Find(paid,e=>e.resource==wood).amount==17, "investment snapshot unaffected by original cost changes");
        Check(!BuildingInvestment.TryCombine(paid,new[]{new ResourceAmount(null,1)},out _), "invalid upgrade costs rejected");
        Check(!BuildingInvestment.TryCombine(new[]{new ResourceAmount(wood,int.MaxValue)},new[]{new ResourceAmount(wood,1)},out _),
            "investment accumulation rejects overflow");
        Check(!BuildingInvestment.TryRefund(paid,101,out _), "invalid refund rate rejected");
        var wallet = new Wallet();
        Check(wallet.Get(food) == 0, "new resources start empty");
        wallet.Add(food, 9);
        Check(wallet.Get(food) == 9, "arbitrary asset supported");
        Check(new Wallet().Get(food) == 0, "wallets have independent state");
        wallet.Wood = 10; wallet.Crystals = 3;
        var repeated = new[] { new ResourceAmount(wood, 7), new ResourceAmount(wood, 4) };
        Check(!wallet.CanAfford(repeated), "duplicate costs aggregated");
        Check(!wallet.Spend(repeated) && wallet.Wood == 10, "duplicate costs cannot overspend");
        var expensive = new[] { new ResourceAmount(wood, 2), new ResourceAmount(crystal, 4) };
        Check(!wallet.Spend(expensive) && wallet.Wood == 10 && wallet.Crystals == 3, "failed payment is atomic");
        int notifications = 0;
        wallet.Changed += (resource, balance) =>
        {
            notifications++;
            Check(wallet.Wood == 5 && wallet.Crystals == 1, "observers see completed payment");
        };
        Check(wallet.Spend(new[] { new ResourceAmount(wood, 2), new ResourceAmount(wood, 3), new ResourceAmount(crystal, 2) }), "multi-resource payment");
        Check(notifications == 2, "one notification per changed resource");
        Check(!wallet.Spend(wood, -1) && wallet.Wood == 5, "negative cost refused");
        Check(!wallet.Spend(new[] { new ResourceAmount(null, 1) }), "unassigned asset refused");
        Check(wallet.Spend(Array.Empty<ResourceAmount>()), "empty costs are free");
        Check(!wallet.Spend(new[] { new ResourceAmount(food, int.MaxValue), new ResourceAmount(food, 1) }), "cost overflow refused");
        Throws<ArgumentOutOfRangeException>(() => wallet.Add(food, -1), "negative grant refused");
        Throws<ArgumentNullException>(() => wallet.Add(null, 1), "null resource grant refused");
        var rich = new Wallet();
        rich.Add(food, int.MaxValue);
        Throws<OverflowException>(() => rich.Add(food, 1), "balance overflow refused");
        Check(rich.Get(food) == int.MaxValue, "overflow preserves balance");
        Check(wallet.Get(food) == 9, "unrelated resources preserved");
        Console.WriteLine("PASS: " + checks + " wallet checks");
    }
}
