using UnityEngine;

/// <summary>
/// 陶罐：被撞碎时发出一声巨响。狗推箱子撞过去，或者被推着撞上墙，都会碎。
/// 这是第一关"把守卫引走"的装置。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BreakableJar : MonoBehaviour
{
    [Tooltip("撞击速度低于这个值不会碎（防止轻轻蹭一下就炸）")]
    public float breakSpeed = 1.4f;

    [Tooltip("碎裂声能传多远（比狗叫更远，是专门用来引人的）")]
    public float noiseRadius = 9f;

    [Tooltip("碎裂特效（可留空）")]
    public GameObject breakEffect;

    [Tooltip("碎了以后留下什么（可留空）")]
    public GameObject debrisPrefab;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.relativeVelocity.magnitude < breakSpeed) return;

        Break();
    }

    public void Break()
    {
        SoundSystem.Ensure().Emit(transform.position, noiseRadius, this, SoundType.Impact);

        if (breakEffect != null) Instantiate(breakEffect, transform.position, Quaternion.identity);
        if (debrisPrefab != null) Instantiate(debrisPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
