using System.Collections.Generic;
using UnityEngine;

// Tactical groups, individual preferences and external escorts all reserve here.
public static class TankCoverFormation
{
    sealed class Layout
    {
        public readonly FormationSlots<Enemy> slots = new FormationSlots<Enemy>();
        public float rear = 1f;
        public float spacing = 1f;
    }
    sealed class Reservation
    {
        public Enemy anchor;
        public int lastFrame;
    }
    static readonly Dictionary<Enemy, Layout> layouts = new Dictionary<Enemy, Layout>();
    static readonly Dictionary<Enemy, Reservation> reservations = new Dictionary<Enemy, Reservation>();
    static readonly List<Enemy> stale = new List<Enemy>();
    static int prunedFrame = -1;

    public static bool CanReach(Enemy member, Enemy anchor, float rear, float spacing)
    {
        Prune();
        if (!Valid(member, anchor)) return false;
        if (layouts.TryGetValue(anchor, out var layout))
        { rear = Mathf.Max(rear, layout.rear); spacing = Mathf.Max(spacing, layout.spacing); }
        return Route(member, anchor, rear, spacing, 0, out _);
    }

    public static bool TrySteer(Enemy member, Enemy anchor, float rear, float spacing, out Vector3 movement)
    {
        movement = Vector3.zero;
        Prune();
        if (!Valid(member, anchor)) { Release(member); return false; }
        if (reservations.TryGetValue(member, out var reservation) && reservation.anchor != anchor)
        { Release(member); reservation = null; }
        if (!layouts.TryGetValue(anchor, out var layout))
        { layout = new Layout(); layouts.Add(anchor, layout); }
        // All followers of this Tank use the same geometry, even with different asset/group settings.
        // Keep conservative dimensions until the formation empties to prevent oscillation.
        layout.rear = Mathf.Max(layout.rear, Mathf.Max(1f, rear));
        layout.spacing = Mathf.Max(layout.spacing, Mathf.Max(1f, spacing));
        int slot = layout.slots.Claim(member);
        if (reservation == null)
        { reservation = new Reservation { anchor = anchor }; reservations[member] = reservation; }
        reservation.lastFrame = Time.frameCount;
        if (Route(member, anchor, layout.rear, layout.spacing, slot, out movement)) return true;
        Release(member);
        return false;
    }

    public static void EndSteering(Enemy member)
    {
        if (reservations.TryGetValue(member, out var reservation) && reservation.lastFrame != Time.frameCount)
            Release(member);
    }

    public static void Release(Enemy member)
    {
        if (ReferenceEquals(member, null) || !reservations.TryGetValue(member, out var reservation)) return;
        if (layouts.TryGetValue(reservation.anchor, out var layout))
        {
            layout.slots.Release(member);
            if (layout.slots.Count == 0) layouts.Remove(reservation.anchor);
        }
        reservations.Remove(member);
    }

    public static void RemoveEnemy(Enemy enemy)
    {
        Release(enemy);
        stale.Clear();
        foreach (var pair in reservations)
            if (ReferenceEquals(pair.Value.anchor, enemy)) stale.Add(pair.Key);
        foreach (var follower in stale) Release(follower);
        layouts.Remove(enemy);
    }

    static void Prune()
    {
        if (prunedFrame == Time.frameCount) return;
        prunedFrame = Time.frameCount;
        stale.Clear();
        foreach (var pair in reservations)
            if (!Valid(pair.Key, pair.Value.anchor) || pair.Value.lastFrame < Time.frameCount - 1) stale.Add(pair.Key);
        foreach (var member in stale) Release(member);
    }

    static bool Valid(Enemy member, Enemy anchor) => member != null && anchor != null && member != anchor &&
        member.isActiveAndEnabled && anchor.isActiveAndEnabled && member.state == EState.March &&
        anchor.state == EState.March && member.hp != null && member.hp.current > 0 &&
        anchor.hp != null && anchor.hp.current > 0 && anchor.type == EnemyType.Tank &&
        member.NavigationGrid != null && member.NavigationGrid == anchor.NavigationGrid &&
        member.gameObject.scene == anchor.gameObject.scene;

    static bool Route(Enemy member, Enemy anchor, float rear, float spacing, int slot, out Vector3 movement)
    {
        movement = Vector3.zero;
        var grid = member.NavigationGrid;
        Vector3 forward = anchor.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 destination = anchor.transform.position - forward * (rear + FormationSlots<Enemy>.Row(slot) * spacing)
            + right * (FormationSlots<Enemy>.Column(slot) * spacing);
        destination.y = grid.transform.position.y;
        var cell = grid.WorldToCell(destination);
        if (!grid.InBounds(cell) || grid.IsHardBlocked(cell) || grid.Get(cell) != null) return false;
        Vector3 delta = destination - member.transform.position;
        delta.y = 0;
        float distance = delta.magnitude;
        if (distance <= 0.8f) return true;
        if (!grid.TryOpenRoute(member.transform.position, cell, out var route, 16f, member.NavigationClearance)) return false;
        float pace = Mathf.Clamp01(anchor.speed / Mathf.Max(0.1f, member.speed) + Mathf.Max(0f, distance - spacing) * 0.15f);
        movement = route * pace;
        return true;
    }
}
