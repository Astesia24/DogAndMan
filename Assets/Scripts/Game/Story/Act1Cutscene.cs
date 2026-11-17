using System.Collections;
using UnityEngine;

/// <summary>
/// 第一幕的整段演出。台词全部来自 Resources/Story/act1.txt。
///
/// 存档点：守卫登场（红字教学）那一刻记一次。之后按 R 重来，
/// 直接跳过前面的对话，从那里继续——魔女A 保持已死，不会再演一遍。
/// </summary>
public class Act1Cutscene : MonoBehaviour
{
    [Header("引用")]
    public Transform dog;
    public DogBark dogBark;
    public Transform witchA;
    public Transform witchB;
    public Transform witch;      // ？？？，就是第三关用的那个魔女预制体
    public Transform exit;

    [Header("触发距离")]
    public float witchMeetDistance = 1.6f;
    [Tooltip("狗离她多近时触发对话")]
    public float girlMeetDistance = 3f;
    [Tooltip("她走到离狗多近算走到了")]
    public float arriveDistance = 1.1f;
    [Tooltip("魔女离出口多近算她也到了")]
    public float witchExitRadius = 2.5f;

    // 存档点（同一次运行内有效）
    private static bool _resume;
    private static Vector2 _resumeDog;
    private static Vector2 _resumeWitch;

    private SpriteRenderer _srA, _srB;
    private SpriteRenderer _guardSr;
    private EnemyGuard _guardBrain;

    private void Start()
    {
        _srA = witchA != null ? witchA.GetComponent<SpriteRenderer>() : null;
        _srB = witchB != null ? witchB.GetComponent<SpriteRenderer>() : null;

        var guardGo = GameObject.Find("Guard_A1");
        if (guardGo != null)
        {
            _guardSr = guardGo.GetComponent<SpriteRenderer>();
            _guardBrain = guardGo.GetComponent<EnemyGuard>();

            if (_guardSr != null) _guardSr.enabled = false;
            if (_guardBrain != null) _guardBrain.enabled = false;
        }

        if (exit != null) exit.gameObject.SetActive(false);
        if (dogBark != null) dogBark.barkUnlocked = false;

        if (_resume) ApplyResume();

        StartCoroutine(Run());
    }

    private void OnDestroy()
    {
        GameplayLock.Release(this);
        GameplayLock.ReleasePlayer(this);
    }

    private IEnumerator Run()
    {
        if (!_resume) yield return RunStory();

        yield return RunGuardEpisode();
        yield return RunEnding();
    }

    /// <summary>重来时把世界恢复成存档点的样子。</summary>
    private void ApplyResume()
    {
        if (_srB != null) _srB.color = new Color(0.32f, 0.30f, 0.36f, 0.70f);
        if (_srA != null) _srA.color = new Color(0.42f, 0.38f, 0.48f, 0.85f);

        MoveTo(dog, _resumeDog);
        if (witch != null) MoveTo(witch, _resumeWitch);

        if (dogBark != null) dogBark.barkUnlocked = true;

        var cam = FindObjectOfType<FollowCamera>();
        if (cam != null) cam.Snap();
    }

    private static void MoveTo(Transform t, Vector2 pos)
    {
        var rb = t.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.position = pos;
        }

