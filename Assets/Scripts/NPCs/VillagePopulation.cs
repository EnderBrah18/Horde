using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Game))]
public sealed class VillagePopulation : MonoBehaviour
{
    public NpcData[] recruitmentCatalog = new NpcData[0];
    readonly List<VillageNpc> members = new List<VillageNpc>();
    public IReadOnlyList<VillageNpc> Members => members;
    Game game;
    float nextHousingCheck;

    void Awake()
    {
        game = GetComponent<Game>();
        if (recruitmentCatalog == null || recruitmentCatalog.Length == 0)
            recruitmentCatalog = Resources.LoadAll<NpcData>("NPCs");
    }

    void Start()
    {
        foreach (var npc in FindObjectsByType<VillageNpc>()) Register(npc);
        ReconcileHousing();
    }

    void Update()
    {
        if (game != null && (game.IsOnExpedition || game.ExpeditionTransition)) return;
        if (Time.time < nextHousingCheck) return;
        nextHousingCheck = Time.time + 0.5f;
        ReconcileHousing();
    }

    public void Register(VillageNpc npc)
    {
        if (npc != null && npc.data != null && !npc.IsDead && !members.Contains(npc)) members.Add(npc);
    }

    public void Remove(VillageNpc npc) => members.Remove(npc);

    public int Occupants(VillageFacility home)
    {
        int count = 0;
        foreach (var npc in members) if (npc != null && !npc.IsDead && npc.Residence == home) count++;
        return count;
    }

    public bool HasVacancy(VillageFacility home, NpcCategory category)
    {
        if (home == null || !home.IsOperational) return false;
        int capacity = category == NpcCategory.Essential ? home.EssentialNpcCapacity : home.PopulationCapacity;
        return Occupants(home) < capacity;
    }

    public bool CanRecruit(NpcData data, VillageFacility home, out string reason)
    {
        reason = "";
        if (game == null || !game.IsExplorationMode) reason = "Recrute durante a exploração, de dia.";
        else if (data == null) reason = "NPC sem dados.";
        else if (!HasVacancy(home, data.category)) reason = "Não há vaga nesta residência para este NPC.";
        else if ((data.unique || data.category == NpcCategory.Essential) && members.Exists(n => n != null && !n.IsDead && n.data == data))
            reason = "Este personagem já está na vila.";
        else if (!game.wallet.CanAfford(data.recruitmentCosts)) reason = "Recursos insuficientes.";
        return reason.Length == 0;
    }

    public bool TryRecruit(NpcData data, VillageFacility home, out string reason)
    {
        ReconcileHousing();
        if (!CanRecruit(data, home, out reason)) return false;
        if (!TryEntrance(home, home.transform.position, out Vector3 position, false))
        { reason = "Não há acesso livre ao redor da residência."; return false; }
        // Prepare an inactive instance before charging or exposing it to the scene.
        var staging = new GameObject("NPC staging");
        staging.SetActive(false);
        GameObject instance = data.prefab != null ? Instantiate(data.prefab, staging.transform) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        instance.transform.SetParent(staging.transform, false);
        instance.SetActive(false);
        instance.name = data.displayName;
        if (data.prefab == null && game.litMaterial != null)
            instance.GetComponent<Renderer>().sharedMaterial = game.litMaterial;
        instance.transform.position = position + Vector3.up;
        var npc = instance.GetComponent<VillageNpc>();
        if (npc == null) npc = instance.AddComponent<VillageNpc>();
        npc.data = data;
        npc.SetResidence(home);
        if (!game.wallet.Spend(data.recruitmentCosts))
        { Destroy(staging); reason = "Recursos insuficientes."; return false; }
        instance.transform.SetParent(null, true);
        Destroy(staging);
        instance.SetActive(true);
        Register(npc);
        reason = data.displayName + " recrutado(a).";
        return true;
    }

