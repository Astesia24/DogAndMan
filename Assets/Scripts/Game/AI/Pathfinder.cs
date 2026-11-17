using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网格 A*（8 方向，不许贴墙角斜穿）。守卫的巡逻 / 调查 / 追击 / 搜索 / 回岗全走这里。
/// 魔女不用——她只朝"听得见的声源"走，而被墙挡住的声音她听不见，所以直线一定通。
///
/// 性能上的三个处理：
///   1. 拉直用 Bresenham 走格子，不再按 0.25 米密集采样
///   2. 拉直时最多往前看 12 个路点，避免 O(k²) 的往返试探
///   3. 堆是动态扩容的，不会因为节点被多次改进而越界
/// </summary>
public static class Pathfinder
{
    private static float[] _g;
    private static int[] _came;
    private static bool[] _closed;
    private static int[] _heap;
    private static float[] _heapF;
    private static int _heapCount;
    private static int _cap;

    /// <summary>拉直时最多往前看多少个路点（约 6 米）。</summary>
    private const int SmoothLookahead = 12;

    /// <summary>贴墙格子的额外代价，越大越不愿意贴墙。</summary>
    private const float WallPenalty = 0.9f;

    /// <summary>离墙几格以内开始加代价。</summary>
    private const int ClearanceAware = 3;

