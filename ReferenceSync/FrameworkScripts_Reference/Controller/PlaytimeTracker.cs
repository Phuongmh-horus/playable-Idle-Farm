using System;
using System.Collections.Generic;
using UnityEngine;

public class PlaytimeTracker : MonoSingleton<PlaytimeTracker>
{
    public float TimeGamePlay { get; private set; }     // Thời gian chơi KHÔNG tính ads
    public float TimeInGame { get; private set; }       // Thời gian chơi CÓ tính ads

    public bool IsGamePlaying { get; private set; }
    public static bool IsWatchingAds { get; set; }

    public bool HasApplicationFocus { get; private set; } = true;
    public bool IsApplicationPaused { get; private set; } = false;

    // Private fields
    private DateTime _startGameTime;
    private bool _isTrackingEnabled = true;
    private bool _isGameSessionActive = false;

    // Tracking
    private HashSet<int> _timePlayTracked = new HashSet<int>();

    // Events
    public System.Action<float> OnTimeGamePlayUpdated;
    public System.Action<float> OnTimeInGameUpdated;
    public System.Action<bool> OnGameStateChanged;

    private void Start()
    {
        var now = DateTime.Now;
    }

    private void Update()
    {
        if (!_isGameSessionActive) return;

        float deltaTime = Time.deltaTime;

        // Luôn cập nhật TimeInGame khi game session active và app có focus
        if (ShouldUpdateTimeInGame())
        {
            TimeInGame += deltaTime;
            OnTimeInGameUpdated?.Invoke(TimeInGame);
        }

        // Chỉ cập nhật TimeGamePlay khi không xem ads
        if (ShouldUpdateGamePlayTime())
        {
            TimeGamePlay += deltaTime;
            OnTimeGamePlayUpdated?.Invoke(TimeGamePlay);
        }

        // Update tracking data
        UpdateTimeTracking(deltaTime);
    }

    private bool ShouldUpdateTimeInGame()
    {
        return HasApplicationFocus;
    }

    private bool ShouldUpdateGamePlayTime()
    {
        return HasApplicationFocus && !IsApplicationPaused &&
               IsGamePlaying && !IsWatchingAds;
    }

    private float _timePlayInGame;
    private string TimePlayInGameKey = "TimePlayInGame";
    private void UpdateTimeTracking(float deltaTime)
    {
        if (!_isTrackingEnabled || !ShouldUpdateTimeInGame()) return;

        var playerData = DataManager.PlayerData;
        if (playerData == null) return;

        if (_timePlayInGame == 0)
        {
            if (PlayerPrefs.HasKey(TimePlayInGameKey))
            {
                _timePlayInGame = PlayerPrefs.GetFloat(TimePlayInGameKey);
            }
            else
            {
                _timePlayInGame = playerData.TimePlayInGame;
            }
        }

        _timePlayInGame += deltaTime;
        int timeInMinutes = Mathf.FloorToInt(playerData.TimePlayInGame / 60f);
        SendTrackingTimePlay(timeInMinutes);
    }

    private void SendTrackingTimePlay(int timeInMinutes)
    {
        if (!_timePlayTracked.Add(timeInMinutes)) return;

        if (timeInMinutes > 15)
        {
            _isTrackingEnabled = false;
            Debug.Log("Time tracking disabled after 15 minutes");
            return;
        }
        PlayerPrefs.SetFloat(TimePlayInGameKey, timeInMinutes);
        DataManager.PlayerData.TimePlayInGame = timeInMinutes;
        //UserBehaviorTracker.SendTimePlayTracking(timeInMinutes).Forget();
        Debug.Log($"Sent time tracking: {timeInMinutes} minutes");
    }

    // Public Methods
    public void SetGameStart()
    {
        _startGameTime = DateTime.Now;
        TimeGamePlay = 0f;
        TimeInGame = 0f;

        IsGamePlaying = true;
        _isGameSessionActive = true;
        _isTrackingEnabled = true;

        LoadTimeTrackingData();
        OnGameStateChanged?.Invoke(true);

#if UNITY_EDITOR
        Debug.Log($"🎮 Game Started: {_startGameTime:HH:mm:ss}");
#endif
    }

