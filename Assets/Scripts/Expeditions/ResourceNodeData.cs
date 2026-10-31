using UnityEngine;

public enum ResourceNodeVisual { Tree, Rock, Crate, Grain }

[CreateAssetMenu(menuName = "Village/Resource Node", fileName = "NewResourceNode")]
public sealed class ResourceNodeData : ScriptableObject
{
    public string displayName = "Coleta";
    public ResourceData resource;
    public ResourceNodeVisual visual;
    public Color color = Color.gray;
    [Min(0)] public int nodesPerRegion = 8;
    [Min(1)] public int minimumStock = 8;
    [Min(1)] public int maximumStock = 14;
    [Min(1)] public int amountPerInteraction = 3;
}
