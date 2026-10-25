using UnityEngine;
using UnityEngine.InputSystem;

// Shared interaction for construction selection and nearby player interaction.
public partial class Game
{
    [Header("Interação com a vila")]
    [Min(0.5f)] public float interactionRange = 5f;
    Component inspectedTarget;
    Component nearbyTarget;
    Component hoveredTarget;
    Vector2 interactionScroll;
    string interactionMessage = "";
    LineRenderer selectionOutline;
    Material selectionMaterial;
    int blockedAttackFrame = -1;

    public bool IsConstructionMode => !night && !combatCameraMode && !lost && !IsOnExpedition && !expeditionTransition;
    public bool IsExplorationMode => !night && combatCameraMode && !lost && !IsOnExpedition && !expeditionTransition;
    public bool IsHandEmpty => Current == null;
    public bool InteractionOpen => inspectedTarget != null;
    public bool BlocksPlayerAttack => InteractionOpen || blockedAttackFrame == Time.frameCount ||
        (Mouse.current != null && PointerOverGameplayUI(GuiMouse(Mouse.current)));

    Rect InteractionRect => new Rect(14f, 176f, Mathf.Min(365f, Screen.width - 28f), Mathf.Clamp(Screen.height - 405f, 180f, 420f));

    void EmptyHand()
    {
        selected = -1;
        CloseInteraction();
        if (cursor != null) cursor.SetActive(false);
        if (previewOutline != null) previewOutline.enabled = false;
    }

    void EquipBuilding(int index)
    {
        CloseInteraction();
        selected = selected == index ? -1 : index;
    }

    void CloseInteraction()
    {
        inspectedTarget = null;
        interactionMessage = "";
        interactionScroll = Vector2.zero;
        if (selectionOutline != null) selectionOutline.enabled = false;
    }

    void DrawConstructionBridge(Component target)
    {
        if (!IsExplorationMode)
        {
            GUILayout.Label("Gestão da vila disponível durante o dia, no modo construção.");
            return;
        }
        if (GUILayout.Button("Gerenciar construção", GUILayout.Height(30f)))
            ManageConstruction(target);
    }

    void ManageConstruction(Component target)
    {
        if (!IsExplorationMode || target == null) return;
        EmptyHand();
        combatCameraMode = false;
        inspectedTarget = target;
        nearbyTarget = hoveredTarget = null;
        blockedAttackFrame = Time.frameCount;
        CenterCameraOn(target.transform.position);
        UpdateSelectionOutline(target);
    }

    bool PointerOverGameplayUI(Vector2 pointer)
    {
        if (IsOnExpedition && (new Rect(10f, 8f, Screen.width - 20f, 100f).Contains(pointer) ||
            new Rect(10f, 115f, 360f, Mathf.Min(230f, Screen.height - 125f)).Contains(pointer))) return true;
        if (new Rect(0f, 0f, Screen.width, 76f).Contains(pointer)) return true;
        if (InteractionOpen && InteractionRect.Contains(pointer)) return true;
        if (IsConstructionMode && (MenuArea().Contains(pointer) || new Rect(0f, 76f, 530f, 96f).Contains(pointer))) return true;
        if (showEventLog)
        {
            float width = Mathf.Min(440f, Screen.width * 0.45f);
            if (new Rect(Screen.width - width - 10f, 108f, width, (recentEvents.Count + 1) * 20f + 12f).Contains(pointer)) return true;
        }
        return false;
    }

