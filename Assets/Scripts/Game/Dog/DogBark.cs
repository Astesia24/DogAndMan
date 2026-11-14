using System;
using UnityEngine;

/// <summary>
/// 狗叫：按一下发出一次声波。这是整个游戏里唯一能主动制造声音的按键。
/// 固定脚本：只负责"我在哪、叫多远"，不知道魔女和敌人的存在。
/// </summary>
public class DogBark : MonoBehaviour
{
    [Header("按键")]
    public KeyCode barkKey = KeyCode.Space;

    [Header("声音")]
    [Tooltip("声波能传多远")]
    public float radius = 7f;
    [Tooltip("冷却时间（秒）")]
    public float cooldown = 3.5f;

    [Header("反馈（可留空）")]
    [Tooltip("叫的时候会缩一下，留空就自动用本体的 SpriteRenderer")]
    public Transform visual;
    public float punchScale = 1.25f;

    /// <summary>还能不能叫。</summary>
    public bool CanBark => _remaining <= 0f;
    public float Remaining => Mathf.Max(0f, _remaining);
    public float CooldownProgress => 1f - Mathf.Clamp01(_remaining / Mathf.Max(0.01f, cooldown));

    public event Action Barked;

    private float _remaining;
    private Vector3 _baseScale;
    private float _punch;

    private void Awake()
    {
        if (visual == null) visual = transform;
        _baseScale = visual.localScale;
    }

    private void Update()
    {
        if (_remaining > 0f) _remaining -= Time.deltaTime;

        if (_punch > 0f)
        {
            _punch = Mathf.Max(0f, _punch - Time.deltaTime * 4f);
            if (visual != null) visual.localScale = _baseScale * (1f + (_punch * (punchScale - 1f)));
        }

        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.LevelState.Playing) return;

        if (Input.GetKeyDown(barkKey) && _remaining <= 0f) Bark();
    }

    public void Bark()
    {
        _remaining = cooldown;
        _punch = 1f;

        SoundSystem.Ensure().Emit(transform.position, radius, this, SoundType.Bark);
        Barked?.Invoke();
    }
}
