using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 魔女的大脑：听觉入口 + 能力容器 + 移动意图。
/// 身上的每一个 WitchAbility 子组件都会在这里自动注册，
/// 所以"加一种魔法"= 加一个组件，不用改这个脚本。
/// </summary>
[RequireComponent(typeof(WitchMotor))]
public class WitchBrain : MonoBehaviour, ISoundListener, IAgentStatus
{
    [SerializeField] private WitchMotor motor;
    [SerializeField] private WitchConcealment concealment;

    private readonly List<WitchAbility> _abilities = new List<WitchAbility>();

    public WitchMotor Motor => motor;
    public bool IsHidden => concealment != null && concealment.IsHidden;
    public IReadOnlyList<WitchAbility> Abilities => _abilities;

    public float LastHeardTime { get; private set; } = -999f;

    // ── ISoundListener ──────────────────────────────────────────────
    public Vector2 SoundPosition => transform.position;
    public bool CanHear => true;

    // ── IAgentStatus ────────────────────────────────────────────────
    public string StatusText
    {
        get
        {
            if (motor != null && motor.HasDestination) return ">>";
            if (IsHidden) return "-";
            return "..";
        }
    }

    public Color StatusColor
    {
        get
        {
            if (IsHidden) return new Color(0.5f, 0.9f, 0.6f);
            if (Time.time - LastHeardTime < 0.6f) return new Color(0.5f, 0.95f, 1f);
            return new Color(0.85f, 0.65f, 1f);
        }
    }

    private void Awake()
    {
        if (motor == null) motor = GetComponent<WitchMotor>();
        if (concealment == null) concealment = GetComponent<WitchConcealment>();

        // 把所有魔法组件收集起来（以后加新魔法不用改这里）
        _abilities.Clear();
        _abilities.AddRange(GetComponents<WitchAbility>());
        for (int i = 0; i < _abilities.Count; i++) _abilities[i].Bind(this);
    }

    private void OnEnable()
    {
        if (!SoundSystem.Listeners.Contains(this)) SoundSystem.Listeners.Add(this);
    }

    private void OnDisable()
    {
        SoundSystem.Listeners.Remove(this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _abilities.Count; i++) _abilities[i].Tick(dt);
    }

    // ── 声音入口 ────────────────────────────────────────────────────
    public void OnSoundHeard(Vector2 source, SoundType type)
    {
        LastHeardTime = Time.time;

        for (int i = 0; i < _abilities.Count; i++)
        {
            _abilities[i].OnSoundHeard(source, type);
        }
    }

    /// <summary>敌人被声音标记时通知所有魔法（以后攻击魔法靠这个锁定目标）。</summary>
    public void NotifyEnemyMarked(EnemyBase enemy)
    {
        for (int i = 0; i < _abilities.Count; i++)
        {
            _abilities[i].OnEnemyMarked(enemy);
        }
    }

    // ── 移动 ────────────────────────────────────────────────────────
    public void SetDestination(Vector2 p)
    {
        if (motor != null) motor.MoveTo(p);
    }

    public void StopMoving()
    {
        if (motor != null) motor.Stop();
    }

    public bool HasDestination => motor != null && motor.HasDestination;

    // ── 给未来的攻击魔法用 ──────────────────────────────────────────
    public List<EnemyBase> GetMarkedEnemies()
    {
        var list = new List<EnemyBase>();
        for (int i = 0; i < EnemyBase.All.Count; i++)
        {
            var e = EnemyBase.All[i];
            if (e != null && e.IsMarked) list.Add(e);
        }
        return list;
    }
}
