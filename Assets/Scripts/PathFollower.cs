using System;
using UnityEngine;

/// <summary>
/// 给一条音轨：先直线走到起点，再沿路径按弧长移动；每一段按各自的墨水决定速度。
/// Rigidbody2D 建议 Body Type = Kinematic、Gravity Scale = 0。
/// 以后要接真正的寻路（A* / NavMesh），只改 TickSeekStart 即可。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PathFollower : MonoBehaviour
{
    public enum Phase
    {
        Idle,
        SeekStart,   // 直线赶往音轨起点
        FollowPath,  // 沿音轨移动
        Arrived
    }

    [Header("移动")]
    [Tooltip("基础速度，实际速度 = 基础速度 × 当前段的速度倍率")]
    public float baseSpeed = 3f;
    [Tooltip("距离起点多近算“已经到了”，直接切到沿线阶段")]
    public float seekArriveRadius = 0.05f;
    [Tooltip("离终点多近算走完全程")]
    public float finishTolerance = 0.02f;

    [Header("朝向（可选）")]
    public bool faceMoveDirection = false;
    [Tooltip("<= 0 表示瞬间转向")]
    public float turnSpeedDeg = 720f;

    [Header("引用")]
    public Rigidbody2D rb;

    /// <summary>走完全程时触发。</summary>
    public event Action Finished;

    public Phase CurrentPhase { get; private set; } = Phase.Idle;
    public TrackData Track { get; private set; }
    public bool IsFollowing => _hasPath;

    private float _travel;      // 已沿路径走过的弧长
    private bool _hasPath;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // 最常见的配置错误：Dynamic + 有重力，MovePosition 会被重力拽着走
        if (rb.bodyType == RigidbodyType2D.Dynamic && Mathf.Abs(rb.gravityScale) > 0.001f)
        {
            Debug.LogWarning(
                $"[{name}] PathFollower 建议把 Rigidbody2D 设成 Kinematic、Gravity Scale = 0，" +
                $"当前是 Dynamic 且有重力，可能会被重力拽走", this);
        }
    }

    /// <summary>开始追随一条音轨（会先直线走到起点）。返回是否接受。</summary>
    public bool Follow(TrackData track)
    {
        if (track == null || !track.IsValid)
        {
            Debug.LogWarning($"[{name}] 音轨无效，忽略", this);
            return false;
        }

        Track = track;
        _travel = 0f;
        _hasPath = true;

        // 起点本来就在脚下 → 直接进入沿线阶段，否则会在起点附近原地抽搐
        float distToStart = Vector2.Distance(rb.position, track.path.Start);
        float snapRadius = Mathf.Max(seekArriveRadius, baseSpeed * Time.fixedDeltaTime);
        CurrentPhase = distToStart <= snapRadius ? Phase.FollowPath : Phase.SeekStart;

        return true;
    }

    /// <summary>中断追随。</summary>
    public void Stop()
    {
        _hasPath = false;
        Track = null;
        CurrentPhase = Phase.Idle;

        if (rb != null) rb.velocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!_hasPath || Track == null || baseSpeed <= 0f) return;

        switch (CurrentPhase)
        {
            case Phase.SeekStart:
                TickSeekStart();
                break;

            case Phase.FollowPath:
                TickFollowPath();
                break;
        }
    }

    // ↓↓↓ 以后要换成真正的寻路，只需要改这一个方法
    private void TickSeekStart()
    {
        Vector2 pos = rb.position;
        Vector2 delta = Track.path.Start - pos;
        float step = baseSpeed * Time.fixedDeltaTime;

        if (delta.magnitude <= step)      // 这一步就能踩到起点，直接吸附，避免在起点附近抖动
        {
            MoveTo(Track.path.Start, delta);
            CurrentPhase = Phase.FollowPath;
            _travel = 0f;
            return;
        }

        MoveTo(pos + delta.normalized * step, delta);
    }

    private void TickFollowPath()
    {
        float total = Track.TotalLength;

        // 当前这一段墨水决定走多快
        TrackStyle style = Track.GetStyle(_travel);
        float speed = baseSpeed * Mathf.Max(0.05f, style.speedMultiplier);
        float step = speed * Time.fixedDeltaTime;

        _travel = Mathf.Min(_travel + step, total);

        Vector2 next = Track.path.GetPointAtDistance(_travel);
        Vector2 dir = Track.path.GetDirectionAtDistance(_travel);
        MoveTo(next, dir);

        if (total - _travel <= finishTolerance)
        {
            _hasPath = false;
            CurrentPhase = Phase.Arrived;
            Track = null;

            Finished?.Invoke();
        }
    }

    private void MoveTo(Vector2 target, Vector2 dir)
    {
        rb.MovePosition(target);      // 走物理位移，不要直接改 transform.position

        if (!faceMoveDirection || dir.sqrMagnitude < 1e-8f) return;

        // 精灵默认朝上，所以 -90；如果你的图朝右就去掉这个偏移
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float angle = turnSpeedDeg <= 0f
            ? targetAngle
            : Mathf.MoveTowardsAngle(rb.rotation, targetAngle, turnSpeedDeg * Time.fixedDeltaTime);

        rb.MoveRotation(angle);
    }

    private void OnDisable()
    {
        if (rb != null) rb.velocity = Vector2.zero;
    }
}
