using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class MainMenu : MonoBehaviour
{
    public string gameplayScene = "CenaPrincipal";
    bool loading;
    string message = "";

    IEnumerator StartRun()
    {
        if (loading) yield break;
        if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
        { message = "A cena da vila não está incluída na build."; yield break; }
        loading = true;
        Game.PrepareNewRun();
        var operation = SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Single);
        if (operation != null) yield return operation;
        else { loading = false; message = "Não foi possível iniciar a partida."; }
    }

    void OnGUI()
    {
        GUI.color = new Color(0.055f,0.065f,0.08f,1f);
        GUI.DrawTexture(new Rect(0f,0f,Screen.width,Screen.height),Texture2D.whiteTexture);
        GUI.color = Color.white;
        float width = Mathf.Min(430f,Screen.width - 32f);
        GUILayout.BeginArea(new Rect((Screen.width-width)*0.5f,Mathf.Max(16f,(Screen.height-300f)*0.5f),width,300f),GUI.skin.box);
        GUILayout.Label("HORDE",new GUIStyle(GUI.skin.label) { fontSize=40,alignment=TextAnchor.MiddleCenter });
        GUILayout.Label("Uma nova tentativa pela vila.",new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter });
        GUILayout.Space(24f);
        bool previous = GUI.enabled;
        GUI.enabled = previous && !loading;
        if (GUILayout.Button(loading ? "Iniciando..." : "Nova run",GUILayout.Height(48f))) StartCoroutine(StartRun());
        if (GUILayout.Button("Sair",GUILayout.Height(38f))) Application.Quit();
        GUI.enabled = previous;
        if (!string.IsNullOrEmpty(message)) GUILayout.Label(message,new GUIStyle(GUI.skin.label) { wordWrap=true });
        GUILayout.EndArea();
    }
}
