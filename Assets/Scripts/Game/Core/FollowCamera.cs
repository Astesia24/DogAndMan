using UnityEngine;

/// <summary>
/// 跟随相机：跟着狗走，平滑插值，并且夹在地图边界内。
/// 视野默认 28 × 16 米 —— 和网页版一致。
/// </summary>
[RequireComponent(typeof(Camera))]
public class FollowCamera : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("视野（米）")]
    public float viewWidth = 28f;
    public float viewHeight = 16f;

    [Header("地图范围（米）")]
    public Vector2 mapOrigin = Vector2.zero;
    public Vector2 mapSize = new Vector2(48f, 28f);

    [Header("手感")]
    [Tooltip("越大跟得越紧")]
    public float smooth = 8f;
    public bool snapOnStart = true;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.orthographic = true;
    }

    private void Start()
    {
        if (snapOnStart) Snap();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector2 want = ClampCenter(target.position);

        // 帧率无关的指数平滑
        float k = 1f - Mathf.Exp(-smooth * Time.deltaTime);

        Vector3 p = transform.position;
        p.x = Mathf.Lerp(p.x, want.x, k);
        p.y = Mathf.Lerp(p.y, want.y, k);
        transform.position = p;
    }

    public void Snap()
    {
        if (target == null) return;

        Vector2 c = ClampCenter(target.position);
        transform.position = new Vector3(c.x, c.y, transform.position.z);
    }

    private Vector2 ClampCenter(Vector3 worldPos)
    {
        float halfW = viewWidth * 0.5f;
        float halfH = viewHeight * 0.5f;

        float minX = mapOrigin.x + halfW;
        float maxX = mapOrigin.x + mapSize.x - halfW;
        float minY = mapOrigin.y + halfH;
        float maxY = mapOrigin.y + mapSize.y - halfH;

        // 地图比视野还小的时候，直接居中
        float x = minX > maxX ? mapOrigin.x + mapSize.x * 0.5f : Mathf.Clamp(worldPos.x, minX, maxX);
        float y = minY > maxY ? mapOrigin.y + mapSize.y * 0.5f : Mathf.Clamp(worldPos.y, minY, maxY);

        _cam.orthographicSize = halfH;

        return new Vector2(x, y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireCube(mapOrigin + mapSize * 0.5f, mapSize);
    }
}
