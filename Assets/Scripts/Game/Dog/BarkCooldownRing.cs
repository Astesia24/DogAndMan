using UnityEngine;

/// <summary>
/// 狗脚下的圆环，现在显示的是肺活量：越短说明越喘不上气，满了就隐藏。
/// 位置和外观和以前的冷却环一样，只是数据来源换成了 DogBark.Stamina01。
/// </summary>
[RequireComponent(typeof(DogBark))]
public class BarkCooldownRing : MonoBehaviour
{
    [Header("外观")]
    public float radius = 0.42f;
    public float width = 0.05f;
    [Range(8, 96)] public int segments = 48;
    public Color color = new Color(0.47f, 0.86f, 1f, 0.9f);
    public int sortingOrder = 11;

    private DogBark _bark;
    private LineRenderer _line;

    private void Awake()
    {
        _bark = GetComponent<DogBark>();

        var go = new GameObject("BarkCooldown");
        go.transform.SetParent(transform, false);
        go.layer = gameObject.layer;

        _line = go.AddComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.loop = false;
        _line.widthMultiplier = width;
        _line.numCapVertices = 2;
        _line.numCornerVertices = 2;
        _line.sortingOrder = sortingOrder;
        _line.textureMode = LineTextureMode.Stretch;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader != null) _line.material = new Material(shader);

        _line.startColor = color;
        _line.endColor = color;
        _line.positionCount = 0;
    }

    private void LateUpdate()
    {
        if (_bark == null || _line == null) return;

        float p = Mathf.Clamp01(_bark.Stamina01);

        if (p >= 0.999f)
        {
            _line.positionCount = 0;
            return;
        }

        int n = Mathf.Max(2, Mathf.CeilToInt(segments * p));
        float sweep = Mathf.PI * 2f * p;
        Vector2 center = transform.position;

        _line.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float a = -Mathf.PI * 0.5f + sweep * (i / (float)(n - 1));
            _line.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(a) * radius,
                center.y + Mathf.Sin(a) * radius,
                0f));
        }
    }
}
