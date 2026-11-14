using UnityEngine;

/// <summary>能给"头顶状态"提供内容的角色（魔女、敌人）。</summary>
public interface IAgentStatus
{
    string StatusText { get; }
    Color StatusColor { get; }
    float LastHeardTime { get; }
}
