using System.Collections.Generic;

/// <summary>
/// 全局操作锁，分两档：
/// 全体锁 —— 狗和魔女都停（对话、暂停）；
/// 只锁玩家 —— 狗不动，魔女照常走（演出里等她走过来）。
/// 狗查 IsPlayerLocked，魔女和守卫查 IsLocked。
/// </summary>
public static class GameplayLock
{
    private static readonly List<object> _global = new List<object>();
    private static readonly List<object> _player = new List<object>();

    public static bool IsLocked => _global.Count > 0;
    public static bool IsPlayerLocked => _global.Count > 0 || _player.Count > 0;

    public static void Hold(object owner) => Add(_global, owner);
    public static void Release(object owner) => Remove(_global, owner);

    public static void HoldPlayer(object owner) => Add(_player, owner);
    public static void ReleasePlayer(object owner) => Remove(_player, owner);

    public static void ReleaseAll()
    {
        _global.Clear();
        _player.Clear();
    }

    private static void Add(List<object> list, object owner)
    {
        if (owner != null && !list.Contains(owner)) list.Add(owner);
    }

    private static void Remove(List<object> list, object owner)
    {
        if (owner != null) list.Remove(owner);
    }
}
