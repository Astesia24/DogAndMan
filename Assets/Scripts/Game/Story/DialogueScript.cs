using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话脚本读取器。文本放在 Resources/Story/*.txt，改台词不用碰代码。
///
/// 格式：
///   # 注释行
///   @块名
///   说话人|台词|立绘|位置|出现|特效|高亮
///   @end
///
/// 字段都可以留空：
///   立绘  first / second / vesper
///   位置  left / center / right
///   出现  fade / instant
///   特效  flash1 / flash2
///   高亮  blue
/// </summary>
public static class DialogueScript
{
    private static readonly Dictionary<string, Dictionary<string, DialogueLine[]>> Cache
        = new Dictionary<string, Dictionary<string, DialogueLine[]>>();

    private static readonly Dictionary<string, Sprite> Portraits
        = new Dictionary<string, Sprite>();

    public static DialogueLine[] Get(string fileName, string block)
    {
        if (!Cache.TryGetValue(fileName, out var blocks))
        {
            blocks = Load(fileName);
            Cache[fileName] = blocks;
        }

        return blocks.TryGetValue(block, out var lines) ? lines : new DialogueLine[0];
    }

    private static Dictionary<string, DialogueLine[]> Load(string fileName)
    {
        var result = new Dictionary<string, DialogueLine[]>();

        var asset = Resources.Load<TextAsset>("Story/" + fileName);
        if (asset == null)
        {
            Debug.LogWarning($"[DialogueScript] 找不到 Resources/Story/{fileName}.txt");
            return result;
        }

        string blockName = null;
        var current = new List<DialogueLine>();

        foreach (var raw in asset.text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            if (line.StartsWith("@"))
            {
                string cmd = line.Substring(1).Trim();

                if (cmd == "end")
                {
                    if (blockName != null) result[blockName] = current.ToArray();
                    current.Clear();
                    blockName = null;
                }
                else
                {
                    blockName = cmd;
                    current.Clear();
                }
                continue;
            }

            if (blockName == null) continue;
            current.Add(ParseLine(line));
        }

        return result;
    }

    private static DialogueLine ParseLine(string line)
    {
        var parts = line.Split('|');

        var d = new DialogueLine
        {
            speaker = Field(parts, 0),
            text = Field(parts, 1),
            side = Side(Field(parts, 3)),
            portraitFade = Field(parts, 4) == "fade" ? 0.45f : 0f,
            effect = Effect(Field(parts, 5)),
        };

        string portraitKey = Field(parts, 2);
        if (portraitKey == "none") d.clearPortrait = true;
        else if (portraitKey == "dim") d.dimPortrait = true;
        else d.portrait = Portrait(portraitKey);

        string highlight = Field(parts, 6);
        if (highlight == "blue") d.textColor = new Color(0.35f, 0.75f, 1f);
        else if (highlight == "red") d.textColor = new Color(0.95f, 0.35f, 0.30f);

        d.nameColor = NameColor(d.speaker);

        return d;
    }

    private static string Field(string[] parts, int index)
    {
        return index < parts.Length ? parts[index].Trim() : "";
    }

    private static Sprite Portrait(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (Portraits.TryGetValue(key, out var cached)) return cached;

        string path = key switch
        {
            "first" => "Portraits/WitchFirst",
            "second" => "Portraits/WitchSecond",
            "vesper" => "Portraits/WitchPortrait",
            _ => null
        };

        Sprite sprite = path == null ? null : Resources.Load<Sprite>(path);
        if (sprite == null && path != null)
        {
            Debug.LogWarning($"[DialogueScript] 立绘加载失败：{path}");
        }

        Portraits[key] = sprite;
        return sprite;
    }

    private static PortraitSide Side(string key)
    {
        return key switch
        {
            "left" => PortraitSide.Left,
            "center" => PortraitSide.Center,
            "right" => PortraitSide.Right,
            _ => PortraitSide.None
        };
    }

    private static DialogueEffect Effect(string key)
    {
        return key switch
        {
            "flash1" => DialogueEffect.FlashRed,
            "flash2" => DialogueEffect.FlashRedTwice,
            "flashRW" => DialogueEffect.FlashRedThenWhite,
            "flashB" => DialogueEffect.FlashBlue,
            _ => DialogueEffect.None
        };
    }

    private static Color NameColor(string speaker)
    {
        return speaker switch
        {
            "我" => new Color(0.88f, 0.78f, 0.55f),
            "魔女A" => new Color(0.92f, 0.45f, 0.45f),
            "魔女B" => new Color(0.50f, 0.66f, 0.95f),
            "？？？" => new Color(0.72f, 0.85f, 0.95f),
            _ => new Color(0.75f, 0.72f, 0.68f)
        };
    }
}
