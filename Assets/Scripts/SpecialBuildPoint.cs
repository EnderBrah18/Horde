using UnityEngine;

/// <summary>Slot reservado para uma categoria específica de construção.</summary>
public sealed class SpecialBuildPoint : MonoBehaviour
{
    public string requiredBuildingId;
    public bool occupied;
    [Min(0.1f)] public float activationRadius = 4f;
    public Structure occupant;

    void LateUpdate()
    {
        if (occupied && occupant == null) occupied = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = occupied ? Color.gray : new Color(0.8f, 0.65f, 0.15f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.2f, new Vector3(5f, 0.4f, 5f));
    }
}
