using System;
using System.Reflection;
using System.Collections.Generic;

// Standalone tests for the actual population transactions. Unity scene objects,
// NPC movement and facility lifecycle are substitutes, not Play Mode coverage.
namespace UnityEngine
{
    public class Object
    {
        public static VillageFacility[] Facilities = Array.Empty<VillageFacility>();
        public static T[] FindObjectsByType<T>() => Facilities as T[] ?? Array.Empty<T>();
        public static void Destroy(Object value) {}
        public static GameObject Instantiate(GameObject original, Transform parent) => new GameObject();
    }
    public class ScriptableObject : Object {}
    public class Sprite : Object {}
    public class Component : Object
    {
        public Transform transform = new Transform();
        public T GetComponent<T>() where T : class => Game.I as T;
        public T[] GetComponentsInChildren<T>() => Array.Empty<T>();
    }
    public class MonoBehaviour : Component {}
    public class GameObject : Object
    {
        public string name;
        public Transform transform = new Transform();
        public GameObject(string name = "") { this.name = name; }
        public void SetActive(bool active) {}
        public T GetComponent<T>() where T : class => null;
        public T AddComponent<T>() where T : new() => new T();
        public static GameObject CreatePrimitive(PrimitiveType type) => new GameObject();
    }
    public class Transform { public Vector3 position; public void SetParent(Transform parent, bool stay) {} }
    public struct Vector3
    {
        public float x,y,z;
        public static Vector3 one => new Vector3(); public static Vector3 up => new Vector3();
        public float sqrMagnitude => 0;
        public static Vector3 operator +(Vector3 a, Vector3 b) => a;
        public static Vector3 operator -(Vector3 a, Vector3 b) => a;
        public static Vector3 operator *(Vector3 a, float b) => a;
    }
    public struct Vector2Int { public int x,y; public Vector2Int(int x, int y) { this.x=x; this.y=y; } }
    public struct Bounds
    {
        public Vector3 min, max;
        public Bounds(Vector3 center, Vector3 size) { min = max = center; }
        public void Encapsulate(Bounds other) {}
    }
    public struct Color { public Color(float r,float g,float b) {} }
    public class Renderer : Component { public Bounds bounds; public object sharedMaterial; }
    public class LineRenderer : Renderer {}
    public enum PrimitiveType { Capsule }
    public static class Resources { public static T[] LoadAll<T>(string path) => Array.Empty<T>(); }
    public static class Time { public static float time; }
    public static class Mathf { public static int Max(int a,int b) => Math.Max(a,b); }
    public sealed class CreateAssetMenu : Attribute { public string menuName,fileName; }
    public sealed class DisallowMultipleComponent : Attribute {}
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) {} }
    public sealed class MinAttribute : Attribute { public MinAttribute(float value) {} }
    public sealed class TextAreaAttribute : Attribute {}
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string value) {} }
}
public class ResourceData {}
public static class DefaultResources { public static ResourceData Wood = new ResourceData(), Crystals = new ResourceData(); }
public struct ResourceAmount
{
    public ResourceData resource; public int amount;
    public ResourceAmount(ResourceData resource,int amount) { this.resource=resource; this.amount=amount; }
}
public class Game
{
    public static Game I;
    public bool IsExplorationMode = true;
    public bool IsOnExpedition, ExpeditionTransition;
    public Wallet wallet = new Wallet();
    public object litMaterial;
}
public enum VillageFacilityKind { Production, Service, Housing, EssentialHousing }
public class VillageFacilityData { public VillageFacilityKind kind; }
public class VillageFacility : UnityEngine.Component
{
    public VillageFacilityLevel CurrentLevel = new VillageFacilityLevel();
    public bool IsOperational = true;
    public int PopulationCapacity, EssentialNpcCapacity;
    public VillageFacilityData data = new VillageFacilityData();
}
public class VillageFacilityLevel { public int maximumWorkers = 8; }
public class VillageNpc : UnityEngine.Component
{
    public NpcData data;
    public bool IsDead, IsInjured;
    public VillageFacility Residence, Workplace;
    public NpcRoleData Role;
    public bool IsHomeless => Residence == null || !Residence.IsOperational;
    public bool CanWork => !IsDead && !IsInjured && Role != null && Role.Accepts(Workplace);
    public bool CanPerform(NpcRoleData role) => Array.IndexOf(data.allowedRoles,role) >= 0;
    public void SetResidence(VillageFacility home) => Residence=home;
    public void SetAssignment(NpcRoleData role,VillageFacility workplace) { Role=role; Workplace=workplace; }
    public void Recover() => IsInjured=false;
}
public class GridWorld
{
    public static GridWorld I;
    public float cellSize=1;
    public UnityEngine.Vector2Int WorldToCell(UnityEngine.Vector3 position) => default;
    public UnityEngine.Vector3 CellToWorld(UnityEngine.Vector2Int cell) => default;
    public bool InBounds(UnityEngine.Vector2Int cell) => true;
    public bool IsHardBlocked(UnityEngine.Vector2Int cell) => false;
    public object Get(UnityEngine.Vector2Int cell) => null;
    public bool TryOpenRoute(UnityEngine.Vector3 from,UnityEngine.Vector2Int cell,out UnityEngine.Vector3 direction) { direction=default; return true; }
}
static class PopulationTests
{
    static int checks;
    static void Check(bool result,string message) { checks++; if (!result) throw new Exception(message); }
    static void Main()
    {
        Game.I = new Game();
        var population = new VillagePopulation();
        typeof(VillagePopulation).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(population,null);
        var house = new VillageFacility { PopulationCapacity=1 };
        var essentialHome = new VillageFacility { EssentialNpcCapacity=1 };
        var resident = new NpcData();
        var essential = new NpcData { category=NpcCategory.Essential };
        Check(population.CanRecruit(resident,house,out _),"common vacancy");
        Check(!population.CanRecruit(essential,house,out _),"essential cannot use common home");
        Check(!population.CanRecruit(resident,essentialHome,out _),"resident cannot use essential home");
        Game.I.IsExplorationMode=false;
        Check(!population.CanRecruit(resident,house,out _),"recruitment blocked outside exploration");
        Game.I.IsExplorationMode=true;
        resident.recruitmentCosts=new[]{new ResourceAmount(DefaultResources.Crystals,2)};
        Check(!population.CanRecruit(resident,house,out _),"recruitment requires resources");
        Game.I.wallet.Crystals=2;
        Check(population.CanRecruit(resident,house,out _),"affordable recruitment");
        var npc=new VillageNpc {data=resident,Residence=house};
        population.Register(npc); population.Register(npc);
        Check(population.Members.Count==1,"registration is idempotent");
        Check(!population.HasVacancy(house,NpcCategory.Resident),"occupied slot unavailable");
        var named=new VillageNpc {data=essential,Residence=essentialHome};
        population.Register(named);
        var secondHome=new VillageFacility {EssentialNpcCapacity=2};
        Check(!population.CanRecruit(essential,secondHome,out _),"named NPC unique even if unique unchecked");
        var role=new NpcRoleData {workplaceKind=VillageFacilityKind.Service,providesService=true,slotsPerFacility=1};
        var forge=new VillageFacility {data=new VillageFacilityData {kind=VillageFacilityKind.Service}};
        Check(!population.TryAssign(npc,role,forge),"role must be allowed");
        resident.allowedRoles=new[]{role}; essential.allowedRoles=new[]{role};
        Check(population.TryAssign(npc,role,forge),"assignment succeeds");
        Check(!population.TryAssign(named,role,forge),"workplace slots enforced");
        Check(population.HasServiceSpecialist(forge),"active specialist supplies service readiness");
        forge.IsOperational=false;
        Check(!population.HasServiceSpecialist(forge),"broken workplace suspends specialist");
        forge.IsOperational=true; npc.IsInjured=true;
        Check(!population.HasServiceSpecialist(forge),"injured specialist unavailable");
        resident.recoveryCosts=new[]{new ResourceAmount(DefaultResources.Crystals,3)};
        Check(!population.TryRecover(npc),"recovery requires resources");
        Game.I.wallet.Crystals=3;
        Check(population.TryRecover(npc) && Game.I.wallet.Crystals==0,"recovery charges exact cost");
        Game.I.IsExplorationMode=false;
        Check(!population.TryClearAssignment(npc),"assignment removal blocked at night/construction");
        Game.I.IsExplorationMode=true;
        Check(population.TryClearAssignment(npc) && npc.Role==null,"assignment can be removed");
        house.IsOperational=false;
        UnityEngine.Object.Facilities=new[]{house};
        population.ReconcileHousing();
        Check(population.Members.Count==2 && npc.IsHomeless,"house loss keeps residents homeless");
        var newHome=new VillageFacility {PopulationCapacity=1};
        UnityEngine.Object.Facilities=new[]{house,newHome};
        population.ReconcileHousing();
        Check(npc.Residence==newHome,"homeless resident receives compatible vacancy");
        npc.IsDead=true; population.ReconcileHousing();
        Check(population.Members.Count==1 && population.Occupants(newHome)==0,"dead resident frees slot");
        role.requiredFacility=new VillageFacilityData {kind=VillageFacilityKind.Service};
        Check(!role.Accepts(forge),"specific workplace requirement enforced");
        var workerRole = new NpcRoleData { productionWorker=true, workplaceKind=VillageFacilityKind.Production, slotsPerFacility=100 };
        resident.allowedRoles=new[]{workerRole}; essential.allowedRoles=new[]{workerRole};
        var mill=new VillageFacility {data=new VillageFacilityData {kind=VillageFacilityKind.Production}};
        mill.CurrentLevel.maximumWorkers=1;
        var worker=new VillageNpc {data=resident,Residence=newHome}; population.Register(worker);
        Check(population.TryAssign(worker,workerRole,mill),"production worker can be assigned");
        Check(population.CountWorkers(mill)==1,"healthy assigned worker counted");
        Check(!population.TryAssign(named,workerRole,mill),"facility worker limit enforced");
        worker.IsInjured=true;
        Check(population.CountWorkers(mill)==0,"injured worker excluded from output");
        Check(!population.TryAssign(named,workerRole,mill),"injured worker retains workplace reservation");
        worker.IsInjured=false; mill.IsOperational=false;
        Check(population.CountWorkers(mill)==0,"broken facility has no eligible workers");
        mill.IsOperational=true;
        Check(population.CountWorkers(mill)==1,"repair restores worker eligibility");
        Check(population.TryClearAssignment(worker) && population.CountWorkers(mill)==0,"unassignment removes worker contribution");
        Console.WriteLine("PASS: "+checks+" population checks");
    }
}
