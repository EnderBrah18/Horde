using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Coloque este script em UM GameObject vazio na cena. Ele cria todo o resto.
public class Game : MonoBehaviour
{
    public static Game I;

    [Header("Economia")]
    public int money = 150;
    public int wallCost = 10;
    public int towerCost = 40;
    public int killReward = 3;

    [Header("Estado (somente leitura)")]
    public int round;
    public int killed;
    public int deserted;
    public bool night;
    public bool lost;

    public Structure core;

    GridWorld grid;
    readonly List<Vector2Int> spawns = new List<Vector2Int>();
    int selected;   // 0 = parede, 1 = torre
    GameObject cursor;
    Vector2Int hoverCell;
    string msg = "";
    float msgTimer;

    public static GameObject Prim(PrimitiveType t, Vector3 pos, Vector3 scale, Color col)
    {
        var go = GameObject.CreatePrimitive(t);
        Destroy(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material.color = col;
        return go;
    }

    void Awake()
    {
        I = this;
        Enemy.All.Clear();
        Structure.All.Clear();
        grid = gameObject.AddComponent<GridWorld>();
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

        // Spawns no meio de cada borda
        spawns.Add(new Vector2Int(grid.width / 2, 0));
        spawns.Add(new Vector2Int(grid.width / 2, grid.height - 1));
        spawns.Add(new Vector2Int(0, grid.height / 2));
        spawns.Add(new Vector2Int(grid.width - 1, grid.height / 2));
        foreach (var s in spawns)
            Prim(PrimitiveType.Cube, grid.CellToWorld(s) + Vector3.up * 0.03f,
                 new Vector3(cs * 0.95f, 0.05f, cs * 0.95f), new Color(0.6f, 0.1f, 0.1f));

        // Coração
        core = MakeStructure(StructureKind.Core, grid.CoreCell, 300f);
        core.health.Died += _ => lost = true;

        // Cursor de construção
        cursor = Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(cs * 0.95f, 0.1f, cs * 0.95f), Color.green);
        cursor.name = "Cursor";

        grid.Recalc();
    }

    Structure MakeStructure(StructureKind kind, Vector2Int cell, float hp)
    {
        float cs = grid.cellSize;
        Vector3 p = grid.CellToWorld(cell);
        GameObject go;
        switch (kind)
        {
            case StructureKind.Wall:
                go = Prim(PrimitiveType.Cube, p + Vector3.up * 0.75f, new Vector3(cs * 0.95f, 1.5f, cs * 0.95f), new Color(0.55f, 0.55f, 0.55f));
                break;
            case StructureKind.Tower:
                go = Prim(PrimitiveType.Cylinder, p + Vector3.up * 1.2f, new Vector3(cs * 0.6f, 1.2f, cs * 0.6f), new Color(0.2f, 0.8f, 0.9f));
                go.AddComponent<Tower>();
                break;
            default:
                go = Prim(PrimitiveType.Cube, p + Vector3.up * 0.9f, new Vector3(cs * 0.9f, 1.8f, cs * 0.9f), new Color(0.2f, 0.4f, 1f));
                break;
        }
        go.name = kind.ToString();
        var h = go.AddComponent<Health>();
        h.Setup(hp);
        var s = go.AddComponent<Structure>();
        s.kind = kind;
        s.cell = cell;
        s.health = h;
        grid.Set(cell, s);
        return s;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (msgTimer > 0f) { msgTimer -= Time.deltaTime; if (msgTimer <= 0f) msg = ""; }

        if (lost) { cursor.SetActive(false); return; }

        if (kb.digit1Key.wasPressedThisFrame) selected = 0;
        if (kb.digit2Key.wasPressedThisFrame) selected = 1;
        if (kb.spaceKey.wasPressedThisFrame && !night) StartCoroutine(RunNight());

        UpdateHover(mouse);

        if (!night)
        {
            if (mouse.leftButton.wasPressedThisFrame) TryBuild();
            if (mouse.rightButton.wasPressedThisFrame) TrySell();
        }
    }

    int CurrentCost => selected == 0 ? wallCost : towerCost;

    bool CanBuild(Vector2Int c)
    {
        if (!grid.InBounds(c) || grid.Get(c) != null) return false;
        foreach (var s in spawns)
            if (Mathf.Abs(s.x - c.x) <= 1 && Mathf.Abs(s.y - c.y) <= 1) return false;   // zona de spawn
        return money >= CurrentCost;
    }

