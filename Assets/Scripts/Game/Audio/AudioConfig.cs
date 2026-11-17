using UnityEngine;

/// <summary>
/// 音频配置。所有关卡共用这一份。
/// 用 Tools → 音频 → 创建或打开音频配置 生成，然后把两条音频拖进来。
/// </summary>
[CreateAssetMenu(menuName = "女巫与狗/音频配置", fileName = "AudioConfig")]
public class AudioConfig : ScriptableObject
{
    [Tooltip("背景音乐：常态循环播放")]
    public AudioClip bgm;

    [Tooltip("追击音乐：有敌人看见魔女时播放，会打断背景音乐")]
    public AudioClip chase;
}
