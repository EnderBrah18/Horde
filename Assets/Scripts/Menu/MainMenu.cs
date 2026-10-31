using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenu : MonoBehaviour
{
    public string gameplayScene = "CenaPrincipal";
    [Header("Telas do Canvas")]
    public GameObject home, options, controls, credits, quit;
    public TMP_Text status;
    public Button startButton;
    public Slider volume;
    public TMP_Text volumeLabel;
    public TMP_Dropdown resolution, quality, displayMode;
    public Toggle vSync;
    readonly List<Vector2Int> sizes = new List<Vector2Int>();
    bool loading;
    GameObject current;

    void Awake()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (home == null) MainMenuCanvas.Build(this);
        foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            if (label.name == "Version") label.text = "DEMO • " + Application.version;
        Bind("Start", BeginRun);
        Bind("Options", () => { ReadOptions(); Show(options); });
        Bind("Controls", () => Show(controls));
        Bind("Credits", () => Show(credits));
        Bind("Quit", () => Show(quit));
        Bind("CancelQuit", () => Show(home));
        Bind("ConfirmQuit", Exit);
        Bind("ApplyOptions", ApplyOptions);
        Bind("Defaults", Defaults);
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (button.name == "Back") button.onClick.AddListener(() => Show(home));
        volume.onValueChanged.AddListener(value => {
            AudioListener.volume = value;
            volumeLabel.text = "Volume geral: " + Mathf.RoundToInt(value * 100) + "%";
        });
        ReadOptions();
        Show(home);
    }

    void Bind(string name, UnityEngine.Events.UnityAction action)
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (button.name == name) button.onClick.AddListener(action);
    }

    void Update()
    {
        if (!loading && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Show(current == home ? quit : home);
    }

    void Show(GameObject panel)
    {
        if (loading) return;
        // Leaving uncommitted options restores the saved audio level too.
        if (current == options && panel != options) MenuPreferences.ApplyAudio();
        foreach (var page in new[] {home, options, controls, credits, quit}) page.SetActive(page == panel);
        current = panel;
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            var first = panel.GetComponentInChildren<Selectable>();
            if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
    }

    void ReadOptions()
    {
        sizes.Clear();
        foreach (var item in Screen.resolutions)
        {
            var size = new Vector2Int(item.width, item.height);
            if (!sizes.Contains(size) && size.x >= 800 && size.y >= 600) sizes.Add(size);
        }
        var actual = new Vector2Int(Screen.width, Screen.height);
        if (!sizes.Contains(actual)) sizes.Add(actual);
        resolution.ClearOptions();
        resolution.AddOptions(sizes.ConvertAll(size => size.x + " × " + size.y));
        resolution.value = Mathf.Max(0, sizes.IndexOf(new Vector2Int(
            PlayerPrefs.GetInt("Menu.Width", Screen.width), PlayerPrefs.GetInt("Menu.Height", Screen.height))));
        quality.ClearOptions(); quality.AddOptions(new List<string>(QualitySettings.names));
        quality.value = Mathf.Clamp(PlayerPrefs.GetInt("Menu.Quality", QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
        displayMode.value = PlayerPrefs.GetInt("Menu.Windowed", Screen.fullScreenMode == FullScreenMode.Windowed ? 1 : 0);
        vSync.isOn = PlayerPrefs.GetInt("Menu.VSync", QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
        volume.value = PlayerPrefs.GetFloat("Menu.Volume", 1);
        volumeLabel.text = "Volume geral: " + Mathf.RoundToInt(volume.value * 100) + "%";
    }

    void Defaults()
    {
        volume.value = 1; displayMode.value = 0; vSync.isOn = true;
        quality.value = Mathf.Min(2, QualitySettings.names.Length - 1);
        var native = Screen.currentResolution;
        int index = sizes.IndexOf(new Vector2Int(native.width, native.height));
        resolution.value = index >= 0 ? index : sizes.Count - 1;
    }

    void ApplyOptions()
    {
        var size = sizes[resolution.value];
        PlayerPrefs.SetInt("Menu.Width", size.x); PlayerPrefs.SetInt("Menu.Height", size.y);
        PlayerPrefs.SetInt("Menu.Quality", quality.value);
        PlayerPrefs.SetInt("Menu.Windowed", displayMode.value);
        PlayerPrefs.SetInt("Menu.VSync", vSync.isOn ? 1 : 0);
        PlayerPrefs.SetFloat("Menu.Volume", volume.value);
        PlayerPrefs.SetInt("Menu.Configured", 1); PlayerPrefs.Save();
        MenuPreferences.Apply();
        status.text = "Opções salvas.";
        Show(home);
    }

    public void BeginRun() { if (!loading) StartCoroutine(StartRun()); }
    IEnumerator StartRun()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
        { status.text = "A cena da vila não está incluída na build."; yield break; }
        loading = true;
        foreach (var selectable in GetComponentsInChildren<Selectable>(true)) selectable.interactable = false;
        status.text = "Preparando a vila…";
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Single); }
        catch (System.Exception error) { Debug.LogException(error); }
        if (operation != null) { yield return operation; yield break; }
        loading = false;
        foreach (var selectable in GetComponentsInChildren<Selectable>(true)) selectable.interactable = true;
        status.text = "Não foi possível iniciar a partida. Tente novamente.";
    }

    void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

public static class MenuPreferences
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize() { Apply(); }
    public static void ApplyAudio() { AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Menu.Volume", 1)); }
    public static void Apply()
    {
        ApplyAudio();
        if (!PlayerPrefs.HasKey("Menu.Configured")) return;
        QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt("Menu.Quality"), 0, QualitySettings.names.Length - 1), true);
        QualitySettings.vSyncCount = PlayerPrefs.GetInt("Menu.VSync", 1) == 1 ? 1 : 0;
        Screen.SetResolution(Mathf.Max(800, PlayerPrefs.GetInt("Menu.Width", 1280)),
            Mathf.Max(600, PlayerPrefs.GetInt("Menu.Height", 720)),
            PlayerPrefs.GetInt("Menu.Windowed") == 1 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow);
    }
}
