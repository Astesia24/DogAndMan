using UnityEngine;

/// <summary>
/// 草丛：魔女走进去就隐身，敌人看不见她（除非敌人勾了 canSeeConcealed）。
/// 挂在带 IsTrigger 的 Collider2D 上。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Bush : MonoBehaviour
{
    [Tooltip("躲进去以后魔女的不透明度")]
    [Range(0.15f, 1f)] public float hiddenAlpha = 0.45f;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var conceal = other.GetComponent<WitchConcealment>();
        if (conceal == null) return;

        conceal.Enter();
        SetAlpha(other, hiddenAlpha);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var conceal = other.GetComponent<WitchConcealment>();
        if (conceal == null) return;

        conceal.Exit();
        SetAlpha(other, conceal.IsHidden ? hiddenAlpha : 1f);
    }

    private static void SetAlpha(Collider2D other, float alpha)
    {
        var sr = other.GetComponentInParent<SpriteRenderer>();
        if (sr == null) return;

        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}
