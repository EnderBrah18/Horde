using UnityEngine;

public enum SpawnSide { North, South, East, West }

/// <summary>Marcador de uma entrada inimiga no mapa da demo.</summary>
public sealed class EnemySpawnPoint : MonoBehaviour
{
    public SpawnSide side;
    [Min(0f)] public float weight = 1f;
    public bool enabledForSpawning = true;

    void OnDrawGizmos()
    {
        Gizmos.color = enabledForSpawning ? new Color(0.9f, 0.15f, 0.12f) : Color.gray;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.25f, 2f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 8f);
    }
}
