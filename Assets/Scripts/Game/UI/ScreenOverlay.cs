using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全屏特效与提示：闪白、淡入黑、角色头顶的提示文字。
///
/// 用法：
///     yield return ScreenOverlay.Ensure().Flash(0.45f);
///     yield return ScreenOverlay.Ensure().FadeToBlack(1.2f);
///     ScreenOverlay.Ensure().ShowPromptAbove(dog, "按下空格键发出声音");
///     ScreenOverlay.Ensure().HidePrompt();
/// </summary>
public class ScreenOverlay : MonoBehaviour
{
    public static ScreenOverlay Instance { get; private set; }

    private Canvas _canvas;
    private Image _fade;
    private Image _flash;
    private GameObject _promptGo;
    private Text _hintText;
    private Font _font;

    public static ScreenOverlay Ensure()
    {
        if (Instance != null) return Instance;
        return new GameObject("ScreenOverlay").AddComponent<ScreenOverlay>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _font = Resources.Load<Font>("Fonts/LXGWWenKai-Regular");
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Build()
    {
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 450;   // 比对话框(500)低，这样黑屏时对话框还看得见

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _fade = NewFullScreen("Fade", new Color(0f, 0f, 0f, 0f));
        _flash = NewFullScreen("Flash", new Color(1f, 1f, 1f, 0f));
    }

    private Image NewFullScreen(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_canvas.transform, false);

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return img;
    }


    /// <summary>闪一下（默认白色）。</summary>
    public IEnumerator Flash(float duration, Color? color = null)
    {
        Color c = color ?? Color.white;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / duration);
            _flash.color = c;
            yield return null;
        }

        c.a = 0f;
        _flash.color = c;
    }

    /// <summary>淡到全黑，且保持黑。</summary>
    public IEnumerator FadeToBlack(float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var c = Color.black;
            c.a = Mathf.Clamp01(t / duration);
            _fade.color = c;
            yield return null;
        }

        _fade.color = Color.black;
    }

    /// <summary>立刻把幕布设成某个颜色（Alpha = 0 表示透明）。</summary>
    public void SetCurtain(Color color)
    {
        if (_fade != null) _fade.color = color;
    }

    /// <summary>立刻全黑（序章用）。</summary>
    public void SetBlack()
    {
        if (_fade != null) _fade.color = Color.black;
    }

    /// <summary>从当前黑度淡回正常画面。</summary>
    public IEnumerator FadeIn(float duration)
    {
        float start = _fade.color.a;
        if (duration <= 0f || start <= 0f)
        {
            _fade.color = new Color(0f, 0f, 0f, 0f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var c = Color.black;
            c.a = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / duration));
            _fade.color = c;
            yield return null;
        }

        _fade.color = new Color(0f, 0f, 0f, 0f);
    }

    public void ClearFade()
    {
        if (_fade != null) _fade.color = new Color(0f, 0f, 0f, 0f);
    }


    /// <summary>在某个物体上方显示一行白字（世界空间，会跟着物体走）。</summary>
    public void ShowPromptAbove(Transform owner, string text)
    {
        HidePrompt();
        if (owner == null) return;

        var go = new GameObject("Prompt", typeof(Canvas));
        go.transform.SetParent(owner, false);
        go.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        go.transform.localRotation = Quaternion.identity;

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 300;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1200f, 110f);
        rt.localScale = Vector3.one * 0.0045f;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);

        var t = textGo.GetComponent<Text>();
        t.font = _font;
        t.fontSize = 66;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.text = text;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        _promptGo = go;
    }

    public void HidePrompt()
    {
        if (_promptGo != null) Destroy(_promptGo);
        _promptGo = null;
    }

    /// <summary>屏幕中央缓缓出现一行字。</summary>
    public void ShowCenterHint(string text, float fadeDuration = 1.5f)
    {
        StartCoroutine(HintRoutine(text, fadeDuration));
    }

    private IEnumerator HintRoutine(string text, float fadeDuration)
    {
        if (_hintText == null)
        {
            var go = new GameObject("Hint", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_canvas.transform, false);

            _hintText = go.GetComponent<Text>();
            _hintText.font = _font;
            _hintText.fontSize = 46;
            _hintText.alignment = TextAnchor.MiddleCenter;
            _hintText.raycastTarget = false;
            _hintText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _hintText.verticalOverflow = VerticalWrapMode.Overflow;

            var rt = _hintText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1200f, 120f);
            rt.anchoredPosition = new Vector2(0f, -120f);

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        _hintText.text = text;

        var color = Color.white;
        color.a = 0f;
        _hintText.color = color;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(t / fadeDuration);
            _hintText.color = color;
            yield return null;
        }
    }

    /// <summary>重来之前清掉黑屏和提示（场景重载时对象会被销毁，一般用不到）。</summary>
    public void ResetAll()
    {
        if (_fade != null) _fade.color = new Color(0f, 0f, 0f, 0f);
        if (_flash != null) _flash.color = new Color(1f, 1f, 1f, 0f);
        if (_hintText != null) _hintText.text = "";
    }
}
