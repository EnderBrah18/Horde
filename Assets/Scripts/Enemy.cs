using System.Collections.Generic;
using UnityEngine;

public enum EnemyType { Common, Tank }
public enum Personality { Objective, Hunter }   // Objective = ObjectiveRooted
public enum EState { Rally, March, Flee }

public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();

    public EnemyType type;
    public Personality personality;
    public float braveza = 1f;
    public EnemyGroup group;
    public EState state = EState.Rally;

    public float speed = 3.2f;
    public float damage = 4f;
    public float attackCooldown = 1f;
    public float attackRange = 3f;
    public float tacticRadius = 10f;   // Hunter enxerga torres a esta distância
    public Health hp;

    float atkTimer;
    float wanderTimer;
    Vector3 wanderDest;
    Vector3 home;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public void Init(EnemyType t, Personality p, float b, EnemyGroup g)
    {
        type = t; personality = p; braveza = b; group = g;
        home = transform.position;
        hp = GetComponent<Health>();

        if (t == EnemyType.Tank) { speed = 2.2f; damage = 14f; hp.Setup(140f); }
        else { speed = 3.2f; damage = 4f; hp.Setup(25f); }
        hp.Died += OnDied;

        Color c;
        if (t == EnemyType.Tank) c = new Color(0.45f, 0f, 0f);
        else if (b >= 0.99f) c = new Color(0.9f, 0.1f, 0.1f);      // Fanático
        else if (b >= 0.4f) c = new Color(1f, 0.55f, 0f);           // Cobiçoso
        else c = new Color(1f, 0.6f, 0.85f);                        // Covarde
        GetComponent<Renderer>().material.color = c;
    }

    void OnDied(Health h)
    {
        if (group != null) group.Remove(this);
        Game.I.killed++;
    }

    public void Desert()
    {
        if (state == EState.Flee) return;
        var g = group;
        group = null;
        if (g != null) g.Remove(this);
        state = EState.Flee;
        GetComponent<Renderer>().material.color = Color.gray;
        Game.I.deserted++;
    }

    void Update()
    {
        if (Game.I == null || Game.I.lost) return;
        float dt = Time.deltaTime;
        atkTimer -= dt;

        Vector3 dir = Vector3.zero;
        float mult = 1f;
        switch (state)
        {
            case EState.Rally:
                dir = Wander(dt); mult = 0.4f;
                if (group != null && group.Marching) state = EState.March;
                break;
            case EState.March:
                dir = MarchDir();
                // Keep attackers stationary while they are in range of their target.
                if (dir.sqrMagnitude > 0.0001f) dir += Separation() * 1.5f;
                break;
            case EState.Flee:
                dir = FleeDir(); mult = 1.2f;
                break;
        }
        if (state != EState.March) dir += Separation() * 1.5f;
        Move(dir, mult, dt);
    }

    Vector3 Wander(float dt)
    {
        wanderTimer -= dt;
        if (wanderTimer <= 0f)
        {
            Vector2 r = Random.insideUnitCircle * 3f;
            wanderDest = home + new Vector3(r.x, 0f, r.y);
            wanderTimer = Random.Range(1f, 2.5f);
        }
        Vector3 d = wanderDest - transform.position;
        d.y = 0f;
        return d.magnitude > 0.3f ? d.normalized : Vector3.zero;
    }

    Vector3 MarchDir()
    {
        Vector3 pos = transform.position;
        Structure t = PickTarget(pos);

        if (t != null && Vector3.Distance(pos, t.transform.position) <= attackRange)
        {
            TryAttack(t);
            return Vector3.zero;
        }

        Vector3 d;
        if (t != null && t.kind == StructureKind.Tower)
            d = GridWorld.I.FlowDir(pos, t.cell); // Hunter desvia de construções até a torre
        else d = GridWorld.I.FlowDir(pos);
        return d;
    }

    // Personalidade de alvo: Objective só quer o Coração. Hunter também se desvia para torres próximas.
    Structure PickTarget(Vector3 pos)
    {
        Structure best = null;
        float bd = float.MaxValue;
        foreach (var s in Structure.All)
        {
            float d = Vector3.Distance(pos, s.transform.position);
            bool ok = s.kind == StructureKind.Core ||
                      (personality == Personality.Hunter && s.kind == StructureKind.Tower && d <= tacticRadius);
            if (ok && d < bd) { best = s; bd = d; }
        }
        return best;
    }

    Vector3 FleeDir()
    {
        Vector3 d = transform.position - GridWorld.I.CellToWorld(GridWorld.I.CoreCell);
        d.y = 0f;
        return d.normalized;
    }

    Vector3 Separation()
    {
        Vector3 f = Vector3.zero;
        foreach (var e in All)
        {
            if (e == this) continue;
            Vector3 d = transform.position - e.transform.position;
            d.y = 0f;
            float m = d.magnitude;
            float r = 0.6f * (transform.localScale.x + e.transform.localScale.x);
            if (m < r && m > 0.001f) f += d / m * (r - m);
        }
        return f;
    }

    void Move(Vector3 dir, float mult, float dt)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        var g = GridWorld.I;
        Vector3 pos = transform.position;
        Vector3 np = pos + dir * speed * mult * dt;
        var c = g.WorldToCell(np);

        if (!g.InBounds(c))
        {
            if (state == EState.Flee) Destroy(gameObject);   // fugiu do mapa
            return;
        }

        var s = g.Get(c);
        if (s != null)
        {
            if (state == EState.March)
            {
                // Separation can push an enemy off its route. Recheck the route before
                // deciding that the blocking structure must be attacked.
                Vector3 routeDir = MarchDir();
                if (routeDir.sqrMagnitude <= 0.0001f) return;
                Vector3 routePos = pos + routeDir.normalized * speed * mult * dt;
                var routeCell = g.WorldToCell(routePos);
                var routeBlocker = g.Get(routeCell);
                if (routeBlocker != null) { TryAttack(routeBlocker); return; }
                np = routePos;
                dir = routeDir;
            }
            else
            {
                Vector3 nx = new Vector3(np.x, pos.y, pos.z);
                Vector3 nz = new Vector3(pos.x, pos.y, np.z);
                if (Free(nx)) np = nx; else if (Free(nz)) np = nz; else return;
            }
        }

        transform.position = np;
        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
    }

    bool Free(Vector3 p)
    {
        var c = GridWorld.I.WorldToCell(p);
        return GridWorld.I.InBounds(c) && GridWorld.I.Get(c) == null;
    }

    void TryAttack(Structure s)
    {
        if (s == null || atkTimer > 0f) return;
        atkTimer = attackCooldown;
        s.health.Damage(damage);
    }
}
