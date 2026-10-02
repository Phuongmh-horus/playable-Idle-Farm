using System;
using UnityEngine;

[Serializable]
public class SoundData
{
    public AudioClipName Name;
    public AudioClip Clip;
    public float VolumeDefault = 1f;
}

public enum AudioClipName
{
    None = 0,
    // Add your audio clip names here
    // Example:
    // ButtonClick,
    // BackgroundMusic,
    // Victory,
}
