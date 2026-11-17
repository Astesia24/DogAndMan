using UnityEngine;

/// <summary>
/// 感知魔法：听到狗叫就走过去，到了停下。
/// 钟声和陶罐碎裂是招守卫的，她不会过去。
/// 听到呼唤（F）时，如果狗站的位置她站得下，两人交换位置。
/// </summary>
public class SenseMagic : WitchAbility
{
    [Tooltip("只对这些声音起反应")]
    public bool reactToBark = true;
    public bool reactToFootstep = false;

    [Tooltip("忽略自己的脚步声，否则她会追着自己的脚走")]
    public bool ignoreOwnFootsteps = true;

    [Tooltip("交换位置时，狗站的这个半径内不能有墙或狗洞")]
    public float swapCheckRadius = 0.3f;

    private DogMotor _dog;

    public override void OnSoundHeard(Vector2 source, SoundType type)
    {
        if (type == SoundType.Call)
        {
            TrySwap();
            return;
        }

        if (type == SoundType.Bell || type == SoundType.Impact) return;
        if (type == SoundType.Bark && !reactToBark) return;
        if (type == SoundType.Footstep && (ignoreOwnFootsteps || !reactToFootstep)) return;

        Brain.SetDestination(source);
    }

    private void TrySwap()
    {
        if (_dog == null) _dog = FindObjectOfType<DogMotor>();
        if (_dog == null) return;

        Vector2 dogPos = _dog.transform.position;
        Vector2 myPos = transform.position;

        // 狗站的地方得是她能站的地方：狗洞里、墙里都不行
        int mask = 1 << LayerMask.NameToLayer("Wall");
        int hole = LayerMask.NameToLayer("DogHole");
        if (hole >= 0) mask |= 1 << hole;

        if (Physics2D.OverlapCircle(dogPos, swapCheckRadius, mask) != null) return;

        // 位置合法才扣次数，扣不动说明这一关用完了
        if (!MagicState.TrySpendSwap())
        {
            StartCoroutine(HintOutOfSwaps());
            return;
        }

        Teleport(_dog, myPos);
        Teleport(transform, dogPos);

        StartCoroutine(Flash());
    }

    private System.Collections.IEnumerator HintOutOfSwaps()
    {
        if (_dog == null) yield break;

        var overlay = ScreenOverlay.Ensure();
        overlay.ShowPromptAbove(_dog.transform, "交换次数已用完");
        yield return new WaitForSeconds(1.5f);
        overlay.HidePrompt();
    }

    private static void Teleport(Component target, Vector2 pos)
    {
        var rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.position = pos;
        }
        else
        {
            target.transform.position = pos;
        }
    }

    private System.Collections.IEnumerator Flash()
    {
        yield return ScreenOverlay.Ensure().Flash(0.18f, new Color(0.6f, 0.85f, 1f));
    }
}
