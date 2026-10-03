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
    bg_music,
    sfx_cash,
    sfx_cash_fly,
    sfx_pickaxe,
    sfx_plant_grow,
    sfx_reaper_collect,
    sfx_selling,
    sfx_sickle_harvest,
    sfx_ui_click,
    sfx_upgrade,
    sfx_watering_machine,
}
