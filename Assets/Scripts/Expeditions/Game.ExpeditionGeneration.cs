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
