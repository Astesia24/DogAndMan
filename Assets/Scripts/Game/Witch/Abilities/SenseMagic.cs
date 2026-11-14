using UnityEngine;

/// <summary>
/// 感知魔法：第一关唯一的魔法。听到声音就走过去，到了停下。
/// 以后她获得攻击魔法时，那是另一个组件，两者互不影响：
/// 这个只管走，那个只管打。
/// </summary>
public class SenseMagic : WitchAbility
{
    [Tooltip("忽略自己的脚步声，否则她会追着自己的脚走")]
    public bool ignoreOwnFootsteps = true;

    [Tooltip("只对这些声音起反应")]
    public bool reactToBark = true;
    public bool reactToImpact = true;
    public bool reactToFootstep = false;

    public override void OnSoundHeard(Vector2 source, SoundType type)
    {
        if (type == SoundType.Bark && !reactToBark) return;
        if (type == SoundType.Impact && !reactToImpact) return;
        if (type == SoundType.Footstep && (ignoreOwnFootsteps || !reactToFootstep)) return;

        Brain.SetDestination(source);
    }
}
