using UnityEngine;

/// <summary>
/// 给守卫寻路用的网格。启动时从物理碰撞体烘焙一次，
/// 只要地图是静态的（墙、家具、关着的闸门）就够了。
/// </summary>
public class LevelGrid : MonoBehaviour
{
    [Tooltip("地图左下角的世界坐标")]
    public Vector2 origin = Vector2.zero;
    [Tooltip("地图尺寸（米）")]
    public Vector2 size = new Vector2(48f, 28f);
    [Tooltip("格子大小，0.5 米够用")]
    public float cellSize = 0.5f;
    [Tooltip("挡路的东西（墙 / 家具 / 闸门），不要包含角色")]
    public LayerMask blockers;
    public bool bakeOnAwake = true;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool Ready { get; private set; }

    private bool[] _walk;

    /// <summary>每格离最近阻挡格的距离（格数），用于给"贴墙"加寻路代价。</summary>
    private byte[] _clearance;

    private void Awake()
    {
        if (blockers.value == 0)
        {
            Debug.LogWarning("[LevelGrid] 没有设置 Blockers（墙 / 家具 / 闸门所在的层），寻路会把整张地图当成空地", this);
        }

        // 狗洞也要算阻挡，否则守卫会以为能从洞里穿过去、一头撞上去
        int hole = LayerMask.NameToLayer("DogHole");
        if (hole >= 0) blockers = blockers.value | (1 << hole);

        if (bakeOnAwake) Bake();
    }

    /// <summary>重新烘焙。闸门开关之后如果想立刻反映，可以再调一次。</summary>
    public void Bake()
    {
        Width = Mathf.Max(1, Mathf.CeilToInt(size.x / cellSize));
        Height = Mathf.Max(1, Mathf.CeilToInt(size.y / cellSize));
        _walk = new bool[Width * Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Vector2 c = CellCenter(x, y);
                var hit = Physics2D.OverlapCircle(c, cellSize * 0.42f, blockers);
                _walk[y * Width + x] = hit == null;
            }
        }

        ComputeClearance();

        Ready = true;
    }

    /// <summary>
    /// 只重算一块区域。闸门开关时用这个，比整张图重烘快几十倍。
    /// </summary>
    public void UpdateArea(Vector2 center, Vector2 areaSize)
    {
        if (!Ready || _walk == null)
        {
            Bake();
            return;
        }

        int x0 = Mathf.FloorToInt((center.x - areaSize.x * 0.5f - origin.x) / cellSize);
        int x1 = Mathf.CeilToInt((center.x + areaSize.x * 0.5f - origin.x) / cellSize);
        int y0 = Mathf.FloorToInt((center.y - areaSize.y * 0.5f - origin.y) / cellSize);
        int y1 = Mathf.CeilToInt((center.y + areaSize.y * 0.5f - origin.y) / cellSize);

        x0 = Mathf.Max(0, x0); x1 = Mathf.Min(Width - 1, x1);
        y0 = Mathf.Max(0, y0); y1 = Mathf.Min(Height - 1, y1);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                Vector2 c = CellCenter(x, y);
                var hit = Physics2D.OverlapCircle(c, cellSize * 0.42f, blockers);
                _walk[y * Width + x] = hit == null;
            }
        }

        ComputeClearance();
    }

    /// <summary>
    /// 从所有阻挡格出发做一次 BFS，标出每格离墙几格。
    /// A* 用它给贴着墙的格子加代价，路径就会自然走走廊中间。
    /// </summary>
    private void ComputeClearance()
    {
        int n = Width * Height;
        _clearance = new byte[n];

        var queue = new System.Collections.Generic.Queue<int>();

        for (int i = 0; i < n; i++)
        {
            if (_walk[i])
            {
                _clearance[i] = 255;
            }
            else
            {
                _clearance[i] = 0;
                queue.Enqueue(i);
            }
        }

        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            int c = _clearance[i] + 1;
            if (c >= 255) continue;

            int cx = i % Width;
            int cy = i / Width;

            if (cx > 0) Relax(i - 1, (byte)c, queue);
            if (cx < Width - 1) Relax(i + 1, (byte)c, queue);
            if (cy > 0) Relax(i - Width, (byte)c, queue);
            if (cy < Height - 1) Relax(i + Width, (byte)c, queue);
        }
    }

    private void Relax(int index, byte value, System.Collections.Generic.Queue<int> queue)
    {
        if (_clearance[index] <= value) return;

        _clearance[index] = value;
        queue.Enqueue(index);
    }

    /// <summary>这一格离最近阻挡格有几格（255 = 很远）。</summary>
    public int Clearance(int x, int y)
    {
        if (!InBounds(x, y) || _clearance == null) return 0;

        return _clearance[y * Width + x];
    }

    public Vector2 CellCenter(int x, int y)
    {
        return origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
    }

    public Vector2Int WorldToCell(Vector2 p)
    {
        return new Vector2Int(
            Mathf.FloorToInt((p.x - origin.x) / cellSize),
            Mathf.FloorToInt((p.y - origin.y) / cellSize));
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool Walkable(int x, int y)
    {
        return InBounds(x, y) && _walk != null && _walk[y * Width + x];
    }

    /// <summary>找离这个点最近的可走格（点落在墙里时用）。</summary>
    public Vector2Int NearestWalkable(Vector2 p, int maxRing = 6)
    {
        Vector2Int c = WorldToCell(p);
        if (Walkable(c.x, c.y)) return c;

        for (int ring = 1; ring <= maxRing; ring++)
        {
            for (int dy = -ring; dy <= ring; dy++)
            {
                for (int dx = -ring; dx <= ring; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring) continue;
                    if (Walkable(c.x + dx, c.y + dy)) return new Vector2Int(c.x + dx, c.y + dy);
                }
            }
        }
        return c;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireCube(origin + size * 0.5f, size);
    }
}
