using System;
using System.Reflection;

// Standalone harness: compiles the actual facility and wallet scripts against
// minimal Unity substitutes. It checks lifecycle/economy, not scene rendering.
namespace UnityEngine
{
    public class Object
    {
        public static VillageFacility[] Facilities = Array.Empty<VillageFacility>();
        public static T[] FindObjectsByType<T>() => Facilities as T[];
        public static int Destroyed;
        public static void Destroy(Object value) { Destroyed++; }
    }
    public class ScriptableObject : Object {}
    public class Sprite : Object {}
    public class GameObject : Object {}
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; }
    }
    public struct Color { public Color(float r, float g, float b, float a) {} }
    public struct Bounds
    {
        public Vector3 min;
        public Bounds(Vector3 center, Vector3 size) { min = center; }
    }
    public class Transform
    {
        public Vector3 position;
        public Vector3 localScale = new Vector3(1,1,1);
        public Vector3 lossyScale => localScale;
    }
    public class MaterialPropertyBlock { public void SetColor(string name, Color color) {} }
    public class Renderer : Object
    {
        public Bounds bounds;
        public void GetPropertyBlock(MaterialPropertyBlock block) {}
        public void SetPropertyBlock(MaterialPropertyBlock block) {}
    }
    public class MonoBehaviour : Object
    {
        public bool isActiveAndEnabled = true;
        public GameObject gameObject = new GameObject();
        Health backingHealth;
        public Health testHealth => backingHealth ??= this as Health ?? new Health();
        public Transform transform = new Transform();
        public T GetComponent<T>() where T : class => typeof(T) == typeof(Health) ? testHealth as T : typeof(T) == typeof(Game) ? Game.I as T : null;
    }
    public sealed class DisallowMultipleComponent : Attribute {}
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) {} }
    public sealed class SerializeField : Attribute {}
    public sealed class MinAttribute : Attribute { public MinAttribute(float value) {} }
    public sealed class RangeAttribute : Attribute { public RangeAttribute(float min, float max) {} }
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string value) {} }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string value) {} }
    public sealed class TextAreaAttribute : Attribute {}
    public sealed class ContextMenu : Attribute { public ContextMenu(string value) {} }
    public sealed class CreateAssetMenu : Attribute { public string menuName, fileName; }
    public static class Application { public static bool isPlaying = true; }
    public static class Debug { public static void Log(object value, Object context) {} }
    public static class Mathf
    {
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
    }
}
public sealed class ResourceData { public string id; }
public static class DefaultResources
{
    public static readonly ResourceData Wood = new ResourceData { id = "wood" };
    public static readonly ResourceData Crystals = new ResourceData { id = "mana_crystal" };
}
public sealed class BuildingData { public ResourceAmount[] costs; }
public struct Vector2Int {}
public sealed class GridWorld
{
    public static GridWorld I = new GridWorld();
    public System.Collections.Generic.HashSet<Structure> Blocked = new System.Collections.Generic.HashSet<Structure>();
    public void Clear(Structure structure) { Blocked.Remove(structure); }
    public void SetFootprint(Structure structure, UnityEngine.Bounds bounds) { Blocked.Add(structure); }
    public void Recalc() {}
}
public enum GameplayEventType { DawnReached, NightStarted, FacilityUnlocked, FacilityUpgraded, FacilitiesProduced, FacilityBroken, FacilityRepaired }
public struct GameplayEvent { public GameplayEventType type; }
public sealed class Game
{
    public T GetComponent<T>() where T : class => TestPopulation as T;
    public VillagePopulation TestPopulation = new VillagePopulation();
    public static Game I;
    public bool lost, night;
    public bool IsConstructionMode => !night && !lost;
    public int round;
    public Wallet wallet = new Wallet();
    public event Action<GameplayEvent> GameplayEventRaised;
    public void RaiseGameplayEvent(GameplayEventType type, UnityEngine.GameObject subject = null, float value = 0f, string details = null)
        => GameplayEventRaised?.Invoke(new GameplayEvent { type = type });
}
public sealed class VillagePopulation
{
    public int workers = 2;
    public int CountWorkers(VillageFacility facility) => workers;
}

