using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 把敌人的视野画出来：扇形被墙切断，颜色跟着状态变。
///   巡逻 / 回岗 → 暗红
///   调查 / 搜索 → 黄色
///   追击       → 亮红
/// 挂在带 EnemyBase 的物体上，会自动建一个子网格。
/// </summary>
[DisallowMultipleComponent]
public class VisionCone : MonoBehaviour
{
    [Range(8, 64)] public int rays = 26;
    public int sortingOrder = 4;
    public float edgeAlpha = 0.55f;

    private EnemyBase _enemy;
    private Mesh _mesh;
    private Vector3[] _verts;
    private Color[] _colors;
    private Material _mat;

    private void Awake()
    {
        _enemy = GetComponent<EnemyBase>();
        if (_enemy == null)
        {
            Debug.LogWarning("[VisionCone] 同物体上没有 EnemyBase，视野锥关掉", this);
            enabled = false;
            return;
        }

        var go = new GameObject("VisionCone");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.layer = gameObject.layer;

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sortingOrder = sortingOrder;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        _mat = new Material(shader);
        mr.material = _mat;

        _mesh = new Mesh { name = "VisionCone" };
        _mesh.MarkDynamic();
        mf.mesh = _mesh;

        _verts = new Vector3[rays + 2];
        _colors = new Color[rays + 2];
        var tris = new int[rays * 3];
        for (int i = 0; i < rays; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }
        _mesh.vertices = _verts;
        _mesh.triangles = tris;
        _mesh.colors = _colors;
    }

    private void LateUpdate()
    {
        if (_enemy == null || _mesh == null) return;

        Vector2 origin = transform.position;
        Vector2 f = _enemy.Facing;
        float baseAngle = Mathf.Atan2(f.y, f.x);
        float half = _enemy.visionAngle * 0.5f * Mathf.Deg2Rad;
        float range = _enemy.visionRange;

        Color c = ColorFor(_enemy.CurrentState);
        c.a *= edgeAlpha;

        _verts[0] = Vector3.zero;
        _colors[0] = c;

        for (int i = 0; i <= rays; i++)
        {
            float a = baseAngle - half + (i / (float)rays) * (half * 2f);
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

            var hit = Physics2D.Raycast(origin, dir, range, _enemy.obstacleMask);
            float d = hit.collider != null ? hit.distance : range;

            Vector2 world = origin + dir * d;
            _verts[i + 1] = transform.InverseTransformPoint(world);
            _colors[i + 1] = c;
        }

        _mesh.vertices = _verts;
        _mesh.colors = _colors;
        _mesh.RecalculateBounds();
    }

    private static Color ColorFor(EnemyBase.State s)
    {
        switch (s)
        {
            case EnemyBase.State.Chase:
                return new Color(1f, 0.25f, 0.22f, 0.30f);
            case EnemyBase.State.Investigate:
            case EnemyBase.State.Search:
                return new Color(1f, 0.78f, 0.25f, 0.22f);
            default:
                return new Color(0.9f, 0.35f, 0.30f, 0.12f);
        }
    }
}
