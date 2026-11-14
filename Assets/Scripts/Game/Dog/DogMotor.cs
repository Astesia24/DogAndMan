using UnityEngine;

/// <summary>
/// 狗的移动。固定脚本：不依赖关卡、不依赖魔女、不发出任何声音。
/// WASD / 方向键，Input Manager 里的 Horizontal、Vertical。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DogMotor : MonoBehaviour
{
    public float speed = 4.5f;
    public Rigidbody2D rb;

    [Tooltip("勾上后狗会转向移动方向（以后接动画用）")]
    public bool faceMoveDirection = false;

    public Vector2 InputDirection { get; private set; }
    public Vector2 Facing { get; private set; } = Vector2.right;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        InputDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    private void FixedUpdate()
    {
        Vector2 dir = Vector2.ClampMagnitude(InputDirection, 1f);
        rb.velocity = dir * speed;

        if (dir.sqrMagnitude > 0.001f)
        {
            Facing = dir.normalized;
            if (faceMoveDirection) transform.up = Facing;
        }
    }
}
