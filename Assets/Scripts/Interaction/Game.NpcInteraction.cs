using UnityEngine;

public partial class Game
{
    void DrawFacilityPopulation(VillageFacility facility)
    {
        var population = GetComponent<VillagePopulation>();
        if (population == null) return;
        bool housing = facility.data.kind == VillageFacilityKind.Housing || facility.data.kind == VillageFacilityKind.EssentialHousing;
        if (housing)
        {
            GUILayout.Label("Moradores vinculados: " + population.Occupants(facility));
            foreach (var npc in population.Members)
            {
                if (npc == null || npc.Residence != facility) continue;
                GUILayout.Label(npc.data.displayName + " • " + NpcStatus(npc));
            }
            if (!IsExplorationMode || !facility.IsOperational) return;
            GUILayout.Label("Recrutamento");
            foreach (var candidate in population.recruitmentCatalog)
            {
                if (candidate == null || (candidate.category == NpcCategory.Essential) !=
                    (facility.data.kind == VillageFacilityKind.EssentialHousing)) continue;
                GUILayout.Label(candidate.displayName + " • " + ResourceText(candidate.recruitmentCosts));
                bool available = population.CanRecruit(candidate, facility, out string reason);
                bool previous = GUI.enabled;
                GUI.enabled = previous && available;
                if (GUILayout.Button("Recrutar " + candidate.displayName))
                {
                    blockedAttackFrame = Time.frameCount;
                    population.TryRecruit(candidate, facility, out interactionMessage);
                }
                GUI.enabled = previous;
                if (!available) GUILayout.Label(reason);
            }
        }
        else
        {
            foreach (var npc in population.Members)
                if (npc != null && npc.Workplace == facility)
                    GUILayout.Label(npc.data.displayName + " • " + npc.Role.displayName + " • " + NpcStatus(npc));
            if (facility.data.kind == VillageFacilityKind.Service)
                GUILayout.Label(population.HasServiceSpecialist(facility) ? "Especialista disponível. Serviço ainda em desenvolvimento." : "Nenhum especialista disponível nesta instalação.");
        }
    }

    static string NpcStatus(VillageNpc npc)
    {
        if (npc.IsInjured) return "Ferido(a)";
        if (npc.IsHomeless) return "Desabrigado(a)";
        switch (npc.State)
        {
            case VillageNpcState.GoingToWork: return "Indo trabalhar";
            case VillageNpcState.Working: return "Trabalhando";
            case VillageNpcState.GoingHome: return "Indo para casa";
            case VillageNpcState.Sheltered: return "Abrigado(a)";
            case VillageNpcState.NoRoute: return "Sem caminho disponível";
            default: return npc.Role != null && !npc.CanWork ? "Trabalho suspenso" : "Livre";
        }
    }

    void DrawNpcActions(VillageNpc npc)
    {
        if (npc.data == null) return;
        GUILayout.Label(npc.data.description, new GUIStyle(GUI.skin.label) { wordWrap = true });
        GUILayout.Label("Estado: " + NpcStatus(npc));
        GUILayout.Label("Moradia: " + (npc.Residence != null ? npc.Residence.data.displayName : "Sem residência"));
        GUILayout.Label("Função: " + (npc.Role != null ? npc.Role.displayName : "Sem função"));
        GUILayout.Label("Trabalho: " + (npc.Workplace != null ? npc.Workplace.data.displayName : "Não atribuído"));
        var health = npc.GetComponent<Health>();
        if (health != null) GUILayout.Label($"Vida: {health.current:0}/{health.max:0}");
        var population = GetComponent<VillagePopulation>();
        if (!IsExplorationMode || population == null) return;
        if (npc.IsInjured)
        {
            GUILayout.Label("Recuperação: " + ResourceText(npc.data.recoveryCosts));
            if (GUILayout.Button("Tratar ferimentos"))
            {
                blockedAttackFrame = Time.frameCount;
                interactionMessage = population.TryRecover(npc) ? "Recuperado(a)." : "É necessária uma moradia ativa e recursos suficientes.";
            }
            return;
        }
        if (npc.Role != null && GUILayout.Button("Remover atribuição"))
        {
            blockedAttackFrame = Time.frameCount;
            population.TryClearAssignment(npc);
        }
        if (npc.data.allowedRoles == null) return;
        var facilities = FindObjectsByType<VillageFacility>();
        foreach (var role in npc.data.allowedRoles)
        {
            if (role == null) continue;
            foreach (var facility in facilities)
            {
                if (!role.Accepts(facility)) continue;
                if (GUILayout.Button(role.displayName + " → " + facility.data.displayName + " (" + facility.name + ")"))
                {
                    blockedAttackFrame = Time.frameCount;
                    interactionMessage = population.TryAssign(npc, role, facility) ? "Trabalho atribuído." : "Atribuição indisponível ou sem vaga de trabalho.";
                }
            }
        }
    }
}
