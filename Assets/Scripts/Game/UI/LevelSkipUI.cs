using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 右上角常驻的"跳过本关"按钮。跨场景保留，最后一关不显示。
/// </summary>
public class LevelSkipUI : MonoBehaviour
{
    private static readonly string[] Order = { "Act1", "Level1", "Level2", "Level3" };

    private GameObject _button;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindObjectOfType<LevelSkipUI>() != null) return;

        var go = new GameObject("LevelSkipUI",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        go.AddComponent<LevelSkipUI>();
    }

    private void Awake()
    {
        UiRuntime.EnsureEventSystem();
        Build();
    }

    private void Update()
    {
        if (_button == null) return;
        _button.SetActive(NextSceneName() != null);
    }

    private void Skip()
    {
        string next = NextSceneName();
        if (next != null) LevelFlow.GoTo(next);
    }

    private static string NextSceneName()
    {
        string current = SceneManager.GetActiveScene().name;

        for (int i = 0; i < Order.Length - 1; i++)
        {
            if (Order[i] == current) return Order[i + 1];
        }

        return null;
    }

    private void Build()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;

        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _button = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        _button.transform.SetParent(transform, false);

        var img = _button.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);

        var rt = _button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(200f, 60f);
        rt.anchoredPosition = new Vector2(-24f, -24f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(_button.transform, false);

        var text = textGo.GetComponent<Text>();
        text.text = "跳过本关";
        text.font = UiRuntime.Font();
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        _button.GetComponent<Button>().onClick.AddListener(Skip);
    }
}
