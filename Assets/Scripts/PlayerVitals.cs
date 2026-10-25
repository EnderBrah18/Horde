using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class PlayerVitals : MonoBehaviour
{
    public static PlayerVitals I { get; private set; }
    [Min(1f)] public float maxHealth = 100f;
    [Tooltip("Proteção após um acerto, para evitar dano simultâneo de toda a horda.")]
    [Min(0f)] public float damageInvulnerability = 0.5f;
    public Health Health { get; private set; }
    public bool IsAlive => Health != null && Health.current > 0f;
    float vulnerableAt;

    void Awake()
    {
        Health = GetComponent<Health>();
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
    }
    public void TakeDamage(float damage, GameObject source)
    {
        if (!IsAlive || damage <= 0f || Time.time < vulnerableAt || Game.I == null || Game.I.lost) return;
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
}