    void UpdateInteraction(Keyboard keyboard, Mouse mouse, bool overUI)
    {
        hoveredTarget = null;
        nearbyTarget = null;
        if (IsConstructionMode && IsHandEmpty)
        {
            if (!overUI) hoveredTarget = PickInteractionTarget(mouse);
            if (!overUI && mouse.leftButton.wasPressedThisFrame)
            {
                inspectedTarget = hoveredTarget;
                interactionScroll = Vector2.zero;
                interactionMessage = "";
            }
        }
        else if (PlayerControlEnabled && PlayerVitals.I != null)
        {
            Vector3 playerPosition = PlayerVitals.I.transform.position;
            float nearest = interactionRange;
            foreach (var structure in Structure.All)
            {
                if (structure == null || !structure.isActiveAndEnabled || (structure.facility == null && structure.data == null)) continue;
                float distance = InteractionDistance(structure, playerPosition);
                if (distance <= nearest) { nearest = distance; nearbyTarget = structure; }
            }
            foreach (var point in specialBuildPoints)
            {
                if (point == null || !point.isActiveAndEnabled) continue;
                float distance = InteractionDistance(point, playerPosition);
                if (distance < nearest) { nearest = distance; nearbyTarget = point; }
            }
            if (IsExplorationMode)
            {
                foreach (var exit in expeditionExits)
                {
                    if (exit == null || !exit.isActiveAndEnabled) continue;
                    float distance = InteractionDistance(exit, playerPosition);
                    if (distance < nearest) { nearest = distance; nearbyTarget = exit; }
                }
                var population = GetComponent<VillagePopulation>();
                if (population != null)
                    foreach (var npc in population.Members)
                    {
                        if (npc == null || !npc.isActiveAndEnabled || npc.IsDead) continue;
                        float distance = InteractionDistance(npc, playerPosition);
                        if (distance < nearest) { nearest = distance; nearbyTarget = npc; }
                    }
            }
            if (night && inspectedTarget is VillageNpc) CloseInteraction();
            if (inspectedTarget != null && InteractionDistance(inspectedTarget, playerPosition) > interactionRange)
                CloseInteraction();
            if (keyboard.eKey.wasPressedThisFrame)
            {
                blockedAttackFrame = Time.frameCount;
                if (InteractionOpen) CloseInteraction();
                else { inspectedTarget = nearbyTarget; interactionMessage = ""; interactionScroll = Vector2.zero; }
            }
        }
        if (mouse.rightButton.wasPressedThisFrame) CloseInteraction();
        UpdateSelectionOutline(inspectedTarget != null ? inspectedTarget : hoveredTarget != null ? hoveredTarget : nearbyTarget);
    }

    Component PickInteractionTarget(Mouse mouse)
    {
        var camera = Camera.main;
        if (camera == null) return null;
        Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
        float nearest = float.PositiveInfinity;
        Component target = null;
        // Renderer bounds remain selectable when colliders are disabled or the
        // structure is a passable ruin and therefore absent from the grid.
        foreach (var structure in Structure.All)
        {
            if (structure == null || !structure.isActiveAndEnabled || !TryGetStructureBounds(structure.gameObject, out Bounds bounds)) continue;
            if (bounds.IntersectRay(ray, out float distance) && distance < nearest)
            { nearest = distance; target = structure; }
        }
        var ground = new Plane(Vector3.up, grid.transform.position);
        if (ground.Raycast(ray, out float groundDistance) && groundDistance < nearest)
        {
            Vector3 position = ray.GetPoint(groundDistance);
            float best = float.PositiveInfinity;
            foreach (var point in specialBuildPoints)
            {
                if (point == null || !point.isActiveAndEnabled) continue;
                float distance = InteractionDistance(point, position);
                if (distance <= point.activationRadius && distance < best) { best = distance; target = point; }
            }
        }
        return target;
    }

    float InteractionDistance(Component target, Vector3 position)
    {
        if (!(target is SpecialBuildPoint) && TryGetStructureBounds(target.gameObject, out Bounds bounds))
        {
            Vector3 sample = position; sample.y = bounds.center.y;
            Vector3 closest = bounds.ClosestPoint(sample);
            closest.y = position.y;
            return Vector3.Distance(position, closest);
        }
        Vector3 delta = target.transform.position - position;
        delta.y = 0f;
        return delta.magnitude;
    }

