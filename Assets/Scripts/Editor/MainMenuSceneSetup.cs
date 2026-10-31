using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MainMenuSceneSetup
{
    const string Path = "Assets/Scenes/MenuPrincipal.unity";
    static MainMenuSceneSetup() { EditorApplication.delayCall += EnsureScene; }
    static void EnsureScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (System.IO.File.ReadAllText(Path).Contains("m_Name: MenuCanvas")) return;
        BuildScene();
    }

    [MenuItem("Tools/Pandora/Reconstruir menu principal (Canvas)")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var previous = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(Path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (!opened && scene.isDirty)
        { Debug.LogWarning("Salve MenuPrincipal antes de reconstruir o Canvas pelo menu Tools/Pandora."); return; }
        if (opened) scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.055f,.07f,.1f);
        var menu = new GameObject("MenuPrincipal").AddComponent<MainMenu>();
        MainMenuCanvas.Build(menu);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Path);
        if (previous.IsValid() && previous.isLoaded && previous != scene) SceneManager.SetActiveScene(previous);
        if (opened) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("MenuPrincipal preparado com Canvas, TextMeshPro e Input System.");
    }
}
