using UnityEngine;

/// <summary>
/// 脚步发声器。魔女走路会发出轻微脚步声，踩在碎石上更响，踩在地毯上消失。
/// 狗身上不要挂这个（狗是完全无声的）。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FootstepEmitter : MonoBehaviour
{
    [Tooltip("走多远算一步")]
    public float stepDistance = 0.5f;
    [Tooltip("一步的声音能传多远（未乘地块系数）")]
    public float noiseRadius = 2.6f;

    [Range(0f, 3f)] public float multiplier = 1f;

    private Rigidbody2D _rb;
    private Vector2 _lastPos;
    private float _accumulated;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _lastPos = _rb.position;
    }

    private void FixedUpdate()
    {
        Vector2 p = _rb.position;
        _accumulated += Vector2.Distance(p, _lastPos);
        _lastPos = p;

        if (_accumulated < stepDistance) return;
        _accumulated = 0f;

        float radius = noiseRadius * multiplier;
        if (radius <= 0.05f) return;

        SoundSystem.Ensure().Emit(p, radius, this, SoundType.Footstep);
    }

    /// <summary>地块通过这个方法改脚步响度（0 = 消音）。</summary>
    public void SetMultiplier(float value)
    {
        multiplier = Mathf.Clamp(value, 0f, 3f);
    }
}
