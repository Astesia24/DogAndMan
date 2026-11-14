using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 狗画音轨：按一下键开始，再按一下结束；绘制过程中换“墨水”，同一条音轨就能由多段不同效果组成。
/// 画完立刻交给接收者，并且锁住画笔，直到盲人走到终点才解锁。
/// </summary>
[RequireComponent(typeof(TrackVisual))]
public class PathDrawer : MonoBehaviour
{
    [Header("笔尖（拖玩家自己）")]
    public Transform penTip;

    [Header("按键")]
    [Tooltip("按一下开始，再按一下结束")]
    public KeyCode drawKey = KeyCode.Space;
    [Tooltip("下一支墨水")]
    public KeyCode nextStyleKey = KeyCode.Q;
    [Tooltip("上一支墨水")]
    public KeyCode prevStyleKey = KeyCode.E;

    [Header("墨水（可在 Inspector 里改颜色/宽度/速度倍率）")]
    public List<TrackStyle> styles = new List<TrackStyle>();

    [Header("采样")]
    [Tooltip("相邻采样点的最小间距，建议 0.05 ~ 0.1")]
    public float minPointDistance = 0.08f;
    [Tooltip("单条音轨的点数上限")]
    public int maxPoints = 1024;

    /// <summary>一笔画完、提交出去时触发。</summary>
    public event Action<TrackData> TrackSubmitted;

    public bool IsDrawing { get; private set; }
    /// <summary>盲人还在路上时为 true，这期间不能开始画下一条。</summary>
    public bool IsLocked { get; private set; }
    public int CurrentStyleIndex => _styleIndex;
    public TrackData CurrentTrack => _track;

    private TrackVisual _visual;
    private PathData _path;
    private TrackData _track;
    private int _styleIndex;
    private Vector2 _lastPen;

    // 在编辑器里添加组件时自动塞三支默认墨水，省得空列表
    private void Reset()
    {
        SeedDefaultStyles();
    }

    private void Awake()
    {
        _visual = GetComponent<TrackVisual>();
        if (_visual == null) _visual = gameObject.AddComponent<TrackVisual>();

        if (styles == null) styles = new List<TrackStyle>();
        if (styles.Count == 0) SeedDefaultStyles();

        _styleIndex = 0;
    }

    private void Update()
    {
        // 盲人没走到终点之前，画笔是锁死的
        if (IsLocked) return;

        if (Input.GetKeyDown(drawKey))
        {
            if (IsDrawing) EndStroke();
            else BeginStroke();
        }

        if (IsDrawing) HandleStyleInput();
    }

    // 采样放 LateUpdate：等 Rigidbody 插值算完再取位置，线才和屏幕上看到的一致
    private void LateUpdate()
    {
        if (!IsDrawing) return;
        SamplePen(GetPenPoint(), false);
    }

    // 防止“画到一半切场景/暂停”导致状态泄漏
    private void OnDisable()
    {
        AbortStroke();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) AbortStroke();
    }

    // ───────────────────────────── 对外接口 ─────────────────────────────

    /// <summary>盲人走到终点后调用，解锁画笔并清掉旧线。</summary>
    public void Unlock()
    {
        if (!IsLocked) return;

        IsLocked = false;
        _visual.ClearAll();
    }

    // ───────────────────────────── 内部流程 ─────────────────────────────

    private void BeginStroke()
    {
        _track = new TrackData();
        _path = _track.path;
        IsDrawing = true;

        Vector2 p = GetPenPoint();
        _path.Add(p, 0f);
        _lastPen = p;

        _track.AddSpan(0f, styles[_styleIndex]);
        _visual.Begin(styles[_styleIndex], p);
    }

    private void EndStroke()
    {
        if (!IsDrawing) return;
        IsDrawing = false;

        // 补最后一个点，避免抬手前最后一段丢失
        SamplePen(GetPenPoint(), true);

        TrackData track = _track;
        _track = null;
        _path = null;

        if (track == null || !track.IsValid)
        {
            // 太短（点一下就结束）直接作废，不消耗画笔
            _visual.ClearAll();
            return;
        }

        if (TrackSubmitted == null)
        {
            // 场景里没人接收（比如还没放盲人），就不要锁住，否则玩家会卡死
            Debug.LogWarning("[PathDrawer] 没有接收者，这条音轨作废", this);
            _visual.ClearAll();
            return;
        }

        IsLocked = true;
        TrackSubmitted.Invoke(track);
    }

    private void AbortStroke()
    {
        if (!IsDrawing) return;

        IsDrawing = false;
        _track = null;
        _path = null;
        _visual.ClearAll();
    }

    private void HandleStyleInput()
    {
        if (styles.Count <= 1) return;

        int wanted = _styleIndex;

        if (Input.GetKeyDown(nextStyleKey))
        {
            wanted = (_styleIndex + 1) % styles.Count;
        }
        else if (Input.GetKeyDown(prevStyleKey))
        {
            wanted = (_styleIndex - 1 + styles.Count) % styles.Count;
        }
        else
        {
            // 数字键 1~9 直接选
            int count = Mathf.Min(9, styles.Count);
            for (int i = 0; i < count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    wanted = i;
                    break;
                }
            }
        }

        if (wanted != _styleIndex) SetStyle(wanted);
    }

    private void SetStyle(int index)
    {
        index = Mathf.Clamp(index, 0, styles.Count - 1);
        _styleIndex = index;

        if (!IsDrawing) return;   // 没在画，只记住这支墨水，下一条用

        // 先在你按键的准确位置补一个点，让分段边界正好落在换墨水的那一刻
        SamplePen(GetPenPoint(), true);

        _track.AddSpan(_path.TotalLength, styles[_styleIndex]);
        _visual.NewSegment(styles[_styleIndex], _lastPen);
    }

    private void SamplePen(Vector2 p, bool force)
    {
        if (_path == null) return;

        if (!force && _path.Count >= maxPoints)
        {
            EndStroke();
            return;
        }

        // force = true 时忽略最小间距，保证分段边界精确
        if (!_path.Add(p, force ? 0f : minPointDistance)) return;

        _lastPen = p;
        _visual.Append(p);
    }

    private Vector2 GetPenPoint()
    {
        return penTip != null ? (Vector2)penTip.position : (Vector2)transform.position;
    }

    private void SeedDefaultStyles()
    {
        styles = new List<TrackStyle>
        {
            new TrackStyle { name = "轻步", color = new Color(0.55f, 0.9f, 1f, 1f), width = 0.06f, speedMultiplier = 1f },
            new TrackStyle { name = "急促", color = new Color(1f, 0.55f, 0.2f, 1f), width = 0.08f, speedMultiplier = 1.6f },
            new TrackStyle { name = "安抚", color = new Color(0.6f, 0.75f, 1f, 1f), width = 0.10f, speedMultiplier = 0.7f },
        };
    }
}
