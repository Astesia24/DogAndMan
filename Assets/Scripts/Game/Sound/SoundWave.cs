using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一次声波：从声源向外扩张，形状被墙切开（64 条射线），
/// 波前碰到谁就让谁听见，然后淡出消失。
/// </summary>
public class SoundWave : MonoBehaviour
{
    private Vector2 _origin;
    private float _maxRadius;
    private float _speed;
    private float _radius;
    private float _fadeLife;
    private float _fadeDuration = 0.35f;

    private LayerMask _blockers;
    private Color _color;
    private Material _material;
    private Object _emitter;
    private SoundType _type;

    private int _segments;
    private Vector2[] _dirs;
    private LineRenderer _line;
    private readonly HashSet<ISoundListener> _hit = new HashSet<ISoundListener>();
    private bool _done;

    public void Init(Vector2 origin, float radius, float speed, int segments, LayerMask blockers,
                     Color color, float width, Material material, int order, Object emitter, SoundType type)
    {
        _origin = origin;
        _maxRadius = radius;
        _speed = Mathf.Max(1f, speed);
        _segments = Mathf.Max(8, segments);
        _blockers = blockers;
        _color = color;
        _material = material;
        _emitter = emitter;
        _type = type;

        _dirs = new Vector2[_segments];
        for (int i = 0; i < _segments; i++)
        {
            float a = i * Mathf.PI * 2f / _segments;
            _dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        var ringGo = new GameObject("Ring");
        ringGo.transform.SetParent(transform, false);

        _line = ringGo.AddComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.loop = true;
        _line.positionCount = _segments;
        _line.widthMultiplier = Mathf.Max(0.01f, width);
        _line.numCapVertices = 0;
        _line.numCornerVertices = 0;
        _line.alignment = LineAlignment.View;
        _line.textureMode = LineTextureMode.Stretch;
        _line.sortingOrder = order;
        if (material != null) _line.sharedMaterial = material;
        _line.startColor = color;
        _line.endColor = color;

        UpdateShape();
    }

    private void Update()
    {
        if (_done) return;

        if (_radius < _maxRadius)
        {
            _radius = Mathf.Min(_radius + _speed * Time.deltaTime, _maxRadius);
            UpdateShape();
        }
        else
        {
            _fadeLife += Time.deltaTime;
            if (_fadeLife >= _fadeDuration)
            {
                _done = true;
                Destroy(gameObject);
                return;
            }

            Color c = _color;
            c.a *= 1f - _fadeLife / _fadeDuration;
            if (_line != null)
            {
                _line.startColor = c;
                _line.endColor = c;
            }
        }

        CheckListeners();
    }

    private void UpdateShape()
    {
        if (_line == null) return;

        for (int i = 0; i < _segments; i++)
        {
            float d = _radius;
            var hit = Physics2D.Raycast(_origin, _dirs[i], _radius, _blockers);
            if (hit.collider != null) d = hit.distance;

            _line.SetPosition(i, _origin + _dirs[i] * d);
        }
    }

    private void CheckListeners()
    {
        var all = SoundSystem.Listeners;

        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (i >= all.Count) continue;

            var listener = all[i];

            // 接口变量不能用 Unity 的 "== null" 判空，得先转成 Object
            if (listener is Object obj && obj == null) continue;
            if (!listener.CanHear) continue;
            if (_emitter != null && (listener as Object) == _emitter) continue;
            if (_hit.Contains(listener)) continue;

            Vector2 p = listener.SoundPosition;
            if (Vector2.Distance(_origin, p) > _radius) continue;

            // 墙挡住了就听不见——声音和视线用同一套规则
            if (Physics2D.Linecast(_origin, p, _blockers)) continue;

            _hit.Add(listener);
            listener.OnSoundHeard(_origin, _type);
        }
    }
}
