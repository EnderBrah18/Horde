using System.Collections.Generic;
using UnityEngine;

// Grid no plano XZ (Y = altura) + campo de fluxo (BFS a partir do Coração).
public class GridWorld : MonoBehaviour
{
    public static GridWorld I;

    public int width = 21;
    public int height = 21;
    public float cellSize = 2f;

    public int breakCost = 10;   // quantas células de desvio "valem" quebrar uma parede

    Structure[,] cells;
    int[,] dist;

    static readonly Vector2Int[] D4 =
        { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
    static readonly Vector2Int[] D8 =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    public Vector2Int CoreCell => new Vector2Int(width / 2, height / 2);

    void Awake()
    {
        I = this;
        cells = new Structure[width, height];
        dist = new int[width, height];
    }

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;

    public Vector3 CellToWorld(Vector2Int c) =>
        transform.position + new Vector3((c.x - width / 2f + 0.5f) * cellSize, 0f, (c.y - height / 2f + 0.5f) * cellSize);

    public Vector2Int WorldToCell(Vector3 p)
    {
        var l = p - transform.position;
        return new Vector2Int(Mathf.FloorToInt(l.x / cellSize + width / 2f), Mathf.FloorToInt(l.z / cellSize + height / 2f));
    }

    public Structure Get(Vector2Int c) => InBounds(c) ? cells[c.x, c.y] : null;
    public void Set(Vector2Int c, Structure s) { cells[c.x, c.y] = s; }

    public void Clear(Structure s)
    {
        if (InBounds(s.cell) && cells[s.cell.x, s.cell.y] == s)
        {
            cells[s.cell.x, s.cell.y] = null;
            Recalc();
        }
    }

    // Campo de fluxo com custos: entrar numa célula custa 1, e custa mais (breakCost) se houver construção nela.
    public void Recalc(Vector2Int? extra = null)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dist[x, y] = int.MaxValue;

        var q = new Queue<Vector2Int>();
        var start = CoreCell;
        dist[start.x, start.y] = 0;
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            foreach (var d in D4)
            {
                var n = c + d;
                if (!InBounds(n)) continue;

                bool blocked = cells[n.x, n.y] != null || (extra.HasValue && extra.Value == n);
                int nd = dist[c.x, c.y] + 1 + (blocked ? breakCost : 0);

                if (nd < dist[n.x, n.y])
                {
                    dist[n.x, n.y] = nd;
                    q.Enqueue(n);
                }
            }
        }
    }

    public bool Reachable(Vector2Int c) => InBounds(c) && dist[c.x, c.y] != int.MaxValue;

    // Direção desejada (a "bússola"): aponta para o vizinho com menor distância ao Coração.
    public Vector3 FlowDir(Vector3 wp)
    {
        var c = WorldToCell(wp);
        Vector3 toCore = CellToWorld(CoreCell) - wp;
        toCore.y = 0f;
        if (!InBounds(c) || dist[c.x, c.y] == int.MaxValue) return toCore.normalized;

        float best = dist[c.x, c.y];
        var pick = c;
        foreach (var d in D8)
        {
            var n = c + d;
            if (!InBounds(n) || dist[n.x, n.y] == int.MaxValue) continue;
            bool diag = d.x != 0 && d.y != 0;
            if (diag)
            {
                if (cells[n.x, n.y] != null) continue;
                if (cells[c.x + d.x, c.y] != null || cells[c.x, c.y + d.y] != null) continue;
            }
            float s = dist[n.x, n.y] + (diag ? 0.5f : 0f);
            if (s < best) { best = s; pick = n; }
        }
        if (pick == c) return Vector3.zero;
        var t = CellToWorld(pick) - wp;
        t.y = 0f;
        return t.normalized;
    }
}
