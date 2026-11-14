using UnityEngine;

/// <summary>
/// 出口：魔女走进来就通关。挂在一个 IsTrigger 的 Collider2D 上。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<WitchBrain>() == null) return;
        if (GameManager.Instance == null) GameManager.Ensure();

        GameManager.Instance.Win();
    }
}
