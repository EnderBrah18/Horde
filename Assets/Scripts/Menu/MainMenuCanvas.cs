using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Shared by the editor scene builder and the runtime recovery path.
public static class MainMenuCanvas
{
    static readonly Color Ink = new Color(.055f, .07f, .10f, 1);
    static readonly Color Paper = new Color(.92f, .88f, .77f, 1);
    static readonly Color Accent = new Color(.34f, .22f, .34f, 1);

    public static void Build(MainMenu menu)
    {
        var canvasObject = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(menu.transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        var backdrop = Box("Background", canvasObject.transform, Ink); Stretch(backdrop);
        var heading = Text("GameTitle", canvasObject.transform, "CRÔNICAS DE PANDORA", 36);
        Rect(heading.rectTransform, new Vector2(.5f, 1), new Vector2(0, -66), new Vector2(1080, 62));
        var subtitle = Text("Subtitle", canvasObject.transform, "S O B R E V I V Ê N C I A", 20);
        Rect(subtitle.rectTransform, new Vector2(.5f, 1), new Vector2(0, -114), new Vector2(1000, 34));
        menu.home = Page("Home", canvasObject.transform);
        var line = Text("Tagline", menu.home.transform, "Mantenha o calor. Reconstrua a vila. Sobreviva à noite.", 19);
        Rect(line.rectTransform, new Vector2(.5f,.5f), new Vector2(0,160), new Vector2(1000,40));
        menu.startButton = Button("Start", menu.home.transform, "Iniciar partida", 90);
        Button("Options", menu.home.transform, "Opções", 30);
        Button("Controls", menu.home.transform, "Como jogar", -30);
        Button("Credits", menu.home.transform, "Créditos", -90);
        Button("Quit", menu.home.transform, "Sair", -150);
        menu.options = Page("OptionsPanel", canvasObject.transform);
        Label(menu.options, "Opções", 168, 28);
        menu.volumeLabel = Label(menu.options, "Volume geral: 100%", 118, 19);
        menu.volume = MakeSlider(menu.options.transform, 76);
        Label(menu.options, "Resolução", 25, 18, -225);
        menu.resolution = Dropdown("Resolution", menu.options.transform, 25);
        Label(menu.options, "Modo de tela", -29, 18, -225);
        menu.displayMode = Dropdown("DisplayMode", menu.options.transform, -29);
        menu.displayMode.AddOptions(new System.Collections.Generic.List<string> { "Tela cheia sem bordas", "Janela" });
        Label(menu.options, "Qualidade gráfica", -83, 18, -225);
        menu.quality = Dropdown("Quality", menu.options.transform, -83);
        var toggleObject = new GameObject("VSync", typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleObject.transform.SetParent(menu.options.transform, false);
        Rect(toggleObject.GetComponent<RectTransform>(), new Vector2(.5f,.5f), new Vector2(-130,-137), new Vector2(28,28));
        toggleObject.GetComponent<Image>().color = Accent;
        var check = Box("Checkmark", toggleObject.transform, Paper);
        Rect(check, new Vector2(.5f,.5f), Vector2.zero, new Vector2(18,18));
        menu.vSync = toggleObject.GetComponent<Toggle>(); menu.vSync.targetGraphic = toggleObject.GetComponent<Image>(); menu.vSync.graphic = check.GetComponent<Image>();
        Label(menu.options, "Sincronização vertical", -137, 18, 30);
        SmallButton("ApplyOptions", menu.options.transform, "Aplicar", -225, -220);
        SmallButton("Defaults", menu.options.transform, "Padrão", -225, 0);
        SmallButton("Back", menu.options.transform, "Voltar", -225, 220);
        menu.controls = Page("ControlsPanel", canvasObject.transform);
        Label(menu.controls, "Como jogar", 155, 28);
        var help = Label(menu.controls,
            "WASD — mover a personagem / câmera livre\nShift — correr com a personagem\nMouse — apontar  •  Botão esquerdo — atacar / selecionar\nE — interagir / coletar\nTab — alternar personagem e câmera de construção\nEsc ou 0 — deixar a mão vazia / fechar seleção\nEspaço — iniciar a noite quando estiver pronta\n\nExplore durante o dia e prepare suas defesas.\nProteja a igreja durante a noite.", -15, 20);
        help.rectTransform.sizeDelta = new Vector2(1060,310);
        Button("Back", menu.controls.transform, "Voltar", -225);
        menu.credits = Page("CreditsPanel", canvasObject.transform);
        Label(menu.credits, "Créditos", 140, 28);
        var creditText = Label(menu.credits, "Crônicas de Pandora: Sobrevivência\n\nUm jogo em desenvolvimento.\n\nObrigado por participar do playtest e ajudar a construir este mundo.", 0, 22);
        creditText.rectTransform.sizeDelta = new Vector2(1000,240);
        Button("Back", menu.credits.transform, "Voltar", -225);
        menu.quit = Page("QuitPanel", canvasObject.transform);
        Label(menu.quit, "Sair do jogo?", 70, 30);
        Button("ConfirmQuit", menu.quit.transform, "Sair", -10);
        Button("CancelQuit", menu.quit.transform, "Cancelar", -75);
        menu.status = Text("Status", canvasObject.transform, "", 18);
        Rect(menu.status.rectTransform, new Vector2(.5f,0), new Vector2(0,65), new Vector2(1100,38));
        var footer = Text("Version", canvasObject.transform, "DEMO • " + Application.version, 15);
        Rect(footer.rectTransform, new Vector2(.5f,0), new Vector2(0,25), new Vector2(1100,28));
        foreach (var page in new[] {menu.options,menu.controls,menu.credits,menu.quit}) page.SetActive(false);
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(menu.transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    static GameObject Page(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>()); return go;
    }
    static TMP_Text Label(GameObject page, string value, float y, float size, float x = 0)
    {
        var label = Text("Label", page.transform, value, size);
        Rect(label.rectTransform, new Vector2(.5f,.5f), new Vector2(x,y), new Vector2(x == 0 ? 1100 : 300,40)); return label;
    }
    static TMP_Text Text(string name, Transform parent, string value, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
        var text = go.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size;
        text.color = Paper; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        text.font = TMP_Settings.defaultFontAsset; return text;
    }
    static RectTransform Box(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent,false);
        go.GetComponent<Image>().color = color; return go.GetComponent<RectTransform>();
    }
    static Button Button(string name, Transform parent, string caption, float y)
    {
        var rect = Box(name,parent,Accent); Rect(rect,new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(390,48));
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
        var colors = button.colors; colors.highlightedColor = new Color(1.3f,1.3f,1.3f); colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.7f,.7f,.7f); button.colors = colors;
        var label = Text("Text (TMP)",rect,caption,22); Stretch(label.rectTransform); return button;
    }
    static void SmallButton(string name, Transform parent, string caption, float y, float x)
    {
        var button = Button(name,parent,caption,y); button.GetComponent<RectTransform>().anchoredPosition = new Vector2(x,y);
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(200,48);
    }
    static TMP_Dropdown Dropdown(string name, Transform parent, float y)
    {
        var go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources()); go.name = name;
        go.transform.SetParent(parent,false);
        Rect(go.GetComponent<RectTransform>(),new Vector2(.5f,.5f),new Vector2(130,y),new Vector2(390,42));
        var dropdown = go.GetComponent<TMP_Dropdown>(); dropdown.ClearOptions();
        foreach (var text in go.GetComponentsInChildren<TMP_Text>(true)) { text.font = TMP_Settings.defaultFontAsset; text.fontSize = 20; }
        return dropdown;
    }
    static Slider MakeSlider(Transform parent, float y)
    {
        var rect = Box("Volume",parent,Accent); Rect(rect,new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(540,24));
        var slider = rect.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
        var area = new GameObject("HandleArea",typeof(RectTransform)).GetComponent<RectTransform>(); area.SetParent(rect,false); Stretch(area);
        area.offsetMin = new Vector2(12,0); area.offsetMax = new Vector2(-12,0);
        var handle = Box("Handle",area,Paper); handle.sizeDelta = new Vector2(24,32);
        slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }
    static void Rect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f,.5f); rect.anchoredPosition = position; rect.sizeDelta = size; }
    static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}

