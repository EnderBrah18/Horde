using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using UnityEngine.Serialization;

// Coloque este script em UM GameObject vazio na cena. Ele cria todo o resto.
public partial class Game : MonoBehaviour
{
    public static Game I;

    [Header("Materiais (obrigatório para a build)")]
    public Material litMaterial;
    public Material lineMaterial;

    [Header("Recursos iniciais")]
    public int startWood = 60;
    [FormerlySerializedAs("startSupplies")] public int startCrystals = 6;

    [Header("Amanhecer (temporário, até os círculos mágicos)")]
    public int dawnWood = 10;
    [FormerlySerializedAs("dawnSuppliesBase")] public int dawnCrystalsBase = 3;

    [Header("Mapa de playtest")]
    [Range(11, 301)] public int gridWidth = 21;
    [Range(11, 301)] public int gridHeight = 21;
    [Range(21, 301)] public int maxGridDimension = 41;
    [Min(41)] public int sceneMapDimension = 281;
    [Min(0.1f)] public float gridCellSize = 2f;
    [Min(0)] public int pathBreakCost = 10;
    [Min(0)] public int spawnBuildClearance = 1;
    [Min(0)] public int coreBuildClearance;
    public SpawnEdges spawnEdges = SpawnEdges.All;

    [Header("Limites do grid de construção")]
    [Tooltip("0 usa a largura total do mapa. Valores menores centralizam uma área construída menor.")]
    [Min(0)] public int buildGridWidth;
    [Tooltip("0 usa a altura total do mapa. Valores menores centralizam uma área construída menor.")]
    [Min(0)] public int buildGridHeight;
    public Vector2Int buildGridOffset;

    [Header("Câmera de playtest")]
    [Min(1f)] public float cameraPanSpeed = 40f;
    [Min(1f)] public float cameraEdgeSize = 28f;
    [Tooltip("Opcional. Se vazio, o modo combate procura um objeto com a tag Player.")]
    public Transform combatCameraTarget;
    public bool PlayerControlEnabled => combatCameraMode && !lost && !expeditionTransition;
    [Min(0.1f)] public float combatCameraFollowSpeed = 8f;
    [Min(1f)] public float buildCameraZoomSpeed = 10f;
    [Min(1f)] public float buildCameraMinZoom = 15f;
    [Min(1f)] public float buildCameraMaxZoom = 60f;
    [Min(0.1f)] public float buildPlacementStep = 0.5f;

    [Header("Playtest")]
    [Min(1f)] public float coreHealth = 300f;
    [Min(1)] public int eventLogEntries = 8;
    public bool showEventLog = true;
    [Tooltip("Registra no Console as decisões de alvo e a direção de movimento dos inimigos.")]
    public bool debugEnemyTargeting;
    public PlaytestScenario scenario;

    [Header("Balanceamento de inimigos")]
    public EnemyBalanceSettings commonEnemy = new EnemyBalanceSettings { tacticRadius = 16f };
    public EnemyBalanceSettings tankEnemy = new EnemyBalanceSettings
    {
        maxHealth = 140f,
        knockbackResistance = 0.8f,
        speed = 2.2f,
        damage = 14f,
        tacticRadius = 100f,
        scale = new Vector3(1.8f, 0.9f, 1.8f)
    };

    [Header("Balanceamento das noites")]
    [Tooltip("Grupos que surgem dentro de cada leva. A próxima leva espera todos serem resolvidos.")]
    [FormerlySerializedAs("initialGroups"), Min(1)] public int groupsPerWave = 3;
    [Min(1)] public int initialWaves = 3;
    [FormerlySerializedAs("groupsPerRounds"), Min(1)] public int nightsPerExtraWave = 2;
    [FormerlySerializedAs("maxGroups"), Min(1)] public int maxWaves = 5;
    [Tooltip("Pausa depois de resolver uma leva, antes de começar a próxima.")]
    [Min(0f)] public float waveInterval = 5f;
    [Min(0)] public int baseCommonEnemies = 4;
    [Tooltip("Bônus fixo por grupo da primeira noite. Depois dela o total é decidido pelo Night Director.")]
    [Min(0)] public int commonsPerRound = 1;
    [Min(1)] public int tankEveryRounds = 2;
    [Range(0f, 1f)] public float hunterBaseChance = 0.3f;
    [Range(0f, 1f)] public float hunterChancePerRound = 0.05f;
    [Range(0f, 1f)] public float hunterChanceCap = 0.6f;
    [Min(0f)] public float spawnInterval = 0.4f;
    [Min(0f)] public float groupInterval = 3f;

    [Header("Balanceamento dos grupos")]
    [Min(1)] public int groupMinSize = 5;
    [Range(0f, 1f)] public float loneWolfChance = 0.1f;
    [Range(0f, 1f)] public float tacticalGroupChance = 0.7f;
    [Min(0f)] public float groupMergeRadius = 16f;
    [Tooltip("Temporário para testar fusões: Fanáticos comuns recebem bravura entre 1 e 1,2.")]
    public bool varyFanaticBraveryForTesting = true;

    [Header("Estado (somente leitura)")]
    [FormerlySerializedAs("round"), SerializeField] int nightsStarted;
    // Compatibility for economy/refund consumers: this is a night ID, never a wave.
    public int round => nightsStarted;
    public int Day => VillageProgressionRules.DayNumber(nightsStarted, night, lost);
    public int CurrentWave { get; private set; }
    public int TotalWaves { get; private set; }
    public int killed;
    public int deserted;
    public bool night;
    public bool lost;
    string defeatReason = "O núcleo foi destruído";

    public void PlayerDied()
    {
        if (lost) return;
        defeatReason = "O jogador morreu";
        lost = true;
    }

    // Runtime balances use a dictionary, which Unity does not serialize.
    [System.NonSerialized] public Wallet wallet = new Wallet();
    public Structure core;

    public event System.Action<GameplayEvent> GameplayEventRaised;

    static string selectedScenarioName;
    public static void PrepareNewRun() { selectedScenarioName = null; }
    readonly List<PlaytestScenario> scenarios = new List<PlaytestScenario>();
    readonly List<GameplayEvent> recentEvents = new List<GameplayEvent>();
    bool scenarioChangePending;

    void OnValidate()
    {
        startWood = Mathf.Max(0, startWood);
        startCrystals = Mathf.Max(0, startCrystals);
        dawnWood = Mathf.Max(0, dawnWood);
        dawnCrystalsBase = Mathf.Max(0, dawnCrystalsBase);
        maxGridDimension = Mathf.Clamp(maxGridDimension, 21, 301);
        gridWidth = Mathf.Clamp(gridWidth, 11, maxGridDimension);
        gridHeight = Mathf.Clamp(gridHeight, 11, maxGridDimension);
        sceneMapDimension = Mathf.Clamp(sceneMapDimension, 41, 301);
        if (sceneMapDimension % 2 == 0) sceneMapDimension++;
        gridCellSize = Mathf.Max(0.1f, gridCellSize);
        pathBreakCost = Mathf.Max(0, pathBreakCost);
        spawnBuildClearance = Mathf.Max(0, spawnBuildClearance);
        coreBuildClearance = Mathf.Max(0, coreBuildClearance);
        buildGridWidth = Mathf.Max(0, buildGridWidth);
        buildGridHeight = Mathf.Max(0, buildGridHeight);
        cameraPanSpeed = Mathf.Max(1f, cameraPanSpeed);
        cameraEdgeSize = Mathf.Max(1f, cameraEdgeSize);
        combatCameraFollowSpeed = Mathf.Max(0.1f, combatCameraFollowSpeed);
        buildCameraZoomSpeed = Mathf.Max(1f, buildCameraZoomSpeed);
        buildCameraMinZoom = Mathf.Max(1f, buildCameraMinZoom);
        buildCameraMaxZoom = Mathf.Max(buildCameraMinZoom, buildCameraMaxZoom);
        buildPlacementStep = Mathf.Max(0.1f, buildPlacementStep);
        coreHealth = Mathf.Max(1f, coreHealth);
        eventLogEntries = Mathf.Max(1, eventLogEntries);
        groupsPerWave = Mathf.Max(1, groupsPerWave);
        initialWaves = Mathf.Max(1, initialWaves);
        waveInterval = Mathf.Max(0f, waveInterval);
        nightsPerExtraWave = Mathf.Max(1, nightsPerExtraWave);
        maxWaves = Mathf.Max(1, maxWaves);
        baseCommonEnemies = Mathf.Max(0, baseCommonEnemies);
        commonsPerRound = Mathf.Max(0, commonsPerRound);
        tankEveryRounds = Mathf.Max(1, tankEveryRounds);
        hunterBaseChance = Mathf.Clamp01(hunterBaseChance);
        hunterChancePerRound = Mathf.Clamp01(hunterChancePerRound);
        hunterChanceCap = Mathf.Clamp01(hunterChanceCap);
        spawnInterval = Mathf.Max(0f, spawnInterval);
        groupInterval = Mathf.Max(0f, groupInterval);
        groupMinSize = Mathf.Max(1, groupMinSize);
        loneWolfChance = Mathf.Clamp01(loneWolfChance);
        tacticalGroupChance = Mathf.Clamp01(tacticalGroupChance);
        groupMergeRadius = Mathf.Max(0f, groupMergeRadius);
        ClampEnemyBalance(commonEnemy);
        ClampEnemyBalance(tankEnemy);
    }

