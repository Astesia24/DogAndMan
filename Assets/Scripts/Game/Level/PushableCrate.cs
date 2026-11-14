using UnityEngine;

/// <summary>
/// 可推的木箱。摩擦力调高一点，推起来有"重"的感觉，松手就停。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PushableCrate : MonoBehaviour
{
    [Tooltip("越大越难推动")]
    public float mass = 4f;
    [Tooltip("越大越快停下来")]
    public float drag = 6f;
    public float angularDrag = 8f;

    private void Reset()
    {
        Apply();
    }

    private void Awake()
    {
        Apply();
    }

    private void Apply()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.mass = mass;
        rb.drag = drag;
        rb.angularDrag = angularDrag;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
}
