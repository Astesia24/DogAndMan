using UnityEngine;

/// <summary>
/// 火枪手。继承普通守卫，会巡逻、会去看声音、会追最后看见的位置，
/// 但一旦看见魔女就不再靠近，而是原地瞄准：
///
///   1. 一条红色枪线一直跟着魔女
///   2. 视野锥在 aimTime 内收缩到 0（这就是开枪倒计时）
///   3. 缩到 0 的瞬间，如果魔女还暴露在他的视野里 → 直接判定死亡
///   4. 瞄准途中魔女脱离视线 → 取消瞄准，像普通守卫一样去最后看见的位置
///
/// 没有巡逻点时就站在原地，也可以正常巡逻。
/// </summary>
public class EnemyMarksman : EnemyGuard
{
    [Header("瞄准")]
    [Tooltip("从看见到开枪要多久（视野锥收缩的时间）")]
    public float aimTime = 1.2f;

    [Tooltip("枪线颜色")]
    public Color aimColor = new Color(1f, 0.18f, 0.15f, 0.95f);

    [Tooltip("枪线粗细")]
    public float lineWidth = 0.07f;

    [Tooltip("枪线的渲染层级（要压在地面和角色之上）")]
    public int lineSortingOrder = 62;

    public bool IsAiming => _aiming;

    private bool _aiming;
    private float _aimTimer;
    private float _savedRange;
    private LineRenderer _line;

    protected override void TickChaseCore(bool sees)
    {
        if (!_aiming)
        {
            if (sees) BeginAim();
            else SetState(State.Search);   // 没看见：和普通守卫一样去最后看见的位置

            return;
        }

        // 瞄准中
        Stop();

        _aimTimer += Time.fixedDeltaTime;

        float k = aimTime > 0.01f ? Mathf.Clamp01(_aimTimer / aimTime) : 1f;

        // 视野锥收缩 —— VisionCone 每帧读 visionRange，所以看上去就是在缩
        visionRange = Mathf.Lerp(_savedRange, 0f, k);

        UpdateLine();

        // 判定用瞄准开始时的视野：她只要中途躲掉就取消
        bool exposed = CanSeeWithRange(_savedRange);

        if (k >= 1f)
        {
            if (exposed) Fire();
            else CancelAim();

            return;
        }

        if (!exposed) CancelAim();
    }

    protected override void OnStateEntered(State next)
    {
        if (_aiming && next != State.Chase) CancelAim();
    }

    private void BeginAim()
    {
        _aiming = true;
        _aimTimer = 0f;
        _savedRange = visionRange;

        Stop();
        ShowLine();
    }

    private void CancelAim()
    {
        if (!_aiming) return;

        _aiming = false;
        visionRange = _savedRange;
        HideLine();
    }

    private void Fire()
    {
        _aiming = false;
        visionRange = _savedRange;

        // 枪线闪一下再消失
        if (_line != null)
        {
            _line.startColor = Color.white;
            _line.endColor = Color.white;
        }

        HideLine();

        GameManager.Ensure().Lose($"{name} 开枪击中了她");
    }

    private void ShowLine()
    {
        if (_line == null)
        {
            var go = new GameObject("AimLine");
            go.transform.SetParent(transform, false);

            _line = go.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.widthMultiplier = lineWidth;
            _line.numCapVertices = 0;
            _line.sortingOrder = lineSortingOrder;
            _line.textureMode = LineTextureMode.Stretch;

            var shader = Shader.Find("Sprites/Default");
            if (shader != null) _line.material = new Material(shader);
        }

        _line.startColor = aimColor;
        _line.endColor = aimColor;
        _line.enabled = true;

        UpdateLine();
    }

    private void UpdateLine()
    {
        if (_line == null || witch == null) return;

        _line.SetPosition(0, transform.position);
        _line.SetPosition(1, witch.position);
    }

    private void HideLine()
    {
        if (_line != null) _line.enabled = false;
    }

    protected override void OnDisable()
    {
        HideLine();
        base.OnDisable();
    }
}
