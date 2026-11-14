using UnityEngine;

/// <summary>
/// 魔女的移动。只做一件事：朝目标点直线走过去。
/// 因为声音被墙挡住，能听见就说明直线是通的，所以这里不需要寻路。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WitchMotor : MonoBehaviour
{
    public float speed = 2.3f;
    [Tooltip("离目标多近算到了")]
    public float arriveRadius = 0.15f;
    public Rigidbody2D rb;

    [Tooltip("勾上后魔女会转向移动方向（以后接动画用）")]
    public bool faceMoveDirection = true;

    public bool HasDestination { get; private set; }
    public Vector2 Destination { get; private set; }
    public Vector2 Facing { get; private set; } = Vector2.right;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void MoveTo(Vector2 target)
    {
        Destination = target;
        HasDestination = true;
    }

    public void Stop()
    {
        HasDestination = false;
        if (rb != null) rb.velocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!HasDestination || rb == null) return;

        Vector2 delta = Destination - rb.position;
        if (delta.magnitude <= arriveRadius)
        {
            Stop();
            return;
        }

        Vector2 dir = delta.normalized;
        rb.velocity = dir * speed;
        Facing = dir;

        if (faceMoveDirection) transform.up = Facing;
    }
}
