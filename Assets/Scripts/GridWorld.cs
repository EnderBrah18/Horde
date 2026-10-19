using System.Collections.Generic;
using UnityEngine;

// Grid no plano XZ (Y = altura) + campo de fluxo (Dijkstra a partir do Coração).
public class GridWorld : MonoBehaviour
{
    public static GridWorld I;

    public int width = 21;
    public int height = 21;
    public float cellSize = 2f;

    public int breakCost = 10;   // quantas células de desvio "valem" quebrar uma parede

    Structure[,] cells;
    int[,] dist;
    readonly Dictionary<Vector2Int, int[,]> targetDistances = new Dictionary<Vector2Int, int[,]>();

    static readonly Vector2Int[] D4 =
        { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
    struct QueueNode
    {
        public Vector2Int cell;
        public int cost;

        public QueueNode(Vector2Int cell, int cost) { this.cell = cell; this.cost = cost; }
    }

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
    public void Set(Vector2Int c, Structure s)
    {
        cells[c.x, c.y] = s;
        targetDistances.Clear();
    }

    public void Clear(Structure s)
    {
        if (InBounds(s.cell) && cells[s.cell.x, s.cell.y] == s)
        {
            cells[s.cell.x, s.cell.y] = null;
            Recalc();
        }
    }

    // Campo de fluxo com custos: entrar numa célula custa 1, e custa mais (breakCost) se houver construção nela.
    // Dijkstra é necessário porque os pesos das células não são todos iguais.
    public void Recalc(Vector2Int? extra = null)
    {
        targetDistances.Clear();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dist[x, y] = int.MaxValue;

        var open = new List<QueueNode>();
        var start = CoreCell;
        dist[start.x, start.y] = 0;
        Push(open, new QueueNode(start, 0));
        int structureCost = Mathf.Max(0, breakCost);

        while (open.Count > 0)
        {
            var node = Pop(open);
            var c = node.cell;
            if (node.cost != dist[c.x, c.y]) continue; // entrada antiga na fila

            foreach (var d in D4)
            {
                var n = c + d;
                if (!InBounds(n)) continue;

                // A busca parte do destino. Ao expandir para trás, o custo da aresta
                // é o custo de entrar em c no trajeto normal de n até o Coração.
                bool blocked = cells[c.x, c.y] != null || (extra.HasValue && extra.Value == c);
                int nd = node.cost + 1 + (blocked ? structureCost : 0);

                if (nd < dist[n.x, n.y])
                {
                    dist[n.x, n.y] = nd;
                    Push(open, new QueueNode(n, nd));
                }
            }
        }
    }

    public bool Reachable(Vector2Int c) => InBounds(c) && dist[c.x, c.y] != int.MaxValue;

    // Direção desejada (a "bússola"): segue o vizinho cardinal com menor custo até o Coração.
    public Vector3 FlowDir(Vector3 wp)
    {
        return FlowDir(wp, dist);
    }

    // Hunters use the same weighted routing toward a tower instead of walking in a straight line through walls.
    public Vector3 FlowDir(Vector3 wp, Vector2Int target)
    {
        if (!InBounds(target)) return FlowDir(wp);
        if (!targetDistances.TryGetValue(target, out var targetDist))
        {
            targetDist = CalculateDistances(target);
            targetDistances[target] = targetDist;
        }
        return FlowDir(wp, targetDist);
    }

    Vector3 FlowDir(Vector3 wp, int[,] distances)
    {
        var c = WorldToCell(wp);
        Vector3 toCore = CellToWorld(CoreCell) - wp;
        toCore.y = 0f;
        if (!InBounds(c) || distances[c.x, c.y] == int.MaxValue) return toCore.normalized;

        int best = distances[c.x, c.y];
        var pick = c;
        foreach (var d in D4)
        {
            var n = c + d;
            if (!InBounds(n) || distances[n.x, n.y] == int.MaxValue) continue;
            int s = distances[n.x, n.y] + 1 + (cells[n.x, n.y] != null ? Mathf.Max(0, breakCost) : 0);
            if (s <= best) { best = s; pick = n; }
        }
        if (pick == c) return Vector3.zero;
        var t = CellToWorld(pick) - wp;
        t.y = 0f;
        return t.normalized;
    }

    int[,] CalculateDistances(Vector2Int start)
    {
        var distances = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                distances[x, y] = int.MaxValue;

        var open = new List<QueueNode>();
        distances[start.x, start.y] = 0;
        Push(open, new QueueNode(start, 0));
        int structureCost = Mathf.Max(0, breakCost);

        while (open.Count > 0)
        {
            var node = Pop(open);
            var c = node.cell;
            if (node.cost != distances[c.x, c.y]) continue;

            foreach (var d in D4)
            {
                var n = c + d;
                if (!InBounds(n)) continue;
                int nd = node.cost + 1 + (cells[c.x, c.y] != null ? structureCost : 0);
                if (nd >= distances[n.x, n.y]) continue;
                distances[n.x, n.y] = nd;
                Push(open, new QueueNode(n, nd));
            }
        }
        return distances;
    }

    static void Push(List<QueueNode> heap, QueueNode node)
    {
        int i = heap.Count;
        heap.Add(node);
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (heap[parent].cost <= node.cost) break;
            heap[i] = heap[parent];
            i = parent;
        }
        heap[i] = node;
    }

    static QueueNode Pop(List<QueueNode> heap)
    {
        QueueNode first = heap[0];
        QueueNode last = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        if (heap.Count == 0) return first;

        int i = 0;
        while (true)
        {
            int left = i * 2 + 1;
            if (left >= heap.Count) break;
            int right = left + 1;
            int child = right < heap.Count && heap[right].cost < heap[left].cost ? right : left;
            if (heap[child].cost >= last.cost) break;
            heap[i] = heap[child];
            i = child;
        }
        heap[i] = last;
        return first;
    }
}
