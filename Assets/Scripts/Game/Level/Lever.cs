using UnityEngine;

/// <summary>
/// 拉杆：只有魔女拉得动，狗按 E 没反应。
/// closeGates 决定拉下之后是开门还是关门。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Lever : MonoBehaviour
{
    [Tooltip("站多近算拉下")]
    public float range = 1f;

    [Tooltip("要操作的闸门")]
    public Gate[] gates;

    [Tooltip("勾上 = 拉下后关门，否则开门")]
    public bool closeGates;

    [Tooltip("留空会自动找场景里的魔女")]
    public Transform witch;

    public bool Pulled { get; private set; }
    public event System.Action OnPulled;

    /// <summary>存档恢复用：把"已拉下"的状态还原，不再触发一次开关。</summary>
    public void RestorePulled(bool pulled)
    {
        Pulled = pulled;
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (witch == null)
        {
            var w = FindObjectOfType<WitchBrain>();
            if (w != null) witch = w.transform;
        }
    }

    private void Update()
    {
        if (Pulled || witch == null) return;
        if (Vector2.Distance(transform.position, witch.position) > range) return;

        Pull();
    }

    public void Pull()
    {
        if (Pulled) return;

        Pulled = true;

        if (gates != null)
        {
            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i] == null) continue;

                if (closeGates) gates[i].Close();
                else gates[i].Open();
            }
        }

        OnPulled?.Invoke();
    }
}
