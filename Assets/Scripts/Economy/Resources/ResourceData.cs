using UnityEngine;

public enum ResourceCategory { Material, Food, Magical, Manufactured }

// Um asset descreve o recurso. Quantidades pertencem à carteira, nunca ao asset.
[CreateAssetMenu(menuName = "Village/Resource", fileName = "NewResource")]
public sealed class ResourceData : ScriptableObject
{
    [Tooltip("Identificador estável para futuros saves. Não reutilize em outro recurso.")]
    public string id;
    public string displayName = "Novo recurso";
    [TextArea] public string description;
    public ResourceCategory category;
    public Sprite icon;
    public Color color = Color.white;
}
