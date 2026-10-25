using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public partial class Game
{
    [Header("Orientação inicial")]
    public bool showInitialDayGuidance = true;
    ExpeditionManager expeditionManager;
    public bool IsOnExpedition { get; private set; }
    public bool ExpeditionTransition => expeditionTransition;
    bool expeditionTransition;
    int lastPersonalExpeditionDay = -1;
    public bool PersonalExpeditionUsedToday => !VillageProgressionRules.CanTakePersonalExpedition(Day, lastPersonalExpeditionDay);
    const string TooLateForExpedition = "Já está muito tarde. Você não pode sair em outra expedição hoje. Aguarde o próximo dia.";
    readonly HashSet<ExpeditionData> discoveredDestinations = new HashSet<ExpeditionData>();
    readonly List<ExpeditionExit> expeditionExits = new List<ExpeditionExit>();
    ExpeditionController expeditionController;
    ExpeditionData activeDestination;
    GridWorld villageGrid;
    Vector3 villagePlayerPosition;
    Quaternion villagePlayerRotation;
    Vector3 extractionPosition;
    Scene expeditionScene;

    void PrepareExpeditions()
    {
        foreach (var candidate in FindObjectsByType<ExpeditionManager>())
            if (candidate.gameObject.scene == gameObject.scene) expeditionManager = candidate;
        if (expeditionManager == null)
        {
            Debug.LogError("Cadastre as expedições em um ExpeditionManager na cena da vila.", this);
            return;
        }
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

    public bool IsDestinationDiscovered(ExpeditionData destination) => expeditionManager != null &&
        expeditionManager.Find(destination) != null &&
        (discoveredDestinations.Contains(destination) ||
         (expeditionManager.unlockExpeditionsOnFirstDay && Day == 1 && !night && !lost));

    void DiscoverExpeditions()
    {
        if (night || lost || expeditionManager == null) return;
        foreach (var destination in expeditionManager.Destinations)
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
        foreach (var destination in expeditionManager != null ? expeditionManager.Destinations : new ExpeditionData[0])
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
        expeditionController = null;
        int controllerCount = 0;
        foreach (var sceneRoot in expeditionScene.GetRootGameObjects())
            foreach (var candidate in sceneRoot.GetComponentsInChildren<ExpeditionController>(true))
                if (candidate.isActiveAndEnabled) { expeditionController = candidate; controllerCount++; }
        bool prepared = false;
        try
        {
            if (controllerCount != 1) throw new System.InvalidOperationException("A cena precisa de exatamente um ExpeditionController ativo.");
            expeditionController.Initialize(destination, litMaterial, expeditionVisit);
            prepared = true;
        }
        catch (System.Exception exception) { Debug.LogException(exception); }
        if (!prepared)
        {
            grid = villageGrid;
            GridWorld.I = villageGrid;
            var failedUnload = SceneManager.UnloadSceneAsync(expeditionScene);
            if (failedUnload != null) yield return failedUnload;
            expeditionController = null;
            activeDestination = null;
            expeditionTransition = false;
            Say("Não foi possível preparar a região. Confira o ExpeditionController da cena.");
            yield break;
        }
        expeditionVisit++;
        grid = expeditionController.Grid;
        GridWorld.I = grid;
        expeditionNodes.Clear();
        expeditionNodes.AddRange(expeditionController.Nodes);
        expeditionCargo = expeditionController.Cargo;
        nearbyResource = null;
        collectionMessage = "";
        extractionPosition = expeditionController.ExtractionPosition;
        player.SetPositionAndRotation(expeditionController.PlayerPosition, expeditionController.PlayerRotation);
        combatCameraMode = true;
        IsOnExpedition = true;
        // Consume the player's daily departure only after the trip is ready.
        lastPersonalExpeditionDay = Day;
        expeditionTransition = false;
        if (Camera.main != null) SetCameraZoom(Camera.main, expeditionController.CameraZoom);
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

    readonly List<ExpeditionCargo> deferredCargo = new List<ExpeditionCargo>();
    float nextDeferredCargoAttempt;

    bool TryDeliverCargo(ExpeditionCargo cargo)
    {
        if (cargo == null) return true;
        try { return cargo.Delivered || cargo.TryDeliver(wallet); }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            return cargo.Delivered;
        }
    }

    void FlushDeferredCargo()
    {
        if (deferredCargo.Count == 0 || expeditionTransition || Time.time < nextDeferredCargoAttempt) return;
        nextDeferredCargoAttempt = Time.time + 1f;
        for (int i = deferredCargo.Count - 1; i >= 0; i--)
            if (TryDeliverCargo(deferredCargo[i])) deferredCargo.RemoveAt(i);
    }

    IEnumerator ReturnFromExpedition(bool defeated = false)
    {
        if (!IsOnExpedition || expeditionTransition || (!defeated && !NearExtraction())) yield break;
        var vitals = PlayerVitals.I;
        if (vitals == null) yield break;
        expeditionTransition = true;
        int lostUnits = 0;
        if (defeated && expeditionCargo != null)
            lostUnits = expeditionCargo.ApplyDefeatLoss(expeditionManager != null ? expeditionManager.defeatCargoLossPercent : 50);
        bool delivered = TryDeliverCargo(expeditionCargo);
        if (!delivered && !defeated)
        {
            expeditionTransition = false;
            collectionMessage = "Não foi possível entregar a carga. Confira o limite de recursos da vila.";
            yield break;
        }
        // Failure to credit an overflowing wallet must never strand a defeated player.
        // Preserve the remainder and retry after returning; never apply the loss twice.
        if (!delivered && expeditionCargo != null) deferredCargo.Add(expeditionCargo);
        var player = vitals.transform;
        grid = villageGrid;
        GridWorld.I = villageGrid;
        player.SetPositionAndRotation(villagePlayerPosition, villagePlayerRotation);
        if (defeated) vitals.RecoverFromExpeditionDefeat(expeditionManager != null ? expeditionManager.defeatReturnHealthPercent : 50);
        if (Camera.main != null) SetCameraZoom(Camera.main, villageCameraZoom);
        CenterCameraOn(player.position);
        var unload = SceneManager.UnloadSceneAsync(expeditionScene);
        if (unload != null) yield return unload;
        IsOnExpedition = false;
        expeditionTransition = false;
        activeDestination = null;
        expeditionController = null;
        expeditionNodes.Clear(); nearbyResource = null; expeditionCargo = null;
        string notice = defeated
            ? $"Você foi derrotado na expedição e voltou à vila com {vitals.Health.current:0.#}/{vitals.Health.max:0.#} de vida. Perdeu {lostUnits} recursos da carga. " +
                (delivered ? "O restante foi entregue." : "O restante está aguardando espaço no saldo da vila.") + " A viagem do dia foi consumida."
            : "Você voltou à vila e entregou a carga. O dia e o progresso foram preservados.";
        RaiseGameplayEvent(GameplayEventType.ExpeditionReturned, null, Day, notice);
        Say(notice, 12f);
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

}
