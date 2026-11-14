using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一条世界坐标下的折线路径。起点 = Points[0]，终点 = Points[Count-1]。
/// 只追加、不插入；内部用累计弧长做 O(log n) 的“按距离取点”。
/// </summary>
[Serializable]
public class PathData
{
    [SerializeField] private List<Vector2> points = new List<Vector2>();

    private float[] _cumulative;   // _cumulative[i] = 从起点走到 points[i] 的弧长
    private float _totalLength;
    private bool _cacheDirty = true;

    public int Count => points.Count;
    public bool IsValid => points.Count >= 2;
    public Vector2 Start => points.Count > 0 ? points[0] : Vector2.zero;
    public Vector2 End => points.Count > 0 ? points[points.Count - 1] : Vector2.zero;
    public IReadOnlyList<Vector2> Points => points;

    public void Clear()
    {
        points.Clear();
        _cumulative = null;
        _totalLength = 0f;
        _cacheDirty = true;
    }

    /// <summary>追加一个点；离上一个点太近就丢掉，避免点过密。返回是否真的加进去了。</summary>
    public bool Add(Vector2 p, float minDistance = 0.05f)
    {
        if (points.Count > 0)
        {
            Vector2 last = points[points.Count - 1];
            if ((p - last).sqrMagnitude < minDistance * minDistance) return false;
        }

        points.Add(p);
        _cacheDirty = true;
        return true;
    }

    public float TotalLength
    {
        get
        {
            BuildCache();
            return _totalLength;
        }
    }

    /// <summary>按弧长取线上的点，超出范围自动 clamp 到两端。</summary>
    public Vector2 GetPointAtDistance(float distance)
    {
        if (points.Count == 0) return Vector2.zero;

        BuildCache();
        if (points.Count == 1 || distance <= 0f) return points[0];
        if (distance >= _totalLength) return points[points.Count - 1];

        int i = Array.BinarySearch(_cumulative, distance);
        if (i < 0) i = ~i - 1;                       // 落在第 i 段内部
        i = Mathf.Clamp(i, 0, points.Count - 2);

        float segLength = _cumulative[i + 1] - _cumulative[i];
        float t = segLength > 1e-6f ? (distance - _cumulative[i]) / segLength : 0f;
        return Vector2.Lerp(points[i], points[i + 1], t);
    }

    /// <summary>按弧长取切线方向（已归一化），用于朝向。</summary>
    public Vector2 GetDirectionAtDistance(float distance)
    {
        if (points.Count < 2) return Vector2.right;

        BuildCache();
        float d = Mathf.Clamp(distance, 0f, _totalLength - 1e-4f);

        int i = Array.BinarySearch(_cumulative, d);
        if (i < 0) i = ~i - 1;
        i = Mathf.Clamp(i, 0, points.Count - 2);

        Vector2 dir = points[i + 1] - points[i];
        return dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector2.right;
    }

    private void BuildCache()
    {
        if (!_cacheDirty) return;

        int n = points.Count;
        if (_cumulative == null || _cumulative.Length != n) _cumulative = new float[n];

        float sum = 0f;
        if (n > 0) _cumulative[0] = 0f;

        for (int i = 1; i < n; i++)
        {
            sum += Vector2.Distance(points[i - 1], points[i]);
            _cumulative[i] = sum;
        }

        _totalLength = sum;
        _cacheDirty = false;
    }

    /// <summary>画完一笔后一定要传副本：下一笔会 Clear 掉原对象。</summary>
    public PathData Clone()
    {
        return new PathData { points = new List<Vector2>(points) };
    }
}
