using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public class SoundManager : MonoSingleton<SoundManager>
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioSource bgMusicSource;
    [SerializeField] private AudioSource fxMusicSource;

    [SerializeField] public SoundDataSO soundDataSO;
    [SerializeField] public List<AudioClip> backgroundMusics;

    private void OnValidate()
    {
        if (audioMixer == null)
        {
            var audioMixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
            if (audioMixers.Length > 0) audioMixer = audioMixers[0];
        }

        if (soundDataSO == null)
        {
            var soundDataSOs = Resources.FindObjectsOfTypeAll<SoundDataSO>();
            if(soundDataSOs.Length > 0) soundDataSO = soundDataSOs[0];
        }
    }

    protected override void Awake()
    {
        base.Awake();
        GameEventBus.OnChangeSound = (OnSoundChange);
        GameEventBus.OnChangeSoundFx = (OnSoundFxChange);
    }

    private void Start()
    {
        var playMusic = PlayerPrefs.GetInt(GameSettingData.IsPlayMusicKey, 1);
        var playSound = PlayerPrefs.GetInt(GameSettingData.IsPlaySoundKey, 1);
        OnSoundChange(playMusic);
        OnSoundFxChange(playSound);
    }

    private bool _isLoopRandomBGM;
    private void Update()
    {
        if (backgroundMusics.Count <= 0)
        {
            return;
        }
        if (_isLoopRandomBGM && !bgMusicSource.isPlaying)
        {
            AudioClip clip = RandomBGM();
            bgMusicSource.clip = clip;
            bgMusicSource.Play();
        }
    }
    public void StopBackGroundMusic()
    {
        _isLoopRandomBGM = false;
        bgMusicSource.Stop();
    }

    public void PlayBackGroundMusic()
    {
        _isLoopRandomBGM = true;
    }

    private List<AudioClip> _backgroundMusicTemp = new();
    private AudioClip RandomBGM()
    {
        if (_backgroundMusicTemp.Count == 0)
        {
            _backgroundMusicTemp.AddRange(backgroundMusics);
        }
        var result = _backgroundMusicTemp[Random.Range(0, _backgroundMusicTemp.Count)];
        _backgroundMusicTemp.Remove(result);
        return result;
    }

    private void OnSoundChange(float currentValue)
    {
        if (audioMixer == null) return;
        float maxRangeDesign = 1f;
        currentValue = Math_Utility.Remap(currentValue, 0, 1, 0, maxRangeDesign);
        var soundValue = currentValue == 0 ? -100 : Mathf.Log10(currentValue) * 20;
        var parameterName = Enum.GetName(typeof(SoundMixerGroup), SoundMixerGroup.BGMusic);
        var checkSet = audioMixer.SetFloat(parameterName, soundValue);
#if UNITY_EDITOR
        if (!checkSet) Debug.LogError($"không set được giá trị audio mixer với parameter {parameterName}");
#endif
    }
    private void OnSoundFxChange(float currentValue)
    {
        if (audioMixer == null) return;
        currentValue *= 2; // sound fx có âm lượng gấp đôi
        var soundValue = currentValue == 0 ? -100 : Mathf.Log10(currentValue) * 20;
        var parameterName = Enum.GetName(typeof(SoundMixerGroup), SoundMixerGroup.SoundFx);
        var checkSet = audioMixer.SetFloat(parameterName, soundValue);
#if UNITY_EDITOR
        if (!checkSet) Debug.LogError($"không set được giá trị audio mixer với parameter {parameterName}");
#endif
    }

    /// <summary>
    /// Hàm phát một âm thanh với Mixer là Sound
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="volume"></param>
    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        fxMusicSource.PlayOneShot(clip, volume);
    }

    public void PlayOneShot(AudioClipName clipName)
    {
        var soundData = soundDataSO.GetSoundData(clipName);
        if (soundData == null) return;
        fxMusicSource.PlayOneShot(soundData.Clip, soundData.VolumeDefault);
    }
    public void StopOneShot()
    {
        fxMusicSource.Stop();
    }
}
// đặt theo exposed Parameters trong audio mixer
public enum SoundMixerGroup
{
    BGMusic,
    SoundFx,
}