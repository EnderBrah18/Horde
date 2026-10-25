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

        Make("Wall", d =>
        {
            d.displayName = "Parede"; d.order = 1; d.kind = BuildingKind.Wall;
            d.description = "Bloqueia o caminho. Inimigos batem nela para abrir passagem.";
            d.shape = PrimitiveType.Cube; d.color = new Color(0.55f, 0.55f, 0.55f);
            d.size = new Vector3(0.95f, 1.5f, 0.95f);
            d.costs = new[] { new ResourceAmount(DefaultResources.Wood, 5) }; d.maxHealth = 60f;
        });

        Make("Tower", d =>
        {
            d.displayName = "Torre"; d.order = 2; d.kind = BuildingKind.Tower;
            d.description = "Atira no inimigo mais próximo.";
            d.shape = PrimitiveType.Cylinder; d.color = new Color(0.2f, 0.8f, 0.9f);
            d.size = new Vector3(0.6f, 1.2f, 0.6f);
            d.costs = new[] { new ResourceAmount(DefaultResources.Wood, 15), new ResourceAmount(DefaultResources.Crystals, 2) }; d.maxHealth = 50f;
            d.towerRange = 12f; d.towerDamage = 7f; d.towerCooldown = 0.45f;
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
