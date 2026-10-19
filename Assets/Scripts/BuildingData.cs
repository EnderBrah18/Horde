using UnityEngine;

// Uma construção = um asset. Balanceie tudo no Inspector, sem mexer em código.
[CreateAssetMenu(menuName = "Village/Building", fileName = "NewBuilding")]
public class BuildingData : ScriptableObject
{
    [Header("Identidade")]
    public string displayName = "Construção";
    [TextArea] public string description;
    public int order;                       // posição no menu (1, 2, 3...)
    public StructureKind kind = StructureKind.Wall;

    [Header("Visual (formas geométricas)")]
    public PrimitiveType shape = PrimitiveType.Cube;
    public Color color = Color.gray;
    [Tooltip("X e Z em fração da célula (0.95 = quase a célula toda). Y é a altura em metros.")]
    public Vector3 size = new Vector3(0.95f, 1.5f, 0.95f);

    [Header("Custo")]
    public int woodCost = 5;
    public int suppliesCost = 0;

    [Header("Atributos")]
    public float maxHealth = 60f;

    [Header("Produção (opcional)")]
    public ResourceType produces = ResourceType.Wood;
    public int amount = 0;                  // 0 = não produz
    public float cycleSeconds = 5f;
}
