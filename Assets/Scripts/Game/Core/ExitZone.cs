using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 出口。挂在一个 IsTrigger 的 Collider2D 上。
///
/// 默认是"魔女走进来就通关"（第三关用）。
/// 第一幕要自己处理结局，就把 triggerWin 关掉、triggerOnDog 打开，
/// 用 onReached 事件接自己的演出。
///
/// nextScene 填了 = 淡黑后切下一关。
/// endingText 填了 = 淡黑后缓缓浮出一行字（最后一关用）。
/// 两个都空 = 显示"按 R 再来一次"。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    [Tooltip("交给谁触发：关 = 魔女（第三关），开 = 狗（第一幕）")]
    public bool triggerOnDog = false;

    [Tooltip("触发就直接判定通关；自己处理结局的关卡关掉")]
    public bool triggerWin = true;

    [Header("收尾")]
    [Tooltip("通关后自动切到哪个场景，留空就不切")]
    public string nextScene = "";

    [Tooltip("通关后缓缓浮出的一行字，留空就没有")]
    public string endingText = "";

    [Tooltip("黑屏淡入时长（秒）")]
    public float endingFade = 2.5f;

    [Tooltip("有人踩进来时触发，不管 triggerWin 开没开")]
    public UnityEvent onReached;

    private bool _fired;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_fired) return;

        bool match = triggerOnDog
            ? other.GetComponentInParent<DogMotor>() != null
            : other.GetComponentInParent<WitchBrain>() != null;

        if (!match) return;

        _fired = true;
        onReached?.Invoke();

        bool hasNext = !string.IsNullOrEmpty(nextScene);
        bool hasEnding = !string.IsNullOrEmpty(endingText);

        if (triggerWin)
        {
            if (GameManager.Instance == null) GameManager.Ensure();
            GameManager.Instance.Win(showText: !hasNext && !hasEnding);
        }

        if (hasNext) StartCoroutine(GoNext());
        else if (hasEnding) StartCoroutine(PlayEnding());
    }

    private IEnumerator GoNext()
    {
        yield return ScreenOverlay.Ensure().FadeToBlack(0.6f);
        yield return new WaitForSecondsRealtime(0.2f);
        LevelFlow.GoTo(nextScene);
    }

    private IEnumerator PlayEnding()
    {
        var overlay = ScreenOverlay.Ensure();

        yield return overlay.FadeToBlack(endingFade);
        yield return new WaitForSecondsRealtime(0.3f);

        overlay.ShowCenterHint(endingText, 2f);
    }
}
