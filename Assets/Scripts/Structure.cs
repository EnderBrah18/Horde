using System.Collections.Generic;
using UnityEngine;

public enum StructureKind { Wall, Tower, Core, Producer }

public class Structure : MonoBehaviour
{
    public static readonly List<Structure> All = new List<Structure>();

    public StructureKind kind;
    public Vector2Int cell;
    public Health health;
    public BuildingData data;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void OnDestroy()
    {
        if (GridWorld.I != null) GridWorld.I.Clear(this);
    }
}
