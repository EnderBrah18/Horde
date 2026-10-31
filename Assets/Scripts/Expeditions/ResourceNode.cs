using UnityEngine;

[DisallowMultipleComponent]
public sealed class ResourceNode : MonoBehaviour
{
    public ResourceNodeData data;
    public int Remaining { get; private set; }
    GridWorld grid;
    Bounds footprint;

    public void Initialize(ResourceNodeData definition, int stock, GridWorld navigation, Bounds bounds)
    {
        data = definition;
        Remaining = Mathf.Max(0, stock);
        grid = navigation;
        footprint = bounds;
    }

    public int Collect(ExpeditionCargo cargo)
    {
        if (!isActiveAndEnabled || data == null || Remaining <= 0 || cargo == null) return 0;
        int amount = cargo.Collect(data.resource, Mathf.Min(Remaining, Mathf.Max(1, data.amountPerInteraction)));
        Remaining -= amount;
        if (Remaining == 0)
        {
            if (grid != null) { grid.SetHardBlocked(footprint, false); grid.Recalc(); }
            gameObject.SetActive(false);
        }
        return amount;
    }
}
