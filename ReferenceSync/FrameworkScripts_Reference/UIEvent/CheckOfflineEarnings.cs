using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CheckOfflineEarnings : UIEventBase
{
    private bool _hasCheckedThisSession;
    private PopupOfflineEarningsData _cachedData;

    public override UniTask<bool> CheckRequire()
    {
        if (_hasCheckedThisSession)
        {
            return UniTask.FromResult(false);
        }

        long lastLogout = DataManager.SessionPreviousLogoutUnixSeconds;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Nếu người chơi mới chưa từng có timestamp logout
        if (lastLogout <= 0)
        {
            if (DataManager.PlayerData != null)
            {
                DataManager.PlayerData.LastLogoutUnixSeconds = now;
            }
            _hasCheckedThisSession = true;
            return UniTask.FromResult(false);
        }

        long rawDelta = now - lastLogout;
        // Chống gian lận chỉnh lùi giờ thiết bị
        if (rawDelta <= 0)
        {
            if (DataManager.PlayerData != null)
            {
                DataManager.PlayerData.LastLogoutUnixSeconds = now;
            }
            _hasCheckedThisSession = true;
            return UniTask.FromResult(false);
        }

        var config = ConfigHolder.Instance != null ? ConfigHolder.Instance.OfflineRewardConfigSO : null;
        float minSeconds = config != null ? config.minOfflineSecondsToPopup : 120f;
        float maxSeconds = config != null ? config.maxOfflineSecondsCap : 7200f;
        float efficiency = config != null ? config.offlineEfficiencyRate : 0.5f;
        float adMultiplier = config != null ? config.adRewardMultiplier : 2.0f;

        if (rawDelta < minSeconds)
        {
            return UniTask.FromResult(false);
        }

        double idleCps = FarmEconomyCalculator.CalculateCurrentIdleCps();
        if (idleCps <= 0.0)
        {
            return UniTask.FromResult(false);
        }

        double baseCash = FarmEconomyCalculator.CalculateOfflineReward(idleCps, lastLogout, now, minSeconds, maxSeconds, efficiency);
        if (baseCash < 1.0)
        {
            return UniTask.FromResult(false);
        }

        float effectiveSeconds = Mathf.Clamp(rawDelta, 0f, maxSeconds);
        double adCash = Math.Floor(baseCash * adMultiplier);

        _cachedData = new PopupOfflineEarningsData
        {
            EffectiveSeconds = effectiveSeconds,
            MaxSecondsCap = maxSeconds,
            BaseCash = baseCash,
            AdCash = adCash,
        };

        _hasCheckedThisSession = true;
        return UniTask.FromResult(true);
    }

    public override void Play()
    {
        LayerManager.Instance.ShowPopupOfflineEarnings(_cachedData);
    }
}
