using System;
using AdMod;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuestOrderCountdownWorldLayer : WorldLayerBase
{
    [SerializeField] private TextMeshProUGUI txtCountdown;
    [SerializeField] private Button btnWatchAds;
    [SerializeField] private string adsPlacement = AdsRewardedType.DAILYGIFTX2;

    private Action _onAdsRewardedCallback;
    private float _remainingSeconds;

    protected override void Awake()
    {
        base.Awake();
        blockSelectionClick = false;
        if (txtCountdown == null) txtCountdown = GetComponentInChildren<TextMeshProUGUI>(true);
        if (btnWatchAds == null) btnWatchAds = GetComponentInChildren<Button>(true);

        if (btnWatchAds != null)
        {
            btnWatchAds.onClick.AddListener(OnWatchAdsClicked);
        }
    }

    public void InitData(float remainingSeconds = 0f, Action onAdsRewarded = null, string placement = null)
    {
        _remainingSeconds = remainingSeconds;
        _onAdsRewardedCallback = onAdsRewarded;
        if (!string.IsNullOrEmpty(placement))
        {
            adsPlacement = placement;
        }
        UpdateDisplayTime();
    }

    public void UpdateTime(float remainingSeconds)
    {
        _remainingSeconds = remainingSeconds;
        UpdateDisplayTime();
    }

    private void UpdateDisplayTime()
    {
        if (txtCountdown != null)
        {
            int totalSec = Mathf.Max(0, Mathf.CeilToInt(_remainingSeconds));
            int minutes = totalSec / 60;
            int seconds = totalSec % 60;
            txtCountdown.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void OnWatchAdsClicked()
    {
        if (GameConfig.NoInternet())
        {
            LayerManager.Instance?.ShowPopupNoInternet();
            return;
        }

        OnRewarded();

        // if (AdmobManager.Instance != null)
        // {
        //     AdmobManager.Instance.TryRequestShowRewardedVideo(
        //         adsPlacement,
        //         OnRewarded,
        //         null
        //     );
        // }
    }

    private void OnRewarded()
    {
        _onAdsRewardedCallback?.Invoke();
    }
}
