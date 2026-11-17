using UnityEngine;

/// <summary>
/// 脚步发声器：只要她还在移动，就按固定间隔发出脚步声；站着不动就不响。
/// 不和地块、速度之类的东西联动 —— 移动 = 有声，停下 = 无声。
/// 狗身上不要挂这个（狗是完全无声的）。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FootstepEmitter : MonoBehaviour
{
    [Tooltip("每隔多久一步（秒）")]
    public float interval = 0.25f;

    [Tooltip("速度低于这个就算站着不动")]
    public float minSpeed = 0.15f;

    [Tooltip("一步的声音能传多远")]
    public float noiseRadius = 3.2f;

    private Rigidbody2D _rb;
    private float _timer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _timer = interval;
    }

    private void FixedUpdate()
    {
        if (_rb == null) return;

        // 站着不动：不响，并且把计时重置 —— 一重新起步就会立刻迈出第一步
        if (_rb.velocity.magnitude < minSpeed)
        {
            _timer = interval;
            return;
        }

        _timer -= Time.fixedDeltaTime;
        if (_timer > 0f) return;

        _timer = interval;
        SoundSystem.Ensure().Emit(_rb.position, noiseRadius, this, SoundType.Footstep);
    }
}
