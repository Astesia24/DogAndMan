using System;
using UnityEngine;

/// <summary>
/// 狗叫。没有冷却，改成肺活量：每叫一次扣一截，自己慢慢回，
/// 不够扣就叫不出来。叫不叫、什么时候叫由玩家自己把握。
/// </summary>
public class DogBark : MonoBehaviour
{
    [Header("按键")]
    public KeyCode barkKey = KeyCode.Space;
    [Tooltip("呼唤键：波形和狗叫一样，但魔女听到会和你交换位置")]
    public KeyCode callKey = KeyCode.F;

    [Header("声音")]
    [Tooltip("声波能传多远")]
    public float radius = 7.5f;

    [Header("肺活量")]
    [Tooltip("满的时候一共能连叫几次，由 barkCost 决定")]
    public float staminaMax = 100f;
    [Tooltip("叫一次消耗多少")]
    public float barkCost = 30f;
    [Tooltip("每秒恢复多少")]
    public float regenPerSecond = 13f;

    [Tooltip("演出解锁：关掉后按空格不会叫。默认开，正常关卡不受影响")]
    public bool barkUnlocked = true;

    [Tooltip("调试用：勾上就跳过剧情解锁，直接能用 F 交换")]
    public bool debugUnlockSwap = false;

    [Header("反馈（可留空）")]
    [Tooltip("叫的时候会缩一下，留空就自动用本体的 SpriteRenderer")]
    public Transform visual;
    public float punchScale = 1.25f;

    public float Stamina { get; private set; }

    /// <summary>0~1，给脚下的圆环用。</summary>
    public float Stamina01 => staminaMax <= 0f ? 0f : Mathf.Clamp01(Stamina / staminaMax);

    public event Action Barked;

    private Vector3 _baseScale;
    private float _punch;

    private void Awake()
    {
        if (visual == null) visual = transform;
        _baseScale = visual.localScale;
        Stamina = staminaMax;
    }

    private void Update()
    {
        if (Stamina < staminaMax)
        {
            Stamina = Mathf.Min(staminaMax, Stamina + regenPerSecond * Time.deltaTime);
        }

        if (_punch > 0f)
        {
            _punch = Mathf.Max(0f, _punch - Time.deltaTime * 4f);
            if (visual != null) visual.localScale = _baseScale * (1f + (_punch * (punchScale - 1f)));
        }

        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.LevelState.Playing) return;
        if (GameplayLock.IsPlayerLocked) return;
        if (!barkUnlocked) return;

        if (Input.GetKeyDown(barkKey)) Bark();
        else if (Input.GetKeyDown(callKey)) Call();
    }

    /// <summary>叫一声。肺活量不够返回 false。</summary>
    public bool Bark() => Vocal(SoundType.Bark);

    /// <summary>呼唤：声音和狗叫一样，但魔女听到会和你换位置。没解锁就没有效果。</summary>
    public bool Call()
    {
        if (!MagicState.SwapUnlocked && !debugUnlockSwap) return false;
        return Vocal(SoundType.Call);
    }

    private bool Vocal(SoundType type)
    {
        if (Stamina < barkCost) return false;

        Stamina -= barkCost;
        _punch = 1f;

        SoundSystem.Ensure().Emit(transform.position, radius, this, type);
        Barked?.Invoke();
        return true;
    }
}
