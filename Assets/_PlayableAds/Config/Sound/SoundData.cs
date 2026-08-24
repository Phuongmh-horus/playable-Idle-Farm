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
    sfx_victory_1,
    sfx_victory_2,
    sfx_coins_fly_start,
    sfx_coins_fly_end,
    sfx_warning,
    sfx_prelose,
    sfx_lose,
    sfx_button,
    sfx_button_close,
    sfx_pay_done,
    sfx_rateus,
    sfx_popup_reward,
    sfx_touch_box,
    bg_music1,
    bg_music2,
    sfx_complete_box,
    sfx_booster_undo,
    sfx_booster_claw,
    sfx_booster_extrabox,
    sfx_praise_user,
    sfx_merge,
    sfx_merge_loop,
}
