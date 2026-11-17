using UnityEngine;

/// <summary>
/// 魔女的移动。只做一件事：朝目标点直线走过去。
/// 因为声音被墙挡住，能听见就说明直线是通的，所以这里不需要寻路。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WitchMotor : MonoBehaviour
{
    public float speed = 2.4f;
    [Tooltip("离目标多近算到了")]
    public float arriveRadius = 0.15f;
    public Rigidbody2D rb;

    [Tooltip("勾上后魔女会转向移动方向（以后接动画用）")]
    public bool faceMoveDirection = true;

    public bool HasDestination { get; private set; }
    public Vector2 Destination { get; private set; }
    public Vector2 Facing { get; private set; } = Vector2.right;

    [Header("卡住检测")]
    [Tooltip("离目标距离要缩短这么多才算有进展")]
    public float progressEpsilon = 0.03f;
    [Tooltip("连续多久没有靠近目标就放弃")]
    public float stuckGiveUpTime = 0.6f;

    private Vector2 _lastPos;
    private float _stuckTime;
    private float _bestDistance = float.MaxValue;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        _lastPos = rb.position;
    }

    public void MoveTo(Vector2 target)
    {
        Destination = target;
        HasDestination = true;
        _stuckTime = 0f;
        _lastPos = rb != null ? rb.position : (Vector2)transform.position;
        _bestDistance = float.MaxValue;
    }

    public void Stop()
    {
        HasDestination = false;
        _stuckTime = 0f;
        if (rb != null) rb.velocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (GameplayLock.IsLocked)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            _stuckTime = 0f;
            return;
        }

        if (!HasDestination || rb == null) return;

        Vector2 pos = rb.position;
        Vector2 delta = Destination - pos;
        float dist = delta.magnitude;

        if (dist <= arriveRadius)
        {
            Stop();
            return;
        }

        // 关键：看"离目标的距离"有没有在缩短，而不是"有没有在动"。
        // 贴着狗洞 / 墙蹭的时候人是在动的，但距离不缩短，旧的判据会一直清零。
        if (dist < _bestDistance - progressEpsilon)
        {
            _bestDistance = dist;
            _stuckTime = 0f;
        }
        else
        {
            _stuckTime += Time.fixedDeltaTime;
            if (_stuckTime >= stuckGiveUpTime)
            {
                Stop();
                return;
            }
        }

        _lastPos = pos;

        Vector2 dir = delta / dist;
        rb.velocity = dir * speed;
        Facing = dir;

        if (faceMoveDirection) transform.up = Facing;
    }
}
