using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using System.Collections;
public class SoundManager : MonoSingleton<SoundManager>
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioSource musicSource;

    [SerializeField] private GameObject sfxParentGO;
    [SerializeField] private GameObject sfxPrefab;
    [SerializeField] public SoundDataSO soundDataSO;
    [SerializeField] private List<AudioSource> sfxSource = new List<AudioSource>();
    private readonly Dictionary<AudioClipName, float> soundCooldowns = new Dictionary<AudioClipName, float>()
    {
        {AudioClipName.sfx_praise_user, 1.5f}
    };

    private readonly Dictionary<AudioClipName, float> lastPlayTime = new Dictionary<AudioClipName, float>();
    private const int MaxSfxSources = 8;

    void OnEnable()
    {
        GameEventBus.OnStartGame += StartGame;
        GameEventBus.OnLoseGame += LoseGame;
        GameEventBus.OnWinGame += WinGame;

    }

    private void OnDisable()
    {
        GameEventBus.OnStartGame -= StartGame;
        GameEventBus.OnLoseGame -= LoseGame;
        GameEventBus.OnWinGame -= WinGame;
    }

    void Start()
    {
        if (sfxParentGO == null)
        {
            sfxParentGO = new GameObject("SFXParent");
        }

        sfxSource = sfxSource ?? new List<AudioSource>();
        sfxSource.Clear();

        foreach (Transform child in sfxParentGO.transform)
        {
            AudioSource source = child.GetComponent<AudioSource>();
            if (source != null)
                sfxSource.Add(source);
        }

        if (sfxSource.Count == 0)
        {
            AudioSource source = CreateSfxSource();
            if (source != null)
                sfxSource.Add(source);
        }
    }

    private void StartGame()
    {
        RestartMusic();
    }

    private void OnCarrierFinished()
    {
        PlayOneShot(AudioClipName.sfx_complete_box);
        if(LunaUIManager.Instance != null && LunaUIManager.Instance.PlayPraiseSfx)
        {
            PlayOneShot(AudioClipName.sfx_praise_user);
        }
    }


    [SerializeField]private float musicVolume = 0.25f;
    private void RestartMusic()
    {
        musicSource.Stop();
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private Coroutine musicCoroutine;
    private void LoseGame()
    {
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }
        musicCoroutine = StartCoroutine(PlayMusicWithDelay(AudioClipName.sfx_lose));
    }
    private void WinGame()
    {
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }
        musicCoroutine = StartCoroutine(PlayMusicWithDelay(AudioClipName.sfx_victory_1));
    }
    private IEnumerator PlayMusicWithDelay(AudioClipName clipName, float delay = 0.15f)
    {
        yield return new WaitForSeconds(delay);
        StopMusic();
        StopAllSfx();
        PlayOneShot(clipName);
    }
    private void StopMusic()
    {
        musicSource.Stop();
    }
    private void StopSFX()
    {
        foreach (AudioSource source in sfxSource)
        {
            source.Stop();
        }
    }
    private void StopAllSound()
    {
        StopMusic();
        StopSFX();
    }
    private void PauseMusic()
    {
        musicSource.Pause();
    }


    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
    }
    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
    }



    private SoundData GetRandomSound(AudioClipName name)
    {
        List<SoundData> sounds = soundDataSO.soundDataList.FindAll(sound => sound.Name == name);
        if (sounds.Count == 0)
            return null;
        
        int randomIndex = Random.Range(0, sounds.Count);
        return sounds[randomIndex];
    }

    private SoundData GetSoundData(AudioClipName name)
    {
        return soundDataSO.soundDataList.Find(sound => sound.Name == name);
    }


    public void PlayMusic(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlayOneShot(AudioClipName name)
    {
        SoundData soundData = GetRandomSound(name);

        if (soundData == null)
        {
            Debug.LogWarning($"SoundManager: no SoundData found for '{name}'");
            return;
        }

        if (soundData.Clip == null)
        {
            Debug.LogWarning($"SoundManager: SoundData.clip is null for '{name}'");
            return;
        }

        if (IsOnCooldown(name))
            return;

        // Debug.Log($"SoundManager {name} -> {soundData.Clip.name}. sfxSource count={sfxSource?.Count ?? 0}");

        AudioSource source = GetAvailableSfxSource();
        if (source == null)
        {
            Debug.LogWarning("SoundManager: failed to get or create an SFX AudioSource.");
            return;
        }
        if(name == AudioClipName.sfx_praise_user)
        {
            LunaUIManager.Instance.PlayPraiseVfx(soundData.Clip.name);
        }

        source.PlayOneShot(soundData.Clip, soundData.VolumeDefault);
    }

    private bool IsOnCooldown(AudioClipName name)
    {
        if (!soundCooldowns.TryGetValue(name, out float cooldown))
            return false;

        float currentTime = Time.time;
        if (!lastPlayTime.TryGetValue(name, out float lastTime) || currentTime - lastTime >= cooldown)
        {
            lastPlayTime[name] = currentTime;
            return false;
        }

        return true;
    }

    private AudioSource GetAvailableSfxSource()
    {
        if (sfxParentGO == null)
        {
            sfxParentGO = new GameObject("SFXParent");
        }

        if (sfxSource == null)
            sfxSource = new List<AudioSource>();

        sfxSource.RemoveAll(source => source == null);

        foreach (AudioSource source in sfxSource)
        {
            if (source != null)
                return source;
        }

        if (sfxSource.Count < MaxSfxSources)
        {
            AudioSource source = CreateSfxSource();
            if (source != null)
                sfxSource.Add(source);
            return source;
        }

        return sfxSource.Count > 0 ? sfxSource[0] : CreateSfxSource();
    }

    private AudioSource CreateSfxSource()
    {
        GameObject go;
        if (sfxPrefab != null)
        {
            go = Instantiate(sfxPrefab, sfxParentGO.transform);
        }
        else
        {
            go = new GameObject("SFXSource");
            go.transform.SetParent(sfxParentGO.transform);
            go.AddComponent<AudioSource>();
        }

        AudioSource source = go.GetComponent<AudioSource>();
        if (source == null)
            source = go.AddComponent<AudioSource>();

        return source;
    }

    
    [SerializeField] private AudioSource loopFxSource;
    private AudioClipName? _currentLoopClipName = null;
    private AudioClipName? _pausedLoopClipName = null;



    public float timeOutForLoopSound = 0.32f;
    private bool isPlayingLoopSound = false;
    /// <summary>
    /// Phát âm thanh lặp vô hạn trên loopFxSource (chỉ một clip tại một thời điểm)
    /// </summary>
    public IEnumerator PlayLoop(AudioClipName clipName, float delay = 0f)
    {
        if(isPlayingLoopSound)
        {
            yield break;
        }
        if(clipName == AudioClipName.sfx_merge_loop)
        {
            yield return new WaitForSeconds(delay);
        }

        if (loopFxSource == null) yield break; ;
        var soundData = soundDataSO.GetSoundData(clipName);
        if (soundData == null || soundData.Clip == null) yield break;
        _currentLoopClipName = clipName;
        loopFxSource.clip = soundData.Clip;
        loopFxSource.volume = soundData.VolumeDefault;
        loopFxSource.loop = true;
        isPlayingLoopSound = true;
        loopFxSource.Play();
    }

    


    /// <summary>
    /// Dừng âm thanh đang loop
    /// </summary>
    public void StopLoop()
    {
        isPlayingLoopSound = false;
        _currentLoopClipName = null;
        _pausedLoopClipName = null;
        if (loopFxSource == null) return;
        loopFxSource.loop = false;
        loopFxSource.Stop();
    }

    // private System.Collections.IEnumerator OnCustomTimeScaleChanged(float timeScale)
    // {
    //     if (timeScale == 0f)
    //     {
    //         if (loopFxSource != null && loopFxSource.isPlaying && _currentLoopClipName.HasValue)
    //         {
    //             _pausedLoopClipName = _currentLoopClipName;
    //             loopFxSource.loop = false;
    //             loopFxSource.Stop();
    //         }
    //     }
    //     else
    //     {
    //         if (_pausedLoopClipName.HasValue)
    //         {
    //             yield return PlayLoop(_pausedLoopClipName.Value);
    //             _pausedLoopClipName = null;
    //         }
    //     }
    // }


    public void StopAllSfx()
    {
        foreach (var source in sfxSource)
        {
            if (source != null)
            {
                source.Stop();
            }
        }
        StopLoop();
    }
}




