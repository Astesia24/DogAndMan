using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 左上角的设置面板：C 键开关，打开时暂停游戏（背景音乐照常）。
/// 三条滑条：全局音量 / 背景音量 / 追击音量。
/// 和跳关按钮一样，运行时自动创建、跨场景保留。
/// </summary>
public class AudioSettingsUI : MonoBehaviour
{
    [Tooltip("开关设置面板的按键")]
    public KeyCode toggleKey = KeyCode.C;

    private GameObject _panel;
    private bool _paused;
    private float _savedTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindObjectOfType<AudioSettingsUI>() != null) return;

        var go = new GameObject("AudioSettingsUI",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        go.AddComponent<AudioSettingsUI>();
    }

    private void Awake()
    {
        UiRuntime.EnsureEventSystem();
        Build();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey)) return;

        // 对话进行中不响应，免得和"任意键继续"打架
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsShowing) return;

        Toggle();
    }

    private void OnDestroy()
    {
        if (_paused)
        {
            GameplayLock.ReleasePlayer(this);
            Time.timeScale = _savedTimeScale;
        }
    }

    private void Toggle()
    {
        bool open = !_panel.activeSelf;
        _panel.SetActive(open);
        SetPaused(open);

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void SetPaused(bool paused)
    {
        if (paused == _paused) return;

        _paused = paused;

        if (paused)
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            GameplayLock.HoldPlayer(this);
        }
        else
        {
            GameplayLock.ReleasePlayer(this);
            Time.timeScale = _savedTimeScale;
        }
    }

    private void Build()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 710;

        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        MakeButton("设置", new Vector2(24f, -24f), Toggle);
        MakeHint();

        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(transform, false);

        var panelBg = _panel.GetComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.72f);

        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(360f, 210f);
        panelRect.anchoredPosition = new Vector2(24f, -106f);

        var audio = AudioManager.Ensure();

        // 滑条按实际音量显示，拖动的值就是真实音量
        MakeRow("全局音量", -20f, audio.master, audio.SetMaster);
        MakeRow("背景音量", -76f, audio.bgmVolume, audio.SetBgm);
        MakeRow("追击音量", -132f, audio.chaseVolume, audio.SetChase);

        _panel.SetActive(false);
    }

    private void MakeHint()
    {
        var go = new GameObject("Hint", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);

        var text = go.GetComponent<Text>();
        text.text = "C";
        text.font = UiRuntime.Font();
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = new Color(0.9f, 0.88f, 0.82f, 0.65f);
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(60f, 26f);
        rect.anchoredPosition = new Vector2(28f, -74f);
    }

    private void MakeRow(string label, float y, float value, UnityAction<float> onChange)
    {
        var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(_panel.transform, false);

        var text = textGo.GetComponent<Text>();
        text.text = label;
        text.font = UiRuntime.Font();
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = new Color(0.95f, 0.92f, 0.85f);
        text.raycastTarget = false;

        var textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.sizeDelta = new Vector2(100f, 28f);
        textRect.anchoredPosition = new Vector2(16f, y);

        MakeSlider(y - 4f, value, onChange);
    }

    private void MakeSlider(float y, float value, UnityAction<float> onChange)
    {
        var root = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(_panel.transform, false);

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.sizeDelta = new Vector2(210f, 20f);
        rootRect.anchoredPosition = new Vector2(130f, y);

        var bg = NewImage(root.transform, "Background", Color.white, 0.18f);
        Stretch(bg.rectTransform);

        // 滑条要能被点到才拖得动，背景必须接收射线
        bg.raycastTarget = true;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        Stretch(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(0f, 3f);
        fillAreaRect.offsetMax = new Vector2(0f, -3f);

        var fill = NewImage(fillArea.transform, "Fill", new Color(0.85f, 0.8f, 0.6f), 0.95f);
        Stretch(fill.rectTransform);

        var handleArea = new GameObject("Handle Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        Stretch(handleArea.GetComponent<RectTransform>());

        var handle = NewImage(handleArea.transform, "Handle", new Color(0.95f, 0.92f, 0.85f), 1f);
        var handleRect = handle.rectTransform;
        handleRect.sizeDelta = new Vector2(14f, 26f);

        var slider = root.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.value = value;
        slider.onValueChanged.AddListener(onChange);
    }

    private void MakeButton(string label, Vector2 position, UnityAction onClick)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(110f, 44f);
        rect.anchoredPosition = position;

        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);

        var text = textGo.GetComponent<Text>();
        text.text = label;
        text.font = UiRuntime.Font();
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        Stretch(text.rectTransform);

        var button = go.GetComponent<Button>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(onClick);
    }

    private static Image NewImage(Transform parent, string name, Color color, float alpha)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = new Color(color.r, color.g, color.b, alpha);
        image.raycastTarget = false;

        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
