using UnityEngine;

/// <summary>
/// 脚步地块：踩上去改变脚步响度。
/// 地毯 / 苔藓 → 0（完全消音）；普通地面 1；碎石 / 干草 → 2（更响）。
/// 挂在带 IsTrigger 的 Collider2D 上。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FootstepZone : MonoBehaviour
{
    [Tooltip("0 = 完全消音，1 = 正常，2 = 加倍")]
    [Range(0f, 3f)] public float noiseMultiplier = 1f;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var e = other.GetComponentInParent<FootstepEmitter>();
        if (e != null) e.SetMultiplier(noiseMultiplier);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var e = other.GetComponentInParent<FootstepEmitter>();
        if (e != null) e.SetMultiplier(1f);
    }
}
