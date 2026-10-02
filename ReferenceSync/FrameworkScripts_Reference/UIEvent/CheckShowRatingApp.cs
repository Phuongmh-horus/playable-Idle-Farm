using Cysharp.Threading.Tasks;
using UnityEngine;

public class CheckShowRatingApp : UIEventBase
{
    private string CheckShowRateUsKey = "CheckShowRateUs";
    private static int CheckShowRateUsAmount = 0;
    // Count number of levels played since the app was opened (not persisted)
    private static int PlaysSinceOpen = 0;
    // Keep last observed level in this session to detect level progress
    private int lastCheckedLevelInSession = 0;
    private int CheckShowRateUsLevel = 0;
    private const int requiredPlaysSinceOpen = 5;
    protected override void Awake()
    {
        base.Awake();
        CheckShowRateUsLevel = PlayerPrefs.GetInt(CheckShowRateUsKey, 0);
    }
    public override UniTask<bool> CheckRequire()
    {
        var playerData = DataManager.PlayerData;
        if (lastCheckedLevelInSession == 0)
        {
            // initialize session level tracker
            lastCheckedLevelInSession = DataManager.PlayerData.LevelDisplay;
        }

        // If the player progressed to a new level since last check, count it as a play
        if (playerData.LevelDisplay > lastCheckedLevelInSession)
        {
            PlaysSinceOpen++;
            lastCheckedLevelInSession = playerData.LevelDisplay;
        }


        if (CheckShowRateUsLevel == playerData.LevelDisplay) return UniTask.FromResult(false);
        var rateUsStartLevel = FirebaseManager.Instance.GetRemoteRateUsStartLevelValue();
        var rateUsRoutineValue = FirebaseManager.Instance.GetRemoteRateUsRoutineValue();
        bool levelShow = (playerData.LevelDisplay >= rateUsStartLevel) 
                         && (playerData.LevelDisplay - rateUsStartLevel) % rateUsRoutineValue == 0;
        var rateUsDisplayAmount = RemoteConfigDatas.RateUsDisplayMaxSeasonValue;
        return UniTask.FromResult(levelShow
                                 && playerData.RateUsValue == 0
                                 && CheckShowRateUsAmount < rateUsDisplayAmount
                                 && PlaysSinceOpen >= requiredPlaysSinceOpen);
    }

    public override void Play()
    {
        CheckShowRateUsLevel = DataManager.PlayerData.LevelDisplay;
        CheckShowRateUsAmount++;
        // reset session play counter after showing popup so next requirement uses the subsequent threshold
        PlaysSinceOpen = 0;
        PlayerPrefs.SetInt(CheckShowRateUsKey, CheckShowRateUsLevel);
        LayerManager.Instance.ShowPopupRatingLayer();
    }
}
