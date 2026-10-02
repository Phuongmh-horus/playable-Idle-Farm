using System;
using System.Threading;
using UnityEngine;

public class HeartSystem : MonoSingleton<HeartSystem>
{
    [SerializeField] private HeartConfigSO heartConfig;

    private void OnValidate()
    {
        if (heartConfig == null)
        {
            var configs = Resources.FindObjectsOfTypeAll<HeartConfigSO>();
            if (configs.Length > 0) heartConfig = configs[0];
        }
    }

    protected override void Awake()
    {
        base.Awake();
        DataManager.OnValidatePlayerData += OnValidatePlayerData;
    }
    private void OnDestroy()
    {
        DataManager.OnValidatePlayerData -= OnValidatePlayerData;
        heartRecoverTokenSource?.TryCancel();
    }
    private void OnValidatePlayerData()
    {
        var playerData = DataManager.PlayerData;
        if (playerData == null) return;
        DataManager.ValidateMaxHeart(heartConfig.MaxHeart);
        if (IsInfiniteHeart())
        {
            DataManager.PlayerData.CurrentHeart = DataManager.PlayerData.MaxHeart;
            return;
        }
        UpdateHeartRecover();
    }

    public void ConsumeHeart()
    {
        if (IsInfiniteHeart()) return;
        var currentTime = DateTime.UtcNow;
        if (IsHeartFull()) PlayerPrefs.SetString(LastConsumeHeartKey, currentTime.ToDefaultString());
        DataManager.ChangeHeart(-1);
        //UserBehaviorTracker.SendSpendHeartTracking();
    }
    public bool IsHeartFull()
    {
        if (IsInfiniteHeart()) return true;
        return DataManager.PlayerData.CurrentHeart >= heartConfig.MaxHeart;
    }

    public void BuyHeartInfinite(int quantity, bool isSave = true)
    {
        var duration = TimeSpan.FromMinutes(quantity);
        var infiniteHeartTime = DataManager.PlayerData.InfiniteHeartTimeString.ToDefaultDateTime();
        infiniteHeartTime = infiniteHeartTime > DateTime.UtcNow
                                ? infiniteHeartTime.Add(duration)
                                : DateTime.UtcNow.Add(duration);

        DataManager.ChangeInfiniteHeartTime(infiniteHeartTime, isSave);
    }

    public static Action<string, bool> OnHeartCoolDownAction;
    private static CancellationTokenSource heartRecoverTokenSource;
    public async void UpdateCoolDownHeart()
    {
        heartRecoverTokenSource?.TryCancel();
        heartRecoverTokenSource = new CancellationTokenSource();

        if (IsInfiniteHeart())
        {
            UpdateHeartInfiniteCoolDown();
            return;
        }
        if (IsHeartFull())
        {
            var currentTime = DateTime.UtcNow;
            PlayerPrefs.SetString(LastConsumeHeartKey, currentTime.ToDefaultString());
            OnHeartCoolDownAction?.Invoke(TextUtility.GetI2("ui_full"), false);
            return;
        }

        var lastConsumeHeart = PlayerPrefs.GetString(LastConsumeHeartKey, "");
        var lastConsumeTime = DateTime.MinValue;
        if (!string.IsNullOrEmpty(lastConsumeHeart))
        {
            lastConsumeTime = lastConsumeHeart.ToDefaultDateTime();
        }

        var heartRecoverRemote = FirebaseManager.Instance.GetRemoteHeartRecoverTimeValue();
        var heartRecover = heartRecoverRemote.ZeroReplace(heartConfig.RecoverTime);
        var nextTime = lastConsumeTime.AddSeconds(heartRecover);
        if (nextTime < DateTime.UtcNow)
        {
            UpdateCountDown("00:00", false);
            return;
        }
        await DateTimeHelper.CountDownTask(
            nextTime,
            (time) => UpdateCountDown(time, false),
            OnCountdownComplete,
            heartRecoverTokenSource.Token);
    }
    public async void UpdateHeartInfiniteCoolDown()
    {
        var nextTime = DataManager.PlayerData.InfiniteHeartTimeString.ToDefaultDateTime();
        if (nextTime < DateTime.UtcNow)
        {
            UpdateCountDown("00:00", true);
            return;
        }
        await DateTimeHelper.CountDownTask(
            nextTime,
            (time) => UpdateCountDown(time, true),
            UpdateCoolDownHeart,
            heartRecoverTokenSource.Token);
    }

    public bool CheckNotEnoughHeart(PopupRefillHeartPlacement placement)
    {
        if(IsInfiniteHeart()) return false;
        var enoughHeart = DataManager.PlayerData.CurrentHeart > 0;
        if (!enoughHeart)
        {
            LayerManager.Instance.ShowPopupRefillHeartLayer(new PopupRefillHeartData()
            {
                Placement = placement
            });
        }
        return !enoughHeart;
    }
    public bool IsInfiniteHeart()
    {
        var playerData = DataManager.PlayerData;
        var infiniteHeartTime = playerData.InfiniteHeartTimeString.ToDefaultDateTime();
        bool isLockHeartSystem = IsLockHeartSystem();
        return isLockHeartSystem
            || infiniteHeartTime > DateTime.MinValue
            && DateTime.UtcNow < infiniteHeartTime;
    }
    public bool IsLockHeartSystem()
    {
        var playerDataLevel = DataManager.PlayerData?.LevelDisplay ?? 1;
        var heartSystemPlayLevel = FirebaseManager.Instance.GetRemoteStartHeartSystemLevelValue();
        return playerDataLevel < heartSystemPlayLevel;
    }

    private void OnCountdownComplete()
    {
        var currentTime = DateTime.UtcNow;
        PlayerPrefs.SetString(LastConsumeHeartKey, currentTime.ToDefaultString());
        DataManager.ValidateHeartRecover(1, true);
    }
    private void UpdateCountDown(string countdownText, bool isInfinite)
    {
        OnHeartCoolDownAction?.Invoke(countdownText, isInfinite);
    }
    private void UpdateHeartRecover()
    {
        var heartRecover = CalculateHeartRecover();
        DataManager.ValidateHeartRecover(heartRecover);
    }

    public static readonly string LastConsumeHeartKey = "LastConsumeHeart";
    private int CalculateHeartRecover()
    {
        var lastConsumeHeart = PlayerPrefs.GetString(LastConsumeHeartKey, "");
        var lastConsumeTime = DateTimeHelper.MinDateTime;
        if (!string.IsNullOrEmpty(lastConsumeHeart))
        {
            lastConsumeTime = lastConsumeHeart.ToDefaultDateTime();
        }
        var currentTime = DateTime.UtcNow;
        var timeDifference = (currentTime - lastConsumeTime).TotalSeconds;
        var heartRecoverRemote = FirebaseManager.Instance.GetRemoteHeartRecoverTimeValue();
        heartRecoverRemote = heartRecoverRemote.ZeroReplace(heartConfig.RecoverTime);
        var heartRecover = Mathf.FloorToInt((float)timeDifference / heartRecoverRemote);

        if (heartRecover > 0 && lastConsumeTime > DateTimeHelper.MinDateTime)
        {
            lastConsumeTime = lastConsumeTime.AddSeconds(heartRecover * heartRecoverRemote);
            PlayerPrefs.SetString(LastConsumeHeartKey, lastConsumeTime.ToDefaultString());
        }

        return Mathf.Clamp(heartRecover, 0, heartConfig.MaxHeart);
    }
}
