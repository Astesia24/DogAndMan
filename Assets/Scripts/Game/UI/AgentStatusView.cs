using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 头顶状态文字：把角色的状态画成 "?" "!" 之类的符号，声音波及时会弹一下。
/// 接美术以后，这里可以换成 Animator 参数，逻辑不用动。
/// </summary>
public class AgentStatusView : MonoBehaviour
{
    [Tooltip("文字浮在多高")]
    public float height = 1.05f;
    public int fontSize = 44;
    public float worldScale = 0.0045f;
    [Tooltip("被声音波及时弹大的幅度")]
    public float pulseAmount = 0.4f;

    private IAgentStatus _status;
    private Canvas _canvas;
    private Text _text;
    private float _lastHeard = -999f;
    private float _pulse;

    private void Awake()
    {
        _status = GetComponent<IAgentStatus>();
        Build();
    }

    private void Build()
    {
        var go = new GameObject("StatusCanvas");
        go.transform.SetParent(transform, false);

        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 200;

        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(260f, 90f);
            rt.localScale = Vector3.one * worldScale;
        }

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);

        _text = textGo.AddComponent<Text>();
        _text.font = LoadFont();
        _text.fontSize = fontSize;
        _text.alignment = TextAnchor.MiddleCenter;
        _text.horizontalOverflow = HorizontalWrapMode.Overflow;
        _text.verticalOverflow = VerticalWrapMode.Overflow;
        _text.raycastTarget = false;
        _text.color = Color.white;

        var trt = textGo.GetComponent<RectTransform>();
        if (trt != null)
        {
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }
    }

    private static Font LoadFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { /* 老版本叫 Arial.ttf */ }

        if (f == null)
        {
            try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            catch { /* 实在没有就算了 */ }
        }

        return f;
    }

    private void LateUpdate()
    {
        if (_canvas == null || _text == null || _status == null) return;

        _canvas.transform.position = transform.position + Vector3.up * height;
        _canvas.transform.rotation = Quaternion.identity;   // 不跟着角色转

        _text.text = _status.StatusText;
        _text.color = _status.StatusColor;

        if (_status.LastHeardTime > _lastHeard + 0.0001f)
        {
            _lastHeard = _status.LastHeardTime;
            _pulse = 1f;
        }

        if (_pulse > 0f) _pulse = Mathf.Max(0f, _pulse - Time.deltaTime * 2.5f);

        _canvas.transform.localScale = Vector3.one * worldScale * (1f + _pulse * pulseAmount);
    }
}
