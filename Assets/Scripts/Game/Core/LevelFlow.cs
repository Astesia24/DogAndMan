using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换。第一幕 → 第一关 → 第二关 → 第三关。
/// </summary>
public static class LevelFlow
{
    public static void GoTo(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        GameplayLock.ReleaseAll();
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
