using UnityEngine;

/// <summary>
/// 盲人：接收狗画好的音轨，先直线走到起点，再沿线走；走到终点后通知画笔解锁。
/// </summary>
[RequireComponent(typeof(PathFollower))]
public class BlindMan : MonoBehaviour
{
    [Tooltip("玩家身上的 PathDrawer；留空会自动在场景里找第一个")]
    [SerializeField] private PathDrawer drawer;

    private PathFollower _follower;

    private void Awake()
    {
        _follower = GetComponent<PathFollower>();

        if (drawer == null)
        {
            drawer = FindObjectOfType<PathDrawer>();
            if (drawer == null)
                Debug.LogWarning($"[{name}] 场景里找不到 PathDrawer，盲人不会动", this);
        }
    }

    private void OnEnable()
    {
        if (drawer != null) drawer.TrackSubmitted += OnTrackSubmitted;
        if (_follower != null) _follower.Finished += OnArrived;
    }

    // 一定要退订：否则对象销毁/重开后旧引用还在，会重复触发
    private void OnDisable()
    {
        if (drawer != null) drawer.TrackSubmitted -= OnTrackSubmitted;
        if (_follower != null) _follower.Finished -= OnArrived;
    }

    private void OnTrackSubmitted(TrackData track)
    {
        // 万一音轨无效，立刻把画笔还回去，否则玩家会被锁死
        if (!_follower.Follow(track)) ReleaseDrawer();
    }

    private void OnArrived()
    {
        ReleaseDrawer();
    }

    private void ReleaseDrawer()
    {
        if (drawer != null) drawer.Unlock();
    }
}
