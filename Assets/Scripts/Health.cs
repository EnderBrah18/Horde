using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float max = 100f;
    public float current = 100f;
    public event Action<Health, float> Damaged;
    public event Action<Health> Died;

    public void Setup(float m) { max = current = m; }

    public void Damage(float d)
    {
        if (current <= 0f) return;
        current -= d;
        Damaged?.Invoke(this, d);
        if (current <= 0f)
        {
            Died?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