    public static List<Vector2> Find(LevelGrid grid, Vector2 from, Vector2 to)
    {
        if (grid == null || !grid.Ready) return null;

        int w = grid.Width, h = grid.Height;
        int n = w * h;
        if (_cap < n)
        {
            _cap = n;
            _g = new float[n];
            _came = new int[n];
            _closed = new bool[n];
            _heap = new int[n * 2];
            _heapF = new float[n * 2];
        }

        Vector2Int startC = grid.NearestWalkable(from);
        Vector2Int goalC = grid.NearestWalkable(to);

        int start = startC.y * w + startC.x;
        int goal = goalC.y * w + goalC.x;

        Vector2Int rawGoal = grid.WorldToCell(to);
        bool goalExact = grid.Walkable(rawGoal.x, rawGoal.y);

        if (start == goal)
            return new List<Vector2> { goalExact ? to : grid.CellCenter(goalC.x, goalC.y) };

        for (int i = 0; i < n; i++)
        {
            _g[i] = float.MaxValue;
            _closed[i] = false;
            _came[i] = -1;
        }

        _heapCount = 0;
        _g[start] = 0f;
        HeapPush(start, Heuristic(startC, goalC));

        bool found = false;
        int loops = 0;

        while (_heapCount > 0 && loops++ < 20000)
        {
            int cur = HeapPop();
            if (cur == goal) { found = true; break; }
            if (_closed[cur]) continue;
            _closed[cur] = true;

            int cx = cur % w;
            int cy = cur / w;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = cx + dx, ny = cy + dy;
                    if (!grid.Walkable(nx, ny)) continue;
                    // 不许贴着墙角斜穿
                    if (dx != 0 && dy != 0 && (!grid.Walkable(cx + dx, cy) || !grid.Walkable(cx, cy + dy))) continue;

                    int ni = ny * w + nx;
                    if (_closed[ni]) continue;

                    bool diagonal = dx != 0 && dy != 0;

                    // 基础步长 + 贴墙代价：贴着墙的格子走起来更"贵"，
                    // 这样路径自然会走走廊中间，不会每面墙都蹭过去
                    float stepCost = diagonal ? 1.4142f : 1f;

                    int clearance = grid.Clearance(nx, ny);
                    if (clearance < ClearanceAware)
                    {
                        stepCost += WallPenalty * (1f - (float)clearance / ClearanceAware);
                    }

                    float ng = _g[cur] + stepCost;
                    if (ng >= _g[ni]) continue;

                    _g[ni] = ng;
                    _came[ni] = cur;
                    HeapPush(ni, ng + Heuristic(new Vector2Int(nx, ny), goalC));
                }
            }
        }

        if (!found) return null;

        var rev = new List<Vector2>();
        int node = goal;
        while (node != start && node >= 0)
        {
            int cx = node % w;
            int cy = node / w;
            rev.Add(grid.CellCenter(cx, cy));
            node = _came[node];
        }
        rev.Reverse();

        if (goalExact && rev.Count > 0) rev[rev.Count - 1] = to;

        return Smooth(grid, from, rev);
    }

    /// <summary>能直线看到的路点就跳过中间那些，走起来不会一格一格拐。</summary>
    private static List<Vector2> Smooth(LevelGrid grid, Vector2 from, List<Vector2> pts)
    {
        if (pts.Count <= 1) return pts;

        var outPts = new List<Vector2>(pts.Count);
        Vector2 cur = from;
        int i = 0;

        while (i < pts.Count)
        {
            // 只往前看固定步数，避免长路径上的反复试探
            int j = Mathf.Min(pts.Count - 1, i + SmoothLookahead);
            while (j > i && !ClearLine(grid, cur, pts[j])) j--;

            outPts.Add(pts[j]);
            cur = pts[j];
            i = j + 1;
        }
        return outPts;
    }

    /// <summary>
    /// 两格之间是否有一条畅通的直线。
    /// 用 Bresenham 走格子（每步只查一格），不再按 0.25 米密集采样。
    /// 斜走时会额外检查两个正交格，防止贴墙角切过去。
    /// </summary>
    private static bool ClearLine(LevelGrid grid, Vector2 a, Vector2 b)
    {
        Vector2Int c0 = grid.WorldToCell(a);
        Vector2Int c1 = grid.WorldToCell(b);

        int dx = Mathf.Abs(c1.x - c0.x);
        int dy = Mathf.Abs(c1.y - c0.y);
        int sx = c0.x < c1.x ? 1 : -1;
        int sy = c0.y < c1.y ? 1 : -1;
        int err = dx - dy;

        int x = c0.x, y = c0.y;

        for (int guard = 0; guard < 512; guard++)
        {
            if (!grid.Walkable(x, y)) return false;
            if (x == c1.x && y == c1.y) return true;

            int e2 = 2 * err;
            bool stepX = e2 > -dy;
            bool stepY = e2 < dx;

            if (stepX && stepY)
            {
                if (!grid.Walkable(x + sx, y) || !grid.Walkable(x, y + sy)) return false;
            }

            if (stepX) { err -= dy; x += sx; }
            if (stepY) { err += dx; y += sy; }
        }
        return false;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx + dy + (1.4142f - 2f) * Mathf.Min(dx, dy);
    }


    private static void GrowHeap()
    {
        int cap = Mathf.Max(16, _heap.Length * 2);
        var nh = new int[cap];
        var nf = new float[cap];
        System.Array.Copy(_heap, nh, _heapCount);
        System.Array.Copy(_heapF, nf, _heapCount);
        _heap = nh;
        _heapF = nf;
    }

    private static void HeapPush(int node, float f)
    {
        if (_heapCount >= _heap.Length) GrowHeap();

        int i = _heapCount++;
        _heap[i] = node;
        _heapF[i] = f;

        while (i > 0)
        {
            int p = (i - 1) >> 1;
            if (_heapF[p] <= _heapF[i]) break;
            Swap(p, i);
            i = p;
        }
    }

    private static int HeapPop()
    {
        int top = _heap[0];
        _heapCount--;

        if (_heapCount > 0)
        {
            _heap[0] = _heap[_heapCount];
            _heapF[0] = _heapF[_heapCount];

            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < _heapCount && _heapF[l] < _heapF[m]) m = l;
                if (r < _heapCount && _heapF[r] < _heapF[m]) m = r;
                if (m == i) break;
                Swap(i, m);
                i = m;
            }
        }
        return top;
    }

    private static void Swap(int a, int b)
    {
        int tn = _heap[a]; _heap[a] = _heap[b]; _heap[b] = tn;
        float tf = _heapF[a]; _heapF[a] = _heapF[b]; _heapF[b] = tf;
    }
}
