using System;
using System.Collections;
using UnityEngine;

/// <summary>开场里的一段：幕布颜色 + 播哪一块台词 + 是否先淡出幕布。</summary>
[Serializable]
public class IntroStep
{
    public string block = "intro";

    [Tooltip("这段的幕布颜色；Alpha = 0 表示不遮")]
    public Color curtain = Color.clear;

    [Tooltip("先淡出幕布再播这段（用于最后一段露出场景）")]
    public bool fadeOutFirst;
}

/// <summary>
/// 关卡开场演出。依次播放 steps 里的段落，每段可以换幕布颜色。
/// 对话期间 GameplayLock 会冻住所有角色和操作。
/// </summary>
public class LevelIntro : MonoBehaviour
{
    [Tooltip("文本文件名，对应 Resources/Story/xxx.txt")]
    public string file = "level1";

    [Tooltip("旧数据兼容：steps 为空时用这个块名")]
    [HideInInspector] public string block = "";

    public IntroStep[] steps;

    private void Start()
    {
        // 这一关的开场已经看过了（按 R 重来），直接跳过
        if (RunState.IntroPlayed(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
        {
            return;
        }

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        var list = steps;

        // 老场景里只存了 block，没有 steps，这里补一段默认的
        if (list == null || list.Length == 0)
        {
            list = new[] { new IntroStep { block = string.IsNullOrEmpty(block) ? "intro" : block } };
        }

        var overlay = ScreenOverlay.Ensure();

        foreach (var step in list)
        {
            if (step == null) continue;

            overlay.SetCurtain(step.curtain);

            if (step.fadeOutFirst) yield return overlay.FadeIn(1.2f);

            yield return Play(step.block);
        }

        RunState.MarkCurrentIntroPlayed();
    }

    private IEnumerator Play(string block)
    {
        var lines = DialogueScript.Get(file, block);
        if (lines.Length == 0) yield break;

        bool done = false;
        DialogueUI.Ensure().Play(lines, () => done = true);
        while (!done) yield return null;
    }
}
