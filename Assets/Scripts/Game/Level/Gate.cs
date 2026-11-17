using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 闸门。
///
/// 关着（IsOpen == false）：碰撞体在 Wall 层，挡路、挡视线、挡声浪。
/// 开着（IsOpen == true） ：碰撞体关掉，并且移到 Ignore Raycast 层，
///                          守卫看得穿、声波穿得过、寻路也走得过去。
///
/// 由拉杆打开或关闭。
/// </summary>
public class Gate : MonoBehaviour
{
    [Tooltip("关门时挡路的碰撞体。必须放在 Wall 层，留空会自动用自己身上的碰撞体")]
    public Collider2D[] blockers;

    [Tooltip("门的视觉（可选）")]
    public SpriteRenderer visual;
    public Color closedColor = new Color(0.42f, 0.35f, 0.24f);
    public Color openColor = new Color(0.26f, 0.24f, 0.20f);

    [Tooltip("开门/关门后要重新烘焙的寻路网格（留空自动找）")]
    public LevelGrid grid;

    [Header("关门夹人")]
    [Tooltip("关上的瞬间夹死贴着门的敌人")]
    public bool crushOnClose = true;
    [Tooltip("敌人中心离门中心多近会被夹死（要小，别误伤）")]
    public float crushRadius = 0.55f;

    [Tooltip("开局是开着的（给「关门拉杆」用）")]
    public bool startOpen;

    public bool IsOpen { get; private set; }

    private int _solidLayer;
    private int _passLayer;
    private bool _stateRestored;

    private void Awake()
    {
        // 没拖碰撞体就用自己身上的
        if (blockers == null || blockers.Length == 0)
        {
            var col = GetComponent<Collider2D>();
            if (col != null) blockers = new[] { col };
        }

        _solidLayer = blockers != null && blockers.Length > 0 && blockers[0] != null
            ? blockers[0].gameObject.layer
            : gameObject.layer;

        _passLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (_passLayer < 0) _passLayer = 2;
    }

    private void Start()
    {
        if (grid == null) grid = FindObjectOfType<LevelGrid>();

        // 存档恢复已经设过状态了，别再被"开局默认值"覆盖
        if (_stateRestored) return;

        IsOpen = startOpen;
        Apply();
    }

    /// <summary>存档恢复用：直接设成某个状态，不触发夹人。</summary>
    public void RestoreState(bool open)
    {
        _stateRestored = true;
        IsOpen = open;
        Apply();
    }

    public void Open()
    {
        if (IsOpen) return;

        IsOpen = true;
        Apply();
    }

    public void Close()
    {
        if (!IsOpen) return;

        if (crushOnClose) Crush();

        IsOpen = false;
        Apply();
    }

    private void Crush()
    {
        var snapshot = new List<EnemyBase>(EnemyBase.All);

        foreach (var e in snapshot)
        {
            if (e == null) continue;
            if (Vector2.Distance(e.transform.position, transform.position) > crushRadius) continue;

            Destroy(e.gameObject);
        }
    }

    private void Apply()
    {
        // 开着的门要彻底透明，关着的门要彻底实心，别弄反
        bool solid = !IsOpen;

        if (blockers != null)
        {
            for (int i = 0; i < blockers.Length; i++)
            {
                var b = blockers[i];
                if (b == null) continue;

                b.enabled = solid;
                b.gameObject.layer = solid ? _solidLayer : _passLayer;
            }
        }

        if (visual != null) visual.color = IsOpen ? openColor : closedColor;

        // 开合之后寻路网格跟着更新，只重算门附近这一小块
        if (grid != null && grid.Ready)
        {
            Vector2 size = BlockSize() + Vector2.one;
            grid.UpdateArea(transform.position, size);
        }
    }

    private Vector2 BlockSize()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box != null) return box.size;

        var col = GetComponent<Collider2D>();
        return col != null ? col.bounds.size : new Vector2(3f, 3f);
    }
}
