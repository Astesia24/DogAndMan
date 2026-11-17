using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 每次场景加载完之后，把存档点的位置还回去。
///
/// 注意：不能用 RuntimeInitializeOnLoadMethod(AfterSceneLoad)——
/// 那个钩子只在**启动时**执行一次，按 R 重载场景不会再触发，
/// 于是存档点永远不生效，复活总是回到场景出生点。
/// 必须挂 SceneManager.sceneLoaded。
/// </summary>
public static class CheckpointRestorer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Checkpoint.Restore();
    }
}
