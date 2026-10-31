using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Owns its coroutine independently of the menu, which is destroyed on scene activation.
public sealed class WhiteFlashTransition : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }
    RawImage glow;
    Image curtain;
    Texture2D glowTexture;
    float previousTimeScale;
    bool ownsPause;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { IsPlaying = false; }

    public static bool Begin(string scene, float growth, float whiteHold, float darken, float darkHold,
        float reveal, Action<string> failed)
    {
        if (IsPlaying) return false;
        IsPlaying = true;
        var root = new GameObject("ClaraoTransition", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root);
        var transition = root.AddComponent<WhiteFlashTransition>();
        transition.CreateOverlay();
        transition.StartCoroutine(transition.Play(scene, growth, whiteHold, darken, darkHold, reveal, failed));
        return true;
    }

    void CreateOverlay()
    {
        var canvas = GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        var curtainObject = new GameObject("WhiteBlackCurtain", typeof(RectTransform), typeof(Image));
        curtainObject.transform.SetParent(transform, false);
        var rect = curtainObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        curtain = curtainObject.GetComponent<Image>(); curtain.color = new Color(1, 1, 1, 0);
        // Even transparent, the full-screen graphic prevents clicking the covered menu.
        curtain.raycastTarget = true;
        var glowObject = new GameObject("GrowingLight", typeof(RectTransform), typeof(RawImage));
        glowObject.transform.SetParent(transform, false);
        glow = glowObject.GetComponent<RawImage>(); glow.raycastTarget = false;
        glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(.5f, .5f);
        glow.rectTransform.anchoredPosition = Vector2.zero;
        glow.rectTransform.sizeDelta = Vector2.zero;
        const int size = 128;
        glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        glowTexture.name = "ClaraoSoftLight"; glowTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1).magnitude;
                float alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.45f, 1, distance));
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        glowTexture.SetPixels(pixels); glowTexture.Apply(false, true); glow.texture = glowTexture;
    }

    IEnumerator Animate(float seconds, Action<float> update)
    {
        seconds = Mathf.Max(0, seconds);
        float elapsed = 0;
        update(0);
        while (elapsed < seconds)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            update(Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / seconds)));
        }
        update(1);
    }

    IEnumerator Play(string scene, float growth, float whiteHold, float darken, float darkHold,
        float reveal, Action<string> failed)
    {
        yield return Animate(growth, t => {
            var size = ((RectTransform)transform).rect.size;
            glow.rectTransform.sizeDelta = Vector2.one * (size.magnitude * 3 * t);
        });
        curtain.color = Color.white;
        glow.gameObject.SetActive(false);
        // Render the fully white overlay before initiating any potentially expensive loading work.
        yield return null;
        previousTimeScale = Time.timeScale; ownsPause = true; Time.timeScale = 0;
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single); }
        catch (Exception error) { Debug.LogException(error); }
        if (operation == null)
        {
            yield return Animate(reveal, t => curtain.color = new Color(1, 1, 1, 1 - t));
            Finish(); failed?.Invoke("Não foi possível iniciar a partida. Tente novamente.");
            Destroy(gameObject); yield break;
        }
        yield return operation;
        // Allow destination Awake/Start and layout to finish beneath the opaque curtain.
        yield return null;
        yield return new WaitForSecondsRealtime(Mathf.Max(0, whiteHold));
        yield return Animate(darken, t => curtain.color = Color.Lerp(Color.white, Color.black, t));
        yield return new WaitForSecondsRealtime(Mathf.Max(0, darkHold));
        yield return Animate(reveal, t => curtain.color = new Color(0, 0, 0, 1 - t));
        Finish(); Destroy(gameObject);
    }

    void Finish()
    {
        if (ownsPause) { Time.timeScale = previousTimeScale; ownsPause = false; }
        IsPlaying = false;
    }
    void OnDestroy()
    {
        Finish();
        if (glowTexture != null) Destroy(glowTexture);
    }
}
