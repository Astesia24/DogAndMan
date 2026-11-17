using UnityEngine;

/// <summary>
/// 关卡根节点上的锁。勾上之后生成器拒绝删除这一关，防止覆盖手调过的场景。
/// </summary>
public class LevelLocked : MonoBehaviour
{
    [Tooltip("勾上后生成器不会删除这一关")]
    public bool locked = true;
}
