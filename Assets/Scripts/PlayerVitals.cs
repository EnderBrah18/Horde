using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class PlayerVitals : MonoBehaviour
{
    public static PlayerVitals I { get; private set; }
    [Min(1f)] public float maxHealth = 100f;
    [Tooltip("Proteção após um acerto, para evitar dano simultâneo de toda a horda.")]
    [Min(0f)] public float damageInvulnerability = 0.5f;
    [Header("Retorno após morte na vila")]
    [Min(0f)] public float villageRespawnDelay = 10f;
    public Health Health { get; private set; }
    public bool IsAlive => Health != null && Health.current > 0f;
    float vulnerableAt;
    Vector3 villageOrigin;
    Quaternion villageOriginRotation;
    Renderer[] hiddenRenderers;
    bool[] rendererStates;
    Collider[] hiddenColliders;
    bool[] colliderStates;

    void Awake()
    {
        villageOrigin = transform.position;
        villageOriginRotation = transform.rotation;
        Health = GetComponent<Health>();
        // Keep the scene player object so an expedition defeat can return it to the village.
        Health.destroyOnDeath = false;
        Health.Setup(maxHealth);
    }
    void OnEnable() { I = this; Health.Died += OnDied; }
    void OnDisable()
    {
        if (I == this) I = null;
        if (Health != null) Health.Died -= OnDied;
    }
    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        damageInvulnerability = Mathf.Max(0f, damageInvulnerability);
        villageRespawnDelay = Mathf.Max(0f, villageRespawnDelay);
    }
    public void TakeDamage(float damage, GameObject source)
    {
        if (!IsAlive || damage <= 0f || Time.time < vulnerableAt ||
            (Game.I != null && (Game.I.lost || Game.I.ExpeditionTransition))) return;
        vulnerableAt = Time.time + damageInvulnerability;
        Health.Damage(damage, source);
    }
    void OnDied(Health health)
    {
        if (Game.I != null) Game.I.PlayerDied();
    }

    public void RestoreAtDawn()
    {
        if (!IsAlive) return;
        Health.RestoreFull();
        vulnerableAt = 0f;
    }

    public void RecoverFromExpeditionDefeat(int healthPercent)
    {
        Health.current = ExpeditionDefeatRules.ReturnHealth(Health.max,healthPercent);
        vulnerableAt = Time.time + damageInvulnerability;
    }

    public void HideUntilRespawn()
    {
        hiddenRenderers = GetComponentsInChildren<Renderer>(true);
        rendererStates = new bool[hiddenRenderers.Length];
        for (int i = 0; i < hiddenRenderers.Length; i++)
        { rendererStates[i] = hiddenRenderers[i].enabled; hiddenRenderers[i].enabled = false; }
        hiddenColliders = GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[hiddenColliders.Length];
        for (int i = 0; i < hiddenColliders.Length; i++)
        { colliderStates[i] = hiddenColliders[i].enabled; hiddenColliders[i].enabled = false; }
    }

    public void RespawnAtVillageOrigin()
    {
        transform.SetPositionAndRotation(villageOrigin, villageOriginRotation);
        Health.current = Health.max;
        vulnerableAt = Time.time + damageInvulnerability;
        if (hiddenRenderers != null)
            for (int i = 0; i < hiddenRenderers.Length; i++)
                if (hiddenRenderers[i] != null) hiddenRenderers[i].enabled = rendererStates[i];
        if (hiddenColliders != null)
            for (int i = 0; i < hiddenColliders.Length; i++)
                if (hiddenColliders[i] != null) hiddenColliders[i].enabled = colliderStates[i];
        hiddenRenderers = null; rendererStates = null;
        hiddenColliders = null; colliderStates = null;
    }
}
