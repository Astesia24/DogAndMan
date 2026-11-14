using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    Bark,      // 狗叫
    Footstep,  // 脚步
    Impact     // 撞击、陶罐碎裂
}

/// <summary>能被声音波及的东西（魔女、敌人）。</summary>
public interface ISoundListener
{
    Vector2 SoundPosition { get; }
    bool CanHear { get; }
    void OnSoundHeard(Vector2 source, SoundType type);
}

/// <summary>
/// 声音总控：负责生成声波、解析遮挡层、以及维护"谁在听"的名单。
/// 场景里不需要手动放，第一次 Emit 时会自动创建；建议手动放一个以便调颜色。
/// </summary>
public class SoundSystem : MonoBehaviour
{
    public static SoundSystem Instance { get; private set; }
    public static readonly List<ISoundListener> Listeners = new List<ISoundListener>();

    [Header("遮挡层：声音和视线都会被这些层挡住（只放墙）")]
    public LayerMask soundBlockers;

    [Header("声波表现")]
    [Tooltip("声波扩散速度（米/秒）")]
    public float waveSpeed = 14f;
    [Tooltip("射线数量，越大形状越精确，64 够用")]
    [Range(16, 128)] public int segments = 64;
    public float lineWidth = 0.07f;
    public Color waveColor = new Color(0.55f, 0.9f, 1f, 0.9f);
    [Tooltip("留空会自动用 Sprites/Default")]
    public Material waveMaterial;
    [Tooltip("画在其它东西之上")]
    public int sortingOrder = 60;

    private Material _runtimeMaterial;

    public static SoundSystem Ensure()
    {
        if (Instance != null) return Instance;

        Instance = FindObjectOfType<SoundSystem>();
        if (Instance == null)
        {
            Instance = new GameObject("SoundSystem").AddComponent<SoundSystem>();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        EnsureBlockers();
    }

    /// <summary>发一次声。radius 是能传多远，emitter 自己不会被自己吵到。</summary>
    public void Emit(Vector2 origin, float radius, Object emitter, SoundType type)
    {
        if (radius <= 0.01f) return;

        var go = new GameObject($"Sound_{type}");
        var wave = go.AddComponent<SoundWave>();
        wave.Init(
            origin: origin,
            radius: radius,
            speed: waveSpeed,
            segments: segments,
            blockers: EnsureBlockers(),
            color: waveColor,
            width: lineWidth,
            material: ResolveMaterial(),
            order: sortingOrder,
            emitter: emitter,
            type: type);
    }

    public LayerMask EnsureBlockers()
    {
        if (soundBlockers.value == 0)
        {
            soundBlockers = LayerMask.GetMask("Wall");
            if (soundBlockers.value == 0)
            {
                Debug.LogWarning("[SoundSystem] 没有设置遮挡层，也没有找到名为 Wall 的 Layer。" +
                                 "声音会无视所有墙壁穿过一切。", this);
            }
        }
        return soundBlockers;
    }

    private Material ResolveMaterial()
    {
        if (waveMaterial != null) return waveMaterial;

        if (_runtimeMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) _runtimeMaterial = new Material(shader);
        }
        return _runtimeMaterial;
    }
}
