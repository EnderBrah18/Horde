using System.Collections.Generic;
using UnityEngine;

// Grid no plano XZ (Y = altura) + campo de fluxo (Dijkstra a partir do Coração).
public class GridWorld : MonoBehaviour
{
    const float LocalDetourRadius = 12f;

    public static GridWorld I;

    public int width = 21;
    public int height = 21;
    public float cellSize = 2f;

    public int breakCost = 10;   // quantas células de desvio "valem" quebrar uma parede

    Structure[,] cells;
    bool[,] hardBlocked;
    int[,] dist;
    int[,] tankDist;
    readonly Dictionary<Vector2Int, int[,]> targetDistances = new Dictionary<Vector2Int, int[,]>();
    readonly Dictionary<Vector2Int, int[,]> openTargetDistances = new Dictionary<Vector2Int, int[,]>();

    static readonly Vector2Int[] D4 =
        { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
    static readonly Vector2Int[] D8 =
    {
        new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1),
        new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1)
    };
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
        Allocate();
    }

    public void Configure(int newWidth, int newHeight, float newCellSize, int newBreakCost)
    {
        width = Mathf.Max(3, newWidth);
        height = Mathf.Max(3, newHeight);
        cellSize = Mathf.Max(0.1f, newCellSize);
        breakCost = Mathf.Max(0, newBreakCost);
        Allocate();
    }

    void Allocate()
    {
        cells = new Structure[width, height];
        hardBlocked = new bool[width, height];
        dist = new int[width, height];
        InvalidateFlowFields();
    }

    void InvalidateFlowFields()
    {
        targetDistances.Clear();
        openTargetDistances.Clear();
        tankDist = null;
    }

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;
    public bool IsHardBlocked(Vector2Int c) => InBounds(c) && hardBlocked[c.x, c.y];
    public bool IsTankBreakableWall(Structure structure)
    {
        return structure != null && structure.kind == StructureKind.Wall &&
               (structure.gameObject.tag == "Muro" || structure.gameObject.tag == "Cerca");
    }

    public Vector3 CellToWorld(Vector2Int c) =>
        transform.position + new Vector3((c.x - width / 2f + 0.5f) * cellSize, 0f, (c.y - height / 2f + 0.5f) * cellSize);

    public Vector2Int WorldToCell(Vector3 p)
    {
        var l = p - transform.position;
        return new Vector2Int(Mathf.FloorToInt(l.x / cellSize + width / 2f), Mathf.FloorToInt(l.z / cellSize + height / 2f));
    }

    public Vector2Int AttackApproachCell(Structure target, int slot, float attackRange)
    {
        if (target == null) return CoreCell;
        int firstDirection = (slot & 0x7fffffff) % D8.Length;
        int maxRing = Mathf.Max(1, Mathf.CeilToInt((attackRange + cellSize * 0.5f) / cellSize));
        Vector3 targetPosition = target.transform.position;
        targetPosition.y = transform.position.y;
        // Scene structures can retain a stale/default Structure.cell when their
        // scene footprint was registered before the runtime grid was configured.
        // Resolve from their current world position so attack routes don't fall
        // back to CoreCell and keep marching toward the village center.
        Vector2Int targetCell = WorldToCell(targetPosition);
        if (!InBounds(targetCell)) return targetCell;

        for (int ring = 1; ring <= maxRing; ring++)
        {
            for (int i = 0; i < D8.Length; i++)
            {
                Vector2Int candidate = targetCell + D8[(firstDirection + i) % D8.Length] * ring;
                if (!InBounds(candidate) || hardBlocked[candidate.x, candidate.y] || cells[candidate.x, candidate.y] != null)
                    continue;

                Vector3 approachPosition = CellToWorld(candidate);
                approachPosition.y = targetPosition.y;
                if (Vector3.Distance(approachPosition, targetPosition) > attackRange)
                    continue;
                return candidate;
            }
        }

        return targetCell;
    }

    // Route through openings first. Structures other than the destination remain blocked.
    public bool TryOpenRoute(Vector3 position, Vector2Int destination, out Vector3 direction,
        float maxExtraDistance = float.PositiveInfinity, float clearance = 0f)
    {
        direction = Vector3.zero;
        Vector2Int start = WorldToCell(position);
        if (!InBounds(start) || !InBounds(destination)) return false;
        if (!openTargetDistances.TryGetValue(destination, out var distances))
        {
            distances = new int[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++) distances[x, y] = int.MaxValue;
            var queue = new Queue<Vector2Int>();
            Structure destinationStructure = Get(destination);
            distances[destination.x, destination.y] = 0;
            queue.Enqueue(destination);
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (var offset in D4)
                {
                    Vector2Int next = current + offset;
                    if (!InBounds(next) || IsHardBlocked(next) || distances[next.x, next.y] != int.MaxValue)
                        continue;
                    Structure obstacle = Get(next);
                    if (obstacle != null && obstacle != destinationStructure) continue;
                    distances[next.x, next.y] = distances[current.x, current.y] + 1;
                    queue.Enqueue(next);
                }
            }
            openTargetDistances[destination] = distances;
        }
        if (distances[start.x, start.y] == int.MaxValue) return false;
        int directSteps = Mathf.Abs(start.x - destination.x) + Mathf.Abs(start.y - destination.y);
        if (distances[start.x, start.y] * cellSize > directSteps * cellSize + maxExtraDistance)
            return false;
        int best = distances[start.x, start.y];
        float bestScore = float.PositiveInfinity;
        Vector2Int step = start;
        foreach (var offset in D4)
        {
            Vector2Int next = start + offset;
            if (!InBounds(next) || distances[next.x, next.y] >= best) continue;
            float score = distances[next.x, next.y] + ClearancePenalty(next, clearance);
            if (score >= bestScore) continue;
            bestScore = score;
            step = next;
        }
        direction = DirectionTo(position, CellToWorld(step));
        return true;
    }

    float ClearancePenalty(Vector2Int cell, float clearance)
    {
        if (clearance <= 0f) return 0f;
        Vector3 point = CellToWorld(cell);
        float nearest = clearance;
        int rings = Mathf.CeilToInt(clearance / cellSize) + 1;
        for (int x = cell.x - rings; x <= cell.x + rings; x++)
            for (int y = cell.y - rings; y <= cell.y + rings; y++)
            {
                Vector2Int obstacle = new Vector2Int(x, y);
                if (InBounds(obstacle) && !IsHardBlocked(obstacle) && Get(obstacle) == null) continue;
                Vector3 center = CellToWorld(obstacle);
                float dx = Mathf.Max(0f, Mathf.Abs(point.x - center.x) - cellSize * 0.5f);
                float dz = Mathf.Max(0f, Mathf.Abs(point.z - center.z) - cellSize * 0.5f);
                nearest = Mathf.Min(nearest, Mathf.Sqrt(dx * dx + dz * dz));
            }
        // A preference rather than a hard block keeps narrow openings usable.
        return (1f - nearest / clearance) * 3f;
    }

    public Structure Get(Vector2Int c) => InBounds(c) ? cells[c.x, c.y] : null;
    public void Set(Vector2Int c, Structure s)
    {
        cells[c.x, c.y] = s;
        InvalidateFlowFields();
    }

    public void Clear(Structure s)
    {
        bool changed = false;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (cells[x, y] == s)
                {
                    cells[x, y] = null;
                    changed = true;
                }
        if (changed) Recalc();
    }

    public void SetHardBlocked(Bounds bounds, bool blocked)
    {
        Vector2Int min = WorldToCell(new Vector3(bounds.min.x, transform.position.y, bounds.min.z));
        Vector2Int max = WorldToCell(new Vector3(bounds.max.x, transform.position.y, bounds.max.z));
        for (int x = Mathf.Max(0, min.x); x <= Mathf.Min(width - 1, max.x); x++)
            for (int y = Mathf.Max(0, min.y); y <= Mathf.Min(height - 1, max.y); y++)
                hardBlocked[x, y] = blocked;
        InvalidateFlowFields();
    }

    // Bridge collider bounds are axis-aligned and can include cells beyond the bridge's
    // actual footprint. Only clear river cells whose centers lie inside the collider.
    public void ClearHardBlockedWithinColliderFootprint(Collider collider)
    {
        if (collider == null || !collider.enabled) return;

        Bounds bounds = collider.bounds;
        Vector2Int min = WorldToCell(new Vector3(bounds.min.x, transform.position.y, bounds.min.z));
        Vector2Int max = WorldToCell(new Vector3(bounds.max.x, transform.position.y, bounds.max.z));
        float sampleY = bounds.center.y;

        for (int x = Mathf.Max(0, min.x); x <= Mathf.Min(width - 1, max.x); x++)
        {
            for (int y = Mathf.Max(0, min.y); y <= Mathf.Min(height - 1, max.y); y++)
            {
                Vector3 cellCenter = CellToWorld(new Vector2Int(x, y));
                Vector3 sample = new Vector3(cellCenter.x, sampleY, cellCenter.z);
                Vector3 closest = collider.ClosestPoint(sample);
                float dx = sample.x - closest.x;
                float dz = sample.z - closest.z;
                if (dx * dx + dz * dz <= 0.0001f)
                    hardBlocked[x, y] = false;
            }
        }

        InvalidateFlowFields();
    }

    public void SetFootprint(Structure structure, Bounds bounds)
    {
        if (structure == null) return;
        Vector2Int min = WorldToCell(new Vector3(bounds.min.x, transform.position.y, bounds.min.z));
        Vector2Int max = WorldToCell(new Vector3(bounds.max.x, transform.position.y, bounds.max.z));
        for (int x = Mathf.Max(0, min.x); x <= Mathf.Min(width - 1, max.x); x++)
            for (int y = Mathf.Max(0, min.y); y <= Mathf.Min(height - 1, max.y); y++)
                cells[x, y] = structure;
        structure.cell = WorldToCell(bounds.center);
        InvalidateFlowFields();
    }

    // Campo de fluxo com custos: entrar numa célula custa 1, e custa mais (breakCost) se houver construção nela.
    // Dijkstra é necessário porque os pesos das células não são todos iguais.
    public void Recalc(Vector2Int? extra = null)
    {
        InvalidateFlowFields();
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
                if (!InBounds(n) || hardBlocked[n.x, n.y]) continue;

                // A busca parte do destino. Ao expandir para trás, o custo da aresta
                // é o custo de entrar em c no trajeto normal de n até o Coração.
                if (hardBlocked[c.x, c.y] && c != start) continue;
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
        return FlowDir(wp, dist, false, true, CellToWorld(CoreCell));
    }

    // Tanks keep the Core as their strategic target. The weighted flow field
    // chooses whether a wall is cheaper to break than to route around.
    public Vector3 FlowDirForTank(Vector3 wp)
    {
        if (tankDist == null) tankDist = CalculateDistances(CoreCell, true);
        return FlowDir(wp, tankDist, true, true, CellToWorld(CoreCell));
    }

    // Hunters use the same weighted routing toward a tower instead of walking in a straight line through walls.
    public Vector3 FlowDir(Vector3 wp, Vector2Int target)
    {
        return FlowDir(wp, target, true);
    }

    public Vector3 FlowDir(Vector3 wp, Vector2Int target, bool allowLocalDetour)
    {
        Vector3 targetWorld = CellToWorld(target);
        if (!InBounds(target)) return DirectionTo(wp, targetWorld);
        if (!targetDistances.TryGetValue(target, out var targetDist))
        {
            targetDist = CalculateDistances(target, false);
            targetDistances[target] = targetDist;
        }
        return FlowDir(wp, targetDist, false, allowLocalDetour, targetWorld);
    }

    Vector3 FlowDir(Vector3 wp, int[,] distances, bool preferBreakingWalls, bool allowLocalDetour,
        Vector3 unreachableDestination)
    {
        var c = WorldToCell(wp);
        Vector3 fallback = unreachableDestination - wp;
        fallback.y = 0f;
        if (!InBounds(c) || distances[c.x, c.y] == int.MaxValue) return fallback.normalized;

        int best = distances[c.x, c.y];
        var pick = c;
        foreach (var d in D4)
        {
            var n = c + d;
            if (!InBounds(n) || hardBlocked[n.x, n.y] || distances[n.x, n.y] == int.MaxValue) continue;
            int s = distances[n.x, n.y] + 1 + (cells[n.x, n.y] != null ? Mathf.Max(0, breakCost) : 0);
            if (s <= best) { best = s; pick = n; }
        }

        // Prefer a nearby open route over the weighted shortest route through a
        // destructible structure. Only consider detours within local awareness;
        // if none makes progress, the existing flow can still choose to attack it.
        if (pick != c && cells[pick.x, pick.y] != null)
        {
            bool selectedWallForTank = preferBreakingWalls &&
                IsTankBreakableWall(cells[pick.x, pick.y]);
            if (!selectedWallForTank && allowLocalDetour)
            {
                Vector2Int detour = FindLocalOpenDetour(c, distances,
                    Mathf.Max(1, Mathf.CeilToInt(LocalDetourRadius / cellSize)));
                if (detour != c) pick = detour;
            }
        }

        if (pick == c) return Vector3.zero;
        var t = CellToWorld(pick) - wp;
        t.y = 0f;
        return t.normalized;
    }

    static Vector3 DirectionTo(Vector3 from, Vector3 destination)
    {
        Vector3 direction = destination - from;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
    }

    Vector2Int FindLocalOpenDetour(Vector2Int start, int[,] distances, int maxSteps)
    {
        if (!InBounds(start) || distances[start.x, start.y] == int.MaxValue) return start;

        int minX = Mathf.Max(0, start.x - maxSteps);
        int maxX = Mathf.Min(width - 1, start.x + maxSteps);
        int minY = Mathf.Max(0, start.y - maxSteps);
        int maxY = Mathf.Min(height - 1, start.y + maxSteps);
        var visited = new bool[maxX - minX + 1, maxY - minY + 1];
        var firstStep = new Vector2Int[maxX - minX + 1, maxY - minY + 1];
        var steps = new int[maxX - minX + 1, maxY - minY + 1];
        var open = new Queue<Vector2Int>();
        visited[start.x - minX, start.y - minY] = true;
        open.Enqueue(start);

        Vector2Int bestGoal = start;
        int bestDistance = distances[start.x, start.y];

        while (open.Count > 0)
        {
            Vector2Int current = open.Dequeue();
            int currentX = current.x - minX;
            int currentY = current.y - minY;
            if (steps[currentX, currentY] >= maxSteps) continue;

            foreach (var direction in D4)
            {
                Vector2Int next = current + direction;
                int nextX = next.x - minX;
                int nextY = next.y - minY;
                if (nextX < 0 || nextY < 0 || nextX >= visited.GetLength(0) || nextY >= visited.GetLength(1) ||
                    visited[nextX, nextY] || hardBlocked[next.x, next.y] ||
                    cells[next.x, next.y] != null) continue;

                visited[nextX, nextY] = true;
                steps[nextX, nextY] = steps[currentX, currentY] + 1;
                firstStep[nextX, nextY] = current == start ? next : firstStep[currentX, currentY];
                open.Enqueue(next);

                int routeDistance = distances[next.x, next.y];
                if (routeDistance < bestDistance)
                {
                    bestDistance = routeDistance;
                    bestGoal = next;
                }
            }
        }

        if (bestGoal == start) return start;
        return firstStep[bestGoal.x - minX, bestGoal.y - minY];
    }

    int[,] CalculateDistances(Vector2Int start, bool wallsOnly)
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
                if (!InBounds(n) || hardBlocked[n.x, n.y] ||
                    (hardBlocked[c.x, c.y] && c != start)) continue;
                Structure nextStructure = cells[n.x, n.y];
                if (wallsOnly && n != start && nextStructure != null && !IsTankBreakableWall(nextStructure))
                    continue;
                Structure structure = cells[c.x, c.y];
                if (wallsOnly && c != start && structure != null && !IsTankBreakableWall(structure))
                    continue;
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