    static void ClampEnemyBalance(EnemyBalanceSettings settings)
    {
        if (settings == null) return;
        settings.maxHealth = Mathf.Max(1f, settings.maxHealth);
        settings.speed = Mathf.Max(0f, settings.speed);
        settings.damage = Mathf.Max(0f, settings.damage);
        settings.attackCooldown = Mathf.Max(0.01f, settings.attackCooldown);
        settings.attackRange = Mathf.Max(0f, settings.attackRange);
        settings.tacticRadius = Mathf.Max(0f, settings.tacticRadius);
        settings.knockbackResistance = Mathf.Clamp01(settings.knockbackResistance);
        settings.scale = new Vector3(Mathf.Max(0.1f, settings.scale.x), Mathf.Max(0.1f, settings.scale.y), Mathf.Max(0.1f, settings.scale.z));
    }

    static Material baseMat;
    public static Material LineMat;

    GridWorld grid;
    VillageBuildArea buildArea;
    CinemachineCamera virtualCamera;
    readonly List<Vector2Int> spawns = new List<Vector2Int>();
    readonly List<SpecialBuildPoint> specialBuildPoints = new List<SpecialBuildPoint>();
    List<BuildingData> menu = new List<BuildingData>();
    readonly List<BuildingData> activeMenu = new List<BuildingData>();
    readonly List<BuildingMenuCategory> menuCategories = new List<BuildingMenuCategory>();
    int selected = -1;
    BuildingMenuCategory activeMenuCategory = BuildingMenuCategory.Automatic;
    int menuPage;
    GameObject cursor;
    LineRenderer buildAreaOutline;
    LineRenderer nextBuildAreaOutline;
    LineRenderer previewOutline;
    float shownBuildRadius = -1f;
    Vector2Int hoverCell;
    Vector3 hoverPosition;
    float buildRotation;
    bool sceneDrivenLayout;
    bool combatCameraMode;
    bool draggingBuildCamera;
    bool snapBuildPosition = true;
    Vector2 lastBuildCameraPointer;
    string msg = "";
    float msgTimer;

    const float CardW = 190f, CardH = 66f, CardGap = 8f;

    public static GameObject Prim(PrimitiveType t, Vector3 pos, Vector3 scale, Color col)
    {
        var go = GameObject.CreatePrimitive(t);
        Destroy(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        if (baseMat != null) r.sharedMaterial = baseMat;
        r.material.color = col;
        return go;
    }

    void Awake()
    {
        I = this;
        if (GetComponent<VillagePopulation>() == null) gameObject.AddComponent<VillagePopulation>();
        virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        baseMat = litMaterial;
        LineMat = lineMaterial;
        Enemy.All.Clear();
        Structure.All.Clear();

        scenarios.AddRange(Resources.LoadAll<PlaytestScenario>("PlaytestScenarios"));
        scenarios.Sort((a, b) => string.Compare(a.displayName, b.displayName, System.StringComparison.Ordinal));
        if (!string.IsNullOrEmpty(selectedScenarioName))
        {
            var selectedScenario = scenarios.Find(s => s.name == selectedScenarioName);
            if (selectedScenario != null) scenario = selectedScenario;
        }
        buildArea = FindAnyObjectByType<VillageBuildArea>();
        sceneDrivenLayout = buildArea != null && GameObject.FindGameObjectWithTag("Nucleo") != null;
        if (scenario != null) scenario.ApplyTo(this);

        wallet.Wood = startWood;
        wallet.Crystals = startCrystals;

        menu = new List<BuildingData>(Resources.LoadAll<BuildingData>("Buildings"));
        menu.RemoveAll(building => !building.showInBuildMenu);
        menu.Sort((a, b) => a.order.CompareTo(b.order));
        if (menu.Count == 0)
            Say("Nenhuma construção encontrada. Rode: Tools > Village > Create Default Buildings", 600f);
        BuildMenuCategories();

        grid = gameObject.AddComponent<GridWorld>();
        if (sceneDrivenLayout)
        {
            grid.Configure(sceneMapDimension, sceneMapDimension, gridCellSize, pathBreakCost);
            BuildSceneWorld();
        }
        else
        {
            grid.Configure(gridWidth, gridHeight, gridCellSize, pathBreakCost);
            BuildWorld();
        }
    }

    void Start()
    {
        var existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (combatCameraTarget == null && existingPlayer != null)
            combatCameraTarget = existingPlayer.transform;
        PrepareExpeditions();
    }

    void BuildSceneWorld()
    {
        spawns.Clear();
        specialBuildPoints.Clear();
        foreach (var point in FindObjectsByType<EnemySpawnPoint>())
        {
            Vector2Int cell = grid.WorldToCell(point.transform.position);
            if (point.enabledForSpawning && grid.InBounds(cell)) spawns.Add(cell);
        }
        specialBuildPoints.AddRange(FindObjectsByType<SpecialBuildPoint>());

        foreach (var river in GameObject.FindGameObjectsWithTag("Rio"))
        {
            var collider = river.GetComponent<Collider>();
            if (collider != null) grid.SetHardBlocked(collider.bounds, true);
        }
        foreach (var bridge in GameObject.FindGameObjectsWithTag("Ponte"))
        {
            var collider = bridge.GetComponent<Collider>();
            if (collider != null) grid.ClearHardBlockedWithinColliderFootprint(collider);
        }

        var registered = new HashSet<GameObject>();
        var coreObject = GameObject.FindGameObjectWithTag("Nucleo");
        if (coreObject != null)
        {
            core = RegisterSceneStructure(coreObject, StructureKind.Core, coreHealth);
            registered.Add(coreObject);
            if (core != null) core.health.Died += _ =>
            {
                lost = true;
                RaiseGameplayEvent(GameplayEventType.CoreDestroyed, coreObject, 0f, "O núcleo da vila foi destruído");
            };
        }
        RegisterTaggedStructures("ProducaoPassiva", StructureKind.Facility, registered);
        RegisterTaggedStructures("ProducaoAtiva", StructureKind.Facility, registered);
        RegisterTaggedStructures("ConstrucaoProdutiva", StructureKind.Facility, registered);
        RegisterTaggedStructures("ConstrucaoServico", StructureKind.Facility, registered);
        RegisterTaggedStructures("Moradia", StructureKind.Facility, registered);
        RegisterTaggedStructures("ConstrucaoDefensiva", StructureKind.Tower, registered);
        RegisterTaggedStructures("Muro", StructureKind.Wall, registered);
        RegisterTaggedStructures("Cerca", StructureKind.Wall, registered);
        RegisterTaggedStructures("Destrocos", StructureKind.Wall, registered);

        grid.Recalc();
        float cs = grid.cellSize;
        cursor = Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(cs, 0.1f, cs), Color.green);
        cursor.name = "BuildPreview";
        CreateBuildAreaOutline();
        CreatePreviewOutline();
    }

