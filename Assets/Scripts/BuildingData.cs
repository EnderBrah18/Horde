using UnityEngine;

public enum BuildingKind { Wall = 0, Tower = 1 }
public enum BuildingMenuCategory { Automatic = 0, Defense = 1, Offense = 2 }

// Uma construção = um asset. Balanceie tudo no Inspector, sem mexer em código.
[CreateAssetMenu(menuName = "Village/Building", fileName = "NewBuilding")]
public class BuildingData : ScriptableObject
{
    [Header("Identidade")]
    public string displayName = "Construção";
    [TextArea] public string description;
    public int order;                       // posição no menu (1, 2, 3...)
    public bool showInBuildMenu = true;
    [Tooltip("Próxima versão desta construção. Os Costs desse asset são cobrados pela melhoria.")]
    public BuildingData upgrade;
    public BuildingKind kind = BuildingKind.Wall;
    public StructureKind RuntimeKind => kind == BuildingKind.Wall ? StructureKind.Wall : StructureKind.Tower;
    [Tooltip("Automatic coloca barreiras em Defesas e torres em Ofensivas.")]
    public BuildingMenuCategory menuCategory = BuildingMenuCategory.Automatic;

    [Header("Visual (formas geométricas)")]
    public PrimitiveType shape = PrimitiveType.Cube;
    public Color color = Color.gray;
    [Tooltip("Escala do protótipo em unidades do mundo. Cilindros e cápsulas têm altura igual a 2 vezes Y.")]
    public Vector3 size = new Vector3(0.95f, 1.5f, 0.95f);

    [Header("Custo")]
    public ResourceAmount[] costs = new ResourceAmount[0];

    [Header("Posicionamento")]
    [Tooltip("Quando ativo, esta construção só pode ocupar um ponto especial com o mesmo ID.")]
    public bool requiresSpecialBuildPoint;
    public string specialBuildPointId;

    [Header("Atributos")]
    public float maxHealth = 60f;

    [Header("Torre (usado quando Tipo = Torre)")]
    [Min(0f)] public float towerRange = 12f;
    [Min(0f)] public float towerDamage = 7f;
    [Min(0.01f)] public float towerCooldown = 0.45f;

    void OnValidate()
    {
        if (costs != null)
            for (int i = 0; i < costs.Length; i++) costs[i].amount = Mathf.Max(0, costs[i].amount);
        maxHealth = Mathf.Max(1f, maxHealth);
        towerRange = Mathf.Max(0f, towerRange);
        towerDamage = Mathf.Max(0f, towerDamage);
        towerCooldown = Mathf.Max(0.01f, towerCooldown);
    }
}
