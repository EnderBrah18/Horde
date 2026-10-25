using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The animated ribbon and its damage use the same pose and lifetime.
public sealed class PlayerSlashEffect : MonoBehaviour
{
    const int Segments = 24;
    readonly Vector3[] vertices = new Vector3[(Segments + 1) * 3];
    readonly Color[] colors = new Color[(Segments + 1) * 3];
    Mesh mesh;
    Material material;
    Vector3 direction;
    Color tint;
    float range, angle, width, duration, elapsed;
    bool reverse;
    GameObject attacker;
    float damage;
    float knockbackDistance, knockbackDuration;
    readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
    readonly List<Enemy> targets = new List<Enemy>();

    public void Initialize(Vector3 origin, Vector3 aim, float attackRange, float attackAngle,
        float slashWidth, float slashDuration, Color color, bool reversed, GameObject source, float attackDamage,
        float pushDistance, float pushDuration)
    {
        transform.position = origin;
        direction = aim;
        range = attackRange;
        angle = attackAngle;
        width = slashWidth;
        duration = Mathf.Max(0.01f, slashDuration);
        tint = color;
        reverse = reversed;
        attacker = source;
        damage = attackDamage;
        knockbackDistance = pushDistance;
        knockbackDuration = pushDuration;
        mesh = new Mesh { name = "PlayerSlash" };
        mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = gameObject.AddComponent<MeshRenderer>();
        // The debug line material can be opaque and ignore vertex alpha.
        // This ribbon needs transparency and two-sided rendering in both directions.
        material = new Material(Shader.Find("Sprites/Default"));
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var triangles = new int[Segments * 9];
        for (int i = 0; i < Segments; i++)
        {
            int v = i * 3, t = i * 9;
            // Dim sweep from the body, followed by the brighter blade edge.
            triangles[t] = v; triangles[t + 1] = v + 4; triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1; triangles[t + 4] = v + 4; triangles[t + 5] = v + 2;
            triangles[t + 6] = v + 2; triangles[t + 7] = v + 4; triangles[t + 8] = v + 5;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        Draw(0f);
    }

    public bool Tick(float deltaTime, Vector3 origin, Vector3 aim)
    {
        float previousProgress = elapsed / duration;
        elapsed = Mathf.Min(duration, elapsed + deltaTime);
        float progress = elapsed / duration;
        Vector3 previousOrigin = transform.position;
        Vector3 previousDirection = direction;
        // Interpolate both the animation and the cursor movement. Fast mouse
        // sweeps and slow frames must not jump over enemies between poses.
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Angle(direction, aim) / 3f),
            Mathf.CeilToInt(Vector3.Distance(previousOrigin, origin) / 0.1f),
            Mathf.CeilToInt((progress - previousProgress) * angle / 3f));
        targets.Clear();
        targets.AddRange(Enemy.All);
        for (int step = 0; step <= steps; step++)
        {
            float t = step / (float)steps;
            DamageRibbon(Vector3.Lerp(previousOrigin, origin, t), Vector3.Slerp(previousDirection, aim, t),
                Mathf.Lerp(previousProgress, progress, t));
            if (Game.I == null || Game.I.lost) return false;
        }
        transform.position = origin;
        direction = aim;
        Draw(elapsed / duration);
        return elapsed < duration;
    }

    void DamageRibbon(Vector3 origin, Vector3 aim, float progress)
    {
        float head = Mathf.Min(1f, progress * 1.3f);
        float tail = Mathf.Max(0f, head - 0.45f);
        if (head <= tail || progress >= 1f) return;
        float sign = reverse ? -1f : 1f;
        Vector3 middle = Quaternion.AngleAxis(((head + tail) * 0.5f - 0.5f) * angle * sign, Vector3.up) * aim;
        float span = (head - tail) * angle;
        foreach (var enemy in targets)
        {
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.hp == null || enemy.hp.current <= 0f || hitEnemies.Contains(enemy)) continue;
            float enemyRadius = Mathf.Max(0.15f, Mathf.Max(enemy.transform.lossyScale.x, enemy.transform.lossyScale.z) * 0.45f);
            if (!TouchesRibbon(enemy.transform.position - origin, middle, 0f, range, span, enemyRadius)) continue;
            hitEnemies.Add(enemy);
            enemy.hp.Damage(damage, attacker);
            if (enemy != null && enemy.hp != null && enemy.hp.current > 0f)
                enemy.ApplyKnockback(enemy.transform.position - origin, knockbackDistance, knockbackDuration);
        }
    }

    // Circle against the ribbon's annular sector on XZ. The enemy radius lets
    // the blade hit its body, rather than requiring its center inside the mesh.
    public static bool TouchesRibbon(Vector3 offset, Vector3 middle, float inner, float outer, float span, float bodyRadius)
    {
        offset.y = 0f;
        middle.y = 0f;
        float distance = offset.magnitude;
        if (Vector3.Angle(middle, offset) <= span * 0.5f)
            return Mathf.Abs(distance - Mathf.Clamp(distance, inner, outer)) <= bodyRadius;
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 edge = Quaternion.AngleAxis(side * span * 0.5f, Vector3.up) * middle.normalized;
            Vector3 closest = edge * Mathf.Clamp(Vector3.Dot(offset, edge), inner, outer);
            if ((offset - closest).sqrMagnitude <= bodyRadius * bodyRadius) return true;
        }
        return false;
    }

    void Draw(float progress)
    {
        float head = Mathf.Min(1f, progress * 1.3f);
        float tail = Mathf.Max(0f, head - 0.45f);
        float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, progress));
        for (int i = 0; i <= Segments; i++)
        {
            float t = i / (float)Segments;
            float sweep = Mathf.Lerp(tail, head, t);
            float degrees = (sweep - 0.5f) * angle * (reverse ? -1f : 1f);
            Vector3 radial = Quaternion.AngleAxis(degrees, Vector3.up) * direction;
            float thickness = Mathf.Sin(t * Mathf.PI) * Mathf.Min(range, width);
            vertices[i * 3] = Vector3.zero;
            vertices[i * 3 + 1] = radial * (range - thickness);
            vertices[i * 3 + 2] = radial * range;
            Color c = tint;
            c.a *= fade * Mathf.Sin(t * Mathf.PI);
            colors[i * 3] = new Color(c.r, c.g, c.b, c.a * 0.12f);
            colors[i * 3 + 1] = new Color(c.r, c.g, c.b, c.a * 0.35f);
            colors[i * 3 + 2] = c;
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateBounds();
    }
    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}
