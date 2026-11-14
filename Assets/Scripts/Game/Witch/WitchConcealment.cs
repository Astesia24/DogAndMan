using UnityEngine;

/// <summary>魔女是否躲在草丛里。草丛通过 Enter/Exit 计数，支持多个草丛重叠。</summary>
public class WitchConcealment : MonoBehaviour
{
    private int _count;

    public bool IsHidden => _count > 0;

    public void Enter()
    {
        _count++;
    }

    public void Exit()
    {
        _count = Mathf.Max(0, _count - 1);
    }
}
