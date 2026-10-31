using System;
using System.Collections.Generic;
using UnityEngine;

public enum ExpeditionSpawnPlacement { RandomInRegion, SpawnPoints, Mixed }

// Place on a modular prefab. Generation is requested after the expedition grid is ready.
[DisallowMultipleComponent]
public sealed class ExpeditionEnemyRegion : MonoBehaviour
{
    [Header("Área local do módulo")]
    public Vector3 localCenter;
    public Vector2 regionSize = new Vector2(50f, 50f);
    [Header("Posicionamento")]
    public ExpeditionSpawnPlacement placement = ExpeditionSpawnPlacement.Mixed;
    public Transform[] spawnPoints = new Transform[0];
    [Range(0f, 1f)] public float spawnPointChance = 0.5f;
    [Tooltip("Se os pontos estiverem ausentes ou bloqueados, permite tentar posições aleatórias na área.")]
    public bool randomFallback = true;
    [Min(1)] public int placementAttempts = 80;
    [Min(0f)] public float safeDistance = 10f;
    [Min(0f)] public float enemySpacing = 2f;
    [Header("Inimigos individuais")]
    [Min(0)] public int individualCount = 6;
    [Tooltip("Somente os tipos desta lista podem aparecer como individuais. Lista vazia não gera individuais.")]
    public EnemyArchetypeData[] individuals = new EnemyArchetypeData[0];
    [Header("Grupos com composição")]
    [Min(0)] public int groupCount;
    public EnemyGroupCompositionData[] compositions = new EnemyGroupCompositionData[0];
    [Tooltip("Tamanho usado quando Spawn Count da composição é zero. Os mínimos de membros sempre são respeitados.")]
    [Min(1)] public int defaultGroupSize = 5;
    [Min(0.1f)] public float groupRadius = 5f;
    bool generated;

    public int Spawn(ExpeditionController controller, System.Random random, List<Vector3> occupied)
    {
        if (generated || !isActiveAndEnabled) return 0;
        generated = true;
        var pool = new List<EnemyArchetypeData>();
        foreach (var enemy in individuals ?? new EnemyArchetypeData[0])
            if (enemy != null && EnemyFactory.CanSpawn(enemy) && !pool.Contains(enemy)) pool.Add(enemy);
        int spawned = 0;
        for (int i = 0; i < Mathf.Clamp(individualCount, 0, 500) && pool.Count > 0; i++)
        {
            var definition = pool[random.Next(pool.Count)];
            if (!TryPosition(controller, random, occupied, definition, null, out var point)) continue;
            if (EnemyFactory.Spawn(definition, point, controller.Grid, null, transform) == null) continue;
            occupied.Add(point);
            spawned++;
        }
        var profiles = new List<EnemyGroupCompositionData>();
        foreach (var profile in compositions ?? new EnemyGroupCompositionData[0])
            if (ValidComposition(profile)) profiles.Add(profile);
        for (int i = 0; i < Mathf.Clamp(groupCount, 0, 100) && profiles.Count > 0; i++)
        {
            var profile = PickProfile(profiles, random);
            var members = BuildMembers(profile, random);
            if (members.Count == 0 || !TryPosition(controller, random, occupied, members[0], null, out var anchor)) continue;
            // Reserve the entire group before creating it; blocked regions never produce half a composition.
            var positions = new List<Vector3> { anchor };
            var reserved = new List<Vector3>(occupied) { anchor };
            for (int m = 1; m < members.Count; m++)
            {
                if (!TryPosition(controller, random, reserved, members[m], anchor, out var point)) break;
                positions.Add(point); reserved.Add(point);
            }
            if (positions.Count != members.Count) continue;
            var obj = new GameObject(profile.displayName);
            obj.transform.SetParent(transform, false);
            obj.transform.position = anchor;
            var group = obj.AddComponent<EnemyGroup>();
            group.expected = members.Count;
            group.minSize = members.Count;
            group.randomizeTactic = false;
            group.tactic = profile.forceTactic ? profile.tactic : GroupTactic.Tactical;
            group.loneWolfChance = 0f;
            var overrides = new EnemySpawnOverrides();
            if (profile.uniformPersonality) overrides.personality = profile.personality;
            if (profile.uniformTemperament) overrides.temperament = profile.temperament;
            if (profile.uniformBehavior) overrides.behavior = profile.behavior;
            if (profile.uniformBravery) overrides.bravery = profile.ResolveBravery(1f);
            for (int m = 0; m < members.Count; m++)
                if (EnemyFactory.Spawn(members[m], positions[m], controller.Grid, group, transform, overrides) != null)
                { occupied.Add(positions[m]); spawned++; }
            group.expected = group.members.Count;
            if (group.expected == 0) Destroy(obj);
        }
        if ((individualCount > 0 && pool.Count == 0) || (groupCount > 0 && profiles.Count == 0))
            Debug.LogWarning("Região sem opções válidas de inimigos. Confira Individuals e Compositions neste ExpeditionEnemyRegion.", this);
        return spawned;
    }

    bool ValidComposition(EnemyGroupCompositionData profile)
    {
        if (profile == null || profile.selectionWeight <= 0f || profile.members == null || profile.members.Length == 0) return false;
        long minimum = 0; float weight = 0f;
        foreach (var slot in profile.members)
        {
            if (slot == null || !EnemyFactory.CanSpawn(slot.archetype)) return false;
            minimum += Mathf.Max(0, slot.minimumPerGroup); weight += Mathf.Max(0f, slot.weight);
        }
        int requested = profile.spawnCount > 0 ? profile.spawnCount : Mathf.Max(1, defaultGroupSize);
        return minimum <= 200 && requested <= 200 && (minimum >= requested || weight > 0f);
    }

