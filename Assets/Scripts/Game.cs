using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

// Coloque este script em UM GameObject vazio na cena. Ele cria todo o resto.
public class Game : MonoBehaviour
{
    public static Game I;

    [Header("Materiais (obrigatório para a build)")]
    public Material litMaterial;
    public Material lineMaterial;

    [Header("Recursos iniciais")]
    public int startWood = 60;
    public int startSupplies = 6;

    [Header("Amanhecer (temporário, até os círculos mágicos)")]
    public int dawnWood = 10;
    public int dawnSuppliesBase = 3;

    [Header("Mapa de playtest")]
    [Range(11, 61)] public int gridWidth = 21;
    [Range(11, 61)] public int gridHeight = 21;
    [Range(21, 61)] public int maxGridDimension = 41;
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
    [Min(1f)] public float cameraPanSpeed = 16f;
    [Min(1f)] public float cameraEdgeSize = 28f;

    [Header("Playtest")]
    [Min(1f)] public float coreHealth = 300f;
    [Min(1)] public int eventLogEntries = 8;
    public bool showEventLog = true;
    public PlaytestScenario scenario;

    [Header("Balanceamento de inimigos")]
    public EnemyBalanceSettings commonEnemy = new EnemyBalanceSettings();
    public EnemyBalanceSettings tankEnemy = new EnemyBalanceSettings
    {
        maxHealth = 140f,
        speed = 2.2f,
        damage = 14f,
        scale = new Vector3(1.8f, 0.9f, 1.8f)
    };

    [Header("Balanceamento das noites")]
    [Min(0)] public int initialGroups = 1;
    [Min(0)] public int groupsPerRounds = 2;
    [Min(1)] public int maxGroups = 4;
    [Min(0)] public int baseCommonEnemies = 4;
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

    [Header("Estado (somente leitura)")]
    public int round;
    public int killed;
    public int deserted;
    public bool night;
    public bool lost;

    public Wallet wallet = new Wallet();
    public Structure core;

    public event System.Action<GameplayEvent> GameplayEventRaised;

    static string selectedScenarioName;
    readonly List<PlaytestScenario> scenarios = new List<PlaytestScenario>();
    readonly List<GameplayEvent> recentEvents = new List<GameplayEvent>();
    bool scenarioChangePending;

    void OnValidate()
    {
        startWood = Mathf.Max(0, startWood);
        startSupplies = Mathf.Max(0, startSupplies);
        dawnWood = Mathf.Max(0, dawnWood);
        dawnSuppliesBase = Mathf.Max(0, dawnSuppliesBase);
        maxGridDimension = Mathf.Clamp(maxGridDimension, 21, 61);
        gridWidth = Mathf.Clamp(gridWidth, 11, maxGridDimension);
        gridHeight = Mathf.Clamp(gridHeight, 11, maxGridDimension);
        gridCellSize = Mathf.Max(0.1f, gridCellSize);
        pathBreakCost = Mathf.Max(0, pathBreakCost);
        spawnBuildClearance = Mathf.Max(0, spawnBuildClearance);
        coreBuildClearance = Mathf.Max(0, coreBuildClearance);
        buildGridWidth = Mathf.Max(0, buildGridWidth);
        buildGridHeight = Mathf.Max(0, buildGridHeight);
        cameraPanSpeed = Mathf.Max(1f, cameraPanSpeed);
        cameraEdgeSize = Mathf.Max(1f, cameraEdgeSize);
        coreHealth = Mathf.Max(1f, coreHealth);
        eventLogEntries = Mathf.Max(1, eventLogEntries);
        initialGroups = Mathf.Max(0, initialGroups);
        groupsPerRounds = Mathf.Max(1, groupsPerRounds);
        maxGroups = Mathf.Max(1, maxGroups);
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
        settings.scale = new Vector3(Mathf.Max(0.1f, settings.scale.x), Mathf.Max(0.1f, settings.scale.y), Mathf.Max(0.1f, settings.scale.z));
    }

    static Material baseMat;
    public static Material LineMat;