        // 刚体的位置要等下一次物理步才同步回 Transform，这里手动写一次，
        // 否则紧接着读 transform.position 会拿到旧值
        t.position = pos;
    }

    /// <summary>序章到相遇结束的整段。</summary>
    private IEnumerator RunStory()
    {
        var overlay = ScreenOverlay.Ensure();
        overlay.SetBlack();

        yield return Play("prologue");

        yield return overlay.FadeIn(1.4f);

        yield return Play("corridor");

        while (NearestWitchDistance() > witchMeetDistance) yield return null;

        yield return Play("witch1");

        GameplayLock.Hold(this);

        Vector2 between = witchA != null && witchB != null
            ? ((Vector2)witchA.position + (Vector2)witchB.position) * 0.5f
            : (Vector2)dog.position;

        yield return MoveDogTo(between, 1.3f);
        GameplayLock.Release(this);

        yield return Play("witch2");

        yield return overlay.Flash(0.5f);

        if (_srB != null) _srB.color = new Color(0.32f, 0.30f, 0.36f, 0.70f);
        if (_srA != null) _srA.color = new Color(0.42f, 0.38f, 0.48f, 0.85f);

        yield return Play("afterDeath");

        while (witch != null &&
               Vector2.Distance(dog.position, witch.position) > girlMeetDistance)
        {
            yield return null;
        }

        yield return Play("meet");

        GameplayLock.HoldPlayer(this);
        overlay.ShowPromptAbove(dog, "按下空格键发出声音");
        yield return null;

        while (!Input.GetKeyDown(KeyCode.Space)) yield return null;

        overlay.HidePrompt();

        if (dogBark != null)
        {
            dogBark.barkUnlocked = true;
            dogBark.Bark();
        }

        yield return WaitWitchArrives();

        GameplayLock.ReleasePlayer(this);

        yield return Play("vesper");
        yield return Play("reply");
    }

    /// <summary>守卫登场 + 红字教学。这里就是存档点。</summary>
    private IEnumerator RunGuardEpisode()
    {
        if (_guardSr != null) _guardSr.enabled = true;
        if (_guardBrain != null)
        {
            _guardBrain.visionRange = 8f;
            _guardBrain.FaceDirection(Vector2.down);
            _guardBrain.enabled = true;
        }

        // 存档点只在"第一次走到这里"时记录，回溯后不要覆盖
        if (!_resume)
        {
            _resume = true;
            _resumeDog = dog.position;
            _resumeWitch = witch != null ? (Vector2)witch.position : (Vector2)dog.position;
        }

        yield return Play("guard");

        // 守卫登场之后，这一幕剩下的时间一直放追击音乐
        AudioManager.Ensure().ForceChase(true);
    }

    /// <summary>出口 + 结局。</summary>
    private IEnumerator RunEnding()
    {
        if (exit != null) exit.gameObject.SetActive(true);

        yield return WaitExitReached();

        var overlay = ScreenOverlay.Ensure();
        yield return overlay.FadeToBlack(1.2f);
        yield return Play("ending");

        GameplayLock.Hold(this);
        yield return new WaitForSeconds(1.2f);
        LevelFlow.GoTo("Level1");
    }

    private static DialogueLine[] Block(string name) => DialogueScript.Get("act1", name);

    private IEnumerator Play(string block)
    {
        bool done = false;
        DialogueUI.Ensure().Play(Block(block), () => done = true);
        while (!done) yield return null;
    }

    private float NearestWitchDistance()
    {
        float d = float.MaxValue;
        if (witchA != null) d = Mathf.Min(d, Vector2.Distance(dog.position, witchA.position));
        if (witchB != null) d = Mathf.Min(d, Vector2.Distance(dog.position, witchB.position));
        return d;
    }

    private IEnumerator WaitWitchArrives()
    {
        if (witch == null) yield break;

        while (Vector2.Distance(witch.position, dog.position) > arriveDistance)
        {
            yield return null;
        }
    }

    private IEnumerator WaitExitReached()
    {
        if (exit == null) yield break;

        while (true)
        {
            bool dogAt = Vector2.Distance(dog.position, exit.position) <= 1.2f;
            bool witchAt = witch == null ||
                           Vector2.Distance(witch.position, exit.position) <= witchExitRadius;

            if (dogAt && witchAt) yield break;
            yield return null;
        }
    }

    private IEnumerator MoveDogTo(Vector2 dest, float duration)
    {
        var rb = dog.GetComponent<Rigidbody2D>();
        Vector2 start = rb != null ? rb.position : (Vector2)dog.position;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            Vector2 p = Vector2.Lerp(start, dest, k);

            if (rb != null) rb.position = p;
            else dog.position = p;

            yield return null;
        }

        if (rb != null) rb.position = dest;
        else dog.position = dest;
    }
}
