using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一种“墨水”：颜色、线宽、盲人走这一段时的速度倍率，以及（预留）可通行性校验用的阻挡层。
/// 这是值类型，会被复制进段里，所以画完以后改调色板不会影响已经画好的音轨。
/// </summary>
[Serializable]
public struct TrackStyle
{
    public string name;
    public Color color;
    public float width;
    [Tooltip("盲人走这一段时的速度倍率")]
    public float speedMultiplier;
    [Tooltip("预留：以后做可通行性校验时，这一段算作障碍的图层")]
    public LayerMask blockedMask;
}

/// <summary>
/// 一段连续的墨水：从路径的第 from 米开始，到下一段的 from 为止。
/// 不存 to —— 下一段的 from 就是这一段的结束，最后一段自然延伸到终点。
/// </summary>
[Serializable]
public struct StyleSpan
{
    public float from;
    public TrackStyle style;
}

/// <summary>
/// 一条完整的音轨 = 一条几何路径 + 若干段墨水。
/// 由 PathDrawer 画完后一次性交给 BlindMan，之后不再修改。
/// </summary>
[Serializable]
public class TrackData
{
    public PathData path = new PathData();
    public List<StyleSpan> spans = new List<StyleSpan>();

    public float TotalLength => path.TotalLength;
    public bool IsValid => path != null && path.IsValid;

    /// <summary>在弧长 distance 处追加（或改写）一段墨水的起点。from 必须递增。</summary>
    public void AddSpan(float from, TrackStyle style)
    {
        // 没移动就换了两次墨水 → 合并，别产生零长度的段
        if (spans.Count > 0 && Mathf.Approximately(spans[spans.Count - 1].from, from))
        {
            StyleSpan last = spans[spans.Count - 1];
            last.style = style;
            spans[spans.Count - 1] = last;
            return;
        }

        spans.Add(new StyleSpan { from = from, style = style });
    }

    /// <summary>取弧长 distance 处的墨水。</summary>
    public TrackStyle GetStyle(float distance)
    {
        if (spans.Count == 0)
            return new TrackStyle { color = Color.white, width = 0.05f, speedMultiplier = 1f };

        TrackStyle result = spans[0].style;
        for (int i = 1; i < spans.Count; i++)
        {
            if (spans[i].from <= distance) result = spans[i].style;
            else break;
        }
        return result;
    }
}