static class FacilityTests
{
    static int checks;
    static readonly ResourceData Stone = new ResourceData { id = "stone" };
    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL: " + label);
        checks++;
    }
    static void Invoke(object instance, string method)
        => instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);
    static VillageFacility Create(bool unlocked = false)
    {
        var facility = new VillageFacility
        {
            startUnlocked = unlocked,
            data = new VillageFacilityData
            {
                levels = new[]
                {
                    new VillageFacilityLevel
                    {
                        costs = new[] { new ResourceAmount(DefaultResources.Wood, 5) },
                        productionPerNight = new[] { new ResourceAmount(Stone, 3), new ResourceAmount(Stone, 2) },
                        repairCosts = new[] { new ResourceAmount(DefaultResources.Wood, 2) },
                        npcCapacity = 2
                    },
                    new VillageFacilityLevel
                    {
                        costs = new[] { new ResourceAmount(DefaultResources.Wood, 10), new ResourceAmount(DefaultResources.Crystals, 2) },
                        productionPerNight = new[] { new ResourceAmount(Stone, 9) },
                        repairCosts = new[] { new ResourceAmount(DefaultResources.Wood, 3) },
                        npcCapacity = 4
                    }
                }
            }
        };
        Invoke(facility, "Awake");
        facility.BindStructure(new Structure { health = facility.testHealth });
        return facility;
    }
    static void Main()
    {
        Game.I = new Game();
        var dawnHealth = new Health { max = 90f, current = 12f };
        Check(dawnHealth.RestoreFull() && dawnHealth.current == 90f, "dawn restores existing maximum health");
        dawnHealth.current = 0f;
        Check(!dawnHealth.RestoreFull() && dawnHealth.current == 0f, "restoration never revives defeated actors");
        var placed = new Structure { placementRound=0, investedCosts=new[]{new ResourceAmount(DefaultResources.Wood,17)} };
        Check(placed.TryGetRefund(0,out var freshRefund) && freshRefund[0].amount==17, "fresh upgraded structure refunds full accumulated investment");
        Check(placed.TryGetRefund(1,out var usedRefund) && usedRefund[0].amount==12, "first night applies discount to accumulated investment");
        var staffed = Create(true);
        Check(!staffed.TryGetProduction(0, out _), "zero workers suspends production");
        Check(!staffed.TryGetProduction(1, out _), "below minimum suspends production");
        Check(staffed.TryGetProduction(2, out var preview) && preview[Stone] == 5, "minimum workers give base output");
        Check(staffed.TryGetProduction(3, out preview) && preview[Stone] == 5, "partial bonus group gives no bonus");
        Check(staffed.TryGetProduction(4, out preview) && preview[Stone] == 6, "two extra workers add one per distinct resource");
        Check(staffed.TryGetProduction(6, out preview) && preview[Stone] == 7, "four extra workers add two");
        Check(staffed.TryGetProduction(50, out preview) && preview[Stone] == 8, "worker bonus capped at facility maximum");
        staffed.CurrentLevel.extraWorkersPerBonus = 1;
        staffed.CurrentLevel.productionBonusPerGroup = 3;
        Check(staffed.TryGetProduction(4, out preview) && preview[Stone] == 11, "editable group and bonus used");
        staffed.CurrentLevel.productionPerNight = new[] { new ResourceAmount(Stone, int.MaxValue) };
        Check(!staffed.TryGetProduction(4, out _), "bonus overflow rejected");
        staffed.CurrentLevel.productionPerNight = new[] { new ResourceAmount(Stone, 0) };
        Check(staffed.TryGetProduction(4, out preview) && preview[Stone] == 0, "zero output does not create bonus resource");
        var staffingWallet = new Wallet();
        Game.I.TestPopulation.workers = 1;
        var payout = Create(true);
        Check(!payout.TryProduce(1, staffingWallet) && staffingWallet.Get(Stone) == 0, "actual payout blocked by staffing");
        Game.I.TestPopulation.workers = 4;
        Check(payout.TryProduce(1, staffingWallet) && staffingWallet.Get(Stone) == 6, "actual payout includes worker bonus");
        Check(!payout.TryProduce(1, staffingWallet) && staffingWallet.Get(Stone) == 6, "bonus cannot pay same dawn twice");
        Game.I.TestPopulation.workers = 2;
        var wallet = Game.I.wallet;
        var facility = Create();
        Check(!facility.IsUnlocked && !facility.TryProduce(1, wallet), "locked facility cannot produce");
        Check(facility.testHealth.invulnerable && !GridWorld.I.Blocked.Contains(FindBoundStructure(facility)), "abandoned facility invulnerable and passable");
        facility.testHealth.Damage(10000f);
        Check(facility.testHealth.current > 0f, "abandoned facility ignores direct damage");
        Check(!facility.TryUnlock(wallet) && facility.Level == 0, "unaffordable unlock preserves state");
        wallet.Wood = 20; wallet.Crystals = 5;
        Check(facility.TryUnlock(wallet) && wallet.Wood == 15 && facility.Level == 1, "unlock pays first-level cost");
        Check(!facility.testHealth.invulnerable && FindBoundStructure(facility).CanBeAttacked, "restored facility targetable");
        Check(facility.NpcCapacity == 2 && GridWorld.I.Blocked.Contains(FindBoundStructure(facility)), "restored house contributes capacity and blocks movement");
        Check(!facility.TryUnlock(wallet) && wallet.Wood == 15, "duplicate unlock cannot charge again");
        Check(!facility.TryProduce(0, wallet), "no payment at scene startup");
        Check(facility.TryProduce(1, wallet) && wallet.Get(Stone) == 5, "multiple outputs aggregate");
        Check(!facility.TryProduce(1, wallet) && wallet.Get(Stone) == 5, "duplicate dawn cannot pay twice");
        Check(facility.TryUpgrade(wallet) && facility.Level == 2 && wallet.Wood == 5 && wallet.Crystals == 3, "upgrade charges next level");
        Check(!facility.TryProduce(1, wallet), "upgrade cannot repay same dawn");
        Check(facility.TryProduce(2, wallet) && wallet.Get(Stone) == 14, "new level replaces previous output");
        Check(!facility.CanUpgrade && !facility.TryUpgrade(wallet), "maximum level enforced");
        int destroyed = UnityEngine.Object.Destroyed;
        facility.testHealth.Damage(10000f);
        Check(facility.IsBroken && facility.Level == 2 && !facility.IsOperational, "lethal hit breaks facility without losing level");
        Check(UnityEngine.Object.Destroyed == destroyed && facility.NpcCapacity == 0, "essential object survives and capacity removed");
        Check(!FindBoundStructure(facility).CanBeAttacked && !GridWorld.I.Blocked.Contains(FindBoundStructure(facility)), "broken facility untargetable and passable");
        Check(!facility.TryProduce(3, wallet), "broken facility does not produce");
        wallet.Wood = 0;
        Check(!facility.TryRepair(wallet) && facility.IsBroken, "repair requires resources");
        wallet.Wood = 5;
        Check(facility.TryRepair(wallet) && wallet.Wood == 2 && facility.Level == 2 && facility.NpcCapacity == 4, "repair restores same level and capacity");
        Check(facility.testHealth.current == facility.data.maxHealth && FindBoundStructure(facility).CanBeAttacked, "repair restores health and targetability");
        facility.testHealth.current = 0f;
        Check(!facility.TryProduce(3, wallet), "destroyed facility cannot produce");
        facility.testHealth.current = 100f; facility.isActiveAndEnabled = false;
        Invoke(facility, "OnDisable");
        Check(!facility.TryProduce(3, wallet), "disabled facility cannot produce");
        Check(!GridWorld.I.Blocked.Contains(FindBoundStructure(facility)), "disabling clears navigation obstacle");
        facility.isActiveAndEnabled = true;
        Invoke(facility, "OnEnable");
        Check(GridWorld.I.Blocked.Contains(FindBoundStructure(facility)), "reenabling restores navigation obstacle");
        Check(facility.TryProduce(3, wallet), "reactivation allows next payment");
        var initial = Create(true);
        Check(initial.IsUnlocked && wallet.Wood == 2, "start-unlocked does not charge wallet");
        var invalid = Create(true);
        invalid.data.levels[0].productionPerNight = new[] { new ResourceAmount(Stone, 5), new ResourceAmount(null, 1) };
        int balance = wallet.Get(Stone);
        Check(!invalid.TryProduce(4, wallet) && wallet.Get(Stone) == balance, "invalid outputs do not partially pay");

        Game.I = new Game();
        var producer = Create(true);
        UnityEngine.Object.Facilities = new[] { producer };
        var manager = new VillageProduction();
        Invoke(manager, "OnEnable");
        Game.I.round = 1; Game.I.night = true;
        Game.I.RaiseGameplayEvent(GameplayEventType.NightStarted);
        Game.I.RaiseGameplayEvent(GameplayEventType.DawnReached);
        Check(Game.I.wallet.Get(Stone) == 0, "no payment during night");
        Game.I.night = false; Game.I.lost = true;
        Game.I.RaiseGameplayEvent(GameplayEventType.DawnReached);
        Check(Game.I.wallet.Get(Stone) == 0, "defeat never pays");
        Game.I.lost = false;
        Game.I.RaiseGameplayEvent(GameplayEventType.DawnReached);
        Check(Game.I.wallet.Get(Stone) == 5, "successful dawn delivers production");
        Game.I.RaiseGameplayEvent(GameplayEventType.DawnReached);
        var duplicate = new VillageProduction(); Invoke(duplicate, "OnEnable");
        Game.I.RaiseGameplayEvent(GameplayEventType.DawnReached);
        Check(Game.I.wallet.Get(Stone) == 5, "duplicate events/managers cannot double payment");
        Game.I.round = 2; Game.I.RaiseGameplayEvent(GameplayEventType.DawnReached);
        Check(Game.I.wallet.Get(Stone) == 10, "next successful night pays once");
        Invoke(manager, "OnDisable"); Invoke(duplicate, "OnDisable");
        Game.I.round = 3; Game.I.RaiseGameplayEvent(GameplayEventType.DawnReached);
        Check(Game.I.wallet.Get(Stone) == 10, "disabled managers unsubscribe");
        var wallHealth = new Health();
        var permanent = new Structure { health = wallHealth, indestructible = true };
        wallHealth.invulnerable = true;
        Check(permanent.BlocksMovement && !permanent.CanBeAttacked, "permanent wall blocks but is not a target");
        wallHealth.Damage(10000f);
        Check(wallHealth.current == 100f, "permanent wall resists direct damage");
        permanent.blocksMovement = false;
        Check(!permanent.BlocksMovement && !permanent.CanBeAttacked, "wall debris is passable and ignored");
        var normalHealth = new Health();
        int beforeDestroyed = UnityEngine.Object.Destroyed;
        normalHealth.Damage(10000f);
        Check(UnityEngine.Object.Destroyed == beforeDestroyed + 1 && normalHealth.current == 0f,
            "ordinary structures and actors retain normal destruction");
        var commonHouse = Create(true);
        commonHouse.data.kind = VillageFacilityKind.Housing;
        var essentialHouse = Create(true);
        essentialHouse.data.kind = VillageFacilityKind.EssentialHousing;
        var service = Create(true);
        service.data.kind = VillageFacilityKind.Service;
        UnityEngine.Object.Facilities = new[] { commonHouse, essentialHouse, service };
        Check(commonHouse.PopulationCapacity == 2 && commonHouse.EssentialNpcCapacity == 0, "common housing never grants named NPC slots");
        Check(essentialHouse.PopulationCapacity == 0 && essentialHouse.EssentialNpcCapacity == 2, "named housing never grants population slots");
        Check(manager.PopulationCapacity == 2 && manager.EssentialNpcCapacity == 2 && manager.HousingCapacity == 2,
            "separate totals ignore service building capacity");
        Game.I.wallet.Wood = 10; Game.I.wallet.Crystals = 2;
        Check(essentialHouse.TryUpgrade(Game.I.wallet) && manager.EssentialNpcCapacity == 4 && manager.PopulationCapacity == 2,
            "essential housing upgrade does not increase common population");
        essentialHouse.testHealth.Damage(10000f);
        Check(manager.EssentialNpcCapacity == 0 && manager.PopulationCapacity == 2, "broken essential residence only removes named slots");
        Game.I.wallet.Wood = 3;
        Check(essentialHouse.TryRepair(Game.I.wallet) && manager.EssentialNpcCapacity == 4,
            "repair restores named slots at upgraded level");
        Console.WriteLine("PASS: " + checks + " facility checks");
    }

    static Structure FindBoundStructure(VillageFacility facility)
        => (Structure)typeof(VillageFacility).GetField("structure", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(facility);
}
