using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Controle no plano XZ, usando o mesmo mapa navegável dos inimigos.
[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [Min(0.1f)] public float moveSpeed = 8f;
    [Min(1f)] public float turnSpeed = 720f;
    [Header("Colisão (metros, com escala do Transform igual a 1)")]
    [Min(0.1f)] public float radius = 0.45f;
    [Min(0.2f)] public float height = 2f;
    const float Skin = 0.03f;
    readonly List<Renderer> structureRenderers = new List<Renderer>();

    int ObstacleMask => LayerMask.GetMask("Structures", "AncientWalls", "NPCs");

    void Awake() { ConfigureCollider(); }

    void OnValidate()
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        turnSpeed = Mathf.Max(1f, turnSpeed);
        radius = Mathf.Max(0.1f, radius);
        height = Mathf.Max(radius * 2f, height);
        ConfigureCollider();
    }

    void ConfigureCollider()
    {
        var body = GetComponent<CapsuleCollider>();
        if (body == null) return;
        body.radius = radius;
        body.height = height;
        body.center = Vector3.zero;
        body.direction = 1;
    }

    void Update()
    {
        if (Game.I == null || !Game.I.PlayerControlEnabled || Keyboard.current == null || GridWorld.I == null)
            return;
        var keyboard = Keyboard.current;
        Vector2 input = new Vector2((keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
            (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
        input = Vector2.ClampMagnitude(input, 1f);
        var camera = Camera.main;
        Vector3 right = camera != null ? camera.transform.right : Vector3.right;
        right.y = 0f;
        right.Normalize();
        Vector3 forward = Vector3.Cross(right, Vector3.up);
        Vector3 motion = (right * input.x + forward * input.y) * moveSpeed * Time.deltaTime;
        // Substeps keep river and boundary checks valid during slow frames.
        int steps = Mathf.Max(1, Mathf.CeilToInt(motion.magnitude / Mathf.Max(0.05f, radius * 0.5f)));
        Vector3 before = transform.position;
        for (int i = 0; i < steps; i++) MoveStep(motion / steps);
        Vector3 facing = transform.position - before;
        if (facing.sqrMagnitude > 0.000001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), turnSpeed * Time.deltaTime);
    }

    void MoveStep(Vector3 delta)
    {
        if (CanMove(transform.position, delta)) { transform.position += delta; return; }
        // Slide along walls instead of stopping diagonal movement completely.
        Vector3 x = new Vector3(delta.x, 0f, 0f);
        Vector3 z = new Vector3(0f, 0f, delta.z);
        if (CanMove(transform.position, x)) transform.position += x;
        if (CanMove(transform.position, z)) transform.position += z;
    }

    bool CanMove(Vector3 position, Vector3 delta)
    {
        if (!CanOccupy(position + delta)) return false;
        float distance = delta.magnitude;
        if (distance < 0.00001f) return true;
        Capsule(position, out Vector3 bottom, out Vector3 top);
        return !Physics.CapsuleCast(bottom, top, radius, delta / distance, distance + Skin,
            ObstacleMask, QueryTriggerInteraction.Ignore);
    }

    public bool CanOccupy(Vector3 position)
    {
        var grid = GridWorld.I;
        if (grid == null) return false;
        // Check every grid cell touched by the circular footprint, including corners.
        Vector2Int min = grid.WorldToCell(position - new Vector3(radius, 0f, radius));
        Vector2Int max = grid.WorldToCell(position + new Vector3(radius, 0f, radius));
        for (int x = min.x; x <= max.x; x++)
            for (int z = min.y; z <= max.y; z++)
            {
                var cell = new Vector2Int(x, z);
                Vector3 center = grid.CellToWorld(cell);
                float dx = Mathf.Max(0f, Mathf.Abs(position.x - center.x) - grid.cellSize * 0.5f);
                float dz = Mathf.Max(0f, Mathf.Abs(position.z - center.z) - grid.cellSize * 0.5f);
                if (dx * dx + dz * dz <= radius * radius && (!grid.InBounds(cell) || grid.IsHardBlocked(cell)))
                    return false;
            }
        if (OverlapsSceneStructure(position)) return false;
        Capsule(position, out Vector3 bottom, out Vector3 top);
        return !Physics.CheckCapsule(bottom, top, radius + Skin, ObstacleMask, QueryTriggerInteraction.Ignore);
    }

    bool OverlapsSceneStructure(Vector3 position)
    {
        Capsule(position, out Vector3 bottom, out Vector3 top);
        float clearance = radius + Skin;
        foreach (var structure in Structure.All)
        {
            if (structure == null || !structure.isActiveAndEnabled) continue;
            var body = structure.GetComponent<Collider>();
            // Enabled colliders already use the precise physics checks above.
            if (body != null && body.enabled && !body.isTrigger) continue;

            // Disabled collider bounds are empty. Use the visible building bounds,
            // just as the scene registration does, without changing enemy physics.
            structure.GetComponentsInChildren<Renderer>(false, structureRenderers);
            foreach (var renderer in structureRenderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer is LineRenderer) continue;
                Bounds bounds = renderer.bounds;
                float dx = Mathf.Max(0f, Mathf.Abs(position.x - bounds.center.x) - bounds.extents.x);
                float dz = Mathf.Max(0f, Mathf.Abs(position.z - bounds.center.z) - bounds.extents.z);
                float dy = Mathf.Max(0f, Mathf.Max(bounds.min.y - top.y, bottom.y - bounds.max.y));
                if (dx * dx + dy * dy + dz * dz <= clearance * clearance) return true;
            }
        }
        return false;
    }

    void Capsule(Vector3 center, out Vector3 bottom, out Vector3 top)
    {
        float offset = Mathf.Max(0f, height * 0.5f - radius);
        bottom = center - Vector3.up * offset;
        top = center + Vector3.up * offset;
    }
}
