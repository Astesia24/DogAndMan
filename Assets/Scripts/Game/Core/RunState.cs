using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// 本次运行内的关卡进度记录：哪些场景的开场已经播过了。
/// 重来（R）时据此跳过开场，直接回到剧情结束后的状态。
/// </summary>
public static class RunState
{
    private static readonly HashSet<string> Played = new HashSet<string>();

    public static bool IntroPlayed(string scene) => Played.Contains(scene);

    public static void MarkIntroPlayed(string scene) => Played.Add(scene);

    public static void MarkCurrentIntroPlayed() => MarkIntroPlayed(SceneManager.GetActiveScene().name);
}
