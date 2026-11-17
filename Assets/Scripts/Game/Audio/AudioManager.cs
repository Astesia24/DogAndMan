using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 音频管理器：一条背景音，一条追击音。
///
/// 常态：背景音循环。
/// 追击：有敌人视野里出现魔女时播追击音，并打断背景音；看不见 0.8 秒后切回。
/// 强制：第一幕守卫登场之后调用 ForceChase(true)，这一幕一直放追击音。
///
/// 每次切场景（含按 R 回溯）都会重置：打断追击音，回到背景音。
///
/// 音频从 Resources/Audio/AudioConfig 读取，没有配置时退回 Audio/BGM、Audio/Chase。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音量（0~1）")]
    [Range(0f, 1f)] public float master = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.85f;
    [Range(0f, 1f)] public float chaseVolume = 0.5f;

    [Tooltip("看不见之后多久才切回背景音，避免来回闪")]
    public float chaseHold = 0.8f;

    private AudioSource _bgm;
    private AudioSource _chase;
    private bool _chasing;
    private float _lastSeen;

    /// <summary>强制追击：不管有没有被发现，一直放追击音。</summary>
    public bool ForcedChase { get; private set; }

    public bool IsChasing => _chasing;

    public static AudioManager Ensure()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        return go.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _bgm = gameObject.AddComponent<AudioSource>();
        _bgm.loop = true;
        _bgm.playOnAwake = false;

        _chase = gameObject.AddComponent<AudioSource>();
        _chase.loop = true;
        _chase.playOnAwake = false;

        LoadClips();

        ApplyVolumes();
        PlayBgm();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetMusic();
    }

    private void LoadClips()
    {
        var config = Resources.Load<AudioConfig>("Audio/AudioConfig");

        if (config != null)
        {
            _bgm.clip = config.bgm;
            _chase.clip = config.chase;
        }
        else
        {
            _bgm.clip = Resources.Load<AudioClip>("Audio/BGM");
            _chase.clip = Resources.Load<AudioClip>("Audio/Chase");
        }

        if (_bgm.clip == null && _chase.clip == null)
        {
            Debug.Log("[AudioManager] 还没配置音频。点 Tools → 音频 → 创建或打开音频配置");
        }
    }

    private void Update()
    {
        bool spotted = AnyEnemySeesWitch();
        if (spotted) _lastSeen = Time.unscaledTime;

        bool wantChase = ForcedChase || spotted || (Time.unscaledTime - _lastSeen) < chaseHold;
        if (wantChase == _chasing) return;

        _chasing = wantChase;

        if (_chasing) PlayChase();
        else PlayBgm();
    }

    /// <summary>强制的追击音（第一幕守卫登场后打开）。</summary>
    public void ForceChase(bool on)
    {
        ForcedChase = on;

        if (on && !_chasing)
        {
            _chasing = true;
            PlayChase();
        }
        else if (!on && _chasing)
        {
            _chasing = false;
            PlayBgm();
        }
    }

    /// <summary>
    /// 切场景 / 回溯时重置：打断追击音，回到背景音。
    /// 背景音本来就还在放的话不打断它——音乐要跨关卡连续，不能每关重头播。
    /// </summary>
    public void ResetMusic()
    {
        ForcedChase = false;
        _chasing = false;
        _lastSeen = 0f;

        if (_chase != null) _chase.Stop();

        if (_bgm != null && _bgm.clip != null && !_bgm.isPlaying)
        {
            _bgm.volume = bgmVolume;
            _bgm.Play();
        }
    }

    private static bool AnyEnemySeesWitch()
    {
        for (int i = 0; i < EnemyBase.All.Count; i++)
        {
            var e = EnemyBase.All[i];
            if (e != null && e.CanSeeWitch()) return true;
        }
        return false;
    }

    private void PlayBgm()
    {
        if (_chase != null) _chase.Stop();
        if (_bgm != null && _bgm.clip != null && !_bgm.isPlaying) _bgm.Play();
    }

    private void PlayChase()
    {
        if (_bgm != null) _bgm.Stop();
        if (_chase != null && _chase.clip != null) _chase.Play();
    }

    public void SetMaster(float value)
    {
        master = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetBgm(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetChase(float value)
    {
        chaseVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = master;

        if (_bgm != null) _bgm.volume = bgmVolume;
        if (_chase != null) _chase.volume = chaseVolume;
    }
}
