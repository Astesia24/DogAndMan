using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 走到范围内就播一段对话。
/// 挂在一个空物体上，把台词填进 lines 数组即可；立绘可以逐条留空。
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    [Header("台词")]
    public DialogueLine[] lines;

    [Header("触发")]
    [Tooltip("勾上 = 按高度触发（狗第一次到达 triggerY 时）；不勾 = 按距离触发")]
    public bool useHeightTrigger = false;

    [Tooltip("高度触发的 y 值")]
    public float triggerY = 0f;

    [Tooltip("离多近触发（只在距离模式下生效）")]
    public float radius = 6f;
    [Tooltip("留空会自动找狗")]
    public Transform target;
    [Tooltip("只播一次")]
    public bool once = true;

    [Header("回调（可以接场景切换）")]
    public UnityEvent onFinished;

    private bool _played;

    private void Start()
    {
        if (target == null)
        {
            var dog = FindObjectOfType<DogBark>();
            if (dog != null) target = dog.transform;
        }
    }

    private void Update()
    {
        if (_played || target == null) return;
        if (DialogueUI.IsActive) return;

        bool hit = useHeightTrigger
            ? target.position.y >= triggerY
            : Vector2.Distance(transform.position, target.position) <= radius;

        if (!hit) return;

        _played = once;
        DialogueUI.Ensure().Play(lines, () => onFinished?.Invoke());
    }

    private void OnDrawGizmosSelected()
    {
        if (useHeightTrigger)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.8f);
            Gizmos.DrawLine(new Vector3(-10f, triggerY, 0f), new Vector3(30f, triggerY, 0f));
            return;
        }

        Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
