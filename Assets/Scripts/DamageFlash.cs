using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class DamageFlash : MonoBehaviour
{
    public Color flashColor = Color.white;
    [Min(0.01f)] public float duration = 0.15f;
    Health health;
    MeshRenderer[] renderers;
    MaterialPropertyBlock[] original;
    MaterialPropertyBlock flash;
    float remaining;
    bool flashing;
    int BaseColor;
    int ColorId;

    void Awake()
    {
        flash = new MaterialPropertyBlock();
        BaseColor = Shader.PropertyToID("_BaseColor");
        ColorId = Shader.PropertyToID("_Color");
        health = GetComponent<Health>();
        renderers = GetComponentsInChildren<MeshRenderer>();
        original = new MaterialPropertyBlock[renderers.Length];
        for (int i = 0; i < original.Length; i++) original[i] = new MaterialPropertyBlock();
    }
    void OnEnable() { health.Damaged += OnDamaged; }
    void OnDamaged(Health source, float amount)
    {
        if (amount <= 0f) return;
        if (!flashing)
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].GetPropertyBlock(original[i]);
        flashing = true;
        remaining = Mathf.Max(0.01f, duration);
        ApplyFlash();
    }
    void LateUpdate()
    {
        if (!flashing) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f) Restore();
        else ApplyFlash();
    }
    void ApplyFlash()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || renderer.sharedMaterial == null) continue;
            renderer.GetPropertyBlock(flash);
            var material = renderer.sharedMaterial;
            Color normal = material.HasProperty(BaseColor) ? material.GetColor(BaseColor) : material.color;
            Color color = UnityEngine.Color.Lerp(normal, flashColor, Mathf.Clamp01(remaining / Mathf.Max(0.01f, duration)));
            flash.SetColor(BaseColor, color);
            flash.SetColor(ColorId, color);
            renderer.SetPropertyBlock(flash);
        }
    }
    void Restore()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].SetPropertyBlock(original[i]);
        flashing = false;
    }
    void OnDisable()
    {
        if (health != null) health.Damaged -= OnDamaged;
        if (flashing) Restore();
    }
}
