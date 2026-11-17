using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>运行时 UI 的共用工具：事件系统和字体。</summary>
public static class UiRuntime
{
    public static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(go);
    }

    public static Font Font()
    {
        var font = Resources.Load<Font>("Fonts/LXGWWenKai-Regular");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font;
    }
}
