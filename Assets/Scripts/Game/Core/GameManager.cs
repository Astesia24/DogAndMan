using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡状态：进行中 / 成功 / 失败。所有成败都走这里，方便以后接 UI 和存档。
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum LevelState { Playing, Won, Lost }

    public static GameManager Instance { get; private set; }

    public LevelState State { get; private set; } = LevelState.Playing;
    public string LoseReason { get; private set; }
    public event System.Action<LevelState> StateChanged;

    private GUIStyle _style;

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
    }

    public void Win()
    {
        if (State != LevelState.Playing) return;

        State = LevelState.Won;
        StateChanged?.Invoke(State);
    }

    public void Lose(string reason)
    {
        if (State != LevelState.Playing) return;

        State = LevelState.Lost;
        LoseReason = reason;
        StateChanged?.Invoke(State);
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
        if (State == LevelState.Playing) return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }

        _style.normal.textColor = State == LevelState.Won
            ? new Color(0.45f, 1f, 0.55f)
            : new Color(1f, 0.45f, 0.4f);

        string text = State == LevelState.Won
            ? "她逃出来了。\n\n按 R 再来一次"
            : $"她被抓住了。\n{LoseReason}\n\n按 R 重来";

        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 260), text, _style);
    }
}
