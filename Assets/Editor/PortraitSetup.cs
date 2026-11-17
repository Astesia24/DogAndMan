using UnityEditor;
using UnityEngine;

/// <summary>
/// 把两张立绘图片设成 Sprite 导入，这样运行时 Resources.Load&lt;Sprite&gt; 才拿得到。
/// 生成第一幕时会自动调用一次；也可以手动点 Tools/修复立绘导入设置。
/// </summary>
public static class PortraitSetup
{
    public const string WitchPath = "Assets/Resources/Portraits/WitchPortrait.png";
    public const string DogPath = "Assets/Resources/Portraits/DogPortrait.jpg";
    public const string FirstPath = "Assets/Resources/Portraits/WitchFirst.png";
    public const string SecondPath = "Assets/Resources/Portraits/WitchSecond.png";

    [MenuItem("Tools/修复立绘导入设置")]
    public static void FixAll()
    {
        EnsureSprite(WitchPath);
        EnsureSprite(DogPath);
        EnsureSprite(FirstPath);
        EnsureSprite(SecondPath);
    }

    static void EnsureSprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
        {
            Debug.LogWarning($"[PortraitSetup] 找不到立绘：{path}");
            return;
        }

        bool dirty = false;

        if (imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            dirty = true;
        }

        if (imp.spriteImportMode != SpriteImportMode.Single)
        {
            imp.spriteImportMode = SpriteImportMode.Single;
            dirty = true;
        }

        if (!imp.alphaIsTransparency)
        {
            imp.alphaIsTransparency = true;
            dirty = true;
        }

        if (imp.maxTextureSize < 2048)
        {
            imp.maxTextureSize = 2048;
            dirty = true;
        }

        if (dirty) imp.SaveAndReimport();
    }
}
