using UnityEditor;
using UnityEngine;

// Menu: Tools > Village > Create Default Buildings
// Cria Parede e Torre em Assets/Resources/Buildings (não sobrescreve se já existirem).
public static class CreateDefaultBuildings
{
    [MenuItem("Tools/Village/Create Default Buildings")]
    static void Create()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Buildings"))
            AssetDatabase.CreateFolder("Assets/Resources", "Buildings");

        Make("Parede", d =>
        {
            d.displayName = "Cerca"; d.order = 1; d.kind = BuildingKind.Wall;
            d.description = "Bloqueia o caminho. Inimigos batem nela para abrir passagem.";
            d.shape = PrimitiveType.Cube; d.color = new Color(0.55f, 0.55f, 0.55f);
            d.size = new Vector3(2.4f, 1.2f, 0.4f);
            d.costs = new[] { new ResourceAmount(DefaultResources.Wood, 8),
                new ResourceAmount(Resources.Load<ResourceData>("ResourceDefinitions/Materials/PlantFiber"),2) }; d.maxHealth = 140f;
            d.upgrade = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Resources/Buildings/CercaReforcada.asset");
        });

        Make("Torre", d =>
        {
            d.displayName = "Torre de vigia"; d.order = 10; d.kind = BuildingKind.Tower;
            d.description = "Atira no inimigo mais próximo.";
            d.shape = PrimitiveType.Cylinder; d.color = new Color(0.2f, 0.8f, 0.9f);
            d.size = new Vector3(1.6f, 2f, 1.6f);
            d.costs = new[] { new ResourceAmount(DefaultResources.Wood, 28),
                new ResourceAmount(Resources.Load<ResourceData>("ResourceDefinitions/Materials/Stone"),8) }; d.maxHealth = 180f;
            d.towerRange = 14f; d.towerDamage = 9f; d.towerCooldown = 1f;
            d.upgrade = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Resources/Buildings/TorreReforcada.asset");
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Construções criadas em Assets/Resources/Buildings");
    }

    static void Make(string file, System.Action<BuildingData> setup)
    {
        string path = "Assets/Resources/Buildings/" + file + ".asset";
        if (AssetDatabase.LoadAssetAtPath<BuildingData>(path) != null) return;
        var d = ScriptableObject.CreateInstance<BuildingData>();
        setup(d);
        AssetDatabase.CreateAsset(d, path);
    }
}
