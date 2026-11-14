using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    //动画相关
    public Animator animator;

    //移动相关
    public float speed = 5f;
    public Rigidbody2D rb;

    //状态机相关
    public StateMachine stateMachine;
    public State Move;
    public State Idle;

    /// <summary>低于这个输入量级视为停止。状态机和移动共用同一个阈值，避免来回抖。</summary>
    public const float MoveThreshold = 0.2f;

    //存储输入相关
    [HideInInspector] public Vector2 playerInput;

    [HideInInspector] public Vector2 direction;

    /// <summary>输入量级 0~1，不是实际速度。真正速度看 rb.velocity.magnitude。</summary>
    [HideInInspector] public float inputMagnitude;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("[Player] 没挂 Animator，动画不会播放", this);
        }

        stateMachine = new StateMachine();
        Move = new MoveState(this, stateMachine);
        Idle = new IdleState(this, stateMachine);

        playerInput = Vector2.zero;
        direction = Vector2.zero;
        inputMagnitude = 0f;

        // 直接在 Awake 里初始化，避免 Start 之前有人调 ChangeState 就 NRE
        stateMachine.Initialize(Idle);
    }

    // 事件型输入（GetKeyDown 之类）要放在这里读，FixedUpdate 会丢帧
    void Update()
    {
        playerInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    void FixedUpdate()
    {
        // ClampMagnitude 已经把长度限制到 1，后面直接乘速度即可，不要再乘一次模长
        direction = Vector2.ClampMagnitude(playerInput, 1f);
        inputMagnitude = direction.magnitude;

        stateMachine.FixedUpdate();
    }

    void OnDisable()
    {
        if (rb != null) rb.velocity = Vector2.zero;
    }
}
