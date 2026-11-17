using UnityEngine;

/// <summary>
/// 钟：狗靠近按 E 敲响，发出一声 13 米的巨响。
/// **只招守卫，不招魔女** —— 她是靠狗叫走的。
/// </summary>
public class Bell : MonoBehaviour
{
    [Header("按键")]
    public KeyCode key = KeyCode.E;
    [Tooltip("站多近才能敲")]
    public float range = 1f;

    [Header("声音")]
    public float radius = 13f;
    public float cooldown = 6f;

    [Header("引用")]
    [Tooltip("留空会自动找场景里的狗")]
    public Transform dog;

    public float CooldownRemaining { get; private set; }

    private void Awake()
    {
        if (dog == null)
        {
            var d = FindObjectOfType<DogMotor>();
            if (d != null) dog = d.transform;
        }
    }

    private void Update()
    {
        if (CooldownRemaining > 0f) CooldownRemaining -= Time.deltaTime;
        if (CooldownRemaining > 0f) return;
        if (!Input.GetKeyDown(key)) return;
        if (dog == null) return;
        if (Vector2.Distance(transform.position, dog.position) > range) return;

        Ring();
    }

    public void Ring()
    {
        if (CooldownRemaining > 0f) return;

        CooldownRemaining = cooldown;
        SoundSystem.Ensure().Emit(transform.position, radius, this, SoundType.Bell);
    }
}