    void UpdateSelectionOutline(Component target)
    {
        if (target == null) { if (selectionOutline != null) selectionOutline.enabled = false; return; }
        if (selectionOutline == null)
        {
            var visual = new GameObject("InteractionSelection");
            visual.transform.SetParent(transform, false);
            selectionOutline = visual.AddComponent<LineRenderer>();
            selectionMaterial = new Material(Shader.Find("Sprites/Default"));
            selectionOutline.sharedMaterial = selectionMaterial;
            selectionOutline.useWorldSpace = true;
            selectionOutline.loop = true;
            selectionOutline.positionCount = 4;
            selectionOutline.startWidth = selectionOutline.endWidth = 0.12f;
            selectionOutline.startColor = selectionOutline.endColor = Color.cyan;
        }
        if (!TryGetStructureBounds(target.gameObject, out Bounds bounds))
            bounds = new Bounds(target.transform.position, new Vector3(5f, 0f, 5f));
        float y = grid.transform.position.y + 0.22f;
        selectionOutline.SetPosition(0, new Vector3(bounds.min.x, y, bounds.min.z));
        selectionOutline.SetPosition(1, new Vector3(bounds.min.x, y, bounds.max.z));
        selectionOutline.SetPosition(2, new Vector3(bounds.max.x, y, bounds.max.z));
        selectionOutline.SetPosition(3, new Vector3(bounds.max.x, y, bounds.min.z));
        selectionOutline.enabled = true;
    }