    void CreateBuildAreaOutline()
    {
        var outlineObject = new GameObject("BuildAreaOutline");
        buildAreaOutline = outlineObject.AddComponent<LineRenderer>();
        buildAreaOutline.useWorldSpace = true;
        buildAreaOutline.loop = true;
        buildAreaOutline.positionCount = 65;
        buildAreaOutline.startWidth = buildAreaOutline.endWidth = 0.45f;
        buildAreaOutline.startColor = buildAreaOutline.endColor = new Color(0.3f, 0.95f, 0.5f, 0.8f);
        buildAreaOutline.material = lineMaterial != null
            ? new Material(lineMaterial)
            : new Material(Shader.Find("Sprites/Default"));

        var nextOutlineObject = new GameObject("NextBuildAreaOutline");
        nextBuildAreaOutline = nextOutlineObject.AddComponent<LineRenderer>();
        nextBuildAreaOutline.useWorldSpace = true;
        nextBuildAreaOutline.loop = true;
        nextBuildAreaOutline.positionCount = 65;
        nextBuildAreaOutline.startWidth = nextBuildAreaOutline.endWidth = 0.22f;
        nextBuildAreaOutline.startColor = nextBuildAreaOutline.endColor = new Color(1f, 0.72f, 0.18f, 0.78f);
        nextBuildAreaOutline.material = lineMaterial != null
            ? new Material(lineMaterial)
            : new Material(Shader.Find("Sprites/Default"));
        RefreshBuildAreaOutline();
    }

    void RefreshBuildAreaOutline()
    {
        if (buildAreaOutline == null || buildArea == null) return;
        bool showBuildArea = !night && !combatCameraMode;
        buildAreaOutline.enabled = showBuildArea;
        nextBuildAreaOutline.enabled = showBuildArea && buildArea.CanExpand;
        if (Mathf.Approximately(shownBuildRadius, buildArea.Radius))
        {
            return;
        }
        shownBuildRadius = buildArea.Radius;
        Vector3 center = buildArea.Center.position;
        for (int i = 0; i < buildAreaOutline.positionCount; i++)
        {
            float angle = i / (float)(buildAreaOutline.positionCount - 1) * Mathf.PI * 2f;
            buildAreaOutline.SetPosition(i, new Vector3(center.x + Mathf.Cos(angle) * shownBuildRadius,
                grid.transform.position.y + 0.16f, center.z + Mathf.Sin(angle) * shownBuildRadius));
            float nextRadius = shownBuildRadius + buildArea.radiusPerUpgrade;
            nextBuildAreaOutline.SetPosition(i, new Vector3(center.x + Mathf.Cos(angle) * nextRadius,
                grid.transform.position.y + 0.12f, center.z + Mathf.Sin(angle) * nextRadius));
        }
    }

    void CreatePreviewOutline()
    {
        var outline = new GameObject("BuildPreviewOutline");
        previewOutline = outline.AddComponent<LineRenderer>();
        previewOutline.useWorldSpace = true;
        previewOutline.loop = false;
        previewOutline.positionCount = 5;
        previewOutline.startWidth = previewOutline.endWidth = 0.16f;
        previewOutline.material = lineMaterial != null ? new Material(lineMaterial) : new Material(Shader.Find("Sprites/Default"));
        previewOutline.enabled = false;
    }

