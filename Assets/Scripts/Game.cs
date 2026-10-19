using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

    [Header("Estado (somente leitura)")]
    public int round;
    public int killed;
    public int deserted;
    public bool night;
    public bool lost;

    public Wallet wallet = new Wallet();
    public Structure core;

    static Material baseMat;
    public static Material LineMat;

    GridWorld grid;
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
        baseMat = litMaterial;
        LineMat = lineMaterial;
        Enemy.All.Clear();
        Structure.All.Clear();

        wallet.Wood = startWood;
        wallet.Supplies = startSupplies;

        menu = new List<BuildingData>(Resources.LoadAll<BuildingData>("Buildings"));
        menu.RemoveAll(b => b.kind == StructureKind.Core);
        menu.Sort((a, b) => a.order.CompareTo(b.order));
        if (menu.Count == 0)
            Say("Nenhuma construção encontrada. Rode: Tools > Village > Create Default Buildings", 600f);

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

        // Coração (dados criados em código; não aparece no menu)
        var cd = ScriptableObject.CreateInstance<BuildingData>();
        cd.displayName = "Coração";
        cd.kind = StructureKind.Core;
        cd.shape = PrimitiveType.Cube;
        cd.color = new Color(0.2f, 0.4f, 1f);
        cd.size = new Vector3(0.9f, 1.8f, 0.9f);
        cd.maxHealth = 300f;
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

        if (d.kind == StructureKind.Tower) go.AddComponent<Tower>();
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
        grid.Set(cell, s);
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

        if (msgTimer > 0f) { msgTimer -= Time.deltaTime; if (msgTimer <= 0f) msg = ""; }

        if (lost) { cursor.SetActive(false); return; }

        for (int i = 0; i < menu.Count && i < 9; i++)
            if (kb[Key.Digit1 + i].wasPressedThisFrame) selected = i;

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
        foreach (var s in spawns)
            if (Mathf.Abs(s.x - c.x) <= 1 && Mathf.Abs(s.y - c.y) <= 1) return false;   // zona de spawn
        return true;
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
            int sup = dawnSuppliesBase + round;
            wallet.Add(ResourceType.Wood, dawnWood);
            wallet.Add(ResourceType.Supplies, sup);
            Say($"Amanheceu! Chegaram +{sup} Suprimentos e +{dawnWood} Madeira. Construa e aperte Espaço.", 5f);
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
        pos.y = grid.transform.position.y + y;

        var go = Prim(shape, pos, scale, Color.red);
        go.name = type + "_" + pers;
        go.AddComponent<Health>();
        var e = go.AddComponent<Enemy>();
        e.Init(type, pers, brav, gp);
        gp.Add(e);
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
            : "Clique esq.: construir  |  Clique dir.: vender  |  [1-9] ou clique no menu: escolher  |  [Espaço]: iniciar noite", st);
        GUI.Label(new Rect(10, 56, 1400, 26), msg, st);

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
            string extra = d.amount > 0 ? $"+{d.amount} {(d.produces == ResourceType.Wood ? "Madeira" : "Supr.")} / {d.cycleSeconds:0}s"
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
