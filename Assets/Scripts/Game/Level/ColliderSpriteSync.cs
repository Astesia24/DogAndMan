using UnityEngine;

/// <summary>
/// 让 SpriteRenderer 的显示尺寸永远等于 Collider 的尺寸。
/// 改 Collider 就自动跟着变，不会再有"图和碰撞体对不上"的问题。
///
/// 注意：不要再用 Transform 的 Scale 去凑大小，那会连碰撞体一起缩放。
/// 另外这里不能在 OnValidate / Awake 里直接改 size —— Unity 会报
/// "SendMessage cannot be called during Awake, CheckConsistency, or OnValidate"，
/// 所以编辑器里改成下一帧再同步。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class ColliderSpriteSync : MonoBehaviour
{
    [Tooltip("想让图形比碰撞体大一点就填正数（比如 0.05）")]
    public Vector2 padding = Vector2.zero;

    private void Start()
    {
        Sync();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 延迟到下一帧，避开 SendMessage 的限制
            UnityEditor.EditorApplication.delayCall -= DeferredSync;
            UnityEditor.EditorApplication.delayCall += DeferredSync;
            return;
        }
#endif
        Sync();
    }

#if UNITY_EDITOR
    private void DeferredSync()
    {
        if (this == null) return;   // 对象可能已经被删了
        Sync();
    }
#endif

    /// <summary>按当前 Collider 重新设置 Sprite 的显示尺寸。</summary>
    [ContextMenu("立即同步")]
    public void Sync()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Vector2 target;
        if (!TryGetTargetSize(out target)) return;
        target += padding;

        // 值没变就别赋，避免触发 Unity 的内部回调
        if (sr.drawMode != SpriteDrawMode.Sliced) sr.drawMode = SpriteDrawMode.Sliced;
        if (sr.size != target) sr.size = target;
    }

    private bool TryGetTargetSize(out Vector2 size)
    {
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            size = box.size;
            return true;
        }

        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            size = Vector2.one * (circle.radius * 2f);
            return true;
        }

        size = Vector2.one;
        return false;
    }
}
