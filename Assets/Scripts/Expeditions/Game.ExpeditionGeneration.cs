using UnityEngine;
using UnityEngine.InputSystem;

public partial class Game
{
    readonly System.Collections.Generic.List<ResourceNode> expeditionNodes = new System.Collections.Generic.List<ResourceNode>();
    ResourceNode nearbyResource;
    ExpeditionCargo expeditionCargo;
    int expeditionVisit;
    string collectionMessage = "";
    float villageCameraZoom;

    GameObject ExpeditionProp(Transform parent, PrimitiveType shape, Vector3 position, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(shape);
        Destroy(obj.GetComponent<Collider>());
        obj.transform.SetPositionAndRotation(position, Quaternion.identity);
        obj.transform.localScale = scale;
        obj.transform.SetParent(parent, true);
        var renderer = obj.GetComponent<Renderer>();
        if (litMaterial != null) renderer.sharedMaterial = litMaterial;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        renderer.SetPropertyBlock(properties);
        return obj;
    }

    void GenerateExpedition(Transform root, int size)
    {
        expeditionNodes.Clear(); nearbyResource = null; collectionMessage = "";
        expeditionCargo = new ExpeditionCargo(activeDestination.cargoCapacity);
        int seed = unchecked(activeDestination.seed + (activeDestination.varySeedPerVisit ? expeditionVisit++ * 7919 : 0));
        var random = new System.Random(seed);
        var definitions = activeDestination.resourceNodes;
        if (definitions == null || definitions.Length == 0) definitions = Resources.LoadAll<ResourceNodeData>("Expeditions/Nodes");
        Vector3 center = root.position;
        var road = new Color(0.48f, 0.39f, 0.26f);
        ExpeditionProp(root, PrimitiveType.Cube, center + Vector3.up * 0.02f, new Vector3(size * 2f, 0.03f, 3f), road).name = "Trilha_LesteOeste";
        ExpeditionProp(root, PrimitiveType.Cube, center + Vector3.up * 0.04f, new Vector3(3f, 0.03f, size * 2f), road).name = "Trilha_NorteSul";
        foreach (var definition in definitions)
        {
            if (definition == null || definition.resource == null) continue;
            int count = Mathf.Clamp(definition.nodesPerRegion, 0, 200);
            for (int n = 0; n < count; n++)
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    var cell = new Vector2Int(random.Next(2, size - 2), random.Next(2, size - 2));
                    Vector3 position = grid.CellToWorld(cell);
                    Vector3 delta = position - center;
                    if (delta.sqrMagnitude < 80f || Mathf.Abs(delta.x) < 3f || Mathf.Abs(delta.z) < 3f) continue;
                    bool free = true;
                    for (int x = -1; x <= 1; x++)
                        for (int z = -1; z <= 1; z++)
                            if (grid.IsHardBlocked(cell + new Vector2Int(x,z))) free = false;
                    if (!free) continue;
                    var obj = new GameObject(definition.displayName);
                    obj.transform.SetParent(root, false); obj.transform.position = position;
                    BuildResourceVisual(obj.transform, definition.visual, definition.color);
                    var footprint = new Bounds(position, new Vector3(1.2f, 3f, 1.2f));
                    grid.SetHardBlocked(footprint, true);
                    var node = obj.AddComponent<ResourceNode>();
                    int stock = random.Next(Mathf.Clamp(definition.minimumStock, 1, 1000000),
                        Mathf.Clamp(Mathf.Max(definition.minimumStock, definition.maximumStock), 1, 1000000) + 1);
                    node.Initialize(definition, stock, grid, footprint);
                    expeditionNodes.Add(node);
                    break;
                }
        }
        // Forest beyond the playable boundary frames the region and its trails.
        float edge = size + 1f;
        for (int i = 0; i < size; i += 2)
        {
            float axis = -size + i * 2f;
            foreach (var p in new[] { new Vector3(axis,0,edge), new Vector3(axis,0,-edge), new Vector3(edge,0,axis), new Vector3(-edge,0,axis) })
            {
                var tree = new GameObject("Floresta_Limite");
                tree.transform.SetParent(root, false); tree.transform.position = center + p;
                BuildResourceVisual(tree.transform, ResourceNodeVisual.Tree, new Color(0.17f, 0.33f, 0.16f));
            }
        }
        grid.Recalc();
    }

    void BuildResourceVisual(Transform root, ResourceNodeVisual visual, Color color)
    {
        Vector3 p = root.position;
        switch (visual)
        {
            case ResourceNodeVisual.Tree:
                ExpeditionProp(root, PrimitiveType.Cylinder, p + Vector3.up * 1.4f, new Vector3(0.5f,1.4f,0.5f), new Color(0.3f,0.2f,0.12f));
                ExpeditionProp(root, PrimitiveType.Sphere, p + Vector3.up * 3.4f, new Vector3(3.2f,3.5f,3.2f), color);
                break;
            case ResourceNodeVisual.Rock:
                ExpeditionProp(root, PrimitiveType.Sphere, p + Vector3.up * 0.6f, new Vector3(2f,1.4f,1.8f), color);
                ExpeditionProp(root, PrimitiveType.Cube, p + new Vector3(0.3f,1f,0.1f), Vector3.one * 0.7f, color * 1.2f);
                break;
            case ResourceNodeVisual.Grain:
                for (int i = 0; i < 5; i++)
                    ExpeditionProp(root, PrimitiveType.Capsule, p + new Vector3((i%3-1)*0.4f,0.6f,(i/3)*0.4f), new Vector3(0.2f,0.6f,0.2f), color);
                break;
            default:
                ExpeditionProp(root, PrimitiveType.Cube, p + Vector3.up * 0.5f, new Vector3(1.2f,1f,1f), color);
                break;
        }
    }

    void UpdateResourceCollection(Keyboard keyboard)
    {
        nearbyResource = null;
        if (PlayerVitals.I == null || lost) return;
        float nearest = interactionRange;
        Vector3 player = PlayerVitals.I.transform.position;
        foreach (var node in expeditionNodes)
        {
            if (node == null || !node.isActiveAndEnabled || node.Remaining <= 0) continue;
            Vector3 delta = node.transform.position - player; delta.y = 0f;
            if (delta.magnitude < nearest) { nearest = delta.magnitude; nearbyResource = node; }
        }
        if (!keyboard.eKey.wasPressedThisFrame || nearbyResource == null || NearExtraction()) return;
        blockedAttackFrame = Time.frameCount;
        int amount = nearbyResource.Collect(expeditionCargo);
        collectionMessage = amount > 0 ? $"+{amount} {nearbyResource.data.resource.displayName} na carga" : "Carga cheia. Retorne à vila para entregar os recursos.";
    }
}
