using System;
using UnityEngine;

public enum PortraitSide
{
    None = 0,
    Left = 1,
    Right = 2,
    Center = 3
}

public enum DialogueEffect
{
    None = 0,
    FlashRed = 1,
    FlashRedTwice = 2,
    FlashRedThenWhite = 3,
    FlashBlue = 4
}

/// <summary>
/// 一条台词。立绘是可选的：portrait 留空、或者 side 设成 None，这条就不显示立绘。
/// </summary>
[Serializable]
public class DialogueLine
{
    [Tooltip("说话的人；留空 = 不显示名字（旁白）")]
    public string speaker = "";

    [TextArea(2, 6)]
    public string text = "";

    public Sprite portrait;
    public PortraitSide side = PortraitSide.None;

    [Tooltip("这一句把立绘清掉（数据里写 none）")]
    public bool clearPortrait;

    [Tooltip("这一句把当前立绘压暗，表示说话的人没有立绘（数据里写 dim）")]
    public bool dimPortrait;

    [Tooltip("立绘染色（没有美术时可以用纯色方块占位）")]
    public Color portraitTint = Color.white;

    [Tooltip("立绘淡入时长；0 = 直接出现")]
    public float portraitFade = 0f;

    [Tooltip("这一句出现时触发的画面效果")]
    public DialogueEffect effect = DialogueEffect.None;

    public Color nameColor = new Color(0.86f, 0.78f, 0.62f, 1f);

    [Tooltip("正文颜色；Alpha = 0 时用对话框默认色")]
    public Color textColor = Color.clear;

    [Tooltip("每秒打出多少字；0 = 瞬间显示")]
    public float charsPerSecond = 26f;
}
