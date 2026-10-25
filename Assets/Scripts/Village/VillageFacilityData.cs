using System;
using UnityEngine;

public enum VillageFacilityKind { Production = 0, Service = 1, Housing = 2, EssentialHousing = 3 }

[Serializable]
public sealed class VillageFacilityLevel
{
    public string displayName = "Nível 1";
    [Tooltip("No primeiro nível, custo de desbloqueio. Nos demais, custo de melhoria.")]
    public ResourceAmount[] costs = new ResourceAmount[0];
    public ResourceAmount[] productionPerNight = new ResourceAmount[0];
    [Header("Trabalhadores (instalações de produção)")]
    [Min(1)] public int minimumWorkers = 2;
    [Min(1)] public int maximumWorkers = 8;
    [Tooltip("Trabalhadores extras além do mínimo necessários para cada bônus.")]
    [Min(1)] public int extraWorkersPerBonus = 2;
    [Tooltip("Unidades adicionais de cada recurso produzido, por grupo extra completo.")]
    [Min(0)] public int productionBonusPerGroup = 1;
    public ResourceAmount[] repairCosts = new ResourceAmount[0];
    [Tooltip("Vagas deste nível: população comum em Housing; NPCs nomeados em Essential Housing.")]
    [Min(0)] public int npcCapacity;
}

[CreateAssetMenu(menuName = "Village/Facility", fileName = "NewVillageFacility")]
public sealed class VillageFacilityData : ScriptableObject
{
    public string displayName = "Instalação";
    [TextArea] public string description;
    public Sprite icon;
    public VillageFacilityKind kind;
    [Min(1f)] public float maxHealth = 120f;
    [Tooltip("O primeiro elemento desbloqueia a instalação. Adicione elementos para criar melhorias.")]
    public VillageFacilityLevel[] levels = { new VillageFacilityLevel() };

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        if (levels == null) return;
        foreach (var level in levels)
        {
            if (level == null) continue;
            ClampAmounts(level.costs);
            ClampAmounts(level.productionPerNight);
            ClampAmounts(level.repairCosts);
            level.npcCapacity = Mathf.Max(0, level.npcCapacity);
            level.minimumWorkers = Mathf.Max(1, level.minimumWorkers);
            level.maximumWorkers = Mathf.Max(level.minimumWorkers, level.maximumWorkers);
            level.extraWorkersPerBonus = Mathf.Max(1, level.extraWorkersPerBonus);
            level.productionBonusPerGroup = Mathf.Max(0, level.productionBonusPerGroup);
        }
    }

    static void ClampAmounts(ResourceAmount[] entries)
    {
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++) entries[i].amount = Mathf.Max(0, entries[i].amount);
    }
}
