using UnityEngine;

[CreateAssetMenu(menuName = "Village/Expedition", fileName = "NewExpedition")]
public sealed class ExpeditionData : ScriptableObject
{
    public string displayName = "Rota da mata";
    [TextArea] public string description;
    [TextArea] public string discoveryMessage = "Um sobrevivente encontrou uma passagem para uma região com recursos.";
    [Min(1)] public int discoveryDay = 2;
    public string sceneName = "Expedicoes";
    [Min(11)] public int testRegionSize = 31;
    public Color groundColor = new Color(0.25f, 0.35f, 0.2f);
    [Header("Região e coleta")]
    public int seed = 12345;
    public bool varySeedPerVisit = true;
    public ResourceNodeData[] resourceNodes = new ResourceNodeData[0];
    [Tooltip("Capacidade temporária medida em unidades de recursos, não em peso.")]
    [Min(1)] public int cargoCapacity = 100;
    [Min(5f)] public float explorationCameraZoom = 18f;
}