    GridWorld grid;
    CinemachineCamera virtualCamera;
    readonly List<Vector2Int> spawns = new List<Vector2Int>();
    List<BuildingData> menu = new List<BuildingData>();
    int selected;
    GameObject cursor;
    Vector2Int hoverCell;
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
        virtualCamera = FindFirstObjectByType<CinemachineCamera>();
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
        if (scenario != null) scenario.ApplyTo(this);

        wallet.Wood = startWood;
        wallet.Supplies = startSupplies;

        menu = new List<BuildingData>(Resources.LoadAll<BuildingData>("Buildings"));
        menu.RemoveAll(b => b.kind == StructureKind.Core);
        menu.Sort((a, b) => a.order.CompareTo(b.order));
        if (menu.Count == 0)
            Say("Nenhuma construção encontrada. Rode: Tools > Village > Create Default Buildings", 600f);

        grid = gameObject.AddComponent<GridWorld>();
        grid.Configure(gridWidth, gridHeight, gridCellSize, pathBreakCost);
        BuildWorld();
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

        // Coração (dados criados em código; não aparece no menu)
        var cd = ScriptableObject.CreateInstance<BuildingData>();
        cd.displayName = "Coração";
        cd.kind = StructureKind.Core;
        cd.shape = PrimitiveType.Cube;
        cd.color = new Color(0.2f, 0.4f, 1f);
        cd.size = new Vector3(0.9f, 1.8f, 0.9f);
        cd.maxHealth = coreHealth;
        core = MakeStructure(cd, grid.CoreCell);
        core.health.Died += _ => lost = true;

