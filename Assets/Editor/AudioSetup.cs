using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 音频配置的创建入口。
/// </summary>
public static class AudioSetup
{
    private const string Folder = "Assets/Resources/Audio";
    private const string AssetPath = Folder + "/AudioConfig.asset";

    [MenuItem("Tools/音频/创建或打开音频配置")]
    public static void OpenOrCreate()
    {
        if (!Directory.Exists(Folder))
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
        }

        var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(AssetPath);

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<AudioConfig>();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);

        Debug.Log("[AudioSetup] 在 Inspector 里把背景音乐拖到 Bgm、追击音乐拖到 Chase。所有关卡共用。");
    }
}