    public void ReconcileHousing()
    {
        members.RemoveAll(n => n == null || n.IsDead);
        var homes = FindObjectsByType<VillageFacility>();
        // Existing residents retain their identity when capacity is lost.
        foreach (var npc in members)
        {
            if (!npc.IsHomeless) continue;
            foreach (var home in homes)
                if (HasVacancy(home, npc.data.category)) { npc.SetResidence(home); break; }
        }
    }

    public bool TryAssign(VillageNpc npc, NpcRoleData role, VillageFacility workplace)
    {
        if (game == null || !game.IsExplorationMode || npc == null || !members.Contains(npc) || npc.IsInjured ||
            role == null || !npc.CanPerform(role) || !role.Accepts(workplace)) return false;
        int occupied = 0;
        foreach (var other in members)
            if (other != null && other != npc && other.Workplace == workplace && other.Role == role) occupied++;
        if (occupied >= Mathf.Max(1, role.slotsPerFacility)) return false;
        if (role.productionWorker && workplace.data.kind == VillageFacilityKind.Production &&
            CountAssignedWorkers(workplace, npc) >= workplace.CurrentLevel.maximumWorkers) return false;
        npc.SetAssignment(role, workplace);
        return true;
    }

    public bool TryClearAssignment(VillageNpc npc)
    {
        if (game == null || !game.IsExplorationMode || !members.Contains(npc)) return false;
        npc.SetAssignment(null, null);
        return true;
    }

    public bool HasServiceSpecialist(VillageFacility facility)
    {
        foreach (var npc in members)
            if (npc != null && npc.CanWork && npc.Workplace == facility && npc.Role.providesService) return true;
        return false;
    }

    public int CountWorkers(VillageFacility facility, VillageNpc except = null)
    {
        int count = 0;
        foreach (var npc in members)
            if (npc != null && npc != except && npc.CanWork && npc.Workplace == facility && npc.Role.productionWorker) count++;
        return count;
    }

    public int CountAssignedWorkers(VillageFacility facility, VillageNpc except = null)
    {
        int count = 0;
        foreach (var npc in members)
            if (npc != null && npc != except && !npc.IsDead && npc.Workplace == facility && npc.Role != null && npc.Role.productionWorker) count++;
        return count;
    }

    public bool TryRecover(VillageNpc npc)
    {
        if (game == null || !game.IsExplorationMode || !members.Contains(npc) || !npc.IsInjured || npc.IsHomeless ||
            !game.wallet.Spend(npc.data.recoveryCosts)) return false;
        npc.Recover();
        return true;
    }

    // Stop outside the footprint: NPCs never need building colliders to be enabled.
    public static bool TryEntrance(VillageFacility facility, Vector3 from, out Vector3 position, bool requireRoute = true)
    {
        position = default;
        var grid = GridWorld.I;
        if (facility == null || grid == null) return false;
        var renderers = facility.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(facility.transform.position, Vector3.one);
        foreach (var renderer in renderers) if (!(renderer is LineRenderer)) bounds.Encapsulate(renderer.bounds);
        var min = grid.WorldToCell(bounds.min - Vector3.one * grid.cellSize);
        var max = grid.WorldToCell(bounds.max + Vector3.one * grid.cellSize);
        var candidates = new List<Vector3>();
        for (int x = min.x; x <= max.x; x++)
            for (int z = min.y; z <= max.y; z++)
            {
                var cell = new Vector2Int(x, z);
                if (!grid.InBounds(cell) || grid.IsHardBlocked(cell) || grid.Get(cell) != null) continue;
                var candidate = grid.CellToWorld(cell);
                if (candidate.x > bounds.min.x - 0.6f && candidate.x < bounds.max.x + 0.6f &&
                    candidate.z > bounds.min.z - 0.6f && candidate.z < bounds.max.z + 0.6f) continue;
                candidates.Add(candidate);
            }
        candidates.Sort((a, b) => (a - from).sqrMagnitude.CompareTo((b - from).sqrMagnitude));
        foreach (var candidate in candidates)
        {
            if (requireRoute && !grid.TryOpenRoute(from, grid.WorldToCell(candidate), out _)) continue;
            position = candidate;
            return true;
        }
        return false;
    }
}
