using UnityEngine;

/// <summary>Slot reservado para uma categoria específica de construção.</summary>
public sealed class SpecialBuildPoint : MonoBehaviour
{
    public string requiredBuildingId;
    public bool occupied;
    [Min(0.1f)] public float activationRadius = 4f;
    public Structure occupant;
    LineRenderer marker;
    Material markerMaterial;

    void Start()
    {
        var visual = new GameObject("SpecialPointMarker");
        visual.transform.SetParent(transform, false);
        marker = visual.AddComponent<LineRenderer>();
        markerMaterial = new Material(Shader.Find("Sprites/Default"));
        marker.sharedMaterial = markerMaterial;
        marker.useWorldSpace = true;
        marker.loop = true;
        marker.positionCount = 32;
        marker.startWidth = marker.endWidth = 0.12f;
        marker.enabled = false;
    }

    void LateUpdate()
    {
        if (occupied && occupant == null) occupied = false;
        if (marker == null || Game.I == null) return;
        bool nearby = PlayerVitals.I != null && Vector3.Distance(PlayerVitals.I.transform.position, transform.position) <= Game.I.interactionRange + activationRadius;
        marker.enabled = !Game.I.lost && ((Game.I.IsConstructionMode && Game.I.IsHandEmpty) || (Game.I.PlayerControlEnabled && nearby));
        if (!marker.enabled) return;
        marker.startColor = marker.endColor = occupied ? Color.gray : new Color(1f, 0.75f, 0.2f);
        for (int i = 0; i < marker.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / marker.positionCount;
            marker.SetPosition(i, transform.position + new Vector3(Mathf.Cos(angle) * 1.5f, 0.18f, Mathf.Sin(angle) * 1.5f));
        }
    }

    void OnDestroy() { if (markerMaterial != null) Destroy(markerMaterial); }

    void OnDrawGizmos()
    {
        Gizmos.color = occupied ? Color.gray : new Color(0.8f, 0.65f, 0.15f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.2f, new Vector3(5f, 0.4f, 5f));
    }
}
