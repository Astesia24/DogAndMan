using UnityEngine;

/// <summary>
/// 一切魔女能力的基类。想加一种新魔法 = 新建一个子类，挂到魔女身上即可，
/// WitchBrain 会自动找到它。
/// </summary>
public abstract class WitchAbility : MonoBehaviour
{
    protected WitchBrain Brain { get; private set; }

    /// <summary>由 WitchBrain 在 Awake 时调用。</summary>
    public void Bind(WitchBrain brain)
    {
        Brain = brain;
        OnBound();
    }

    /// <summary>绑定完成时调用一次。</summary>
    protected virtual void OnBound() { }

    /// <summary>听到声音时调用（所有能力都会收到）。</summary>
    public virtual void OnSoundHeard(Vector2 source, SoundType type) { }

    /// <summary>有敌人被声音标记时调用。</summary>
    public virtual void OnEnemyMarked(EnemyBase enemy) { }

    /// <summary>每帧调用，不受"有没有听到声音"影响——攻击魔法要能随时发动。</summary>
    public virtual void Tick(float dt) { }
}
