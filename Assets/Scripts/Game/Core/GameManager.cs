using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡状态：进行中 / 成功 / 失败。
/// 第一次失败会播一段演出（黑屏 + 台词），之后每一次都只是缓慢变黑 + 红光一闪。
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum LevelState { Playing, Won, Lost }

    public static GameManager Instance { get; private set; }

    public LevelState State { get; private set; } = LevelState.Playing;
    public string LoseReason { get; private set; }
    public event System.Action<LevelState> StateChanged;

    /// <summary>本次运行里是否已经播过第一次失败的演出。</summary>
    public static bool FirstDeathSeen { get; private set; }

    private GUIStyle _style;
    private bool _showWinText = true;

    public static GameManager Ensure()
    {
        if (Instance != null) return Instance;

        Instance = FindObjectOfType<GameManager>();
        if (Instance == null)
        {
            Instance = new GameObject("GameManager").AddComponent<GameManager>();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
    }

    /// <summary>通关。showText = false 用于直接切下一关，不弹"按 R 再来一次"。</summary>
    public void Win(bool showText = true)
    {
        if (State != LevelState.Playing) return;

        State = LevelState.Won;
        _showWinText = showText;
        Time.timeScale = 0f;
        StateChanged?.Invoke(State);
    }

    public void Lose(string reason)
    {
        if (State != LevelState.Playing) return;

        State = LevelState.Lost;
        LoseReason = reason;
        Time.timeScale = 0f;
        StateChanged?.Invoke(State);

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        var overlay = ScreenOverlay.Ensure();
        bool first = !FirstDeathSeen;

        if (first)
        {
            FirstDeathSeen = true;

            yield return overlay.FadeToBlack(0.6f);

            bool done = false;
            DialogueUI.Ensure().Play(DialogueScript.Get("death", "firstDeath"), () => done = true);
            while (!done) yield return null;
        }
        else
        {
            yield return overlay.FadeToBlack(1.2f);
            yield return overlay.Flash(0.35f, new Color(0.80f, 0.10f, 0.10f));
        }

        overlay.ShowCenterHint("按R回溯到开始", 1.5f);
    }

    private void Update()
    {
        if (State != LevelState.Playing && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnGUI()
    {
        if (State != LevelState.Won || !_showWinText) return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }

        _style.normal.textColor = new Color(0.45f, 1f, 0.55f);

        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 260),
                  "她逃出来了。\n\n按 R 再来一次", _style);
    }
}