    public void SetGameEnd()
    {
        IsGamePlaying = false;
        _isGameSessionActive = false;

        OnGameStateChanged?.Invoke(false);

        Debug.Log($"🏁 Game Ended - Start: {_startGameTime:HH:mm:ss}, " +
                 $"Pure Gameplay: {GetFormattedTime(TimeGamePlay)}, " +
                 $"Total Time: {GetFormattedTime(TimeInGame)}");
    }

    // Ads Control
    public static void StartWatchingAds()
    {
        IsWatchingAds = true;
        Debug.Log($"🎬 Started watching ads at {DateTime.Now:HH:mm:ss}");
    }

    public static void FinishWatchingAds()
    {
        IsWatchingAds = false;
        Debug.Log($"🎬 Finished watching ads at {DateTime.Now:HH:mm:ss}");
    }


    private void LoadTimeTrackingData()
    {
        _timePlayTracked.Clear();
        var playerData = DataManager.PlayerData;
        if (playerData == null) return;

        var preTimePlayInGame = Mathf.FloorToInt(playerData.TimePlayInGame / 60f);
        for (int i = 1; i <= preTimePlayInGame; i++)
        {
            _timePlayTracked.Add(i);
        }

    }

    private string LastTimeFocus = "LastTimeFocus";
    // Unity Application Events
    private void OnApplicationFocus(bool hasFocus)
    {
        HasApplicationFocus = hasFocus;
        LogStateChange($"Application Focus: {hasFocus}");
        if (_isGameSessionActive)
        {
            if (!hasFocus)
            {
                PlayerPrefs.SetString(LastTimeFocus, DateTime.Now.ToDefaultString());
            }
            else if (PlayerPrefs.HasKey(LastTimeFocus))
            {
                var lastTimeFocus = PlayerPrefs.GetString(LastTimeFocus);
                var timeSpan = DateTime.Now - lastTimeFocus.ToDefaultDateTime();
                TimeInGame += (float)timeSpan.TotalSeconds;
                PlayerPrefs.DeleteKey(LastTimeFocus);
            }
            else
            {
                PlayerPrefs.DeleteKey(LastTimeFocus);
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        IsApplicationPaused = pauseStatus;
        LogStateChange($"Application Pause: {pauseStatus}");
    }

    private void LogStateChange(string reason)
    {
        bool canTrackTimeInGame = ShouldUpdateTimeInGame();
        bool canTrackGamePlay = ShouldUpdateGamePlayTime();

        Debug.Log($"📱 {reason} | " +
                 $"TimeInGame Tracking: {canTrackTimeInGame} | " +
                 $"GamePlay Tracking: {canTrackGamePlay} | " +
                 $"Watching Ads: {IsWatchingAds}");
    }

    // Getters
    public DateTime GetStartGameTime() => _startGameTime;
    public float GetTimeGamePlay() => TimeGamePlay;
    public float GetTimeInGame() => TimeInGame;

    public string GetFormattedTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return $"{minutes:00}:{seconds:00}";
    }

    public float GetAdsTimeApproximate()
    {
        return TimeInGame - TimeGamePlay;
    }

    public float GetGamePlayPercentage()
    {
        return TimeInGame > 0 ? (TimeGamePlay / TimeInGame) * 100f : 0f;
    }

    // Debug Info
    [ContextMenu("Log Current State")]
    public void LogCurrentState()
    {
        Debug.Log($"=== TRACKING STATE ===\n" +
                 $"🎮 Game Playing: {IsGamePlaying}\n" +
                 $"🎬 Watching Ads: {IsWatchingAds}\n" +
                 $"📱 App Focus: {HasApplicationFocus}\n" +
                 $"⏸️ App Paused: {IsApplicationPaused}\n" +
                 $"🕐 Pure Gameplay: {GetFormattedTime(TimeGamePlay)}\n" +
                 $"🕐 Total Time: {GetFormattedTime(TimeInGame)}\n" +
                 $"🎬 Estimated Ads Time: {GetFormattedTime(GetAdsTimeApproximate())}\n" +
                 $"📊 Gameplay %: {GetGamePlayPercentage():F1}%");
    }
}
