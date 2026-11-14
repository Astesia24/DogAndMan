using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有敌人的基类。提供：听见声音、看见目标、巡逻/调查/追击/搜索/回岗的状态机。
/// 第一关的守卫只是"巡逻 + 去看声音"，不会攻击狗。
/// 以后要做"会咬狗的""会喊人的""会设陷阱的"，都继承它，只重写需要的虚方法。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour, ISoundListener, IAgentStatus
{
    public enum State
    {
        Patrol,      // 巡逻
        Investigate, // 听到声音，去查看
        Chase,       // 看见了，追
        Search,      // 跟丢了，去最后看到的位置找
        Return       // 回岗
    }

    public static readonly List<EnemyBase> All = new List<EnemyBase>();

    [Header("移动")]
    public float moveSpeed = 2f;

    [Header("感知")]
    public float visionRange = 5f;
    [Range(0f, 360f)] public float visionAngle = 80f;
    [Tooltip("视线被这些层挡住（留空自动用 Wall）")]
    public LayerMask obstacleMask;
    [Tooltip("能不能看见躲在草丛里的目标（第一关的守卫：不能）")]
    public bool canSeeConcealed = false;

    [Header("状态时长")]
    [Tooltip("走到声音位置后，原地看多久")]
    public float investigatePause = 2.5f;
    [Tooltip("追丢后，在最后位置找多久")]
    public float searchPause = 3f;

    [Header("抓捕")]
    [Tooltip("靠多近算抓住（只在追/找/查看状态下生效）")]
    public float catchRadius = 0.7f;

    [Header("引用")]
    public Transform witch;
    public Rigidbody2D rb;

    public State CurrentState { get; private set; } = State.Patrol;
    public event Action<State> StateChanged;

    public float LastHeardTime { get; private set; } = -999f;

    public Vector2 SoundPosition => transform.position;
    public bool CanHear => true;

    // ── IAgentStatus ────────────────────────────────────────────────
    public string StatusText
    {
        get
        {
            switch (CurrentState)
            {
                case State.Chase: return "!";
                case State.Investigate: return "?";
                case State.Search: return "??";
                case State.Return: return "<<";
                default: return "..";
            }
        }
    }

    public Color StatusColor
    {
        get
        {
            if (IsMarked) return new Color(0.4f, 0.95f, 1f);
            switch (CurrentState)
            {
                case State.Chase: return new Color(1f, 0.25f, 0.25f);
                case State.Investigate: return new Color(1f, 0.85f, 0.3f);
                case State.Search: return new Color(1f, 0.6f, 0.2f);
                default: return new Color(0.85f, 0.3f, 0.3f);
            }
        }
    }

    private Vector2 _facing = Vector2.right;
    private Vector2 _lookAt;
    private float _stateTimer;
    private bool _arrived;
    private float _markedUntil;
    private WitchBrain _witchBrain;

    public bool IsMarked => Time.time < _markedUntil;

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (obstacleMask.value == 0) obstacleMask = LayerMask.GetMask("Wall");
        if (obstacleMask.value == 0)
        {
            Debug.LogWarning($"[{name}] 没有设置遮挡层，也没有 Wall 层，敌人会隔墙看见人", this);
        }

        ResolveWitch();
        if (!All.Contains(this)) All.Add(this);
    }

    protected virtual void OnDisable()
    {
        All.Remove(this);
        if (rb != null) rb.velocity = Vector2.zero;
    }

    private void ResolveWitch()
    {
        if (witch != null) return;

        _witchBrain = FindObjectOfType<WitchBrain>();
        if (_witchBrain != null) witch = _witchBrain.transform;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.LevelState.Playing)
        {
            Stop();
            return;
        }

        if (witch == null) ResolveWitch();

        _stateTimer += Time.fixedDeltaTime;

        bool sees = CanSeeWitch();
        if (sees)
        {
            _lookAt = witch.position;
            if (CurrentState != State.Chase && CurrentState != State.Investigate) SetState(State.Chase);
        }

        switch (CurrentState)
        {
            case State.Patrol:
                TickPatrol();
                if (sees) SetState(State.Chase);
                break;

            case State.Investigate:
                TickInvestigate(sees);
                break;

            case State.Chase:
                TickChase(sees);
                break;

            case State.Search:
                TickSearch(sees);
                break;

            case State.Return:
                TickReturn();
                break;
        }

        CheckCatch();
    }

    // ── 状态 ────────────────────────────────────────────────────────
    protected void SetState(State next)
    {
        if (CurrentState == next) return;

        CurrentState = next;
        _stateTimer = 0f;
        _arrived = false;
        OnStateEntered(next);
        StateChanged?.Invoke(next);
    }

    protected virtual void OnStateEntered(State next) { }

    private void TickInvestigate(bool sees)
    {
        if (sees)
        {
            SetState(State.Chase);
            return;
        }

        if (!_arrived)
        {
            if (MoveTowards(_lookAt))
            {
                _arrived = true;
                _stateTimer = 0f;
            }
            return;
        }

        Stop();
        if (_stateTimer >= investigatePause) SetState(State.Return);
    }

    private void TickChase(bool sees)
    {
        if (!sees)
        {
            SetState(State.Search);
            return;
        }

        MoveTowards(witch.position);
    }

    private void TickSearch(bool sees)
    {
        if (sees)
        {
            SetState(State.Chase);
            return;
        }

        if (!_arrived)
        {
            if (MoveTowards(_lookAt))
            {
                _arrived = true;
                _stateTimer = 0f;
            }
            return;
        }

        Stop();
        if (_stateTimer >= searchPause)
        {
            SetState(State.Return);
        }
    }

    private void TickReturn()
    {
        if (MoveTowards(PostPosition)) SetState(State.Patrol);
    }

    // ── 巡逻（子类实现）────────────────────────────────────────────
    /// <summary>巡逻时每帧调用。</summary>
    protected virtual void TickPatrol() { Stop(); }

    /// <summary>回岗要回到哪里。</summary>
    protected virtual Vector2 PostPosition => transform.position;

    // ── 移动 ────────────────────────────────────────────────────────
    /// <summary>朝目标走，到了返回 true。</summary>
    protected bool MoveTowards(Vector2 target)
    {
        if (rb == null) return true;

        Vector2 delta = target - rb.position;
        if (delta.magnitude <= 0.12f)
        {
            Stop();
            return true;
        }

        Vector2 dir = delta.normalized;
        rb.velocity = dir * moveSpeed;
        _facing = dir;
        return false;
    }

    protected void Stop()
    {
        if (rb != null) rb.velocity = Vector2.zero;
    }

    // ── 感知 ────────────────────────────────────────────────────────
    public bool CanSeeWitch()
    {
        if (witch == null) return false;

        var conceal = witch.GetComponent<WitchConcealment>();
        if (conceal != null && conceal.IsHidden && !canSeeConcealed) return false;

        Vector2 pos = rb.position;
        Vector2 to = (Vector2)witch.position - pos;
        float dist = to.magnitude;

        if (dist > visionRange) return false;
        if (dist > 0.001f && Vector2.Angle(_facing, to) > visionAngle * 0.5f) return false;

        // 视线被墙挡住
        if (Physics2D.Linecast(pos, witch.position, obstacleMask)) return false;

        return true;
    }

    public virtual void OnSoundHeard(Vector2 source, SoundType type)
    {
        LastHeardTime = Time.time;
        Mark(5f);

        if (CurrentState == State.Chase) return;

        _lookAt = source;
        SetState(State.Investigate);
    }

    public void Mark(float duration)
    {
        _markedUntil = Mathf.Max(_markedUntil, Time.time + duration);

        if (_witchBrain == null) _witchBrain = FindObjectOfType<WitchBrain>();
        if (_witchBrain != null) _witchBrain.NotifyEnemyMarked(this);
    }

    private void CheckCatch()
    {
        if (witch == null) return;
        if (CurrentState != State.Chase && CurrentState != State.Search && CurrentState != State.Investigate) return;

        if (Vector2.Distance(rb.position, witch.position) <= catchRadius)
        {
            GameManager.Ensure().Lose($"{name} 抓住了她");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.45f, 0.2f, 0.6f);
        Vector3 pos = transform.position;
        Vector3 f = new Vector3(_facing.x, _facing.y, 0f);
        Vector3 l = Quaternion.Euler(0f, 0f, visionAngle * 0.5f) * f;
        Vector3 r = Quaternion.Euler(0f, 0f, -visionAngle * 0.5f) * f;

        Gizmos.DrawLine(pos, pos + l * visionRange);
        Gizmos.DrawLine(pos, pos + r * visionRange);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(pos, catchRadius);
    }
}
