using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Area de construção livre centrada no núcleo da vila. A área aumenta por níveis,
/// sem depender de uma malha de células.
/// </summary>
public sealed class VillageBuildArea : MonoBehaviour
{
    [SerializeField] Transform center;
    [Min(1f)] public float initialRadius = 80f;
    [Min(0f)] public float radiusPerUpgrade = 20f;
    [Min(0)] public int expansionLevel;
    [Min(0)] public int maxExpansionLevel = 4;
    [Min(0)] public int baseWoodCost = 30;
    [Min(0)] public int woodCostPerLevel = 20;
    [FormerlySerializedAs("baseSuppliesCost")]
    [Min(0)] public int baseCrystalsCost = 5;
    [FormerlySerializedAs("suppliesCostPerLevel")]
    [Min(0)] public int crystalsCostPerLevel = 3;

    public Transform Center => center != null ? center : transform;
    public float Radius => initialRadius + radiusPerUpgrade * expansionLevel;

    public bool Contains(Vector3 worldPosition, float footprintRadius = 0f)
    {
        Vector2 fromCenter = new Vector2(worldPosition.x - Center.position.x,
            worldPosition.z - Center.position.z);
        return fromCenter.magnitude + Mathf.Max(0f, footprintRadius) <= Radius;
    }

    public bool CanExpand => expansionLevel < maxExpansionLevel;
    public int NextWoodCost => baseWoodCost + expansionLevel * woodCostPerLevel;
    public int NextCrystalsCost => baseCrystalsCost + expansionLevel * crystalsCostPerLevel;

    public bool TryExpand(Wallet wallet)
    {
        if (!CanExpand || wallet == null || !wallet.Spend(NextWoodCost, NextCrystalsCost))
            return false;

        expansionLevel++;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        var c = Center != null ? Center.position : transform.position;
        Gizmos.color = new Color(0.25f, 0.9f, 0.45f, 0.8f);
        Gizmos.DrawWireSphere(new Vector3(c.x, 0.1f, c.z), Radius);
    }
}