    void UpdateHover(Mouse mouse)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        var plane = new Plane(Vector3.up, grid.transform.position);
        if (plane.Raycast(ray, out float d))
        {
            hoverCell = grid.WorldToCell(ray.GetPoint(d));
            bool show = grid.InBounds(hoverCell) && !night;
            cursor.SetActive(show);
            if (show)
            {
                cursor.transform.position = grid.CellToWorld(hoverCell) + Vector3.up * 0.06f;
                cursor.GetComponent<Renderer>().material.color =
                    CanBuild(hoverCell) ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.2f, 0.2f);
            }
        }
        else cursor.SetActive(false);
    }

    void TryBuild()
    {
        if (!CanBuild(hoverCell)) { Say("Não dá para construir aqui (ou sem dinheiro)."); return; }

        money -= CurrentCost;
        MakeStructure(selected == 0 ? StructureKind.Wall : StructureKind.Tower, hoverCell, selected == 0 ? 60f : 50f);
        grid.Recalc();
    }

    void TrySell()
    {
        var s = grid.Get(hoverCell);
        if (s == null || s.kind == StructureKind.Core) return;
        money += Mathf.RoundToInt((s.kind == StructureKind.Wall ? wallCost : towerCost) * 0.7f);
        Destroy(s.gameObject);   // OnDestroy limpa a célula e recalcula o campo de fluxo
    }

    void Say(string t) { msg = t; msgTimer = 2f; }

    // ---------- Noite ----------
    IEnumerator RunNight()
    {
        night = true;
        round++;
        int groups = Mathf.Min(1 + (round - 1) / 2, 4);
        int commons = 4 + round;
        bool tank = round % 2 == 0;

        for (int g = 0; g < groups; g++)
        {
            var cell = spawns[Random.Range(0, spawns.Count)];
            yield return SpawnGroup(cell, commons, tank);
            yield return new WaitForSeconds(3f);
        }

        yield return new WaitUntil(() => Enemy.All.Count == 0 || lost);
        night = false;
        if (!lost)
        {
            int reward = 40 + 10 * round;
            money += reward;
            Say("Amanheceu! +" + reward + " de dinheiro. Construa e aperte Espaço.");
        }
    }

    IEnumerator SpawnGroup(Vector2Int cell, int commons, bool tank)
    {
        var gp = new GameObject("Group").AddComponent<EnemyGroup>();
        gp.expected = commons + (tank ? 1 : 0);
        float hunterChance = Mathf.Min(0.3f + 0.05f * round, 0.6f);

        for (int i = 0; i < gp.expected; i++)
        {
            bool isTank = tank && i == gp.expected - 1;   // o Tank chega por último
            var pers = (!isTank && Random.value < hunterChance) ? Personality.Hunter : Personality.Objective;
            float r = Random.value;
            float brav = isTank ? 1f : (r < 0.4f ? 1f : (r < 0.75f ? 0.5f : 0.2f));
            SpawnEnemy(gp, cell, isTank ? EnemyType.Tank : EnemyType.Common, pers, brav);
            yield return new WaitForSeconds(0.4f);
        }
    }

    void SpawnEnemy(EnemyGroup gp, Vector2Int cell, EnemyType type, Personality pers, float brav)
    {
        PrimitiveType shape = type == EnemyType.Tank ? PrimitiveType.Cylinder
                            : (pers == Personality.Hunter ? PrimitiveType.Cube : PrimitiveType.Sphere);
        Vector3 scale = type == EnemyType.Tank ? new Vector3(1.8f, 0.9f, 1.8f) : Vector3.one * 0.9f;
        float y = type == EnemyType.Tank ? 0.9f : 0.45f;

        Vector3 pos = grid.CellToWorld(cell) + new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
        pos.y = y;

        var go = Prim(shape, pos, scale, Color.red);
        go.name = type + "_" + pers;
        go.AddComponent<Health>();
        var e = go.AddComponent<Enemy>();
        e.Init(type, pers, brav, gp);
        gp.Add(e);
    }

    // ---------- HUD ----------
    void OnGUI()
    {
        var st = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        st.normal.textColor = Color.white;

        float coreHp = core != null ? core.health.current : 0f;
        GUI.Label(new Rect(10, 8, 1400, 26),
            $"Rodada {round}   {(night ? "NOITE" : "DIA")}   Dinheiro: {money}   Core: {coreHp:0}   Abates: {killed}   Desertores: {deserted}", st);

        string tool = selected == 0 ? $"Parede ({wallCost})" : $"Torre ({towerCost})";
        GUI.Label(new Rect(10, 32, 1400, 26), night
            ? "Sobreviva à noite..."
            : $"[1] Parede  [2] Torre  |  Selecionado: {tool}  |  Clique esq.: construir  |  Clique dir.: vender  |  [Espaço]: iniciar noite", st);
        GUI.Label(new Rect(10, 56, 1400, 26), msg, st);

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
