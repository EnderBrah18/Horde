using UnityEngine;

[CreateAssetMenu(menuName = "Village/Playtest Scenario", fileName = "NewPlaytestScenario")]
public class PlaytestScenario : ScriptableObject
{
    [Header("Identificação")]
    public string displayName = "Cenário";
    [TextArea] public string instructions;

    [Header("Mapa")]
    [Range(11, 61)] public int gridWidth = 21;
    [Range(11, 61)] public int gridHeight = 21;
    [Min(0.1f)] public float gridCellSize = 2f;
    [Min(0)] public int pathBreakCost = 10;
    [Min(0)] public int spawnBuildClearance = 1;
    [Min(0)] public int coreBuildClearance;
    public SpawnEdges spawnEdges = SpawnEdges.All;

    [Header("Área construída")]
    [Tooltip("0 permite construir pela largura total do mapa.")]
    [Min(0)] public int buildGridWidth;
    [Tooltip("0 permite construir pela altura total do mapa.")]
    [Min(0)] public int buildGridHeight;
    public Vector2Int buildGridOffset;

    [Header("Economia e núcleo")]
    [Min(0)] public int startWood = 60;
    [Min(0)] public int startSupplies = 6;
    [Min(1f)] public float coreHealth = 300f;

    [Header("Ondas")]
    [Min(0)] public int initialGroups = 1;
    [Min(1)] public int groupsPerRounds = 2;
    [Min(1)] public int maxGroups = 4;
    [Min(0)] public int baseCommonEnemies = 4;
    [Min(0)] public int commonsPerRound = 1;
    [Min(1)] public int tankEveryRounds = 2;
    [Range(0f, 1f)] public float hunterBaseChance = 0.3f;
    [Range(0f, 1f)] public float hunterChancePerRound = 0.05f;
    [Range(0f, 1f)] public float hunterChanceCap = 0.6f;
    [Min(0f)] public float spawnInterval = 0.4f;
    [Min(0f)] public float groupInterval = 3f;

    [Header("Inimigos")]
    public EnemyBalanceSettings commonEnemy = new EnemyBalanceSettings { tacticRadius = 16f };
    public EnemyBalanceSettings tankEnemy = new EnemyBalanceSettings
    {
        maxHealth = 140f,
        speed = 2.2f,
        damage = 14f,
        tacticRadius = 100f,
        scale = new Vector3(1.8f, 0.9f, 1.8f)
    };
    [Range(0f, 1f)] public float loneWolfChance = 0.1f;
    [Range(0f, 1f)] public float tacticalGroupChance = 0.7f;
    [Min(1)] public int groupMinSize = 5;
    [Min(0f)] public float groupMergeRadius = 16f;

    void OnValidate()
    {
        gridWidth = Mathf.Clamp(gridWidth, 11, 61);
        gridHeight = Mathf.Clamp(gridHeight, 11, 61);
        buildGridWidth = Mathf.Clamp(buildGridWidth, 0, gridWidth);
        buildGridHeight = Mathf.Clamp(buildGridHeight, 0, gridHeight);
        gridCellSize = Mathf.Max(0.1f, gridCellSize);
        pathBreakCost = Mathf.Max(0, pathBreakCost);
        spawnBuildClearance = Mathf.Max(0, spawnBuildClearance);
        coreBuildClearance = Mathf.Max(0, coreBuildClearance);
        groupMergeRadius = Mathf.Max(0f, groupMergeRadius);
    }

    public void ApplyTo(Game game)
    {
        if (game == null) return;
        game.gridWidth = gridWidth;
        game.gridHeight = gridHeight;
        game.gridCellSize = gridCellSize;
        game.pathBreakCost = pathBreakCost;
        game.spawnBuildClearance = spawnBuildClearance;
        game.coreBuildClearance = coreBuildClearance;
        game.spawnEdges = spawnEdges;
        game.buildGridWidth = buildGridWidth;
        game.buildGridHeight = buildGridHeight;
        game.buildGridOffset = buildGridOffset;
        game.startWood = startWood;
        game.startSupplies = startSupplies;
        game.coreHealth = coreHealth;
        game.initialGroups = initialGroups;
        game.groupsPerRounds = groupsPerRounds;
        game.maxGroups = maxGroups;
        game.baseCommonEnemies = baseCommonEnemies;
        game.commonsPerRound = commonsPerRound;
        game.tankEveryRounds = tankEveryRounds;
        game.hunterBaseChance = hunterBaseChance;
        game.hunterChancePerRound = hunterChancePerRound;
        game.hunterChanceCap = hunterChanceCap;
        game.spawnInterval = spawnInterval;
        game.groupInterval = groupInterval;
        game.commonEnemy = Copy(commonEnemy);
        game.tankEnemy = Copy(tankEnemy);
        game.loneWolfChance = loneWolfChance;
        game.tacticalGroupChance = tacticalGroupChance;
        game.groupMinSize = groupMinSize;
        game.groupMergeRadius = groupMergeRadius;
    }

    static EnemyBalanceSettings Copy(EnemyBalanceSettings source)
    {
        if (source == null) return null;
        return new EnemyBalanceSettings
        {
            maxHealth = source.maxHealth,
            speed = source.speed,
            damage = source.damage,
            attackCooldown = source.attackCooldown,
            attackRange = source.attackRange,
            tacticRadius = source.tacticRadius,
            scale = source.scale
        };
    }
}

[System.Flags]
public enum SpawnEdges
{
    None = 0,
    North = 1,
    South = 2,
    West = 4,
    East = 8,
    All = North | South | West | East
}
