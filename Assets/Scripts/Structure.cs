using System.Collections.Generic;
using UnityEngine;

public enum StructureKind { Wall = 0, Tower = 1, Core = 2, Facility = 3 }

public class Structure : MonoBehaviour
{
    public static readonly List<Structure> All = new List<Structure>();

    public StructureKind kind;
    public Vector2Int cell;
    public Health health;
    public BuildingData data;
    [System.NonSerialized] public ResourceAmount[] investedCosts;
    public ResourceAmount[] Investment => investedCosts ?? (data != null ? data.costs : null);
    public bool TryGetRefund(int currentRound, out ResourceAmount[] refund) =>
        BuildingInvestment.TryRefund(Investment, RefundRate(currentRound) == 1f ? 100 : 70, out refund);
    // Round counts nights already started. Scene structures default to the used rate.
    [System.NonSerialized] public int placementRound = -1;
    public float RefundRate(int currentRound) => placementRound >= 0 && placementRound == currentRound ? 1f : 0.7f;
    public bool indestructible;
    public bool blocksMovement = true;
    [System.NonSerialized] public VillageFacility facility;
    public bool BlocksMovement => isActiveAndEnabled && blocksMovement && (facility == null || facility.IsOperational);
    public bool CanBeAttacked => BlocksMovement && !indestructible && health != null && !health.invulnerable && health.current > 0f;

    public void RefreshNavigation()
    {
        var grid = GridWorld.I;
        if (grid == null) return;
        grid.Clear(this);
        if (!BlocksMovement) return;
        var renderer = GetComponent<Renderer>();
        Bounds bounds = renderer != null ? renderer.bounds : new Bounds(transform.position, transform.lossyScale);
        grid.SetFootprint(this, bounds);
        grid.Recalc();
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void OnDestroy()
    {
        if (GridWorld.I != null) GridWorld.I.Clear(this);
    }
}