    static EnemyGroupCompositionData PickProfile(List<EnemyGroupCompositionData> profiles, System.Random random)
    {
        double total = 0;
        foreach (var profile in profiles) total += profile.selectionWeight;
        double roll = random.NextDouble() * total;
        foreach (var profile in profiles) { roll -= profile.selectionWeight; if (roll < 0) return profile; }
        return profiles[profiles.Count - 1];
    }

    List<EnemyArchetypeData> BuildMembers(EnemyGroupCompositionData profile, System.Random random)
    {
        var result = new List<EnemyArchetypeData>();
        float total = 0f;
        foreach (var slot in profile.members)
        {
            for (int i = 0; i < Mathf.Max(0, slot.minimumPerGroup); i++) result.Add(slot.archetype);
            total += Mathf.Max(0f, slot.weight);
        }
        int count = Mathf.Max(result.Count, profile.spawnCount > 0 ? profile.spawnCount : Mathf.Max(1, defaultGroupSize));
        while (result.Count < count)
        {
            double roll = random.NextDouble() * total;
            EnemyArchetypeData chosen = null;
            foreach (var slot in profile.members)
                if (slot.weight > 0f) { chosen = slot.archetype; roll -= slot.weight; if (roll < 0) break; }
            result.Add(chosen);
        }
        return result;
    }

    bool TryPosition(ExpeditionController controller, System.Random random, List<Vector3> occupied,
        EnemyArchetypeData enemy, Vector3? anchor, out Vector3 position)
    {
        position = default;
        var grid = controller.Grid;
        for (int attempt = 0; attempt < Mathf.Clamp(placementAttempts, 1, 500); attempt++)
        {
            bool usePoint = !anchor.HasValue && placement != ExpeditionSpawnPlacement.RandomInRegion &&
                (placement == ExpeditionSpawnPlacement.SpawnPoints || random.NextDouble() < spawnPointChance);
            if (anchor.HasValue)
            {
                double angle = random.NextDouble() * Math.PI * 2;
                float radius = Mathf.Sqrt((float)random.NextDouble()) * Mathf.Max(0.1f, groupRadius);
                position = anchor.Value + new Vector3((float)Math.Cos(angle) * radius, 0, (float)Math.Sin(angle) * radius);
            }
            else if (usePoint && spawnPoints != null && spawnPoints.Length > 0)
            {
                var point = spawnPoints[random.Next(spawnPoints.Length)];
                if (point == null || !point.gameObject.activeInHierarchy)
                {
                    if (!randomFallback) continue;
                    position = RandomPosition(random);
                }
                else position = point.position;
            }
            else
            {
                if (placement == ExpeditionSpawnPlacement.SpawnPoints && !randomFallback) continue;
                position = RandomPosition(random);
            }
            // Exhausting invalid explicit markers must not prevent the optional random fallback.
            if (!anchor.HasValue && usePoint && randomFallback && attempt >= Mathf.Clamp(placementAttempts, 1, 500) / 2)
                position = RandomPosition(random);
            position.y = grid.transform.position.y;
            Vector3 local = transform.InverseTransformPoint(position) - localCenter;
            if (Mathf.Abs(local.x) > Mathf.Max(1f, regionSize.x) * 0.5f || Mathf.Abs(local.z) > Mathf.Max(1f, regionSize.y) * 0.5f) continue;
            Vector3 playerDelta = position - controller.PlayerPosition, exitDelta = position - controller.ExtractionPosition;
            playerDelta.y = exitDelta.y = 0;
            float safety = Mathf.Max(0f, safeDistance);
            if (playerDelta.sqrMagnitude < safety * safety || exitDelta.sqrMagnitude < safety * safety) continue;
            var cell = grid.WorldToCell(position);
            float clearance = Mathf.Max(enemy.balance.scale.x, enemy.balance.scale.z) * 0.5f;
            bool free = true;
            int rings = Mathf.CeilToInt(clearance / grid.cellSize);
            for (int x = -rings; x <= rings; x++)
                for (int z = -rings; z <= rings; z++)
                {
                    var neighbor = cell + new Vector2Int(x,z);
                    if (!grid.InBounds(neighbor) || grid.IsHardBlocked(neighbor) || grid.Get(neighbor) != null) free = false;
                }
            foreach (var other in occupied)
                if ((position - other).sqrMagnitude < Mathf.Max(0.1f, enemySpacing) * Mathf.Max(0.1f, enemySpacing)) free = false;
            if (!free || !grid.TryOpenRoute(position, grid.WorldToCell(controller.PlayerPosition), out _, float.PositiveInfinity, clearance)) continue;
            return true;
        }
        return false;
    }

    Vector3 RandomPosition(System.Random random) => transform.TransformPoint(localCenter + new Vector3(
        ((float)random.NextDouble() - 0.5f) * Mathf.Max(1f, regionSize.x), 0,
        ((float)random.NextDouble() - 0.5f) * Mathf.Max(1f, regionSize.y)));

    void OnDrawGizmosSelected()
    {
        var previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(localCenter, new Vector3(Mathf.Max(1f, regionSize.x), 1f, Mathf.Max(1f, regionSize.y)));
        Gizmos.matrix = previous;
        Gizmos.color = Color.yellow;
        foreach (var point in spawnPoints ?? new Transform[0])
            if (point != null) Gizmos.DrawWireSphere(point.position, 0.6f);
    }
}
