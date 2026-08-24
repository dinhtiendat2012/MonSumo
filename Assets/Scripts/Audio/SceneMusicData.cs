using System;
using UnityEngine;

[Serializable]
public class SceneMusicData
{
    [Tooltip("Tên Scene đúng như trong Build Settings.")]
    public string sceneName;

    [Tooltip("BGM sẽ phát khi Scene này được load.")]
    public AudioClip musicClip;
}
