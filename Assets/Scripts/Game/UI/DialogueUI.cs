using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RPG 式对话框：屏幕下方一个框，显示「名字 + 台词」，任意键继续。
/// 框上方可以放角色立绘（左/右），也可以不放。
///
/// 对话期间通过 GameplayLock 锁住操作（狗、魔女、守卫都停），
/// 但世界时间照常走，所以打字机用 unscaledTime 也一样稳。
///
/// 用法：
///     DialogueUI.Ensure().Play(lines, () => Debug.Log("演完了"));
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }
    public static bool IsActive => Instance != null && Instance._active;

    [Header("字体（留空会自动从 Resources/Fonts 加载）")]
    public Font bodyFont;
    public Font nameFont;

    [Header("配色")]
    public Color panelColor = new Color(0.07f, 0.06f, 0.05f, 0.94f);
    public Color frameColor = new Color(0.62f, 0.52f, 0.32f, 1f);
    public Color bodyColor = new Color(0.93f, 0.90f, 0.84f, 1f);
    public Color arrowColor = new Color(0.85f, 0.75f, 0.45f, 1f);

    [Header("布局（1920×1080 参考分辨率）")]
    public Vector2 panelSize = new Vector2(1680f, 300f);
    public float panelBottom = 60f;
    public Vector2 portraitSize = new Vector2(420f, 640f);
    public float portraitSideMargin = 200f;

    [Header("操作")]
    [Tooltip("跳过整段对话（只跳当前这一段，不是全部）")]
    public KeyCode skipKey = KeyCode.Escape;

    private Canvas _canvas;
    private Image _frameImg, _panelImg, _portraitImg;
    private RectTransform _portraitRect;
    private Text _nameText, _bodyText, _arrowText;

    private DialogueLine[] _lines;
    private Action _onFinish;
    private bool _active;

    /// <summary>对话框正在显示（设置面板据此决定要不要响应按键）。</summary>
    public bool IsShowing => _active;
    [Tooltip("有些台词没有立绘，这时把上一个人的立绘压暗")]
    [Range(0.1f, 1f)] public float dimAlpha = 0.35f;

    private float _openedAt;
    private Coroutine _routine;
    private Coroutine _fadeRoutine;
    private PortraitSide _lastSide = PortraitSide.None;


    public static DialogueUI Ensure()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("DialogueUI");
        return go.AddComponent<DialogueUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveFonts();
        Build();
        _canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        GameplayLock.Release(this);
        if (Instance == this) Instance = null;
    }

    private void ResolveFonts()
    {
        if (bodyFont == null) bodyFont = Resources.Load<Font>("Fonts/LXGWWenKai-Regular");
        if (nameFont == null) nameFont = Resources.Load<Font>("Fonts/SmileySans-Oblique");

        if (bodyFont == null) bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (bodyFont == null) Debug.LogWarning("[DialogueUI] 找不到任何字体，对话文字不会显示。");

        if (nameFont == null) nameFont = bodyFont;
    }


    public void Play(DialogueLine[] lines, Action onFinish = null)
    {
        if (lines == null || lines.Length == 0)
        {
            onFinish?.Invoke();
            return;
        }

        if (_routine != null) StopCoroutine(_routine);

        _lines = lines;
        _onFinish = onFinish;
        _active = true;
        _openedAt = Time.unscaledTime;

        // 用统一的锁，而不是直接把 timeScale 归零 —— 这样世界照常动，只是狗不听操作
        GameplayLock.Hold(this);
        _canvas.gameObject.SetActive(true);

        _routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        for (int i = 0; i < _lines.Length; i++)
        {
            var line = _lines[i];
            ShowLine(line);

            string full = line.text ?? "";
            int total = VisibleLength(full);
            float cps = line.charsPerSecond > 0f ? line.charsPerSecond : 9999f;
            float t = 0f;
            int shown = 0;

            while (shown < total)
            {
                if (SkipPressed()) { SkipBlock(); yield break; }
                if (AdvancePressed()) break;

                t += Time.unscaledDeltaTime;
                int want = Mathf.Min(total, Mathf.FloorToInt(t * cps) + 1);
                if (want > shown)
                {
                    shown = want;
                    _bodyText.text = Reveal(full, shown);
                }
                yield return null;
            }

            _bodyText.text = full;
            _arrowText.gameObject.SetActive(true);
            yield return null;

            while (!AdvancePressed())
            {
                if (SkipPressed()) { SkipBlock(); yield break; }
                yield return null;
            }

            _arrowText.gameObject.SetActive(false);
            yield return null;
        }

        Close();
    }

    /// <summary>跳过这一整段对话，onFinish 照常触发，演出继续往下走。</summary>
    private void SkipBlock()
    {
        Close();
    }

    private bool SkipPressed()
    {
        return Input.GetKeyDown(skipKey);
    }

    private void Close()
    {
        _active = false;
        _routine = null;

        if (_canvas != null) _canvas.gameObject.SetActive(false);
        GameplayLock.Release(this);

        var cb = _onFinish;
        _onFinish = null;
        cb?.Invoke();
    }

    private bool AdvancePressed()
    {
        // 打开对话的那一下不算，避免一按就跳过第一句
        if (Time.unscaledTime - _openedAt < 0.12f) return false;

        // 跳过键不当作"继续"
        if (Input.GetKeyDown(skipKey)) return false;

        return Input.anyKeyDown;
    }


    private void ShowLine(DialogueLine line)
    {
        bool hasName = !string.IsNullOrEmpty(line.speaker);
        _nameText.gameObject.SetActive(hasName);
        if (hasName)
        {
            _nameText.text = line.speaker;
            _nameText.color = line.nameColor;
        }

        ShowPortrait(line);

        if (line.effect != DialogueEffect.None) StartCoroutine(PlayEffect(line.effect));

        _bodyText.text = "";
        _bodyText.color = line.textColor.a > 0.001f ? line.textColor : bodyColor;
        _arrowText.gameObject.SetActive(false);
    }

    private void ShowPortrait(DialogueLine line)
    {
        if (line.clearPortrait)
        {
            HidePortrait();
            return;
        }

        if (line.dimPortrait)
        {
            DimPortrait();
            return;
        }

        // 没写立绘 = 保持上一句的样子，不闪
        if (line.portrait == null) return;

        bool same = _portraitImg.gameObject.activeSelf &&
                    _portraitImg.sprite == line.portrait &&
                    _lastSide == line.side;

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _portraitImg.gameObject.SetActive(true);
        _portraitImg.sprite = line.portrait;
        _lastSide = line.side;
        PlacePortrait(line.side);

        if (line.portraitFade > 0f && !same)
        {
            var start = line.portraitTint;
            start.a = 0f;
            _portraitImg.color = start;
            _fadeRoutine = StartCoroutine(FadeIn(start, line.portraitTint, line.portraitFade));
        }
        else
        {
            _portraitImg.color = line.portraitTint;
        }
    }

    private void DimPortrait()
    {
        if (!_portraitImg.gameObject.activeSelf) return;

        var c = _portraitImg.color;
        c.a = dimAlpha;
        _portraitImg.color = c;
    }

    private void HidePortrait()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _portraitImg.gameObject.SetActive(false);
        _lastSide = PortraitSide.None;
    }

    private IEnumerator FadeIn(Color from, Color to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var c = to;
            c.a = Mathf.Lerp(from.a, to.a, t / duration);
            _portraitImg.color = c;
            yield return null;
        }

        _portraitImg.color = to;
        _fadeRoutine = null;
    }

    /// <summary>不算标签、只数真正会显示的字。</summary>
    private static int VisibleLength(string text)
    {
        int count = 0;
        int i = 0;

        while (i < text.Length)
        {
            if (text[i] == '<')
            {
                int close = text.IndexOf('>', i);
                if (close > i)
                {
                    i = close + 1;
                    continue;
                }
            }

            count++;
            i++;
        }

        return count;
    }

    /// <summary>
    /// 只显示前 visibleCount 个字，标签原样保留并自动闭合，
    /// 这样 <color> 打到一半也不会露出乱码。
    /// </summary>
    private static string Reveal(string text, int visibleCount)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        var open = new System.Collections.Generic.List<string>();

        int shown = 0;
        int i = 0;

        while (i < text.Length)
        {
            if (text[i] == '<')
            {
                int close = text.IndexOf('>', i);
                if (close > i)
                {
                    string tag = text.Substring(i, close - i + 1);
                    sb.Append(tag);
                    TrackOpen(tag, open);
                    i = close + 1;
                    continue;
                }
            }

            if (shown >= visibleCount) break;

            sb.Append(text[i]);
            shown++;
            i++;
        }

        for (int k = open.Count - 1; k >= 0; k--) sb.Append(open[k]);

        return sb.ToString();
    }

    private static void TrackOpen(string tag, System.Collections.Generic.List<string> open)
    {
        if (tag.StartsWith("</"))
        {
            string name = tag.Substring(2).TrimEnd('>');

            for (int i = open.Count - 1; i >= 0; i--)
            {
                if (open[i].StartsWith("</" + name)) { open.RemoveAt(i); break; }
            }
            return;
        }

        string opened = tag.TrimStart('<');
        int eq = opened.IndexOf('=');
        if (eq >= 0) opened = opened.Substring(0, eq);
        opened = opened.TrimEnd('>', '/');

        open.Add("</" + opened + ">");
    }

    private IEnumerator PlayEffect(DialogueEffect effect)    {
        var overlay = ScreenOverlay.Ensure();
        var red = new Color(0.80f, 0.10f, 0.10f);

        yield return overlay.Flash(0.30f, red);

        if (effect == DialogueEffect.FlashRedTwice)
        {
            yield return new WaitForSecondsRealtime(0.20f);
            yield return overlay.Flash(0.30f, red);
        }
        else if (effect == DialogueEffect.FlashRedThenWhite)
        {
            yield return new WaitForSecondsRealtime(0.15f);
            yield return overlay.Flash(0.30f, Color.white);
        }
        else if (effect == DialogueEffect.FlashBlue)
        {
            yield return overlay.Flash(0.40f, new Color(0.35f, 0.65f, 1f));
        }
    }

    private void PlacePortrait(PortraitSide side)
    {
        float top = panelBottom + panelSize.y + 8f;

        switch (side)
        {
            case PortraitSide.Left:
                _portraitRect.anchorMin = new Vector2(0f, 0f);
                _portraitRect.anchorMax = new Vector2(0f, 0f);
                _portraitRect.pivot = new Vector2(0f, 0f);
                _portraitRect.anchoredPosition = new Vector2(portraitSideMargin, top);
                break;

            case PortraitSide.Center:
                _portraitRect.anchorMin = new Vector2(0.5f, 0f);
                _portraitRect.anchorMax = new Vector2(0.5f, 0f);
                _portraitRect.pivot = new Vector2(0.5f, 0f);
                _portraitRect.anchoredPosition = new Vector2(0f, top);
                break;

            default:
                _portraitRect.anchorMin = new Vector2(1f, 0f);
                _portraitRect.anchorMax = new Vector2(1f, 0f);
                _portraitRect.pivot = new Vector2(1f, 0f);
                _portraitRect.anchoredPosition = new Vector2(-portraitSideMargin, top);
                break;
        }

        _portraitRect.sizeDelta = portraitSize;
    }

    private void Update()
    {
        if (!_active || _arrowText == null) return;
        if (!_arrowText.gameObject.activeSelf) return;

        var c = arrowColor;
        c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
        _arrowText.color = c;
    }


    private void Build()
    {
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 500;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // 立绘：先建，画在框底下。没图的时候要关掉，否则会显示一块白矩形
        _portraitImg = NewImage(canvasGo.transform, "Portrait", Color.white);
        _portraitRect = _portraitImg.rectTransform;
        _portraitImg.preserveAspect = true;
        _portraitImg.gameObject.SetActive(false);

        // 外框（比底板大一圈，做出金边）
        _frameImg = NewImage(canvasGo.transform, "Frame", frameColor);
        AnchorBottomCenter(_frameImg.rectTransform, panelSize + new Vector2(16f, 16f), panelBottom - 8f);

        // 底板
        _panelImg = NewImage(canvasGo.transform, "Panel", panelColor);
        AnchorBottomCenter(_panelImg.rectTransform, panelSize, panelBottom);
        var panel = _panelImg.transform;

        // 名字
        _nameText = NewText(panel, "Name", nameFont, 44, TextAnchor.MiddleLeft, frameColor);
        var nr = _nameText.rectTransform;
        nr.anchorMin = new Vector2(0f, 1f);
        nr.anchorMax = new Vector2(1f, 1f);
        nr.pivot = new Vector2(0.5f, 1f);
        nr.sizeDelta = new Vector2(-88f, 56f);
        nr.anchoredPosition = new Vector2(0f, -26f);

        // 分隔线
        var divider = NewImage(panel, "Divider", new Color(frameColor.r, frameColor.g, frameColor.b, 0.45f));
        var dr = divider.rectTransform;
        dr.anchorMin = new Vector2(0f, 1f);
        dr.anchorMax = new Vector2(1f, 1f);
        dr.pivot = new Vector2(0.5f, 1f);
        dr.sizeDelta = new Vector2(-88f, 2f);
        dr.anchoredPosition = new Vector2(0f, -92f);

        // 正文
        _bodyText = NewText(panel, "Body", bodyFont, 38, TextAnchor.UpperLeft, bodyColor);
        var br = _bodyText.rectTransform;
        br.anchorMin = new Vector2(0f, 1f);
        br.anchorMax = new Vector2(1f, 1f);
        br.pivot = new Vector2(0.5f, 1f);
        br.sizeDelta = new Vector2(-88f, 160f);
        br.anchoredPosition = new Vector2(0f, -108f);
        _bodyText.lineSpacing = 1.15f;

        // 继续箭头
        _arrowText = NewText(panel, "Arrow", nameFont, 34, TextAnchor.MiddleCenter, arrowColor);
        _arrowText.text = "▼";
        var ar = _arrowText.rectTransform;
        ar.anchorMin = new Vector2(1f, 0f);
        ar.anchorMax = new Vector2(1f, 0f);
        ar.pivot = new Vector2(1f, 0f);
        ar.sizeDelta = new Vector2(44f, 44f);
        ar.anchoredPosition = new Vector2(-36f, 22f);
        _arrowText.gameObject.SetActive(false);

        // 左下角的跳过提示
        var hint = NewText(panel, "SkipHint", nameFont, 20, TextAnchor.MiddleLeft,
                           new Color(0.75f, 0.72f, 0.68f, 0.55f));
        hint.text = "Esc 跳过这段";

        var hr = hint.rectTransform;
        hr.anchorMin = new Vector2(0f, 0f);
        hr.anchorMax = new Vector2(0f, 0f);
        hr.pivot = new Vector2(0f, 0f);
        hr.sizeDelta = new Vector2(220f, 28f);
        hr.anchoredPosition = new Vector2(36f, 16f);
    }

    private static Image NewImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Text NewText(Transform parent, string name, Font font, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;   // 支持 <b> 加粗
        return text;
    }

    private static void AnchorBottomCenter(RectTransform rt, Vector2 size, float bottom)
    {
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(0f, bottom);
    }
}
