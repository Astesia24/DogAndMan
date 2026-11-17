using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 奄奄一息的魔女（雪莉）。
///
/// 狗靠近时她头上会出现「按 E 开启对话」。按下 E 才开始判定：
///   1. 魔女（Vesper）有没有走到她身边 —— 没有就提示一句
///   2. 守卫是不是都还在平静状态 —— 有被惊动的就提示一句
/// 两个条件都满足才播雪莉那段，演完解锁交换魔法并存档。
/// </summary>
public class DyingWitch : MonoBehaviour
{
    [Header("交互距离")]
    [Tooltip("狗离多近会出现「按 E 开启对话」")]
    public float radius = 2f;

    [Tooltip("魔女必须离她多近。比狗的要求更小，得贴到身边才算")]
    public float witchRadius = 1.4f;

    [Tooltip("关掉就不检查魔女的位置")]
    public bool requireWitch = true;

    [Header("对话")]
    public string storyFile = "level3";
    public string storyBlock = "sherry";
    [Tooltip("魔女没过来时说的那句")]
    public string needWitchBlock = "sherry_needwitch";
    [Tooltip("守卫被惊动时说的那句")]
    public string alertBlock = "sherry_alert";

    [Tooltip("靠近时的提示文字")]
    public string promptText = "按 E 开启对话";

    [Tooltip("开启对话的按键")]
    public KeyCode interactKey = KeyCode.E;

    public UnityEvent onTriggered;

    private DogMotor _dog;
    private WitchBrain _witch;
    private bool _fired;
    private bool _busy;
    private bool _promptShown;

    private void Start()
    {
        // 存档点已经过这里了，重来时不再演一遍
        if (Checkpoint.SherryDone)
        {
            enabled = false;
            return;
        }

        _dog = FindObjectOfType<DogMotor>();
        _witch = FindObjectOfType<WitchBrain>();
    }

    private void OnDisable()
    {
        HidePrompt();
    }

    private void Update()
    {
        if (_fired || _busy || _dog == null) return;

        // 演出 / 对话期间不响应
        if (GameplayLock.IsLocked) return;

        bool inRange = Vector2.Distance(transform.position, _dog.transform.position) <= radius;

        if (!inRange)
        {
            HidePrompt();
            return;
        }

        ShowPrompt();

        if (Input.GetKeyDown(interactKey)) StartCoroutine(Interact());
    }

    private IEnumerator Interact()
    {
        _busy = true;
        HidePrompt();

        // 条件 1：魔女得站到她身边
        if (requireWitch && !WitchIsClose())
        {
            yield return PlayBlock(needWitchBlock);
            _busy = false;
            yield break;
        }

        // 条件 2：守卫得都平静
        if (AnyGuardAlerted())
        {
            yield return PlayBlock(alertBlock);
            _busy = false;
            yield break;
        }

        _fired = true;

        yield return PlayBlock(storyBlock);

        MagicState.UnlockSwap();

        // 先把存档写掉，再去触发外部事件（事件坏了也不能丢存档）
        Checkpoint.Save();
        Checkpoint.SherryDone = true;

        try
        {
            onTriggered?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DyingWitch] onTriggered 出错：{e.Message}", this);
        }

        var overlay = ScreenOverlay.Ensure();
        overlay.ShowPromptAbove(_dog.transform, "按 F 与魔女交换位置");
        yield return new WaitForSeconds(3f);
        overlay.HidePrompt();

        _busy = false;
    }

    private IEnumerator PlayBlock(string block)
    {
        var lines = DialogueScript.Get(storyFile, block);
        if (lines.Length == 0) yield break;

        bool done = false;
        DialogueUI.Ensure().Play(lines, () => done = true);
        while (!done) yield return null;
    }

    private bool WitchIsClose()
    {
        if (_witch == null) return false;

        return Vector2.Distance(transform.position, _witch.transform.position) <= witchRadius;
    }

    private static bool AnyGuardAlerted()
    {
        for (int i = 0; i < EnemyBase.All.Count; i++)
        {
            var e = EnemyBase.All[i];
            if (e == null) continue;

            // 只要不是"在巡逻"或"在回岗"，都算还没平静：
            // Chase（追）、Investigate（黄圈问号）、Search（跟丢后去最后位置）
            if (e.CurrentState != EnemyBase.State.Patrol &&
                e.CurrentState != EnemyBase.State.Return) return true;
        }

        return false;
    }

    private void ShowPrompt()
    {
        if (_promptShown) return;

        _promptShown = true;
        ScreenOverlay.Ensure().ShowPromptAbove(transform, promptText);
    }

    private void HidePrompt()
    {
        if (!_promptShown) return;

        _promptShown = false;
        ScreenOverlay.Ensure().HidePrompt();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.35f, 0.9f, 0.45f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);

        Gizmos.color = new Color(0.9f, 0.8f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, witchRadius);
    }
}
