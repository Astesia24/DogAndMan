using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡中途的存档点。第三关走到雪莉那里时记录一次位置，
/// 按 R 重来时把狗、魔女和守卫放回那个位置。
/// </summary>
public static class Checkpoint
{
    public static bool Has { get; private set; }
    public static string SceneName { get; private set; } = "";
    public static Vector2 DogPosition { get; private set; }
    public static Vector2 WitchPosition { get; private set; }
    public static bool SherryDone { get; set; }

    private static readonly Dictionary<string, Vector2> Enemies = new Dictionary<string, Vector2>();
    private static readonly Dictionary<string, bool> Gates = new Dictionary<string, bool>();
    private static readonly Dictionary<string, bool> Levers = new Dictionary<string, bool>();

    public static void Save()
    {
        var dog = Object.FindObjectOfType<DogMotor>();
        var witch = Object.FindObjectOfType<WitchBrain>();
        if (dog == null) return;

        Has = true;
        SceneName = SceneManager.GetActiveScene().name;
        DogPosition = Position(dog.transform);
        WitchPosition = witch != null ? Position(witch.transform) : DogPosition;

        Enemies.Clear();
        foreach (var e in EnemyBase.All)
        {
            if (e != null) Enemies[e.name] = Position(e.transform);
        }

        // 门和拉杆的状态也要记住，不然回溯后门会回到关卡默认状态
        Gates.Clear();
        foreach (var g in Object.FindObjectsOfType<Gate>(true))
        {
            if (g != null) Gates[g.name] = g.IsOpen;
        }

        Levers.Clear();
        foreach (var l in Object.FindObjectsOfType<Lever>(true))
        {
            if (l != null) Levers[l.name] = l.Pulled;
        }
    }

    private static Vector2 Position(Transform t)
    {
        var rb = t.GetComponent<Rigidbody2D>();
        return rb != null ? rb.position : (Vector2)t.position;
    }

    public static void Restore()
    {
        if (!Has) return;
        if (SceneName != SceneManager.GetActiveScene().name) return;

        var dog = Object.FindObjectOfType<DogMotor>();
        if (dog != null) Move(dog.transform, DogPosition);

        var witch = Object.FindObjectOfType<WitchBrain>();
        if (witch != null) Move(witch.transform, WitchPosition);

        foreach (var e in EnemyBase.All)
        {
            if (e != null && Enemies.TryGetValue(e.name, out var pos)) Move(e.transform, pos);
        }

        // 门的状态最后还原（不夹人、不重复触发拉杆）
        foreach (var l in Object.FindObjectsOfType<Lever>(true))
        {
            if (l != null && Levers.TryGetValue(l.name, out bool pulled)) l.RestorePulled(pulled);
        }

        foreach (var g in Object.FindObjectsOfType<Gate>(true))
        {
            if (g != null && Gates.TryGetValue(g.name, out bool open)) g.RestoreState(open);
        }
    }

    public static void Clear()
    {
        Has = false;
        SceneName = "";
        Enemies.Clear();
        Gates.Clear();
        Levers.Clear();
        SherryDone = false;
    }

    private static void Move(Transform t, Vector2 pos)
    {
        var rb = t.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.position = pos;
        }

        t.position = pos;
    }
}
