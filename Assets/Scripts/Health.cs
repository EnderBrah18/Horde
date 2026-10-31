using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float max = 100f;
    public float current = 100f;
    public bool destroyOnDeath = true;
    public bool invulnerable;
    public event Action<Health, float> Damaged;
    public event Action<Health, float, GameObject> DamagedBy;
    public event Action<Health> Died;

    public void Setup(float m) { max = current = m; }

    public bool RestoreFull()
    {
        if (current <= 0f || max <= 0f) return false;
        current = max;
        return true;
    }

    public void Damage(float d, GameObject source = null)
    {
        if (invulnerable || current <= 0f || d <= 0f) return;
        current = Mathf.Max(0f, current - d);
        Damaged?.Invoke(this, d);
        DamagedBy?.Invoke(this, d, source);
        if (current <= 0f)
        {
            Died?.Invoke(this);
            if (destroyOnDeath) Destroy(gameObject);
        }
    }
}
