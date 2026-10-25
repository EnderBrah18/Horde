using UnityEngine;

public enum NpcCategory { Resident, Essential }

[CreateAssetMenu(menuName = "Village/NPC", fileName = "NewNpc")]
public sealed class NpcData : ScriptableObject
{
    public string displayName = "Morador";
    [TextArea] public string description;
    public Sprite portrait;
    public GameObject prefab;
    public NpcCategory category;
    [Tooltip("Personagens nomeados só podem ser recrutados uma vez enquanto estiverem na vila.")]
    public bool unique;
    [Min(1f)] public float maxHealth = 50f;
    [Min(0.1f)] public float moveSpeed = 3f;
    public Color placeholderColor = new Color(0.3f, 0.7f, 0.8f);
    public ResourceAmount[] recruitmentCosts = new ResourceAmount[0];
    public ResourceAmount[] recoveryCosts = new ResourceAmount[0];
    public NpcRoleData[] allowedRoles = new NpcRoleData[0];
}