        // Cursor de construção
        cursor = Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(cs * 0.95f, 0.1f, cs * 0.95f), Color.green);
        cursor.name = "Cursor";

        grid.Recalc();
    }

    // Cria qualquer construção a partir de um BuildingData.
    Structure MakeStructure(BuildingData d, Vector2Int cell)
    {
        float cs = grid.cellSize;
        Vector3 p = grid.CellToWorld(cell);
        bool tall = d.shape == PrimitiveType.Cylinder || d.shape == PrimitiveType.Capsule;   // altura = 2 * escala Y
        float centerY = d.size.y * (tall ? 2f : 1f) / 2f;

        var go = Prim(d.shape, p + Vector3.up * centerY,
                      new Vector3(d.size.x * cs, d.size.y, d.size.z * cs), d.color);
        go.name = d.displayName;

        if (d.kind == StructureKind.Tower) go.AddComponent<Tower>().Setup(d);
        if (d.amount > 0)
        {
            var pr = go.AddComponent<Producer>();
            pr.Setup(d);
        }

        var h = go.AddComponent<Health>();
        h.Setup(d.maxHealth);
        var s = go.AddComponent<Structure>();
        s.kind = d.kind;
        s.cell = cell;
        s.health = h;
        s.data = d;
        h.Damaged += (health, amount) => RaiseGameplayEvent(GameplayEventType.StructureAttacked, go, amount,
            $"{d.displayName} recebeu {amount:0.#} dano ({health.current:0.#}/{health.max:0.#} vida)");
        h.Died += _ =>
        {
            RaiseGameplayEvent(GameplayEventType.StructureDestroyed, go, 0f, $"{d.displayName} foi destruída");
            if (d.kind == StructureKind.Core)
            {
                RaiseGameplayEvent(GameplayEventType.CoreDestroyed, go, 0f, "O Coração foi destruído");
                lost = true;
            }
        };
        grid.Set(cell, s);
        RaiseGameplayEvent(GameplayEventType.StructureBuilt, go, 0f, $"Construída: {d.displayName}");
        return s;
    }

    // ---------- Menu (retângulos usados tanto para desenhar quanto para detectar clique) ----------
    Rect CardRect(int i)
    {
        float total = menu.Count * (CardW + CardGap) - CardGap;
        float x0 = (Screen.width - total) / 2f;
        return new Rect(x0 + i * (CardW + CardGap), Screen.height - 134f, CardW, CardH);
    }

    Rect MenuArea()
    {
        if (menu.Count == 0) return new Rect(0, 0, 0, 0);
        var first = CardRect(0);
        var last = CardRect(menu.Count - 1);
        return new Rect(first.x - 8f, first.y - 8f, last.xMax - first.x + 16f, CardH + 16f);
    }

    static Vector2 GuiMouse(Mouse m)
    {
        var p = m.position.ReadValue();
        return new Vector2(p.x, Screen.height - p.y);
    }

    int CardAt(Vector2 gm)
    {
        for (int i = 0; i < menu.Count; i++)
            if (CardRect(i).Contains(gm)) return i;
        return -1;
    }

    // ---------- Loop ----------
    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (kb.f3Key.wasPressedThisFrame) showEventLog = !showEventLog;
        if (kb.f1Key.wasPressedThisFrame) SelectNextScenario();

        if (msgTimer > 0f) { msgTimer -= Time.deltaTime; if (msgTimer <= 0f) msg = ""; }

        if (lost) { cursor.SetActive(false); return; }

        for (int i = 0; i < menu.Count && i < 9; i++)
            if (kb[Key.Digit1 + i].wasPressedThisFrame) selected = i;

        UpdateCameraPan(mouse);

        if (kb.spaceKey.wasPressedThisFrame && !night) StartCoroutine(RunNight());

        Vector2 gm = GuiMouse(mouse);
        bool overMenu = MenuArea().Contains(gm);

        if (overMenu && mouse.leftButton.wasPressedThisFrame)
        {
            int c = CardAt(gm);
            if (c >= 0) selected = c;
        }

        UpdateHover(mouse, overMenu);

        if (!night && !overMenu)
        {
            if (mouse.leftButton.wasPressedThisFrame) TryBuild();
            if (mouse.rightButton.wasPressedThisFrame) TrySell();
        }
    }

    BuildingData Current => (menu.Count > 0 && selected < menu.Count) ? menu[selected] : null;

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

    void UpdateCameraPan(Mouse mouse)
    {
        var cam = Camera.main;
        if (cam == null || grid == null) return;

        Vector2 pointer = mouse.position.ReadValue();
        float x = pointer.x <= cameraEdgeSize ? -1f : (pointer.x >= Screen.width - cameraEdgeSize ? 1f : 0f);
        float y = pointer.y <= cameraEdgeSize ? -1f : (pointer.y >= Screen.height - cameraEdgeSize ? 1f : 0f);
        if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f)) return;

        Transform panTransform = virtualCamera != null ? virtualCamera.transform : cam.transform;
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
        Vector3 nextCenter = center + direction * cameraPanSpeed * Time.unscaledDeltaTime;
        float halfWidth = grid.width * grid.cellSize * 0.5f;
        float halfHeight = grid.height * grid.cellSize * 0.5f;
        nextCenter.x = Mathf.Clamp(nextCenter.x, grid.transform.position.x - halfWidth, grid.transform.position.x + halfWidth);
        nextCenter.z = Mathf.Clamp(nextCenter.z, grid.transform.position.z - halfHeight, grid.transform.position.z + halfHeight);
        Vector3 shift = nextCenter - center;
        shift.y = 0f;
        panTransform.position += shift;
        if (panTransform != cam.transform) cam.transform.position += shift;
    }

    void UpdateHover(Mouse mouse, bool overMenu)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        var plane = new Plane(Vector3.up, grid.transform.position);
        if (plane.Raycast(ray, out float d))
        {
            hoverCell = grid.WorldToCell(ray.GetPoint(d));
            bool show = grid.InBounds(hoverCell) && !night && !overMenu && Current != null;
            cursor.SetActive(show);
            if (show)
            {
                var cur = Current;
                bool ok = CellFree(hoverCell) && wallet.CanAfford(cur.woodCost, cur.suppliesCost);
                cursor.transform.position = grid.CellToWorld(hoverCell) + Vector3.up * 0.06f;
                cursor.GetComponent<Renderer>().material.color =
                    ok ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.2f, 0.2f);
            }
        }
        else cursor.SetActive(false);
    }

    void TryBuild()
    {
        var d = Current;
        if (d == null) return;
        if (!CellFree(hoverCell)) { Say("Não dá para construir aqui."); return; }
        if (!wallet.Spend(d.woodCost, d.suppliesCost)) { Say("Recursos insuficientes."); return; }

        MakeStructure(d, hoverCell);
        grid.Recalc();
    }

    void TrySell()
    {
        var s = grid.Get(hoverCell);
        if (s == null || s.kind == StructureKind.Core || s.data == null) return;
        wallet.Wood += Mathf.RoundToInt(s.data.woodCost * 0.7f);
        wallet.Supplies += Mathf.RoundToInt(s.data.suppliesCost * 0.7f);
        Destroy(s.gameObject);   // OnDestroy limpa a célula e recalcula o campo de fluxo
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
        if (night || lost) yield break;

        night = true;
        round++;
        RaiseGameplayEvent(GameplayEventType.NightStarted, null, round, $"Noite {round} começou");
        int groups = Mathf.Min(initialGroups + (round - 1) / Mathf.Max(1, groupsPerRounds), maxGroups);
        int commons = baseCommonEnemies + round * commonsPerRound;
        bool tank = round % Mathf.Max(1, tankEveryRounds) == 0;

        for (int g = 0; g < groups; g++)
        {
            if (lost) break;
            var cell = spawns[Random.Range(0, spawns.Count)];
            yield return SpawnGroup(cell, commons, tank);
            if (lost) break;

            float groupDelay = 0f;
            while (!lost && groupDelay < groupInterval)
            {
                groupDelay += Time.deltaTime;
                yield return null;
            }
        }

        if (!lost)
            yield return new WaitUntil(() => Enemy.All.Count == 0 || lost);

        night = false;
        if (lost) yield break;

        int sup = dawnSuppliesBase + round;
        wallet.Add(ResourceType.Wood, dawnWood);
        wallet.Add(ResourceType.Supplies, sup);
        RaiseGameplayEvent(GameplayEventType.DawnReached, null, round, $"Amanheceu após a noite {round}");
        Say($"Amanheceu! Chegaram +{sup} Suprimentos e +{dawnWood} Madeira. Construa e aperte Espaço.", 5f);
    }

    IEnumerator SpawnGroup(Vector2Int cell, int commons, bool tank)
    {
        var gp = new GameObject("Group").AddComponent<EnemyGroup>();
        gp.minSize = groupMinSize;
        gp.loneWolfChance = loneWolfChance;
        gp.tacticalGroupChance = tacticalGroupChance;
        gp.expected = commons + (tank ? 1 : 0);
        float hunterChance = Mathf.Min(hunterBaseChance + hunterChancePerRound * round, hunterChanceCap);

        for (int i = 0; i < gp.expected; i++)
        {
            if (lost) yield break;
            bool isTank = tank && i == gp.expected - 1;   // o Tank chega por último
            var pers = (!isTank && Random.value < hunterChance) ? Personality.Hunter : Personality.Objective;
            float r = Random.value;
            float brav = isTank ? 1f : (r < 0.4f ? 1f : (r < 0.75f ? 0.5f : 0.2f));
            SpawnEnemy(gp, cell, isTank ? EnemyType.Tank : EnemyType.Common, pers, brav);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnEnemy(EnemyGroup gp, Vector2Int cell, EnemyType type, Personality pers, float brav)
    {
        PrimitiveType shape = type == EnemyType.Tank ? PrimitiveType.Cylinder
                            : (pers == Personality.Hunter ? PrimitiveType.Cube : PrimitiveType.Sphere);
        var balance = type == EnemyType.Tank ? tankEnemy : commonEnemy;
        Vector3 scale = balance.scale;
        float y = scale.y * 0.5f;

        Vector3 pos = grid.CellToWorld(cell) + new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
        pos.y = grid.transform.position.y + y;

        var go = Prim(shape, pos, scale, Color.red);
        go.name = type + "_" + pers;
        go.AddComponent<Health>();
        var e = go.AddComponent<Enemy>();
        e.Init(type, pers, brav, gp);
        e.ApplyBalance(balance);
        gp.Add(e);
        gp.ApplySocialProfile(e);
        RaiseGameplayEvent(GameplayEventType.EnemySpawned, go, 0f, $"Surgiu {type} ({pers})");
    }

    // ---------- HUD ----------
    static string CostText(BuildingData d)
    {
        var parts = new List<string>();
        if (d.woodCost > 0) parts.Add("Madeira " + d.woodCost);
        if (d.suppliesCost > 0) parts.Add("Supr. " + d.suppliesCost);
        return parts.Count > 0 ? string.Join("  ", parts) : "Grátis";
    }

    void OnGUI()
    {
        var st = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        st.normal.textColor = Color.white;

        float coreHp = core != null ? core.health.current : 0f;
        GUI.Label(new Rect(10, 8, 1400, 26),
            $"Rodada {round}   {(night ? "NOITE" : "DIA")}   Madeira: {wallet.Wood}   Suprimentos: {wallet.Supplies}   Core: {coreHp:0}   Abates: {killed}   Desertores: {deserted}", st);

        GUI.Label(new Rect(10, 32, 1400, 26), night
            ? "Sobreviva à noite..."
            : "Clique esq.: construir  |  Clique dir.: vender  |  [1-9] escolher  |  [Espaço] noite  |  [R] reiniciar  |  [F1] cenário  |  [F3] eventos", st);
        GUI.Label(new Rect(10, 56, 1400, 26), msg, st);
        string scenarioLabel = scenario != null ? scenario.displayName : "Inspector";
        if (scenarioChangePending && scenarios.Count > 0)
        {
            var pending = scenarios.Find(s => s.name == selectedScenarioName);
            if (pending != null) scenarioLabel = $"{scenarioLabel} -> {pending.displayName} (R para aplicar)";
        }
        GUI.Label(new Rect(10, 80, 900, 22), $"Cenário: {scenarioLabel}   Mapa: {grid.width} x {grid.height}   Entradas: {spawns.Count}", st);

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

        if (scenario != null && !string.IsNullOrEmpty(scenario.instructions))
            GUI.Label(new Rect(10, 104, 700, 38), scenario.instructions, st);

        // Menu de construção
        var nameSt = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        var smallSt = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        for (int i = 0; i < menu.Count; i++)
        {
            var d = menu[i];
            var r = CardRect(i);
            bool afford = wallet.CanAfford(d.woodCost, d.suppliesCost);

            GUI.color = i == selected ? new Color(0.22f, 0.42f, 0.28f, 0.95f) : new Color(0.1f, 0.1f, 0.13f, 0.88f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = d.color;
            GUI.DrawTexture(new Rect(r.x + 6, r.y + 8, 10, r.height - 16), Texture2D.whiteTexture);
            GUI.color = Color.white;

            nameSt.normal.textColor = Color.white;
            GUI.Label(new Rect(r.x + 22, r.y + 4, r.width - 26, 22), $"[{i + 1}] {d.displayName}", nameSt);

            smallSt.normal.textColor = afford ? new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.5f, 0.5f);
            GUI.Label(new Rect(r.x + 22, r.y + 24, r.width - 26, 18), CostText(d), smallSt);

            smallSt.normal.textColor = new Color(0.75f, 0.75f, 0.8f);
            string extra = d.kind == StructureKind.Tower
                ? $"Vida {d.maxHealth:0}  Dano {d.towerDamage:0}  Alcance {d.towerRange:0}"
                : d.amount > 0 ? $"+{d.amount} {(d.produces == ResourceType.Wood ? "Madeira" : "Supr.")} / {d.cycleSeconds:0}s"
                               : $"Vida {d.maxHealth:0}";
            GUI.Label(new Rect(r.x + 22, r.y + 42, r.width - 26, 18), extra, smallSt);
        }

        GUI.Label(new Rect(10, Screen.height - 54, 1600, 26),
            "Esfera = Objetivo (só quer o Core)   Cubo = Caçador (vai atrás de torres)   Cilindro grande = Tank   Bolinha branca = líder", st);
        GUI.Label(new Rect(10, Screen.height - 30, 1600, 26),
            "Vermelho = Fanático   Laranja = Cobiçoso   Rosa = Covarde   Cinza = desertor", st);

        if (lost)
        {
            var big = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 120),
                $"DERROTA — você sobreviveu a {Mathf.Max(0, round - 1)} noite(s)\n[R] reiniciar", big);
        }
    }
}
