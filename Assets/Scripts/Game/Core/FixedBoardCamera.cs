using UnityEngine;

/// <summary>
/// 固定相机：按"地图外框尺寸"自动算 orthographicSize，
/// 保证任何窗口比例下整张地图都完整可见（宁可上下留边，也不切掉内容）。
/// 第一关用这个就够了；等关卡大于一屏，再换成跟随相机。
/// </summary>
[RequireComponent(typeof(Camera))]
public class FixedBoardCamera : MonoBehaviour
{
    [Header("地图外框（含墙），单位：米")]
    public Vector2 boardSize = new Vector2(21f, 12f);

    [Tooltip("四周留白，0 = 地图贴满屏幕")]
    public float margin = 0.4f;

    [Header("位置")]
    public bool centerOnBoard = true;
    [Tooltip("地图中心的世界坐标")]
    public Vector2 boardCenter = Vector2.zero;

    private Camera _cam;
    private float _lastAspect = -1f;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        Apply();
    }

    // 在编辑器里改参数立刻生效，方便摆图
    private void OnValidate()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        Apply();
    }

    private void Update()
    {
        // 窗口比例变了就重算，否则拉窗口会把地图切掉
        if (!Mathf.Approximately(_lastAspect, _cam.aspect))
        {
            _lastAspect = _cam.aspect;
            Apply();
        }
    }

    public void Apply()
    {
        if (_cam == null || !_cam.orthographic) return;

        float w = boardSize.x + margin * 2f;
        float h = boardSize.y + margin * 2f;

        float sizeByHeight = h * 0.5f;
        float sizeByWidth = w / (2f * Mathf.Max(0.01f, _cam.aspect));

        _cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);

        if (centerOnBoard)
        {
            Vector3 p = transform.position;
            transform.position = new Vector3(boardCenter.x, boardCenter.y, p.z);
        }
    }
}
