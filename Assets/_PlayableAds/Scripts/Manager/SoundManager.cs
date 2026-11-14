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
    [Header("SFX Optimization / Throttle Settings")]
    [SerializeField] private float defaultSfxCooldown = 0.08f;
    private readonly Dictionary<AudioClipName, float> soundCooldowns = new Dictionary<AudioClipName, float>();
    private readonly Dictionary<AudioClipName, float> lastPlayTime = new Dictionary<AudioClipName, float>();
    private const int MaxSfxSources = 6;
    private int _roundRobinIndex = 0;
    private int _lastPlayFrame = -1;
    private int _sfxPlayedThisFrame = 0;
    private const int MaxSfxPerFrame = 2;

    protected override void Awake()
    {
        base.Awake();
        InitCooldowns();
    }

    private void InitCooldowns()
    {
        // Tối ưu số lần phát mỗi giây cho các âm thanh tưới nước, thu hoạch, bán và mọc cây
        soundCooldowns[AudioClipName.sfx_watering_machine] = 0.28f; // ~3.5 lần/giây khi vòi tưới xoay qua hàng loạt cây
        soundCooldowns[AudioClipName.sfx_reaper_collect] = 0.28f;   // ~3.5 lần/giây khi lưỡi cưa cắt cây liên tục
        soundCooldowns[AudioClipName.sfx_sickle_harvest] = 0.25f;
        soundCooldowns[AudioClipName.sfx_cash_fly] = 0.25f;        // khi hàng chục quả/tiền bay về xe
        soundCooldowns[AudioClipName.sfx_cash] = 0.2f;            // tiền bán về túi
        soundCooldowns[AudioClipName.sfx_selling] = 0.50f;         // xe khởi hành bán
        soundCooldowns[AudioClipName.sfx_plant_grow] = 0.2f;      // cây mọc/mở ring hàng loạt
        soundCooldowns[AudioClipName.sfx_upgrade] = 0.25f;         // nâng cấp
        soundCooldowns[AudioClipName.sfx_ui_click] = 0.10f;        // click nút
    }

    void OnEnable()
    {
        GameEventBus.OnStartGame += StartGame;

    }

    private void OnDisable()
    {
        GameEventBus.OnStartGame -= StartGame;
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

    [SerializeField] private float musicVolume = 0.25f;
    private void RestartMusic()
    {
        musicSource.Stop();
        musicSource.volume = musicVolume;
        musicSource.Play();
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
        // 1. Kiểm tra giới hạn số lượng SFX trên cùng 1 frame (tránh âm thanh spike/crackle khi mở nhiều plot)
        int currentFrame = Time.frameCount;
        if (currentFrame != _lastPlayFrame)
        {
            _lastPlayFrame = currentFrame;
            _sfxPlayedThisFrame = 0;
        }

        // Bỏ qua nếu đã phát quá số SFX cho phép trong frame này, trừ các SFX UI quan trọng
        if (_sfxPlayedThisFrame >= MaxSfxPerFrame && name != AudioClipName.sfx_ui_click && name != AudioClipName.sfx_upgrade)
        {
            return;
        }

        // 2. Kiểm tra cooldown theo loại âm thanh
        if (IsOnCooldown(name))
            return;

        SoundData soundData = GetRandomSound(name);
        if (soundData == null || soundData.Clip == null)
            return;

        AudioSource source = GetAvailableSfxSource();
        if (source == null)
            return;

        _sfxPlayedThisFrame++;

        // Random pitch nhẹ (0.96 - 1.04) để các âm thanh lặp lại nghe tự nhiên và êm tai
        source.pitch = Random.Range(0.96f, 1.04f);
        source.PlayOneShot(soundData.Clip, soundData.VolumeDefault);
    }

    private bool IsOnCooldown(AudioClipName name)
    {
        float cooldown = soundCooldowns.TryGetValue(name, out float cd) ? cd : defaultSfxCooldown;
        if (cooldown <= 0f)
            return false;

        float currentTime = Time.unscaledTime;
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

        // 1. Tìm AudioSource đang rảnh rỗi
        for (int i = 0; i < sfxSource.Count; i++)
        {
            if (sfxSource[i] != null && !sfxSource[i].isPlaying)
                return sfxSource[i];
        }

        // 2. Nếu tất cả đều bận và chưa chạm MaxSfxSources, tạo thêm source mới
        if (sfxSource.Count < MaxSfxSources)
        {
            AudioSource source = CreateSfxSource();
            if (source != null)
            {
                sfxSource.Add(source);
                return source;
            }
        }

        // 3. Nếu đã đạt tối đa số source, dùng round-robin xoay vòng
        if (sfxSource.Count > 0)
        {
            _roundRobinIndex = (_roundRobinIndex + 1) % sfxSource.Count;
            return sfxSource[_roundRobinIndex];
        }

        return CreateSfxSource();
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
        if (isPlayingLoopSound)
        {
            yield break;
        }
        if (clipName == AudioClipName.sfx_pickaxe && delay > 0f)
        {
            while (delay > 0f)
            {
                delay -= Time.deltaTime;
                yield return null;
            }
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




