using UnityEngine;

public enum ProductionMode { None, Active, Passive }
public enum BuildingMenuCategory { Automatic, Defense, Production, Services, Housing, Utility, Other }

// Uma construção = um asset. Balanceie tudo no Inspector, sem mexer em código.
[CreateAssetMenu(menuName = "Village/Building", fileName = "NewBuilding")]
public class BuildingData : ScriptableObject
{
    [Header("Identidade")]
    public string displayName = "Construção";
    [TextArea] public string description;
    public int order;                       // posição no menu (1, 2, 3...)
    public StructureKind kind = StructureKind.Wall;
    [Tooltip("Automatic classifies walls/towers as defense and producers by their output. Choose a category to override it.")]
    public BuildingMenuCategory menuCategory = BuildingMenuCategory.Automatic;

    [Header("Visual (formas geométricas)")]
    public PrimitiveType shape = PrimitiveType.Cube;
    public Color color = Color.gray;
    [Tooltip("X e Z em fração da célula (0.95 = quase a célula toda). Y é a altura em metros.")]
    public Vector3 size = new Vector3(0.95f, 1.5f, 0.95f);

    [Header("Custo")]
    public int woodCost = 5;
    public int suppliesCost = 0;

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

    [Header("Produção (opcional)")]
    public ProductionMode productionMode = ProductionMode.None;
    public ResourceType produces = ResourceType.Wood;
    public int amount = 0;                  // 0 = não produz
    public float cycleSeconds = 5f;

    void OnValidate()
    {
        woodCost = Mathf.Max(0, woodCost);
        suppliesCost = Mathf.Max(0, suppliesCost);
        maxHealth = Mathf.Max(1f, maxHealth);
        towerRange = Mathf.Max(0f, towerRange);
        towerDamage = Mathf.Max(0f, towerDamage);
        towerCooldown = Mathf.Max(0.01f, towerCooldown);
        cycleSeconds = Mathf.Max(0.1f, cycleSeconds);
    }
}
