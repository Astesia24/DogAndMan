using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有敌人的基类：听觉、视觉、巡逻/调查/追击/搜索/回岗的状态机，
/// 以及网格 A* 寻路（追、找、回岗都会绕墙）。
/// 第一关的守卫只是"巡逻 + 去看声音"，不会攻击狗。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour, ISoundListener, IAgentStatus
{
    public enum State
    {
        Patrol,
        Investigate,
        Chase,
        Search,
        Return
    }

    public static readonly List<EnemyBase> All = new List<EnemyBase>();

    [Header("移动")]
    public float moveSpeed = 2.6f;

    [Tooltip("每秒最多转多少度。调很大就是瞬间转向")]
    public float turnSpeed = 400f;

    [Header("感知")]
    public float visionRange = 5.6f;
    [Range(0f, 360f)] public float visionAngle = 80f;
    [Tooltip("视线和寻路都被这些层挡住（留空自动用 Wall 层）")]
    public LayerMask obstacleMask;
    [Tooltip("能不能看见钻到床底下的目标（第一关：不能）")]
    public bool canSeeConcealed = false;

    [Header("状态时长")]
    public float investigatePause = 2.5f;
    public float searchPause = 3f;

    [Header("抓捕")]
    [Tooltip("靠多近算抓住（只在追/找/查看状态下生效）")]
    public float catchRadius = 0.65f;

    [Header("寻路")]
    [Tooltip("留空会自动在场景里找；找不到就退化成直线走")]
    public LevelGrid grid;

    [Header("引用")]
    public Transform witch;
    public Rigidbody2D rb;

    public State CurrentState { get; private set; } = State.Patrol;
    public event Action<State> StateChanged;
    public float LastHeardTime { get; private set; } = -999f;

    public Vector2 SoundPosition => transform.position;
    public bool CanHear => true;

    /// <summary>当前朝向（给视野锥用）。</summary>
    public Vector2 Facing => _facing;

    /// <summary>直接设定朝向，通常用于登场时先看向某个方向。</summary>
    public void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;

        _facing = dir.normalized;
        _desiredFacing = _facing;
    }

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
    private Vector2 _desiredFacing = Vector2.right;
    private Vector2 _lookAt;
    private Vector2 _returnTo;
    private bool _hasReturnTo;
    private float _stateTimer;
    private bool _arrived;
    private float _markedUntil;
    private WitchBrain _witchBrain;

    // 寻路
    private List<Vector2> _path;
    private int _pathIndex;
    private float _repathTimer;
    private Vector2 _lastPos;
    private float _stuck;

    public bool IsMarked => Time.time < _markedUntil;

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (obstacleMask.value == 0) obstacleMask = DefaultVisionMask();

        // 旧预制体上的遮挡层可能只有墙，这里补上狗洞
        int holeLayer = LayerMask.NameToLayer("DogHole");
        if (holeLayer >= 0) obstacleMask = obstacleMask.value | (1 << holeLayer);
        if (obstacleMask.value == 0)
        {
            Debug.LogWarning($"[{name}] 没有设置遮挡层，也没有 Wall 层，敌人会隔墙看见人", this);
        }

        if (grid == null) grid = FindObjectOfType<LevelGrid>();
        if (grid != null && !grid.Ready) grid.Bake();   // Awake 顺序不保证，谁先到谁烘

        ResolveWitch();
        _lastPos = rb.position;
        if (!All.Contains(this)) All.Add(this);
    }

    protected virtual void OnEnable()
    {
        // 关键：不加这一句，守卫永远听不见狗叫
        if (!SoundSystem.Listeners.Contains(this)) SoundSystem.Listeners.Add(this);
    }

    protected virtual void OnDisable()
    {
        SoundSystem.Listeners.Remove(this);
        All.Remove(this);
        if (rb != null) rb.velocity = Vector2.zero;
    }

    /// <summary>视线会被墙和狗洞挡住（声音不会，声音只被墙挡）。</summary>
    private static LayerMask DefaultVisionMask()
    {
        int mask = 1 << LayerMask.NameToLayer("Wall");
        int hole = LayerMask.NameToLayer("DogHole");
        if (hole >= 0) mask |= 1 << hole;

        return mask;
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

        // 对话 / 演出期间敌人也停住
        if (GameplayLock.IsLocked)
        {
            Stop();
            return;
        }

        if (witch == null) ResolveWitch();

        _facing = RotateTowards(_facing, _desiredFacing, turnSpeed * Time.fixedDeltaTime);

        _stateTimer += Time.fixedDeltaTime;

        bool sees = CanSeeWitch();
        if (sees)
        {
            _lookAt = witch.position;
            if (CurrentState != State.Chase && CurrentState != State.Investigate) SetState(State.Chase);
        }

        if (_stuck > 1.8f) { _stuck = 0f; SetState(State.Return); }

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

    protected void SetState(State next)
    {
        if (CurrentState == next) return;

        CurrentState = next;
        _stateTimer = 0f;
        _arrived = false;
        _path = null;
        _repathTimer = 0f;
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
        TickChaseCore(sees);
    }

    /// <summary>追击/远距离瞄准的行为。子类可以覆盖（火枪手就是这么做的）。</summary>
    protected virtual void TickChaseCore(bool sees)
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
        if (_stateTimer >= searchPause) SetState(State.Return);
    }

    private void TickReturn()
    {
        Vector2 target = _hasReturnTo ? _returnTo : PostPosition;

        if (MoveTowards(target))
        {
            _hasReturnTo = false;
            SetState(State.Patrol);
        }
    }

    protected virtual void TickPatrol() { Stop(); }

    protected virtual Vector2 PostPosition => transform.position;

    protected bool MoveTowards(Vector2 target)
    {
        Vector2 pos = rb == null ? (Vector2)transform.position : rb.position;

        if (Vector2.Distance(pos, target) <= 0.12f)
        {
            Stop();
            _path = null;
            return true;
        }

        // 卡住检测：真的推不动才累加
        float moved = Vector2.Distance(pos, _lastPos);
        if (moved < moveSpeed * Time.fixedDeltaTime * 0.3f) _stuck += Time.fixedDeltaTime;
        else _stuck = Mathf.Max(0f, _stuck - Time.fixedDeltaTime * 2f);
        _lastPos = pos;

        // 每 0.45 秒重新规划一次（追移动目标时不会踩空）
        _repathTimer -= Time.fixedDeltaTime;
        if (_path == null || _repathTimer <= 0f)
        {
            _path = grid != null ? Pathfinder.Find(grid, pos, target) : null;

            // 没有网格才退化成直线；有网格但走不到就原地等，别撞墙
            if (_path == null && grid == null) _path = new List<Vector2> { target };

            if (_path == null)
            {
                Stop();
                _stuck += Time.fixedDeltaTime;
                return false;
            }

            _pathIndex = 0;
            _repathTimer = 0.45f;
        }

        // 跳过走过的路点，但最后一个不跳（会跟到达判定打架）
        while (_pathIndex < _path.Count - 1 &&
               Vector2.Distance(pos, _path[_pathIndex]) < 0.2f)
        {
            _pathIndex++;
        }

        Vector2 wp = _path[_pathIndex];
        Vector2 delta = wp - pos;
        float d = delta.magnitude;

        if (d < 0.05f)
        {
            _path = null;
            _repathTimer = 0f;
            Stop();
            _stuck += Time.fixedDeltaTime;
            return false;
        }

        Vector2 dir = delta / d;
        rb.velocity = dir * moveSpeed;
        _desiredFacing = dir;

        return false;
    }

    /// <summary>朝目标方向转最多 maxDegrees，避免朝向瞬间翻转。</summary>
    private static Vector2 RotateTowards(Vector2 from, Vector2 to, float maxDegrees)
    {
        float angle = Vector2.SignedAngle(from, to);
        if (Mathf.Abs(angle) <= maxDegrees) return to;

        float rad = Mathf.Sign(angle) * maxDegrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);

        return new Vector2(from.x * c - from.y * s, from.x * s + from.y * c);
    }

    protected void Stop()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            _lastPos = rb.position;
        }
    }

    public bool CanSeeWitch()
    {
        return CanSeeWithRange(visionRange);
    }

    /// <summary>
    /// 用指定视野半径判断能不能看见魔女（角度、遮挡、藏身都照常生效）。
    /// 火枪手瞄准时视野会收缩，但判定要用"瞄准刚开始时的视野"。
    /// </summary>
    protected bool CanSeeWithRange(float range)
    {
        if (witch == null) return false;

        var conceal = witch.GetComponent<WitchConcealment>();
        if (conceal != null && conceal.IsHidden && !canSeeConcealed) return false;

        Vector2 pos = rb.position;
        Vector2 to = (Vector2)witch.position - pos;
        float dist = to.magnitude;

        if (dist > range) return false;
        if (dist > 0.001f && Vector2.Angle(_facing, to) > visionAngle * 0.5f) return false;

        if (Physics2D.Linecast(pos, witch.position, obstacleMask)) return false;

        return true;
    }

    public virtual void OnSoundHeard(Vector2 source, SoundType type)
    {
        // 狗叫和呼唤都没法把他从半路上再勾走；钟声和陶罐可以
        bool barkLike = type == SoundType.Bark || type == SoundType.Call;

        if (barkLike &&
            (CurrentState == State.Investigate || CurrentState == State.Search || CurrentState == State.Return))
        {
            return;
        }

        LastHeardTime = Time.time;

        if (CurrentState == State.Chase) return;

        // 听得到但过不去（比如声音来自狗洞后面的死胡同），不白跑
        if (!CanReach(source)) return;

        // 记住被勾走前站的位置，之后要回到这里才算恢复
        _returnTo = rb.position;
        _hasReturnTo = true;

        Mark(5f);
        _lookAt = source;
        SetState(State.Investigate);
    }

    /// <summary>
    /// 声音来源能不能走到。
    /// 只看 A* 有没有路——不再要求“声源那一格必须可走”。
    /// 狗贴着狗洞、墙边叫的时候，脚下那格会被网格标成阻挡，
    /// 以前就会被判成“过不去”而静默忽略；现在交给 A* 判断（它会自动吸附到最近的可走格）。
    /// </summary>
    private bool CanReach(Vector2 target)
    {
        if (grid == null) return true;

        return Pathfinder.Find(grid, rb.position, target) != null;
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
