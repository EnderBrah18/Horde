using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerAttack : MonoBehaviour
{
    [Header("Área do ataque (setor na direção do cursor)")]
    [Tooltip("Alcance do corte a partir do corpo, em metros. Não há distância mínima para acertar.")]
    [Min(0.1f)] public float attackRange = 4f;
    [Tooltip("Abertura total da área em graus. 360 atinge ao redor do jogador.")]
    [Range(1f, 360f)] public float attackAngle = 100f;
    [Header("Ataque")]
    [Min(0f)] public float attackDamage = 10f;
    [Min(0.01f)] public float attackCooldown = 0.4f;
    [Header("Repulsão do ataque")]
    [Tooltip("Distância em metros antes da resistência do inimigo.")]
    [Min(0f)] public float knockbackDistance = 1.5f;
    [Min(0.01f)] public float knockbackDuration = 0.2f;
    [Header("Visualização")]
    public bool showAttackArea = false;
    public Color areaColor = new Color(0.25f, 0.85f, 1f, 0.65f);
    public Color hitColor = new Color(1f, 0.85f, 0.25f, 1f);
    [Header("Efeito de corte")]
    [Min(0.01f)] public float slashDuration = 0.22f;
    [Tooltip("Espessura da borda luminosa. A área de dano sempre começa no corpo.")]
    [Min(0.01f)] public float slashWidth = 0.9f;
    [Min(0f)] public float slashHeight = 0.9f;
    public Color slashColor = new Color(0.8f, 0.95f, 1f, 1f);
    bool reverseSlash;

    const int ArcSegments = 32;
    readonly List<PlayerSlashEffect> activeSlashes = new List<PlayerSlashEffect>();
    Vector3 aimDirection = Vector3.forward;
    bool hasAim;
    float nextAttackTime;
    float flashUntil;
    LineRenderer area;
    Material areaMaterial;

    void Start()
    {
        var visual = new GameObject("AttackArea");
        visual.transform.SetParent(transform, false);
        area = visual.AddComponent<LineRenderer>();
        area.useWorldSpace = true;
        area.loop = true;
        area.positionCount = ArcSegments + 2;
        area.startWidth = area.endWidth = 0.06f;
        areaMaterial = Game.LineMat != null ? new Material(Game.LineMat)
            : new Material(Shader.Find("Sprites/Default"));
        area.sharedMaterial = areaMaterial;
        area.enabled = false;
    }

    void OnValidate()
    {
        attackRange = Mathf.Max(0.1f, attackRange);
        attackAngle = Mathf.Clamp(attackAngle, 1f, 360f);
        attackDamage = Mathf.Max(0f, attackDamage);
        attackCooldown = Mathf.Max(0.01f, attackCooldown);
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0.01f, knockbackDuration);
        slashDuration = Mathf.Max(0.01f, slashDuration);
        slashWidth = Mathf.Max(0.01f, slashWidth);
        slashHeight = Mathf.Max(0f, slashHeight);
    }

    void LateUpdate()
    {
        bool active = Game.I != null && Game.I.PlayerControlEnabled;
        hasAim = active && !Game.I.BlocksPlayerAttack && TryAim();
        if (hasAim)
        {
            transform.rotation = Quaternion.LookRotation(aimDirection);
            if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextAttackTime)
                Attack();
        }
        for (int i = activeSlashes.Count - 1; i >= 0; i--)
        {
            var slash = activeSlashes[i];
            if (slash == null) { activeSlashes.RemoveAt(i); continue; }
            if (!active || !slash.Tick(Time.deltaTime, SlashOrigin(), aimDirection))
            {
                Destroy(slash.gameObject);
                activeSlashes.RemoveAt(i);
            }
        }
        if (area == null) return;
        area.enabled = hasAim && showAttackArea;
        if (!area.enabled) return;
        area.startColor = area.endColor = Time.time < flashUntil ? hitColor : areaColor;
        Vector3 origin = AreaOrigin();
        area.SetPosition(0, origin);
        for (int i = 0; i <= ArcSegments; i++)
            area.SetPosition(i + 1, ArcPoint(origin, aimDirection, i / (float)ArcSegments));
    }

    bool TryAim()
    {
        var camera = Camera.main;
        var mouse = Mouse.current;
        if (camera == null || mouse == null) return false;
        Vector2 pointer = mouse.position.ReadValue();
        if (pointer.x < 0f || pointer.y < 0f || pointer.x >= Screen.width || pointer.y >= Screen.height)
            return false;
        // Project onto the ground, independently of disabled building colliders.
        float groundY = GridWorld.I != null ? GridWorld.I.transform.position.y : transform.position.y - 1f;
        var plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
        Ray ray = camera.ScreenPointToRay(pointer);
        if (!plane.Raycast(ray, out float distance)) return false;
        Vector3 direction = ray.GetPoint(distance) - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return false;
        aimDirection = direction.normalized;
        return true;
    }

    void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;
        flashUntil = Time.time + 0.12f;
        var slash = new GameObject("PlayerSlash").AddComponent<PlayerSlashEffect>();
        slash.Initialize(SlashOrigin(), aimDirection, attackRange, attackAngle, slashWidth, slashDuration,
            slashColor, reverseSlash, gameObject, attackDamage, knockbackDistance, knockbackDuration);
        activeSlashes.Add(slash);
        reverseSlash = !reverseSlash;
    }

    Vector3 SlashOrigin()
    {
        Vector3 origin = AreaOrigin();
        origin.y += slashHeight - 0.15f;
        return origin;
    }

    public static bool ContainsOffset(Vector3 offset, Vector3 direction, float range, float angle)
    {
        offset.y = 0f;
        direction.y = 0f;
        return offset.sqrMagnitude <= range * range &&
            (offset.sqrMagnitude < 0.0001f || angle >= 360f || Vector3.Angle(direction, offset) <= angle * 0.5f);
    }

    Vector3 AreaOrigin()
    {
        float y = GridWorld.I != null ? GridWorld.I.transform.position.y : transform.position.y - 1f;
        return new Vector3(transform.position.x, y + 0.15f, transform.position.z);
    }

    Vector3 ArcPoint(Vector3 origin, Vector3 direction, float fraction)
    {
        return origin + Quaternion.AngleAxis((fraction - 0.5f) * attackAngle, Vector3.up) * direction * attackRange;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = areaColor;
        Vector3 origin = AreaOrigin();
        Vector3 direction = Application.isPlaying && hasAim ? aimDirection : transform.forward;
        Vector3 previous = origin;
        for (int i = 0; i <= ArcSegments; i++)
        {
            Vector3 point = ArcPoint(origin, direction, i / (float)ArcSegments);
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
        Gizmos.DrawLine(previous, origin);
    }

    void OnDisable()
    {
        if (area != null) area.enabled = false;
        foreach (var slash in activeSlashes)
            if (slash != null) Destroy(slash.gameObject);
        activeSlashes.Clear();
    }
    void OnDestroy() { if (areaMaterial != null) Destroy(areaMaterial); }
}
