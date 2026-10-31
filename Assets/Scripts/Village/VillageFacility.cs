using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VillageFacility : MonoBehaviour
{
    public VillageFacilityData data;
    public bool startUnlocked;
    [Min(1)] public int startingLevel = 1;
    [SerializeField, Tooltip("0 = bloqueada. Alterado pelas operações de desbloqueio e melhoria.")]
    int currentLevel;
    [SerializeField] int lastProductionRound;
    [SerializeField] bool broken;
    [Header("Visual de ruína provisório")]
    [Range(0.05f, 1f)] public float ruinHeight = 0.2f;
    public Color ruinColor = new Color(0.32f, 0.30f, 0.27f, 1f);
    bool initialized;
    Health boundHealth;
    Structure structure;
    Vector3 intactScale;
    float intactY, groundY;
    Renderer mainRenderer;
    MaterialPropertyBlock intactProperties;
    MaterialPropertyBlock ruinProperties;

    public int Level => currentLevel;
    public bool IsUnlocked => currentLevel > 0;
    public bool IsBroken => broken;
    public int NpcCapacity => IsOperational ? CurrentLevel.npcCapacity : 0;
    public int PopulationCapacity => data != null && data.kind == VillageFacilityKind.Housing ? NpcCapacity : 0;
    public int EssentialNpcCapacity => data != null && data.kind == VillageFacilityKind.EssentialHousing ? NpcCapacity : 0;
    public bool IsOperational
    {
        get
        {
            var health = GetComponent<Health>();
            return isActiveAndEnabled && !broken && data != null && IsUnlocked && CurrentLevel != null &&
                (health == null || health.current > 0f);
        }
    }
    public VillageFacilityLevel CurrentLevel => data != null && data.levels != null &&
        currentLevel > 0 && currentLevel <= data.levels.Length ? data.levels[currentLevel - 1] : null;
    public bool CanUpgrade => IsOperational && currentLevel < data.levels.Length;

    void Awake()
    {
        InitializeState();
    }

    void InitializeState()
    {
        if (initialized) return;
        initialized = true;
        currentLevel = startUnlocked && data != null && data.levels != null && data.levels.Length > 0
            ? Mathf.Clamp(startingLevel, 1, data.levels.Length) : 0;
        lastProductionRound = 0;
        broken = false;
        intactScale = transform.localScale;
        intactY = transform.position.y;
        mainRenderer = GetComponent<Renderer>();
        groundY = mainRenderer != null ? mainRenderer.bounds.min.y : intactY;
        intactProperties = new MaterialPropertyBlock();
        ruinProperties = new MaterialPropertyBlock();
        if (mainRenderer != null) mainRenderer.GetPropertyBlock(intactProperties);
    }

    public void BindStructure(Structure target)
    {
        InitializeState();
        structure = target;
        structure.facility = this;
        if (boundHealth != null) boundHealth.Died -= OnBroken;
        boundHealth = target.health;
        boundHealth.destroyOnDeath = false;
        boundHealth.Died += OnBroken;
        RefreshState();
    }

    void OnBroken(Health health)
    {
        broken = true;
        RefreshState();
        Publish(GameplayEventType.FacilityBroken, "quebrada; necessita reparo");
    }

    void RefreshState()
    {
        bool active = IsUnlocked && !broken;
        if (boundHealth != null) boundHealth.invulnerable = !active;
        transform.localScale = new Vector3(intactScale.x, intactScale.y * (active ? 1f : ruinHeight), intactScale.z);
        Vector3 position = transform.position;
        position.y = active ? intactY : groundY + (intactY - groundY) * ruinHeight;
        transform.position = position;
        if (mainRenderer != null)
        {
            if (active) mainRenderer.SetPropertyBlock(intactProperties);
            else
            {
                mainRenderer.GetPropertyBlock(ruinProperties);
                ruinProperties.SetColor("_BaseColor", ruinColor);
                ruinProperties.SetColor("_Color", ruinColor);
                mainRenderer.SetPropertyBlock(ruinProperties);
            }
        }
        if (structure != null) structure.RefreshNavigation();
    }

    public bool TryRepair(Wallet wallet)
    {
        if (!isActiveAndEnabled || !broken || CurrentLevel == null || wallet == null || boundHealth == null) return false;
        if (!wallet.Spend(CurrentLevel.repairCosts)) return false;
        broken = false;
        boundHealth.Setup(data.maxHealth);
        RefreshState();
        Publish(GameplayEventType.FacilityRepaired, "reparada");
        return true;
    }

    public bool TryUnlock(Wallet wallet)
    {
        var health = GetComponent<Health>();
        if (!isActiveAndEnabled || IsUnlocked || data == null || data.levels == null || data.levels.Length == 0 ||
            data.levels[0] == null || wallet == null || (health != null && health.current <= 0f)) return false;
        if (!wallet.Spend(data.levels[0].costs)) return false;
        currentLevel = 1;
        if (boundHealth != null) boundHealth.Setup(data.maxHealth);
        RefreshState();
        Publish(GameplayEventType.FacilityUnlocked, "desbloqueada");
        return true;
    }

    public bool TryUpgrade(Wallet wallet)
    {
        if (!CanUpgrade || wallet == null || data.levels[currentLevel] == null) return false;
        if (!wallet.Spend(data.levels[currentLevel].costs)) return false;
        currentLevel++;
        Publish(GameplayEventType.FacilityUpgraded, "melhorada");
        return true;
    }

    // Chamado pelo VillageProduction ao amanhecer. Cada objeto paga no máximo
    // uma vez por rodada, inclusive se o evento for repetido ou houver dois gestores.
    public int WorkerCount => Game.I != null && Game.I.GetComponent<VillagePopulation>() != null
        ? Game.I.GetComponent<VillagePopulation>().CountWorkers(this) : 0;

    public bool TryGetProduction(int workers, out Dictionary<ResourceData, int> total)
    {
        total = new Dictionary<ResourceData, int>();
        if (!IsOperational) return false;
        var level = CurrentLevel;
        bool staffed = data.kind == VillageFacilityKind.Production;
        if (staffed && workers < Mathf.Max(1, level.minimumWorkers)) return false;
        long bonus = staffed ? (long)(Mathf.Min(workers, Mathf.Max(level.minimumWorkers, level.maximumWorkers)) -
            Mathf.Max(1, level.minimumWorkers)) / Mathf.Max(1, level.extraWorkersPerBonus) * Mathf.Max(0, level.productionBonusPerGroup) : 0;
        if (level.productionPerNight != null)
            foreach (var output in level.productionPerNight)
            {
                if (output.resource == null || output.amount < 0) return false;
                total.TryGetValue(output.resource, out int amount);
                if (output.amount > int.MaxValue - amount) return false;
                total[output.resource] = amount + output.amount;
            }
        // Apply the bonus once per distinct output, never to zero-output entries.
        foreach (var resource in new List<ResourceData>(total.Keys))
        {
            if (total[resource] <= 0) continue;
            if (bonus > int.MaxValue - total[resource]) return false;
            total[resource] += (int)bonus;
        }
        return true;
    }

    public bool TryProduce(int completedRound, Wallet wallet)
    {
        if (!IsOperational || wallet == null || completedRound <= 0 || completedRound <= lastProductionRound) return false;
        if (!TryGetProduction(WorkerCount, out var total)) return false;
        foreach (var output in total)
            if (output.Value > int.MaxValue - wallet.Get(output.Key)) return false;
        lastProductionRound = completedRound;
        bool produced = false;
        foreach (var output in total)
        {
            wallet.Add(output.Key, output.Value);
            produced |= output.Value > 0;
        }
        return produced;
    }

    void Publish(GameplayEventType type, string action)
    {
        if (Game.I != null)
            Game.I.RaiseGameplayEvent(type, gameObject, currentLevel, $"{data.displayName} {action} (nível {currentLevel})");
    }

    [ContextMenu("Desbloquear (pagar custo em Play Mode)")]
    void UnlockFromInspector()
    {
        if (Application.isPlaying && Game.I != null && Game.I.IsConstructionMode)
            Debug.Log(TryUnlock(Game.I.wallet) ? "Instalação desbloqueada." : "Não foi possível desbloquear: verifique dados, estado e recursos.", this);
    }

    [ContextMenu("Melhorar (pagar custo em Play Mode)")]
    void UpgradeFromInspector()
    {
        if (Application.isPlaying && Game.I != null && Game.I.IsConstructionMode)
            Debug.Log(TryUpgrade(Game.I.wallet) ? "Instalação melhorada." : "Não foi possível melhorar: verifique nível e recursos.", this);
    }

    [ContextMenu("Reparar (pagar custo em Play Mode)")]
    void RepairFromInspector()
    {
        if (Application.isPlaying && Game.I != null && Game.I.IsConstructionMode)
            Debug.Log(TryRepair(Game.I.wallet) ? "Instalação reparada." : "Não foi possível reparar.", this);
    }

    void OnDestroy()
    {
        if (boundHealth != null) boundHealth.Died -= OnBroken;
    }

    void OnEnable()
    {
        if (initialized && structure != null) RefreshState();
    }

    void OnDisable()
    {
        if (boundHealth != null) boundHealth.invulnerable = true;
        if (structure != null) structure.RefreshNavigation();
    }
}
