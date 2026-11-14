using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音轨的“画笔”。一段墨水一条 LineRenderer，切换墨水就新开一条线。
/// 自己身上的 LineRenderer 只当模板（存材质、排序层），不参与渲染。
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class TrackVisual : MonoBehaviour
{
    [Tooltip("模板线：只用来存材质/排序层等设置，本身不渲染")]
    public LineRenderer template;

    [Tooltip("最多同时存在几条线，超过以后视觉上合并到当前这条（数据里仍然是分段的）")]
    public int maxSegments = 32;

    private readonly List<LineRenderer> _lines = new List<LineRenderer>();
    private LineRenderer _current;
    private int _used;

    private void Awake()
    {
        if (template == null) template = GetComponent<LineRenderer>();

        template.useWorldSpace = true;
        template.positionCount = 0;
        template.enabled = false;

        if (template.sharedMaterial == null)
        {
            Debug.LogWarning("[TrackVisual] 模板 LineRenderer 没有指定材质，画出来的线会看不见", this);
        }
    }

    /// <summary>开始一段新音轨（会清掉上一条）。</summary>
    public void Begin(TrackStyle style, Vector2 p)
    {
        ClearAll();

        _current = NextLine();
        ApplyStyle(_current, style);
        _current.positionCount = 1;
        _current.SetPosition(0, p);
    }

    /// <summary>往当前这条线追加一个点。</summary>
    public void Append(Vector2 p)
    {
        if (_current == null) return;

        int n = _current.positionCount;
        _current.positionCount = n + 1;
        _current.SetPosition(n, p);
    }

    /// <summary>换墨水：结束当前线，从 from 这个点开始一条新线。</summary>
    public void NewSegment(TrackStyle style, Vector2 from)
    {
        if (_used >= maxSegments && _current != null)
        {
            // 段数到上限，视觉上继续沿用当前这条线（数据里仍然分段，玩法不受影响）
            return;
        }

        _current = NextLine();
        ApplyStyle(_current, style);
        _current.positionCount = 1;
        _current.SetPosition(0, from);
    }

    /// <summary>清空所有线（音轨被消费掉之后调用）。</summary>
    public void ClearAll()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i] != null) _lines[i].positionCount = 0;
        }

        _current = null;
        _used = 0;
    }

    private LineRenderer NextLine()
    {
        if (_used >= _lines.Count) _lines.Add(CreateLine(_lines.Count));

        LineRenderer lr = _lines[_used];
        _used++;
        lr.positionCount = 0;
        return lr;
    }

    private LineRenderer CreateLine(int index)
    {
        GameObject go = new GameObject($"TrackSegment_{index}");
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;                       // 线挂在子物体上也要用世界坐标
        lr.sharedMaterial = template.sharedMaterial;
        lr.sortingLayerID = template.sortingLayerID;
        lr.sortingOrder = template.sortingOrder;
        lr.numCapVertices = template.numCapVertices;
        lr.numCornerVertices = template.numCornerVertices;
        lr.textureMode = template.textureMode;
        lr.alignment = template.alignment;
        lr.positionCount = 0;
        return lr;
    }

    private void ApplyStyle(LineRenderer lr, TrackStyle style)
    {
        lr.widthMultiplier = Mathf.Max(0.001f, style.width);
        lr.startColor = style.color;
        lr.endColor = style.color;
        lr.enabled = true;
    }
}
