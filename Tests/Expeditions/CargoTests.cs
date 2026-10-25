using System;

// Actual wallet, cargo and node logic, with minimal scene/configuration substitutes.
public class ResourceData {}
public struct ResourceAmount
{
    public ResourceData resource; public int amount;
    public ResourceAmount(ResourceData resource,int amount) { this.resource=resource; this.amount=amount; }
}
public static class DefaultResources { public static ResourceData Wood=new ResourceData(), Crystals=new ResourceData(); }
namespace UnityEngine
{
    public class GameObject { public bool active=true; public void SetActive(bool value) => active=value; }
    public class MonoBehaviour { public GameObject gameObject=new GameObject(); public bool isActiveAndEnabled => gameObject.active; }
    public struct Bounds {}
    public static class Mathf { public static int Min(int a,int b)=>Math.Min(a,b); public static int Max(int a,int b)=>Math.Max(a,b); }
    public sealed class DisallowMultipleComponent : Attribute {}
}
public class ResourceNodeData { public ResourceData resource; public int amountPerInteraction=3; }
public class GridWorld
{
    public int cleared, recalculated;
    public void SetHardBlocked(UnityEngine.Bounds bounds,bool blocked) { if (!blocked) cleared++; }
    public void Recalc() { recalculated++; }
}
static class CargoTests
{
    static int checks;
    static void Check(bool value,string label) { checks++; if(!value) throw new Exception(label); }
    static void Main()
    {
        var wood=DefaultResources.Wood; var iron=new ResourceData();
        var cargo=new ExpeditionCargo(10); var wallet=new Wallet();
        Check(cargo.Collect(null,3)==0,"missing resource rejected");
        Check(cargo.Collect(wood,-1)==0,"negative collection rejected");
        Check(cargo.Collect(wood,7)==7,"collect first resource");
        Check(cargo.Collect(iron,8)==3 && cargo.Count==10,"partial collection respects shared unit capacity");
        Check(cargo.Collect(wood,1)==0,"full cargo cannot collect");
        Check(wallet.Get(wood)==0 && wallet.Get(iron)==0,"collection does not credit village");
        bool committedBeforeNotify=false; bool duplicateBlocked=false;
        wallet.Changed+=(resource,amount)=>
        {
            committedBeforeNotify=wallet.Get(wood)==7 && wallet.Get(iron)==3;
            duplicateBlocked=!cargo.TryDeliver(wallet);
        };
        Check(cargo.TryDeliver(wallet),"extraction deposits load");
        Check(committedBeforeNotify && duplicateBlocked,"atomic deposit and reentrant duplicate blocked");
        Check(wallet.Get(wood)==7 && wallet.Get(iron)==3,"correct credited amounts");
        Check(cargo.Count==0 && cargo.Items.Count==0 && cargo.Delivered,"delivered cargo cleared");
        Check(!cargo.TryDeliver(wallet) && wallet.Get(wood)==7,"second extraction cannot duplicate");
        Check(cargo.Collect(wood,1)==0,"finished session cannot collect");
        var overflow=new ExpeditionCargo(20); var fullWallet=new Wallet();
        fullWallet.Set(iron,int.MaxValue); overflow.Collect(wood,3); overflow.Collect(iron,1);
        Check(!overflow.TryDeliver(fullWallet),"overflow rejects whole deposit");
        Check(fullWallet.Get(wood)==0 && !overflow.Delivered && overflow.Count==4,"failed extraction keeps cargo and wallet intact");
        fullWallet.Set(iron,0);
        Check(overflow.TryDeliver(fullWallet) && fullWallet.Get(wood)==3,"failed extraction can retry");
        var empty=new ExpeditionCargo(0);
        Check(empty.Capacity==1 && empty.TryDeliver(new Wallet()),"empty extraction and minimum capacity supported");
        var node=new ResourceNode(); var grid=new GridWorld(); var small=new ExpeditionCargo(2);
        node.Initialize(new ResourceNodeData {resource=wood},5,grid,default);
        Check(node.Collect(small)==2 && node.Remaining==3,"node stock decreases only by accepted amount");
        Check(node.Collect(small)==0 && node.Remaining==3,"full cargo cannot consume node stock");
        var secondCargo=new ExpeditionCargo(10);
        Check(node.Collect(secondCargo)==3 && node.Remaining==0,"stock exhaustion");
        Check(!node.isActiveAndEnabled && grid.cleared==1 && grid.recalculated==1,"exhausted node disappears and opens navigation once");
        Check(node.Collect(secondCargo)==0 && secondCargo.Count==3,"depleted node cannot pay twice");
        var invalid=new Wallet();
        Check(!invalid.TryDeposit(new[]{new ResourceAmount(wood,2),new ResourceAmount(null,1)}) && invalid.Get(wood)==0,"invalid deposit has no partial credit");
        var observerCargo=new ExpeditionCargo(10); observerCargo.Collect(wood,2); observerCargo.Collect(iron,3);
        var observerWallet=new Wallet(); observerWallet.Changed+=(r,n)=>throw new Exception("test observer failure");
        try { observerCargo.TryDeliver(observerWallet); } catch(Exception) {}
        Check(observerWallet.Get(wood)==2 && observerWallet.Get(iron)==3 && !observerCargo.TryDeliver(observerWallet),"observer failure cannot partially commit or duplicate cargo");
        Console.WriteLine("PASS: "+checks+" cargo/node checks");
    }
}
