using UnityEngine;

/// <summary>
/// 第一关的巡逻守卫：在两个（或更多）点之间来回走，听到声音就去看一眼。
/// 不会攻击狗，也不会喊人——这些留给以后的子类。
/// </summary>
public class EnemyGuard : EnemyBase
{
    [Header("巡逻点（按顺序来回走）")]
    public Transform[] patrolPoints;

    [Tooltip("走到一个点后停多久")]
    public float waitAtPoint = 0.8f;

    private int _index;
    private float _waitTimer;

    protected override Vector2 PostPosition
    {
        get
        {
            if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] != null)
                return patrolPoints[0].position;
            return transform.position;
        }
    }

    protected override void TickPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Stop();
            return;
        }

        int len = patrolPoints.Length;
        if (patrolPoints[_index] == null)
        {
            _index = (_index + 1) % len;
            return;
        }

        if (MoveTowards(patrolPoints[_index].position))
        {
            _waitTimer += Time.fixedDeltaTime;
            if (_waitTimer >= waitAtPoint)
            {
                _waitTimer = 0f;
                _index = (_index + 1) % len;
            }
        }
    }
}
