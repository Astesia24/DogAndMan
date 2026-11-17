using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 跨场景的魔法解锁状态。目前只有"与魔女交换位置"。
///
/// 解锁一次之后一直有效，但**每关只能用固定次数**：
/// 次数在每次切场景（含按 R 回溯复活）时重置。
/// </summary>
public static class MagicState
{
    public const int SwapsPerLevel = 3;

    public static bool SwapUnlocked { get; private set; }

    /// <summary>本关还剩几次交换。</summary>
    public static int SwapUsesLeft { get; private set; } = SwapsPerLevel;

    public static void UnlockSwap()
    {
        SwapUnlocked = true;
    }

    /// <summary>扣一次使用次数；次数用完返回 false。</summary>
    public static bool TrySpendSwap()
    {
        if (!SwapUnlocked || SwapUsesLeft <= 0) return false;

        SwapUsesLeft--;
        return true;
    }

    /// <summary>开新游戏时调用。</summary>
    public static void ResetAll()
    {
        SwapUnlocked = false;
        SwapUsesLeft = SwapsPerLevel;
    }

    /// <summary>
    /// 每次加载场景（进下一关、或者按 R 回溯）都把次数补满。
    /// 用 RuntimeInitializeOnLoadMethod 挂一次即可，不需要场景里放任何东西。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookSceneReset()
    {
        SwapUsesLeft = SwapsPerLevel;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SwapUsesLeft = SwapsPerLevel;
    }
}