    static string ResourceText(ResourceAmount[] entries)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (entries != null)
            foreach (var entry in entries)
                if (entry.resource != null && entry.amount > 0) parts.Add($"{entry.amount} {entry.resource.displayName}");
        return parts.Count == 0 ? "Grátis" : string.Join(" + ", parts);
    }

    void DrawInteractionUI()
    {
        if (lost) return;
        if (IsConstructionMode && GUI.Button(new Rect(14f, Screen.height - 166f, 185f, 26f),
            IsHandEmpty ? "Mão vazia [0] • selecionada" : "Mão vazia [0 / Esc]")) EmptyHand();
        if (PlayerControlEnabled && nearbyTarget != null && !InteractionOpen)
            GUI.Box(new Rect(14f, Screen.height - 100f, 380f, 30f), "[E] Interagir: " + TargetName(nearbyTarget));
        if (!InteractionOpen) return;
        GUILayout.BeginArea(InteractionRect, GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label(TargetName(inspectedTarget), new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true });
        if (GUILayout.Button("X", GUILayout.Width(28f))) { blockedAttackFrame = Time.frameCount; CloseInteraction(); }
        GUILayout.EndHorizontal();
        interactionScroll = GUILayout.BeginScrollView(interactionScroll);
        if (inspectedTarget != null)
        {
            var facility = inspectedTarget.GetComponent<VillageFacility>();
            var point = inspectedTarget as SpecialBuildPoint;
            if (inspectedTarget is ExpeditionExit exit) DrawExpeditionExit(exit);
            else if (inspectedTarget is VillageNpc npc) DrawNpcActions(npc);
            else if (facility != null && facility.data != null) DrawFacilityActions(facility);
            else if (point != null) DrawPointActions(point);
            else DrawStructureActions(inspectedTarget.GetComponent<Structure>());
            if (!string.IsNullOrEmpty(interactionMessage)) GUILayout.Label(interactionMessage, new GUIStyle(GUI.skin.label) { wordWrap = true });
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    string TargetName(Component target)
    {
        if (target is ExpeditionExit) return "Rotas de expedição";
        if (target is VillageNpc npc && npc.data != null) return npc.data.displayName;
        var facility = target.GetComponent<VillageFacility>();
        return facility != null && facility.data != null ? facility.data.displayName : target.name;
    }

    void DrawFacilityActions(VillageFacility facility)
    {
        GUILayout.Label(facility.data.description, new GUIStyle(GUI.skin.label) { wordWrap = true });
        GUILayout.Label(!facility.IsUnlocked ? "Abandonada" : facility.IsBroken ? $"Quebrada • nível {facility.Level}" : $"Ativa • nível {facility.Level}");
        var health = facility.GetComponent<Health>();
        if (health != null && facility.IsUnlocked) GUILayout.Label($"Vida: {health.current:0}/{health.max:0}");
        if (!IsConstructionMode)
        {
            if (IsExplorationMode)
            {
                if (!facility.IsOperational)
                    GUILayout.Label("Restaure ou repare esta instalação no modo construção para utilizá-la.");
                else if (facility.data.kind == VillageFacilityKind.Service)
                    GUILayout.Label("Instalação disponível. Recrute e atribua um especialista para preparar seus serviços.");
                else
                    GUILayout.Label("A gestão desta construção é feita no modo construção.");
            }
            DrawConstructionBridge(facility);
        }
        else if (!facility.IsUnlocked && facility.data.levels != null && facility.data.levels.Length > 0 && facility.data.levels[0] != null)
            FacilityButton("Restaurar / desbloquear", facility.data.levels[0].costs, () => facility.TryUnlock(wallet));
        else if (facility.IsBroken && facility.CurrentLevel != null)
            FacilityButton("Reparar", facility.CurrentLevel.repairCosts, () => facility.TryRepair(wallet));
        else if (facility.CanUpgrade && facility.data.levels[facility.Level] != null)
            FacilityButton("Melhorar para nível " + (facility.Level + 1), facility.data.levels[facility.Level].costs, () => facility.TryUpgrade(wallet));
        else if (facility.IsOperational) GUILayout.Label("Nível máximo");
        if (facility.CurrentLevel != null)
        {
            if (facility.CurrentLevel.productionPerNight != null && facility.CurrentLevel.productionPerNight.Length > 0)
            {
                int workers = facility.WorkerCount;
                if (facility.data.kind == VillageFacilityKind.Production)
                {
                    var level = facility.CurrentLevel;
                    GUILayout.Label($"Trabalhadores aptos: {workers} • mínimo: {level.minimumWorkers} • máximo: {level.maximumWorkers}");
                    var population = GetComponent<VillagePopulation>();
                    if (population != null) GUILayout.Label("Vagas de trabalho ocupadas: " + population.CountAssignedWorkers(facility));
                    GUILayout.Label($"A cada {level.extraWorkersPerBonus} extras: +{level.productionBonusPerGroup} de cada recurso.");
                }
                if (facility.TryGetProduction(workers, out var outputs))
                {
                    var amounts = new System.Collections.Generic.List<ResourceAmount>();
                    foreach (var output in outputs) amounts.Add(new ResourceAmount(output.Key, output.Value));
                    GUILayout.Label("Previsão por noite: " + ResourceText(amounts.ToArray()));
                }
                else GUILayout.Label("Produção suspensa: instalação inativa ou trabalhadores insuficientes.");
            }
            if (facility.data.kind == VillageFacilityKind.Housing) GUILayout.Label("Vagas de população: " + facility.PopulationCapacity);
            if (facility.data.kind == VillageFacilityKind.EssentialHousing) GUILayout.Label("Vagas para NPCs essenciais: " + facility.EssentialNpcCapacity);
        }
        DrawFacilityPopulation(facility);
    }

    void FacilityButton(string label, ResourceAmount[] costs, System.Func<bool> action)
    {
        GUILayout.Label("Custo: " + ResourceText(costs), new GUIStyle(GUI.skin.label) { wordWrap = true });
        bool previous = GUI.enabled;
        bool affordable = wallet.CanAfford(costs);
        GUI.enabled = previous && affordable && IsConstructionMode;
        if (GUILayout.Button(label, GUILayout.Height(30f)))
        {
            blockedAttackFrame = Time.frameCount;
            interactionMessage = IsConstructionMode && action() ? "Concluído." : "Ação indisponível neste estado.";
        }
        GUI.enabled = previous;
        if (!affordable) GUILayout.Label("Recursos insuficientes.");
    }

    void DrawPointActions(SpecialBuildPoint point)
    {
        if (point.occupant != null) { DrawStructureActions(point.occupant); return; }
        if (!IsConstructionMode) { DrawConstructionBridge(point); return; }
        bool found = false;
        foreach (var building in menu)
        {
            if (!building.requiresSpecialBuildPoint || building.specialBuildPointId != point.requiredBuildingId) continue;
            found = true;
            bool previous = GUI.enabled;
            bool placeable = CanPlace(building, point.transform.position, out var matched) && matched == point;
            GUI.enabled = previous && placeable;
            FacilityButton("Construir " + building.displayName, building.costs, () => BuildAtPoint(point, building));
            GUI.enabled = previous;
            if (!placeable) GUILayout.Label("Amplie a área de construção ou libere o espaço deste ponto.");
        }
        if (!found) GUILayout.Label("Nenhuma construção compatível configurada para este ponto.");
    }

    bool BuildAtPoint(SpecialBuildPoint point, BuildingData building)
    {
        if (!IsConstructionMode || point.occupied || !CanPlace(building, point.transform.position, out var matched) || matched != point) return false;
        if (!wallet.Spend(building.costs)) return false;
        point.occupant = MakeStructureAt(building, point.transform.position, Quaternion.Euler(0f, buildRotation, 0f), false);
        point.occupied = true;
        grid.Recalc();
        return true;
    }

    void DrawStructureActions(Structure structure)
    {
        if (structure == null) return;
        if (structure.health != null) GUILayout.Label($"Vida: {structure.health.current:0}/{structure.health.max:0}");
        if (structure.data == null) { GUILayout.Label(structure.indestructible ? "Obstáculo permanente." : "Sem ações disponíveis."); return; }
        if (!IsConstructionMode) { DrawConstructionBridge(structure); return; }
        if (structure.data.upgrade != null)
            FacilityButton("Melhorar: " + structure.data.upgrade.displayName, structure.data.upgrade.costs, () => UpgradePlaced(structure));
        float refundRate = structure.RefundRate(round);
        GUILayout.Label(refundRate == 1f ? "Reembolso integral até o início da próxima noite." : "Esta construção já passou por uma noite: reembolso de 70%.");
        if (GUILayout.Button($"Vender • recuperar {refundRate * 100f:0}% dos recursos") && IsConstructionMode)
        {
            blockedAttackFrame = Time.frameCount;
            if (!structure.TryGetRefund(round, out var refund) || !wallet.TryDeposit(refund))
            { interactionMessage = "Não foi possível reembolsar os recursos."; return; }
            structure.blocksMovement = false;
            Destroy(structure.gameObject);
            CloseInteraction();
        }
    }

    bool UpgradePlaced(Structure structure)
    {
        var next = structure.data.upgrade;
        if (!IsConstructionMode || next == null) return false;
        Vector3 position = sceneDrivenLayout ? structure.transform.position : grid.CellToWorld(structure.cell);
        if (sceneDrivenLayout && TryGetStructureBounds(structure.gameObject, out Bounds bounds)) position.y = bounds.min.y;
        float previousRotation = buildRotation;
        buildRotation = structure.transform.eulerAngles.y;
        bool placeable = CanPlace(next, position, out _, structure);
        buildRotation = previousRotation;
        if (!placeable || !BuildingInvestment.TryCombine(structure.Investment, next.costs, out var investment) ||
            !wallet.Spend(next.costs)) return false;
        var replacement = MakeStructureAt(next, position, structure.transform.rotation, !sceneDrivenLayout);
        replacement.placementRound = structure.placementRound;
        replacement.investedCosts = investment;
        foreach (var point in specialBuildPoints)
            if (point != null && point.occupant == structure) point.occupant = replacement;
        structure.blocksMovement = false;
        Destroy(structure.gameObject);
        inspectedTarget = replacement;
        grid.Recalc();
        return true;
    }

    void OnDestroy()
    {
        ReleaseExpeditionMaterials();
        if (selectionMaterial != null) Destroy(selectionMaterial);
    }
}
