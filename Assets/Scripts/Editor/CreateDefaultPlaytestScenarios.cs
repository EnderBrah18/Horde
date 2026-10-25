using UnityEditor;
using UnityEngine;

public static class CreateDefaultPlaytestScenarios
{
    [MenuItem("Tools/Village/Create Playtest Scenario Templates")]
    static void Create()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/PlaytestScenarios"))
            AssetDatabase.CreateFolder("Assets/Resources", "PlaytestScenarios");

        Make("Crossroads", s =>
        {
            s.displayName = "Encruzilhada";
            s.instructions = "Cenário de referência: quatro entradas e espaço equilibrado para construir.";
        });
        Make("WideDefense", s =>
        {
            s.displayName = "Defesa ampla";
            s.instructions = "Mapa maior, com duas frentes opostas. Compare linhas de torres e corredores de paredes.";
            s.gridWidth = 31; s.gridHeight = 31;
            s.spawnEdges = SpawnEdges.North | SpawnEdges.South;
            s.groupsPerWave = 2; s.maxWaves = 4;
            s.baseCommonEnemies = 5; s.commonsPerRound = 1;
            s.buildGridWidth = 25; s.buildGridHeight = 25;
        });
        Make("Enxame", s =>
        {
            s.displayName = "Enxame";
            s.instructions = "Pressão alta com grupos numerosos e surgimento rápido por várias direções.";
            s.gridWidth = 31; s.gridHeight = 31;
            s.groupsPerWave = 2; s.nightsPerExtraWave = 1; s.maxWaves = 5;
            s.baseCommonEnemies = 6; s.commonsPerRound = 2;
            s.spawnInterval = 0.12f; s.groupInterval = 1f;
            s.hunterBaseChance = 0.2f; s.hunterChanceCap = 0.45f;
            s.buildGridWidth = 21; s.buildGridHeight = 21;
            s.startWood = 80; s.startCrystals = 8;
        });
        Make("CercoPesado", s =>
        {
            s.displayName = "Cerco pesado";
            s.instructions = "Uma frente principal com tanks mais frequentes. Teste durabilidade e poder de fogo.";
            s.gridWidth = 31; s.gridHeight = 21;
            s.spawnEdges = SpawnEdges.North;
            s.tankEveryRounds = 1;
            s.baseCommonEnemies = 3; s.commonsPerRound = 1;
            s.tankEnemy.maxHealth = 210f; s.tankEnemy.damage = 20f;
            s.buildGridWidth = 23; s.buildGridHeight = 15;
            s.startWood = 80; s.startCrystals = 8;
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Cenários de playtest criados em Assets/Resources/PlaytestScenarios");
    }

    static void Make(string file, System.Action<PlaytestScenario> setup)
    {
        string path = "Assets/Resources/PlaytestScenarios/" + file + ".asset";
        if (AssetDatabase.LoadAssetAtPath<PlaytestScenario>(path) != null) return;
        var scenario = ScriptableObject.CreateInstance<PlaytestScenario>();
        setup(scenario);
        AssetDatabase.CreateAsset(scenario, path);
    }
}