    void UpdatePreviewOutline(Vector3 center, Vector3 footprint, Quaternion rotation, Color color)
    {
        if (previewOutline == null) return;
        previewOutline.startColor = previewOutline.endColor = color;
        float halfX = footprint.x * 0.5f;
        float halfZ = footprint.z * 0.5f;
        Vector3[] corners =
        {
            new Vector3(-halfX, 0f, -halfZ), new Vector3(-halfX, 0f, halfZ),
            new Vector3(halfX, 0f, halfZ), new Vector3(halfX, 0f, -halfZ),
            new Vector3(-halfX, 0f, -halfZ)
        };
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 world = center + rotation * corners[i];
            world.y = grid.transform.position.y + 0.15f;
            previewOutline.SetPosition(i, world);
        }
    }

    void RegisterTaggedStructures(string tag, StructureKind kind, HashSet<GameObject> registered)
    {
        foreach (var go in GameObject.FindGameObjectsWithTag(tag))
        {
            if (go == null || registered.Contains(go)) continue;
            var facility = go.GetComponent<VillageFacility>();
            RegisterSceneStructure(go, kind, facility != null && facility.data != null ? facility.data.maxHealth : 120f);
            registered.Add(go);
        }
    }

    Structure RegisterSceneStructure(GameObject go, StructureKind kind, float maxHealth)
    {
        if (go == null) return null;
        var health = go.GetComponent<Health>();
        if (health == null) health = go.AddComponent<Health>();
        health.Setup(maxHealth);
        var structure = go.GetComponent<Structure>();
        if (structure == null) structure = go.AddComponent<Structure>();
        if (!Structure.All.Contains(structure)) Structure.All.Add(structure);
        structure.kind = kind;
        structure.health = health;
        if (!TryGetStructureBounds(go, out Bounds bounds))
            bounds = new Bounds(go.transform.position, go.transform.lossyScale);
        structure.indestructible = go.CompareTag("Muro") || go.CompareTag("Destrocos");
        structure.blocksMovement = !go.CompareTag("Destrocos");
        health.invulnerable = structure.indestructible;
        var facility = go.GetComponent<VillageFacility>();
        if (facility != null) facility.BindStructure(structure);
        else if (structure.BlocksMovement) grid.SetFootprint(structure, bounds);
        if (go.CompareTag("Muro")) grid.SetHardBlocked(bounds, true);
        health.Damaged += (h, amount) => RaiseGameplayEvent(GameplayEventType.StructureAttacked, go, amount,
            $"{go.name} recebeu {amount:0.#} dano ({h.current:0.#}/{h.max:0.#} vida)");
        health.Died += _ =>
        {
            if (facility == null) RaiseGameplayEvent(GameplayEventType.StructureDestroyed, go, 0f, $"{go.name} foi destruída");
        };
        return structure;
    }

    bool TryGetStructureBounds(GameObject go, out Bounds bounds)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy)
        {
            bounds = collider.bounds;
            return true;
        }

        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) renderer = go.GetComponentInChildren<Renderer>();
        if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
        {
            bounds = renderer.bounds;
            return true;
        }

        bounds = default;
        return false;
    }

    void BuildWorld()
    {
        float cs = grid.cellSize;
        float w = grid.width * cs, h = grid.height * cs;
        Vector3 o = transform.position;

        // Chão xadrez
        var floor = Prim(PrimitiveType.Cube, o + new Vector3(0, -0.1f, 0), new Vector3(w, 0.2f, h), Color.white);
        floor.name = "Floor";
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var a = new Color(0.22f, 0.24f, 0.28f);
        var b = new Color(0.28f, 0.30f, 0.35f);
        tex.SetPixels(new[] { a, b, b, a });
        tex.Apply();
        var m = floor.GetComponent<Renderer>().material;
        m.mainTexture = tex;
        m.mainTextureScale = new Vector2(grid.width / 2f, grid.height / 2f);
        m.color = Color.white;

        // Entradas configuráveis para variar as estratégias testadas no mapa.
        if ((spawnEdges & SpawnEdges.North) != 0) spawns.Add(new Vector2Int(grid.width / 2, grid.height - 1));
        if ((spawnEdges & SpawnEdges.South) != 0) spawns.Add(new Vector2Int(grid.width / 2, 0));
        if ((spawnEdges & SpawnEdges.West) != 0) spawns.Add(new Vector2Int(0, grid.height / 2));
        if ((spawnEdges & SpawnEdges.East) != 0) spawns.Add(new Vector2Int(grid.width - 1, grid.height / 2));
        if (spawns.Count == 0) spawns.Add(new Vector2Int(grid.width / 2, 0));
        foreach (var s in spawns)
            Prim(PrimitiveType.Cube, grid.CellToWorld(s) + Vector3.up * 0.03f,
                 new Vector3(cs * 0.95f, 0.05f, cs * 0.95f), new Color(0.6f, 0.1f, 0.1f));

        DrawBuildGridBoundary();

        // O núcleo é um objetivo do mapa, não uma construção posicionável.
        var coreObject = Prim(PrimitiveType.Cube, grid.CellToWorld(grid.CoreCell) + Vector3.up * 0.9f,
            new Vector3(0.9f * cs, 1.8f, 0.9f * cs), new Color(0.2f, 0.4f, 1f));
        coreObject.name = "Coração";
        coreObject.tag = "Nucleo";
        coreObject.layer = LayerMask.NameToLayer("Structures");
        coreObject.AddComponent<BoxCollider>();
        core = RegisterSceneStructure(coreObject, StructureKind.Core, coreHealth);
        core.health.Died += _ =>
        {
            lost = true;
            RaiseGameplayEvent(GameplayEventType.CoreDestroyed, coreObject, 0f, "O Coração foi destruído");
        };

        // Cursor de construção
        cursor = Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(cs * 0.95f, 0.1f, cs * 0.95f), Color.green);
        cursor.name = "Cursor";

        grid.Recalc();
    }

    // Cria qualquer construção a partir de um BuildingData.
    Structure MakeStructure(BuildingData d, Vector2Int cell)
    {
        return MakeStructureAt(d, grid.CellToWorld(cell), Quaternion.identity, true);
    }

    Structure MakeStructureAt(BuildingData d, Vector3 groundPosition, Quaternion rotation, bool scaleByCell)
    {
        float cs = scaleByCell ? grid.cellSize : 1f;
        bool tall = d.shape == PrimitiveType.Cylinder || d.shape == PrimitiveType.Capsule;   // altura = 2 * escala Y
        float centerY = d.size.y * (tall ? 2f : 1f) / 2f;

        var go = Prim(d.shape, groundPosition + Vector3.up * centerY,
                      new Vector3(d.size.x * cs, d.size.y, d.size.z * cs), d.color);
        go.name = d.displayName;
        go.transform.rotation = rotation;
        // Player-built walls are fences, separate from the ancient wall ring in the scene.
        go.layer = LayerMask.NameToLayer("Structures");
        go.AddComponent<BoxCollider>();
        go.tag = d.kind == BuildingKind.Wall ? "Cerca" : "ConstrucaoDefensiva";

        if (d.kind == BuildingKind.Tower) go.AddComponent<Tower>().Setup(d);

        var h = go.AddComponent<Health>();
        h.Setup(d.maxHealth);
        var s = go.AddComponent<Structure>();
        s.kind = d.RuntimeKind;
        s.cell = grid.WorldToCell(groundPosition);
        s.health = h;
        s.data = d;
        s.investedCosts = d.costs != null ? (ResourceAmount[])d.costs.Clone() : new ResourceAmount[0];
        s.placementRound = round;
        h.Damaged += (health, amount) => RaiseGameplayEvent(GameplayEventType.StructureAttacked, go, amount,
            $"{d.displayName} recebeu {amount:0.#} dano ({health.current:0.#}/{health.max:0.#} vida)");
        h.Died += _ =>
        {
            RaiseGameplayEvent(GameplayEventType.StructureDestroyed, go, 0f, $"{d.displayName} foi destruída");
        };
        var collider = go.GetComponent<Collider>();
        grid.SetFootprint(s, collider.bounds);
        RaiseGameplayEvent(GameplayEventType.StructureBuilt, go, 0f, $"Construída: {d.displayName}");
        return s;
    }

    // ---------- Menu (retângulos usados tanto para desenhar quanto para detectar clique) ----------
    BuildingMenuCategory ResolveMenuCategory(BuildingData building)
    {
        if (building.menuCategory != BuildingMenuCategory.Automatic) return building.menuCategory;
        return building.kind == BuildingKind.Wall ? BuildingMenuCategory.Defense : BuildingMenuCategory.Offense;
    }

    string MenuCategoryLabel(BuildingMenuCategory category)
    {
        switch (category)
        {
            case BuildingMenuCategory.Defense: return "Defesas";
            default: return "Ofensivas";
        }
    }

    void BuildMenuCategories()
    {
        menuCategories.Clear();
        BuildingMenuCategory[] order =
        {
            BuildingMenuCategory.Defense, BuildingMenuCategory.Offense
        };
        foreach (var category in order)
        {
            for (int i = 0; i < menu.Count; i++)
            {
                if (ResolveMenuCategory(menu[i]) != category) continue;
                menuCategories.Add(category);
                break;
            }
        }
        if (menuCategories.Count > 0) SelectMenuCategory(menuCategories[0]);
    }

    void SelectMenuCategory(BuildingMenuCategory category)
    {
        activeMenuCategory = category;
        activeMenu.Clear();
        for (int i = 0; i < menu.Count; i++)
            if (ResolveMenuCategory(menu[i]) == category) activeMenu.Add(menu[i]);
        selected = -1;
        CloseInteraction();
        menuPage = 0;
    }

    int CardsPerPage => Mathf.Max(1, Mathf.FloorToInt((Screen.width - 90f) / (CardW + CardGap)));
    int VisibleMenuStart => menuPage * CardsPerPage;
    int VisibleMenuCount => Mathf.Min(CardsPerPage, Mathf.Max(0, activeMenu.Count - VisibleMenuStart));

    Rect CardRect(int i)
    {
        float total = VisibleMenuCount * (CardW + CardGap) - CardGap;
        float x0 = (Screen.width - total) / 2f;
        return new Rect(x0 + i * (CardW + CardGap), Screen.height - 130f, CardW, CardH);
    }

    Rect MenuArea()
    {
        return new Rect(0f, Screen.height - 204f, Screen.width, 204f);
    }

    static Vector2 GuiMouse(Mouse m)
    {
        var p = m.position.ReadValue();
        return new Vector2(p.x, Screen.height - p.y);
    }

    int CardAt(Vector2 gm)
    {
        for (int i = 0; i < VisibleMenuCount; i++)
            if (CardRect(i).Contains(gm)) return VisibleMenuStart + i;
        return -1;
    }

    // ---------- Loop ----------
    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (kb.rKey.wasPressedThisFrame)
        { SceneManager.LoadScene(gameObject.scene.name); return; }

        if (expeditionTransition) return;
        if (IsOnExpedition)
        {
            UpdateExpeditionInput(kb, mouse);
            return;
        }

        if (kb.f3Key.wasPressedThisFrame) showEventLog = !showEventLog;
        if (kb.f1Key.wasPressedThisFrame) SelectNextScenario();
        if (kb.tabKey.wasPressedThisFrame) { combatCameraMode = !combatCameraMode; CloseInteraction(); }
        if (kb.escapeKey.wasPressedThisFrame || kb.digit0Key.wasPressedThisFrame) EmptyHand();
        if (sceneDrivenLayout && kb.cKey.wasPressedThisFrame && core != null)
            CenterCameraOn(core.transform.position);

        if (msgTimer > 0f) { msgTimer -= Time.deltaTime; if (msgTimer <= 0f) msg = ""; }
        RefreshBuildAreaOutline();
        menuPage = Mathf.Clamp(menuPage, 0, Mathf.Max(0, (activeMenu.Count - 1) / CardsPerPage));

        if (lost) { cursor.SetActive(false); CloseInteraction(); return; }

        for (int i = 0; i < VisibleMenuCount && i < 9; i++)
            if (IsConstructionMode && kb[Key.Digit1 + i].wasPressedThisFrame) EquipBuilding(VisibleMenuStart + i);

        if (sceneDrivenLayout && IsConstructionMode && Current != null)
        {
            float rotationStep = kb.leftAltKey.isPressed || kb.rightAltKey.isPressed ? 1f
                : kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed ? 15f : 5f;
            if (kb.eKey.wasPressedThisFrame) buildRotation = Mathf.Repeat(buildRotation + rotationStep, 360f);
            if (kb.qKey.wasPressedThisFrame) buildRotation = Mathf.Repeat(buildRotation - rotationStep, 360f);
            if (kb.gKey.wasPressedThisFrame) snapBuildPosition = !snapBuildPosition;
        }

        if (kb.spaceKey.wasPressedThisFrame && !night && !InteractionOpen) StartCoroutine(RunNight());

        Vector2 gm = GuiMouse(mouse);
        bool overMenu = PointerOverGameplayUI(gm);
        if (overMenu && mouse.leftButton.wasPressedThisFrame) blockedAttackFrame = Time.frameCount;

        if (IsConstructionMode && overMenu && mouse.leftButton.wasPressedThisFrame)
        {
            int c = CardAt(gm);
            if (c >= 0) EquipBuilding(c);
        }

        UpdateHover(mouse, overMenu);
        UpdateInteraction(kb, mouse, overMenu);

        if (!night && !combatCameraMode && !overMenu)
        {
            if (mouse.leftButton.wasPressedThisFrame && Current != null) TryBuild();
            if (mouse.rightButton.wasPressedThisFrame) EmptyHand();
        }
    }

    BuildingData Current => (selected >= 0 && selected < activeMenu.Count) ? activeMenu[selected] : null;

    bool CellFree(Vector2Int c)
    {
        if (!grid.InBounds(c) || grid.Get(c) != null) return false;
        if (!InBuildGrid(c)) return false;
        foreach (var s in spawns)
            if (Mathf.Abs(s.x - c.x) <= spawnBuildClearance && Mathf.Abs(s.y - c.y) <= spawnBuildClearance) return false;
        if (Mathf.Abs(grid.CoreCell.x - c.x) <= coreBuildClearance &&
            Mathf.Abs(grid.CoreCell.y - c.y) <= coreBuildClearance) return false;
        return true;
    }

    bool InBuildGrid(Vector2Int c)
    {
        int width = buildGridWidth <= 0 ? grid.width : Mathf.Min(buildGridWidth, grid.width);
        int height = buildGridHeight <= 0 ? grid.height : Mathf.Min(buildGridHeight, grid.height);
        int minX = Mathf.Clamp((grid.width - width) / 2 + buildGridOffset.x, 0, grid.width - width);
        int minY = Mathf.Clamp((grid.height - height) / 2 + buildGridOffset.y, 0, grid.height - height);
        return c.x >= minX && c.x < minX + width && c.y >= minY && c.y < minY + height;
    }

    void DrawBuildGridBoundary()
    {
        int width = buildGridWidth <= 0 ? grid.width : Mathf.Min(buildGridWidth, grid.width);
        int height = buildGridHeight <= 0 ? grid.height : Mathf.Min(buildGridHeight, grid.height);
        if (width == grid.width && height == grid.height && buildGridOffset == Vector2Int.zero) return;

        int minX = Mathf.Clamp((grid.width - width) / 2 + buildGridOffset.x, 0, grid.width - width);
        int minY = Mathf.Clamp((grid.height - height) / 2 + buildGridOffset.y, 0, grid.height - height);
        float half = grid.cellSize * 0.5f;
        float left = grid.CellToWorld(new Vector2Int(minX, minY)).x - half;
        float right = grid.CellToWorld(new Vector2Int(minX + width - 1, minY)).x + half;
        float bottom = grid.CellToWorld(new Vector2Int(minX, minY)).z - half;
        float top = grid.CellToWorld(new Vector2Int(minX, minY + height - 1)).z + half;
        float y = grid.transform.position.y + 0.12f;

        var border = new GameObject("BuildGridBoundary").AddComponent<LineRenderer>();
        border.useWorldSpace = true;
        border.loop = true;
        border.positionCount = 4;
        border.startWidth = border.endWidth = 0.08f;
        border.startColor = border.endColor = new Color(0.35f, 1f, 0.55f, 0.9f);
        border.material = lineMaterial != null ? new Material(lineMaterial) : new Material(Shader.Find("Sprites/Default"));
        border.SetPositions(new[]
        {
            new Vector3(left, y, bottom), new Vector3(right, y, bottom),
            new Vector3(right, y, top), new Vector3(left, y, top)
        });
    }

    void LateUpdate()
    {
        if (!lost && Mouse.current != null) UpdateCameraPan(Mouse.current);
    }

    void UpdateCameraPan(Mouse mouse)
    {
        var cam = Camera.main;
        if (cam == null || grid == null) return;

        Transform panTransform = virtualCamera != null ? virtualCamera.transform : cam.transform;
        if (combatCameraMode)
        {
            Transform followTarget = combatCameraTarget;
            if (followTarget == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                followTarget = player != null ? player.transform : (core != null ? core.transform : null);
            }
            if (followTarget != null)
            {
                var followPlane = new Plane(Vector3.up, grid.transform.position);
                Ray followRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (followPlane.Raycast(followRay, out float followDistance))
                {
                    Vector3 delta = followTarget.position - followRay.GetPoint(followDistance);
                    delta.y = 0f;
                    panTransform.position += delta * (1f - Mathf.Exp(-combatCameraFollowSpeed * Time.unscaledDeltaTime));
                }
                draggingBuildCamera = false;
                return;
            }
        }

        if (PointerOverGameplayUI(GuiMouse(mouse))) { draggingBuildCamera = false; return; }
        Vector2 pointer = mouse.position.ReadValue();
        float x = pointer.x <= cameraEdgeSize ? -1f : (pointer.x >= Screen.width - cameraEdgeSize ? 1f : 0f);
        float y = pointer.y <= cameraEdgeSize ? -1f : (pointer.y >= Screen.height - cameraEdgeSize ? 1f : 0f);
        Vector3 dragShift = Vector3.zero;
        bool dragged = false;
        if (!combatCameraMode)
        {
            if (Keyboard.current.wKey.isPressed) y += 1f;
            if (Keyboard.current.sKey.isPressed) y -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;

            float scroll = mouse.scroll.ReadValue().y;
            if (!Mathf.Approximately(scroll, 0f))
                SetCameraZoom(cam, Mathf.Clamp(GetCameraZoom(cam) - scroll * buildCameraZoomSpeed * 0.01f,
                    buildCameraMinZoom, buildCameraMaxZoom));

            if (mouse.middleButton.wasPressedThisFrame)
            {
                draggingBuildCamera = true;
                lastBuildCameraPointer = pointer;
            }
            if (mouse.middleButton.wasReleasedThisFrame) draggingBuildCamera = false;
            if (draggingBuildCamera && mouse.middleButton.isPressed)
            {
                Vector2 drag = pointer - lastBuildCameraPointer;
                lastBuildCameraPointer = pointer;
                Vector3 rightOnGround = panTransform.right;
                Vector3 upOnGround = panTransform.up;
                rightOnGround.y = 0f;
                upOnGround.y = 0f;
                float worldPerPixel = GetCameraZoom(cam) * 2f / Mathf.Max(1f, Screen.height);
                dragShift = -rightOnGround.normalized * drag.x * worldPerPixel
                    - upOnGround.normalized * drag.y * worldPerPixel;
                dragged = dragShift.sqrMagnitude > 0f;
            }
        }
        if (dragged)
        {
            panTransform.position += dragShift;
            if (panTransform != cam.transform) cam.transform.position += dragShift;
            return;
        }
        if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f)) return;

        Vector3 right = panTransform.right;
        Vector3 forward = panTransform.forward;
        right.y = 0f;
        forward.y = 0f;
        Vector3 direction = right.normalized * x + forward.normalized * y;
        if (direction.sqrMagnitude < 0.001f) return;
        direction.Normalize();

        var ground = new Plane(Vector3.up, grid.transform.position);
        Ray centerRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!ground.Raycast(centerRay, out float distance)) return;
        Vector3 center = centerRay.GetPoint(distance);
        bool shiftPan = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        Vector3 nextCenter = center + direction * cameraPanSpeed * (shiftPan ? 3f : 1f) * Time.unscaledDeltaTime;
        float halfWidth = grid.width * grid.cellSize * 0.5f;
        float halfHeight = grid.height * grid.cellSize * 0.5f;
        nextCenter.x = Mathf.Clamp(nextCenter.x, grid.transform.position.x - halfWidth, grid.transform.position.x + halfWidth);
        nextCenter.z = Mathf.Clamp(nextCenter.z, grid.transform.position.z - halfHeight, grid.transform.position.z + halfHeight);
        Vector3 shift = nextCenter - center;
        shift.y = 0f;
        panTransform.position += shift;
        if (panTransform != cam.transform) cam.transform.position += shift;
    }

    void CenterCameraOn(Vector3 worldPosition)
    {
        var cam = Camera.main;
        if (cam == null || grid == null) return;
        Transform panTransform = virtualCamera != null ? virtualCamera.transform : cam.transform;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var ground = new Plane(Vector3.up, grid.transform.position);
        if (!ground.Raycast(ray, out float distance)) return;
        Vector3 shift = worldPosition - ray.GetPoint(distance);
        shift.y = 0f;
        panTransform.position += shift;
        if (panTransform != cam.transform) cam.transform.position += shift;
    }

    float GetCameraZoom(Camera cam)
    {
        return virtualCamera != null ? virtualCamera.Lens.OrthographicSize : cam.orthographicSize;
    }

    void SetCameraZoom(Camera cam, float size)
    {
        if (virtualCamera != null)
        {
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = size;
            virtualCamera.Lens = lens;
        }
        else cam.orthographicSize = size;
    }

    void UpdateHover(Mouse mouse, bool overMenu)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        var plane = new Plane(Vector3.up, grid.transform.position);
        if (plane.Raycast(ray, out float d))
        {
            hoverPosition = ray.GetPoint(d);
            if (sceneDrivenLayout && snapBuildPosition && !Keyboard.current.leftAltKey.isPressed && !Keyboard.current.rightAltKey.isPressed)
            {
                hoverPosition.x = Mathf.Round(hoverPosition.x / buildPlacementStep) * buildPlacementStep;
                hoverPosition.z = Mathf.Round(hoverPosition.z / buildPlacementStep) * buildPlacementStep;
            }
            hoverCell = grid.WorldToCell(hoverPosition);
            bool show = grid.InBounds(hoverCell) && !night && !combatCameraMode && !overMenu && Current != null;
            cursor.SetActive(show);
            if (previewOutline != null) previewOutline.enabled = show;
            if (show)
            {
                var cur = Current;
                SpecialBuildPoint slot;
                bool placeable = CanPlace(cur, hoverPosition, out slot);
                bool ok = placeable && wallet.CanAfford(cur.costs);
                Vector3 previewPosition = slot != null ? slot.transform.position : hoverPosition;
                if (!sceneDrivenLayout) previewPosition = grid.CellToWorld(hoverCell);
                cursor.transform.position = previewPosition + Vector3.up * 0.06f;
                cursor.transform.rotation = Quaternion.Euler(0f, sceneDrivenLayout ? buildRotation : 0f, 0f);
                cursor.transform.localScale = sceneDrivenLayout
                    ? new Vector3(Mathf.Max(0.5f, cur.size.x), 0.12f, Mathf.Max(0.5f, cur.size.z))
                    : new Vector3(grid.cellSize * 0.95f, 0.1f, grid.cellSize * 0.95f);
                cursor.GetComponent<Renderer>().material.color =
                    ok ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.2f, 0.2f);
                UpdatePreviewOutline(cursor.transform.position, cursor.transform.localScale,
                    cursor.transform.rotation, ok ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.2f, 0.2f));
            }
        }
        else
        {
            cursor.SetActive(false);
            if (previewOutline != null) previewOutline.enabled = false;
        }
    }

    void TryBuild()
    {
        var d = Current;
        if (d == null) return;
        SpecialBuildPoint slot;
        if (!CanPlace(d, hoverPosition, out slot)) { Say("Não dá para construir aqui."); return; }
        if (!wallet.Spend(d.costs)) { Say("Recursos insuficientes."); return; }

        if (sceneDrivenLayout)
        {
            Vector3 position = slot != null ? slot.transform.position : hoverPosition;
            Structure placed = MakeStructureAt(d, position, Quaternion.Euler(0f, buildRotation, 0f), false);
            if (slot != null)
            {
                slot.occupant = placed;
                slot.occupied = true;
            }
        }
        else MakeStructure(d, hoverCell);
        grid.Recalc();
    }

    bool CanPlace(BuildingData data, Vector3 position, out SpecialBuildPoint matchedSlot, Structure ignored = null)
    {
        matchedSlot = null;
        if (!sceneDrivenLayout)
        {
            Vector2Int cell = grid.WorldToCell(position);
            if (data == null || (!CellFree(cell) && (ignored == null || grid.Get(cell) != ignored))) return false;
            Vector3 center = grid.CellToWorld(cell) + Vector3.up;
            Vector3 half = new Vector3(data.size.x * grid.cellSize * 0.5f, 2f,
                data.size.z * grid.cellSize * 0.5f);
            return !Physics.CheckBox(center, half, Quaternion.identity, 1 << 2, QueryTriggerInteraction.Ignore);
        }
        if (buildArea == null || data == null) return false;

        float width = data.size.x;
        float depth = data.size.z;
        float radius = Mathf.Sqrt(width * width + depth * depth) * 0.5f;
        Vector3 candidate = position;
        if (data.requiresSpecialBuildPoint)
        {
            float bestDistance = float.MaxValue;
            foreach (var point in specialBuildPoints)
            {
                if (point == null || (point.occupied && point.occupant != ignored) || point.requiredBuildingId != data.specialBuildPointId) continue;
                Vector2 delta = new Vector2(position.x - point.transform.position.x, position.z - point.transform.position.z);
                if (delta.magnitude > point.activationRadius || delta.magnitude >= bestDistance) continue;
                bestDistance = delta.magnitude;
                matchedSlot = point;
            }
            if (matchedSlot == null) return false;
            candidate = matchedSlot.transform.position;
        }

        if (!buildArea.Contains(candidate, radius)) return false;
        int solidMask = (1 << 2) | (1 << 9) | (1 << 10) | (1 << 11) | (1 << 12);
        bool tall = data.shape == PrimitiveType.Cylinder || data.shape == PrimitiveType.Capsule;
        float height = data.size.y * (tall ? 2f : 1f);
        Vector3 halfExtents = new Vector3(width * 0.5f, Mathf.Max(0.5f, height * 0.5f), depth * 0.5f);
        Vector3 boxCenter = candidate + Vector3.up * halfExtents.y;
        Quaternion rotation = Quaternion.Euler(0f, buildRotation, 0f);
        Physics.SyncTransforms();
        var hits = Physics.OverlapBox(boxCenter, halfExtents, rotation, solidMask, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
            if (ignored == null || hit.GetComponentInParent<Structure>() != ignored) return false;
        return !OverlapsExistingStructure(candidate, width, height, depth, rotation, ignored);
    }

    bool OverlapsExistingStructure(Vector3 candidate, float width, float height, float depth, Quaternion rotation, Structure ignored = null)
    {
        Vector3 candidateRight3 = rotation * Vector3.right;
        Vector3 candidateForward3 = rotation * Vector3.forward;
        Vector2 candidateRight = new Vector2(candidateRight3.x, candidateRight3.z);
        Vector2 candidateForward = new Vector2(candidateForward3.x, candidateForward3.z);
        Vector2 center = new Vector2(candidate.x, candidate.z);
        Vector2[] axes = { candidateRight, candidateForward, Vector2.right, Vector2.up };
        float candidateTop = candidate.y + height;

        foreach (var structure in Structure.All)
        {
            if (structure == null || structure == ignored) continue;
            if (!TryGetStructureBounds(structure.gameObject, out Bounds bounds)) continue;
            if (bounds.max.y <= candidate.y + 0.01f || bounds.min.y >= candidateTop - 0.01f) continue;

            Vector2 otherCenter = new Vector2(bounds.center.x, bounds.center.z);
            Vector2 delta = otherCenter - center;
            bool separated = false;
            foreach (Vector2 axis in axes)
            {
                float candidateRadius = width * 0.5f * Mathf.Abs(Vector2.Dot(candidateRight, axis))
                    + depth * 0.5f * Mathf.Abs(Vector2.Dot(candidateForward, axis));
                float otherRadius = bounds.extents.x * Mathf.Abs(axis.x) + bounds.extents.z * Mathf.Abs(axis.y);
                if (Mathf.Abs(Vector2.Dot(delta, axis)) >= candidateRadius + otherRadius - 0.01f)
                {
                    separated = true;
                    break;
                }
            }
            if (!separated) return true;
        }
        return false;
    }

    void Say(string t, float seconds = 2.5f) { msg = t; msgTimer = seconds; }

    public void RaiseGameplayEvent(GameplayEventType type, GameObject subject = null, float value = 0f, string details = null)
    {
        Vector3 position = subject != null ? subject.transform.position : Vector3.zero;
        var gameEvent = new GameplayEvent(type, position, subject, value,
            string.IsNullOrEmpty(details) ? (subject != null ? subject.name : type.ToString()) : details);
        recentEvents.Add(gameEvent);
        while (recentEvents.Count > eventLogEntries) recentEvents.RemoveAt(0);
        GameplayEventRaised?.Invoke(gameEvent);
    }

    void SelectNextScenario()
    {
        if (scenarios.Count == 0)
        {
            Say("Sem cenários. Use Tools > Village > Create Playtest Scenario Templates no Editor.");
            return;
        }

        int index = scenarios.FindIndex(s => s.name == selectedScenarioName);
        index = (index + 1) % scenarios.Count;
        selectedScenarioName = scenarios[index].name;
        scenarioChangePending = true;
        Say($"Próximo cenário: {scenarios[index].displayName}. Aperte R para reiniciar e aplicar.", 5f);
    }

    // ---------- Noite ----------
    IEnumerator RunNight()
    {
        if (night || lost || IsOnExpedition || expeditionTransition) yield break;
        if (spawns.Count == 0) { Say("Não há pontos de spawn configurados para esta noite."); yield break; }

        NightPlan nightPlan = null;
        try { nightPlan = PlanNight(round + 1); }
        catch (System.Exception exception) { Debug.LogException(exception); Say("Não foi possível preparar a composição desta noite."); }
        if (nightPlan == null) yield break;

        night = true;
        combatCameraMode = true;
        nightsStarted++;
        CloseInteraction();
        TotalWaves = nightPlan.waves;
        PlannedNightEnemies = nightPlan.total;
        SpawnedThisNight = 0;
        CurrentWave = 0;
        RaiseGameplayEvent(GameplayEventType.NightStarted, null, round,
            $"Noite do Dia {Day} começou • {TotalWaves} levas • {PlannedNightEnemies} inimigos • mínimo {CurrentNightMinimum}");

        bool endedByDesertion = false;
        var waves = new NightWaveSequence(TotalWaves, nightPlan.groupsPerWave);
        while (!lost)
        {
            CurrentWave = waves.CurrentWave;
            RaiseGameplayEvent(GameplayEventType.WaveStarted, null, CurrentWave,
                $"Dia {Day} • leva {CurrentWave}/{TotalWaves} • {waves.GroupsPerWave} grupos");
            for (int group = 0; group < waves.GroupsPerWave && !lost; group++)
            {
                var cell = spawns[Random.Range(0, spawns.Count)];
                yield return SpawnGroup(cell, nightPlan.groups[(CurrentWave - 1) * waves.GroupsPerWave + group]);
                if (lost) break;
                waves.RecordGroupSpawned();
                if (group + 1 < waves.GroupsPerWave)
                {
                    float delay = 0f;
                    while (!lost && delay < groupInterval)
                    { delay += Time.deltaTime; yield return null; }
                }
            }
            // A wave is resolved only after its entire spawn schedule and its
            // surviving enemies are gone. Group spacing never advances waves.
            while (!lost && Enemy.All.Count > 0)
            {
                if (AllRemainingEnemiesAreDeserters())
                {
                    endedByDesertion = true;
                    foreach (var enemy in new List<Enemy>(Enemy.All))
                        if (enemy != null && enemy.state == EState.Flee) Destroy(enemy.gameObject);
                    yield return null;
                    continue;
                }
                yield return null;
            }
            if (lost || waves.CanFinishNight(Enemy.All.Count)) break;
            float waveDelay = 0f;
            while (!lost && waveDelay < waveInterval)
            { waveDelay += Time.deltaTime; yield return null; }
            while (!lost && !waves.TryAdvance(Enemy.All.Count)) yield return null;
        }

        night = false;
        combatCameraMode = false;
        if (lost) yield break;

        nightPopulation.RecordCompleted(round, SpawnedThisNight);
        if (core != null && core.health != null) core.health.RestoreFull();
        if (PlayerVitals.I != null) PlayerVitals.I.RestoreAtDawn();
        int crystals = dawnCrystalsBase + round;
        wallet.Add(DefaultResources.Wood, dawnWood);
        wallet.Add(DefaultResources.Crystals, crystals);
        string dawnDetails = endedByDesertion
            ? $"Amanheceu após a deserção dos inimigos restantes na noite {round}"
            : $"Amanheceu após a noite {round}";
        RaiseGameplayEvent(GameplayEventType.DawnReached, null, round, dawnDetails);
        Say(endedByDesertion
            ? $"Os inimigos restantes fugiram. Amanheceu! +{crystals} {DefaultResources.Crystals.displayName} e +{dawnWood} {DefaultResources.Wood.displayName}."
            : $"Amanheceu! Chegaram +{crystals} {DefaultResources.Crystals.displayName} e +{dawnWood} {DefaultResources.Wood.displayName}. Construa e aperte Espaço.", 5f);
        DiscoverExpeditions();
    }

    bool AllRemainingEnemiesAreDeserters()
    {
        bool foundEnemy = false;
        foreach (var enemy in Enemy.All)
        {
            if (enemy == null) continue;
            foundEnemy = true;
            if (enemy.state != EState.Flee) return false;
        }
        return foundEnemy;
    }

    IEnumerator SpawnGroup(Vector2Int cell, NightGroupPlan plan)
    {
        var gp = new GameObject(plan.name).AddComponent<EnemyGroup>();
        gp.minSize = Mathf.Min(groupMinSize, plan.members.Length);
        gp.loneWolfChance = plan.fixedOpening ? 0f : loneWolfChance;
        gp.tacticalGroupChance = tacticalGroupChance;
        gp.randomizeTactic = false;
        gp.tactic = plan.tactic;
        gp.groupMergeRadius = groupMergeRadius;
        gp.expected = plan.members.Length;

        for (int i = 0; i < gp.expected; i++)
        {
            if (lost) yield break;
            SpawnEnemy(gp, cell, plan.members[i]);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnEnemy(EnemyGroup gp, Vector2Int cell, NightEnemySpec spec)
    {
        EnemyType type = spec.type;
        Personality pers = spec.personality;
        PrimitiveType shape = type == EnemyType.Tank ? PrimitiveType.Cylinder
                            : (pers == Personality.Hunter ? PrimitiveType.Cube : PrimitiveType.Sphere);
        var balance = spec.archetype != null ? spec.archetype.balance : type == EnemyType.Tank ? tankEnemy : commonEnemy;
        Vector3 scale = balance.scale;
        float y = scale.y * 0.5f;

        Vector3 pos = grid.CellToWorld(cell) + new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
        pos.y = grid.transform.position.y + y;

        var go = Prim(shape, pos, scale, Color.red);
        go.name = type + "_" + pers;
        go.layer = LayerMask.NameToLayer("Enemies");
        go.AddComponent<Health>();
        var e = go.AddComponent<Enemy>();
        e.Init(type, pers, spec.bravery, gp);
        e.archetype = spec.archetype;
        e.ApplyBalance(balance);
        e.socialProfile.randomizeOnSpawn = false;
        e.socialProfile.temperament = spec.temperament;
        e.socialProfile.behavior = spec.behavior;
        gp.Add(e);
        gp.ApplySocialProfile(e);
        SpawnedThisNight++;
        RaiseGameplayEvent(GameplayEventType.EnemySpawned, go, 0f, $"Surgiu {type} ({pers})");
    }

    // ---------- HUD ----------
    static string CostText(BuildingData d)
    {
        var parts = new List<string>();
        if (d.costs != null)
            foreach (var cost in d.costs)
                if (cost.resource != null && cost.amount > 0) parts.Add(cost.resource.displayName + " " + cost.amount);
        return parts.Count > 0 ? string.Join("  ", parts) : "Grátis";
    }

    void DrawStructureLabels()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        labelStyle.normal.textColor = Color.white;
        foreach (var structure in Structure.All)
        {
            if (structure == null || structure.data != null
                || (structure.kind == StructureKind.Wall && structure.data == null)) continue;
            var renderer = structure.GetComponent<Renderer>();
            Vector3 worldPosition = renderer != null
                ? renderer.bounds.center + Vector3.up * (renderer.bounds.extents.y + 0.35f)
                : structure.transform.position + Vector3.up * 2f;
            Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f || screenPosition.x < 0f || screenPosition.x > Screen.width
                || screenPosition.y < 0f || screenPosition.y > Screen.height) continue;

            string label = structure.data != null ? structure.data.displayName : structure.gameObject.name;
            Rect rect = new Rect(screenPosition.x - 68f, Screen.height - screenPosition.y - 13f, 136f, 24f);
            GUI.color = new Color(0.025f, 0.03f, 0.04f, 0.78f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, label, labelStyle);
        }
    }

    void OnGUI()
    {
        if (IsOnExpedition || expeditionTransition) { DrawExpeditionUI(); return; }
        var st = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        st.normal.textColor = Color.white;
        bool showConstructionUI = !night && !combatCameraMode;
        string activeModeName = night
            ? (combatCameraMode ? "Combate" : "Livre")
            : (combatCameraMode ? "Exploração" : "Construção");
        string alternateModeName = night
            ? (combatCameraMode ? "Livre" : "Combate")
            : (combatCameraMode ? "Construção" : "Exploração");
        string modeHint = $"Modo {activeModeName}  |  Tab: Modo {alternateModeName}";

        if (sceneDrivenLayout && showConstructionUI) DrawStructureLabels();

        float coreHp = core != null ? core.health.current : 0f;
        GUI.Label(new Rect(10, 8, 1400, 26),
            $"Dia {Day}   {(night ? $"NOITE • Leva {CurrentWave}/{TotalWaves}" : "MANHÃ")}   {DefaultResources.Wood.displayName}: {wallet.Wood}   {DefaultResources.Crystals.displayName}: {wallet.Crystals}   Core: {coreHp:0}   Jogador: {(PlayerVitals.I != null ? Mathf.Max(0f, PlayerVitals.I.Health.current) : 0f):0}   Abates: {killed}   Desertores: {deserted}", st);

        GUI.Label(new Rect(10, 32, 1400, 26), night
            ? modeHint + (combatCameraMode
                ? "  |  WASD: mover jogador  |  E: interagir  |  Sobreviva à noite..."
                : "  |  WASD/borda: mover, roda: zoom, botão do meio: arrastar")
            : combatCameraMode
                ? modeHint + "  |  WASD: mover jogador  |  E: interagir  |  Espaço: iniciar noite"
            : sceneDrivenLayout
                ? modeHint + "  |  Clique: selecionar / construir  |  Dir./0/Esc: mão vazia  |  Q/E: girar  |  [1-9] escolher  |  [Espaço] noite  |  WASD: câmera"
                : modeHint + "  |  Clique: selecionar / construir  |  Dir./0/Esc: mão vazia  |  [1-9] escolher  |  [Espaço] noite  |  [R] reiniciar", st);
        GUI.Label(new Rect(10, 56, 1400, 26), msg, st);
        if (night)
            GUI.Label(new Rect(10, 82, 1100, 24),
                $"Inimigos surgidos: {SpawnedThisNight}/{PlannedNightEnemies} • Mínimo do bloco: {CurrentNightMinimum} • Recorde de noite vencida no bloco: {(nightPopulation != null ? nightPopulation.PeakForBlock(round) : 0)}", st);
        if (sceneDrivenLayout && showConstructionUI && buildArea != null)
        {
            GUI.Label(new Rect(10, 82, 510, 24), $"Área: {buildArea.Radius:0} m ({buildArea.expansionLevel}/{buildArea.maxExpansionLevel})  |  Verde: atual, amarelo: próxima expansão", st);
            if (!night && buildArea.CanExpand && GUI.Button(new Rect(10, 108, 230, 30),
                    $"Expandir área ({buildArea.NextWoodCost} M / {buildArea.NextCrystalsCost} C)"))
            {
                if (buildArea.TryExpand(wallet)) Say("A área de construção foi expandida.");
                else Say("Recursos insuficientes para expandir a área.");
            }
            else if (!buildArea.CanExpand)
                GUI.Label(new Rect(10, 108, 230, 24), "Área no tamanho máximo", st);
        }
        string scenarioLabel = scenario != null ? scenario.displayName : "Inspector";
        if (scenarioChangePending && scenarios.Count > 0)
        {
            var pending = scenarios.Find(s => s.name == selectedScenarioName);
            if (pending != null) scenarioLabel = $"{scenarioLabel} -> {pending.displayName} (R para aplicar)";
        }
        if (!sceneDrivenLayout || showConstructionUI)
        {
            float scenarioY = sceneDrivenLayout ? 144f : 80f;
            GUI.Label(new Rect(10, scenarioY, 1100, 22), $"Cenário: {scenarioLabel}   Mapa: {grid.width} x {grid.height}   Entradas: {spawns.Count}", st);
        }

        if (showEventLog)
        {
            float rowHeight = 20f;
            float panelWidth = Mathf.Min(440f, Screen.width * 0.45f);
            float panelHeight = (recentEvents.Count + 1) * rowHeight + 12f;
            var panel = new Rect(Screen.width - panelWidth - 10f, 108f, panelWidth, panelHeight);
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.82f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 8, panel.y + 4, panel.width - 16, rowHeight), "EVENTOS DE PLAYTEST [F3]", st);
            for (int i = 0; i < recentEvents.Count; i++)
            {
                var ev = recentEvents[recentEvents.Count - 1 - i];
                GUI.Label(new Rect(panel.x + 8, panel.y + 4 + (i + 1) * rowHeight, panel.width - 16, rowHeight),
                    $"{ev.time:0}s  {ev.details}", st);
            }
        }

        if (!sceneDrivenLayout && scenario != null && !string.IsNullOrEmpty(scenario.instructions))
            GUI.Label(new Rect(10, 104, 700, 38), scenario.instructions, st);

        if (showConstructionUI)
        {
        // Menu de construção categorizado, com paginação horizontal para permitir crescimento.
        var nameSt = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        var smallSt = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        GUI.color = new Color(0.035f, 0.045f, 0.06f, 0.93f);
        GUI.DrawTexture(new Rect(0f, Screen.height - 207f, Screen.width, 207f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float categoryWidth = menuCategories.Count > 0
            ? Mathf.Min(150f, (Screen.width - 28f) / menuCategories.Count) : 0f;
        for (int i = 0; i < menuCategories.Count; i++)
        {
            var category = menuCategories[i];
            int count = 0;
            for (int b = 0; b < menu.Count; b++) if (ResolveMenuCategory(menu[b]) == category) count++;
            Rect tab = new Rect(14f + i * categoryWidth, Screen.height - 198f, categoryWidth - 5f, 28f);
            bool wasSelected = category == activeMenuCategory;
            if (GUI.Toggle(tab, wasSelected, $"{MenuCategoryLabel(category)} ({count})", GUI.skin.button) && !wasSelected)
                SelectMenuCategory(category);
        }

        if (menuPage > 0 && GUI.Button(new Rect(8f, Screen.height - 125f, 34f, 58f), "‹")) menuPage--;
        if (VisibleMenuStart + VisibleMenuCount < activeMenu.Count &&
            GUI.Button(new Rect(Screen.width - 42f, Screen.height - 125f, 34f, 58f), "›")) menuPage++;

        for (int i = 0; i < VisibleMenuCount; i++)
        {
            int buildingIndex = VisibleMenuStart + i;
            var d = activeMenu[buildingIndex];
            var r = CardRect(i);
            bool afford = wallet.CanAfford(d.costs);

            GUI.color = buildingIndex == selected ? new Color(0.22f, 0.42f, 0.28f, 0.95f) : new Color(0.1f, 0.1f, 0.13f, 0.88f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = d.color;
            GUI.DrawTexture(new Rect(r.x + 6, r.y + 8, 10, r.height - 16), Texture2D.whiteTexture);
            GUI.color = Color.white;

            nameSt.normal.textColor = Color.white;
            GUI.Label(new Rect(r.x + 22, r.y + 4, r.width - 26, 22), $"[{i + 1}] {d.displayName}", nameSt);

            smallSt.normal.textColor = afford ? new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.5f, 0.5f);
            GUI.Label(new Rect(r.x + 22, r.y + 24, r.width - 26, 18), CostText(d), smallSt);

            smallSt.normal.textColor = new Color(0.75f, 0.75f, 0.8f);
            string extra = d.kind == BuildingKind.Tower
                ? $"Vida {d.maxHealth:0}  Dano {d.towerDamage:0}  Alcance {d.towerRange:0}"
                : $"Vida {d.maxHealth:0}";
            GUI.Label(new Rect(r.x + 22, r.y + 42, r.width - 26, 18), extra, smallSt);
        }

        if (sceneDrivenLayout && !night)
        {
            var controlsStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            controlsStyle.normal.textColor = new Color(0.8f, 0.84f, 0.9f);
            GUI.Label(new Rect(14f, Screen.height - 29f, Screen.width - 28f, 22f),
                "Q/E: girar 5° (Alt: 1°, Shift: 15°)  |  G: encaixe 0,5 m  |  Alt: posicionamento livre  |  Shift + mover: pan rápido  |  C: centralizar na Igreja",
                controlsStyle);
        }
        }

        GUI.Label(new Rect(10, Screen.height - 54, 1600, 26),
            "Esfera = Objective (ataca estruturas próximas)   Cubo = Hunter (prioriza torres)   Cilindro grande = Tank (prioriza o Núcleo; rompe muros/cercas conforme a rota)   Bolinha branca = líder", st);
        GUI.Label(new Rect(10, Screen.height - 30, 1600, 26),
            "Vermelho = Fanático   Laranja = Cobiçoso   Rosa = Covarde   Cinza = desertor", st);

        DrawInteractionUI();
        if (lost)
        {
            var big = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 120),
                $"DERROTA — {defeatReason}\n[R] reiniciar", big);
            if (Application.CanStreamedLevelBeLoaded("MenuPrincipal") &&
                GUI.Button(new Rect(Screen.width / 2f - 120f, Screen.height / 2f + 75f, 240f, 40f), "Voltar ao menu principal"))
                SceneManager.LoadScene("MenuPrincipal");
        }
    }
}
