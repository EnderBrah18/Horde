using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public partial class Game
{
    [Header("Descoberta de expedições")]
    public ExpeditionData[] expeditionDestinations = new ExpeditionData[0];
    public bool showInitialDayGuidance = true;
    public bool IsOnExpedition { get; private set; }
    public bool ExpeditionTransition => expeditionTransition;
    bool expeditionTransition;
    int lastPersonalExpeditionDay = -1;
    public bool PersonalExpeditionUsedToday => !VillageProgressionRules.CanTakePersonalExpedition(Day, lastPersonalExpeditionDay);
    const string TooLateForExpedition = "Já está muito tarde. Você não pode sair em outra expedição hoje. Aguarde o próximo dia.";
    readonly HashSet<ExpeditionData> discoveredDestinations = new HashSet<ExpeditionData>();
    readonly List<ExpeditionExit> expeditionExits = new List<ExpeditionExit>();
    readonly List<Material> expeditionMaterials = new List<Material>();
    ExpeditionData activeDestination;
    GridWorld villageGrid;
    Vector3 villagePlayerPosition;
    Quaternion villagePlayerRotation;
    Vector3 extractionPosition;
    Scene expeditionScene;

    void PrepareExpeditions()
    {
        if (expeditionDestinations == null || expeditionDestinations.Length == 0)
            expeditionDestinations = Resources.LoadAll<ExpeditionData>("Expeditions");
        expeditionExits.AddRange(FindObjectsByType<ExpeditionExit>());
        if (expeditionExits.Count == 0)
        {
            var marker = Prim(PrimitiveType.Cube, new Vector3(-8f, 0.75f, -12f), new Vector3(2f, 1.5f, 1f), new Color(0.8f, 0.6f, 0.2f));
            marker.name = "Posto_Expedicoes";
            expeditionExits.Add(marker.AddComponent<ExpeditionExit>());
        }
        DiscoverExpeditions();
        if (Day == 1 && showInitialDayGuidance)
            Say("Dia 1: explore a vila, restaure casas e locais de trabalho, recrute moradores e prepare as defesas. Espaço inicia a noite.", 16f);
    }

    public bool IsDestinationDiscovered(ExpeditionData destination) => destination != null && discoveredDestinations.Contains(destination);

    void DiscoverExpeditions()
    {
        if (night || lost || expeditionDestinations == null) return;
        foreach (var destination in expeditionDestinations)
        {
            if (destination == null || !VillageProgressionRules.CanDiscover(Day, destination.discoveryDay,
                night, lost, discoveredDestinations.Contains(destination))) continue;
            discoveredDestinations.Add(destination);
            string notice = destination.discoveryMessage + " Rota descoberta: " + destination.displayName + ". Consulte o posto de expedições.";
            RaiseGameplayEvent(GameplayEventType.ExpeditionDiscovered, null, Day, notice);
            Say(notice, 20f);
        }
    }

    void DrawExpeditionExit(ExpeditionExit exit)
    {
        GUILayout.Label("Caminhos e transportes descobertos por sobreviventes e mensagens.", new GUIStyle(GUI.skin.label) { wordWrap = true });
        GUILayout.Label("Você pode realizar apenas uma expedição pessoal por dia.", new GUIStyle(GUI.skin.label) { wordWrap = true });
        if (PersonalExpeditionUsedToday)
            GUILayout.Label(TooLateForExpedition, new GUIStyle(GUI.skin.label) { wordWrap = true, fontStyle = FontStyle.Bold });
        bool any = false;
        foreach (var destination in expeditionDestinations)
        {
            if (!IsDestinationDiscovered(destination)) continue;
            any = true;
            GUILayout.Label(destination.displayName, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label(destination.discoveryMessage, new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Label(destination.description, new GUIStyle(GUI.skin.label) { wordWrap = true });
            bool ready = Application.CanStreamedLevelBeLoaded(destination.sceneName);
            bool previous = GUI.enabled;
            GUI.enabled = previous && ready && IsExplorationMode && !PersonalExpeditionUsedToday && PlayerVitals.I != null && PlayerVitals.I.IsAlive;
            if (GUILayout.Button("Viajar: " + destination.displayName))
            {
                blockedAttackFrame = Time.frameCount;
                StartCoroutine(EnterExpedition(destination));
            }
            GUI.enabled = previous;
            if (!ready) GUILayout.Label("Cena indisponível. Confira a lista de cenas da build.");
        }
        if (!any) GUILayout.Label("Nenhuma rota descoberta. Novas notícias podem revelar caminhos após as próximas noites.");
        GUILayout.Label("Explore, colete com E e volte pelo ponto de extração para entregar a carga à vila.");
        GUILayout.Label("Armazém da vila:");
        foreach (var balance in wallet.Balances)
            if (balance.Key != null && balance.Value > 0) GUILayout.Label($"{balance.Key.displayName}: {balance.Value}");
    }

    IEnumerator EnterExpedition(ExpeditionData destination)
    {
        if (!IsExplorationMode || !IsDestinationDiscovered(destination) || PlayerVitals.I == null ||
            !PlayerVitals.I.IsAlive || !Application.CanStreamedLevelBeLoaded(destination.sceneName)) yield break;
        if (PersonalExpeditionUsedToday) { interactionMessage = TooLateForExpedition; yield break; }
        // Refuse an already loaded scene so ownership and unloading remain unambiguous.
        if (SceneManager.GetSceneByName(destination.sceneName).isLoaded) { Say("Esta região já está carregada."); yield break; }
        expeditionTransition = true;
        EmptyHand();
        var player = PlayerVitals.I.transform;
        villagePlayerPosition = player.position;
        villagePlayerRotation = player.rotation;
        villageGrid = grid;
        villageCameraZoom = Camera.main != null ? GetCameraZoom(Camera.main) : 30f;
        var load = SceneManager.LoadSceneAsync(destination.sceneName, LoadSceneMode.Additive);
        if (load == null) { expeditionTransition = false; Say("Não foi possível carregar a expedição."); yield break; }
        yield return load;
        expeditionScene = SceneManager.GetSceneByName(destination.sceneName);
        if (!expeditionScene.isLoaded) { expeditionTransition = false; Say("A cena de expedição não foi carregada."); yield break; }
        activeDestination = destination;
        var root = new GameObject("ExpeditionRuntime");
        SceneManager.MoveGameObjectToScene(root, expeditionScene);
        root.transform.position = new Vector3(10000f, villageGrid.transform.position.y, 10000f);
        grid = root.AddComponent<GridWorld>();
        int size = Mathf.Clamp(destination.testRegionSize, 11, 101);
        grid.Configure(size, size, 2f, 0);
        GridWorld.I = grid;
        var ground = Prim(PrimitiveType.Cube, root.transform.position - Vector3.up * 0.25f,
            new Vector3(size * 2f, 0.5f, size * 2f), destination.groundColor);
        ground.name = "ExpeditionTestGround";
        expeditionMaterials.Add(ground.GetComponent<Renderer>().sharedMaterial);
        ground.transform.SetParent(root.transform, true);
        extractionPosition = root.transform.position;
        var marker = Prim(PrimitiveType.Cylinder, extractionPosition + Vector3.right * 3f + Vector3.up * 0.3f,
            new Vector3(1f, 0.3f, 1f), Color.yellow);
        marker.name = "Extracao_Retorno";
        expeditionMaterials.Add(marker.GetComponent<Renderer>().sharedMaterial);
        marker.transform.SetParent(root.transform, true);
        extractionPosition = marker.transform.position;
        GenerateExpedition(root.transform, size);
        player.position = root.transform.position + Vector3.up;
        combatCameraMode = true;
        IsOnExpedition = true;
        // Consume the player's daily departure only after the trip is ready.
        lastPersonalExpeditionDay = Day;
        expeditionTransition = false;
        if (Camera.main != null) SetCameraZoom(Camera.main, Mathf.Clamp(destination.explorationCameraZoom, 5f, 60f));
        CenterCameraOn(player.position);
        RaiseGameplayEvent(GameplayEventType.ExpeditionDeparted, null, Day, "Viagem iniciada: " + destination.displayName);
    }

    void UpdateExpeditionInput(Keyboard keyboard, Mouse mouse)
    {
        CloseInteraction();
        UpdateResourceCollection(keyboard);
        if (keyboard.eKey.wasPressedThisFrame && NearExtraction()) StartCoroutine(ReturnFromExpedition());
    }

    bool NearExtraction()
    {
        if (PlayerVitals.I == null) return false;
        Vector3 delta = PlayerVitals.I.transform.position - extractionPosition;
        delta.y = 0f;
        return delta.sqrMagnitude <= interactionRange * interactionRange;
    }

    IEnumerator ReturnFromExpedition()
    {
        if (!IsOnExpedition || expeditionTransition || !NearExtraction()) yield break;
        bool delivered;
        try { delivered = expeditionCargo != null && expeditionCargo.TryDeliver(wallet); }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            delivered = expeditionCargo != null && expeditionCargo.Delivered;
        }
        if (!delivered) { collectionMessage = "Não foi possível entregar a carga. Confira o limite de recursos da vila."; yield break; }
        expeditionTransition = true;
        var player = PlayerVitals.I.transform;
        grid = villageGrid;
        GridWorld.I = villageGrid;
        player.SetPositionAndRotation(villagePlayerPosition, villagePlayerRotation);
        if (Camera.main != null) SetCameraZoom(Camera.main, villageCameraZoom);
        CenterCameraOn(player.position);
        ReleaseExpeditionMaterials();
        var unload = SceneManager.UnloadSceneAsync(expeditionScene);
        if (unload != null) yield return unload;
        IsOnExpedition = false;
        expeditionTransition = false;
        activeDestination = null;
        expeditionNodes.Clear(); nearbyResource = null; expeditionCargo = null;
        RaiseGameplayEvent(GameplayEventType.ExpeditionReturned, null, Day, "Retorno à vila. A manhã continua.");
        Say("Você voltou à vila e entregou a carga. O dia e o progresso foram preservados.", 8f);
    }

    void DrawExpeditionUI()
    {
        GUI.Box(new Rect(10f, 8f, Screen.width - 20f, 100f), expeditionTransition ? "Preparando viagem..." :
            $"Dia {Day} • Expedição: {(activeDestination != null ? activeDestination.displayName : "Região")}\n" +
            "A manhã da vila está suspensa • WASD: mover • R: reiniciar partida\n" +
            (lost ? "Jogador derrotado. [R] Reiniciar partida." : NearExtraction() ? "[E] Voltar à vila e entregar a carga" :
                nearbyResource != null ? $"[E] Coletar {nearbyResource.data.displayName} • Restante: {nearbyResource.Remaining}" : "Procure árvores, pedras, minérios e alimentos. Retorno: marcador amarelo."));
        if (!expeditionTransition && expeditionCargo != null)
        {
            GUILayout.BeginArea(new Rect(10f, 115f, 360f, Mathf.Min(230f, Screen.height - 125f)), GUI.skin.box);
            GUILayout.Label($"Carga: {expeditionCargo.Count}/{expeditionCargo.Capacity}");
            foreach (var item in expeditionCargo.Items) GUILayout.Label($"{item.Key.displayName}: {item.Value}");
            if (expeditionCargo.Count == 0) GUILayout.Label("Vazia");
            GUILayout.Label(collectionMessage, new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.EndArea();
        }
    }

    void ReleaseExpeditionMaterials()
    {
        foreach (var material in expeditionMaterials) if (material != null) Destroy(material);
        expeditionMaterials.Clear();
    }
}
