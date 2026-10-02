using System;
using AdMod;
using UnityEngine;
using UnityEngine.UI;

public class AdRewardSprinklerIcon : WorldLayerBase
{
    [SerializeField] private Button watchAdButton;

    private Action _onWatchAdCallback;

    protected override void Reset()
    {
        base.Reset();
        layerDepth = WorldLayerDepth.StatusIcon;
        EnsureUIElements();
        ApplySorting();
    }

    protected override void Awake()
    {
        if (layerDepth == WorldLayerDepth.Popup)
        {
            layerDepth = WorldLayerDepth.StatusIcon;
        }
        base.Awake();
        EnsureUIElements();

        if (watchAdButton != null)
        {
            watchAdButton.onClick.AddListener(HandleButtonClicked);
        }
    }

    protected virtual void OnDestroy()
    {
        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveListener(HandleButtonClicked);
        }
        base.OnDestroy();
    }

    private void EnsureUIElements()
    {
        if (watchAdButton == null)
        {
            watchAdButton = GetComponentInChildren<Button>(true);
        }
    }

    public void InitData(Action onWatchAd = null)
    {
        _onWatchAdCallback = onWatchAd;
    }

    private void HandleButtonClicked()
    {
        if (AdmobManager.Instance != null)
        {
            AdmobManager.Instance.TryRequestShowRewardedVideo(AdsRewardedType.ADS_MINI_SPRINKLER, () =>
            {
                _onWatchAdCallback?.Invoke();
            });
        }
        else
        {
            // Fallback khi không có Admob (Editor/Dev Mode)
            _onWatchAdCallback?.Invoke();
        }
    }
}
