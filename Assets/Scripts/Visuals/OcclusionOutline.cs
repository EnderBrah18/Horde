using UnityEngine;
using UnityEngine.Rendering;

// GPU depth/stencil outline: works behind visible meshes without physics colliders.
[DisallowMultipleComponent]
public sealed class OcclusionOutline : MonoBehaviour
{
    public Color color = new Color(1f, 0.35f, 0.15f, 1f);
    [Min(0.01f)] public float width = 0.1f;
    MeshRenderer maskRenderer, outlineRenderer;
    Material outlineMaterial;

    void Start()
    {
        var source = GetComponent<MeshFilter>();
        var mask = Resources.Load<Material>("Effects/OcclusionMask");
        var outline = Resources.Load<Material>("Effects/OcclusionOutline");
        if (source == null || source.sharedMesh == null || mask == null || outline == null) return;
        outlineMaterial = new Material(outline);
        outlineMaterial.SetColor("_OutlineColor", color);
        outlineMaterial.SetFloat("_OutlineWidth", width);
        maskRenderer = CreateProxy("OcclusionMask", source.sharedMesh, mask);
        outlineRenderer = CreateProxy("OcclusionOutline", source.sharedMesh, outlineMaterial);
    }

    MeshRenderer CreateProxy(string objectName, Mesh mesh, Material material)
    {
        var proxy = new GameObject(objectName);
        proxy.layer = LayerMask.NameToLayer("Ignore Raycast");
        proxy.transform.SetParent(transform, false);
        proxy.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = proxy.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.allowOcclusionWhenDynamic = false;
        return renderer;
    }

    void OnEnable() { SetVisible(true); }
    void OnDisable() { SetVisible(false); }
    void SetVisible(bool visible)
    {
        if (maskRenderer != null) maskRenderer.enabled = visible;
        if (outlineRenderer != null) outlineRenderer.enabled = visible;
    }
    void OnValidate()
    {
        width = Mathf.Max(0.01f, width);
        if (outlineMaterial == null) return;
        outlineMaterial.SetColor("_OutlineColor", color);
        outlineMaterial.SetFloat("_OutlineWidth", width);
    }
    void OnDestroy() { if (outlineMaterial != null) Destroy(outlineMaterial); }
}
