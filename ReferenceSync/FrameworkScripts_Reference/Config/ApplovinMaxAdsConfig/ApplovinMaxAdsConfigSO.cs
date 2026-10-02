using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AdsType
{
    Interstitial = 0,
    Banner = 1,
    OpenApp = 2,
    Rewarded = 3,
    AppOpen = 4
}

public enum BannerAdsPosition
{
    TopCenter = 0,
    BottomCenter = 1,
    TopLeft = 2,
    TopRight = 3,
    Centered = 4,
    CenterLeft = 5,
    CenterRight = 6,
    BottomLeft = 7,
    BottomRight = 8,
}

[Flags]
public enum ApplovinMaxAdsEnabledAds
{
    None = 0,
    Interstitial = 1 << 0,
    Banner = 1 << 1,
    OpenApp = 1 << 2,
    Rewarded = 1 << 3
}

[Serializable]
public class ApplovinMaxAdsUnitId
{
    public AdsType Type => type;
    public string AndroidUnitId => androidUnitId;
    public string IosUnitId => iosUnitId;
    public string GetUnitId(bool isAndroid) => isAndroid ? androidUnitId : iosUnitId;

    public string UnitId
        => Application.platform switch
        {
#if UNITY_EDITOR
            _ => androidUnitId,
#else
                RuntimePlatform.IPhonePlayer => iosUnitId,
                RuntimePlatform.Android => androidUnitId,
                _ => null
#endif
        };

    [SerializeField]
    private AdsType type;

    [SerializeField]
    private string androidUnitId;

    [SerializeField]
    private string iosUnitId;
}

[Serializable]
public class ApplovinMaxAdsWaterfallUnitId
{
    public string AndroidUnitId => androidUnitId;
    public string IosUnitId => iosUnitId;
    public string GetUnitId(bool isAndroid) => isAndroid ? androidUnitId : iosUnitId;

    public string UnitId
        => Application.platform switch
        {
#if UNITY_EDITOR
            _ => androidUnitId,
#else
                RuntimePlatform.IPhonePlayer => iosUnitId,
                RuntimePlatform.Android => androidUnitId,
                _ => null
#endif
        };

    [SerializeField]
    private string tierName;

    [SerializeField]
    private string androidUnitId;

    [SerializeField]
    private string iosUnitId;
}

[CreateAssetMenu(fileName = "ApplovinMaxAdsConfigSO", menuName = "ScriptableObjects/ApplovinMaxAdsConfigSO", order = 1)]
public class ApplovinMaxAdsConfigSO : ScriptableObject
{
    public bool IsTestAds => isTestAds;
    public ApplovinMaxAdsEnabledAds EnabledAds => enabledAds;
    public BannerAdsPosition BannerAdPosition => bannerAdsDisplayPosition;
    public bool IsBannerAutoSized => bannerAutoSized;
    public Color BannerBackgroundColor => bannerBackgroundColor;
    public string[] InterstitialUnitIds => GetFullscreenUnitIds(interstitialWaterfallUnitIds, AdsType.Interstitial);
    public string[] RewardedUnitIds => GetFullscreenUnitIds(rewardedWaterfallUnitIds, AdsType.Rewarded);
    public string[] RewardInterUnitIds => GetFullscreenUnitIds(rewardInterWaterfallUnitIds, AdsType.Interstitial);

    public string[] GetInterstitialUnitIds(bool isAndroid) => GetFullscreenUnitIds(interstitialWaterfallUnitIds, AdsType.Interstitial, isAndroid);
    public string[] GetRewardedUnitIds(bool isAndroid) => GetFullscreenUnitIds(rewardedWaterfallUnitIds, AdsType.Rewarded, isAndroid);
    public string[] GetRewardInterUnitIds(bool isAndroid) => GetFullscreenUnitIds(rewardInterWaterfallUnitIds, AdsType.Interstitial, isAndroid);

    public MappedList<AdsType, ApplovinMaxAdsUnitId> UnitIdsMapping
    {
        get
        {
            if (_unitIdsMapping == null)
            {
                _unitIdsMapping = new Lazy<MappedList<AdsType, ApplovinMaxAdsUnitId>>(ConstructMappedList);
            }
            return _unitIdsMapping.Value;
        }
    }

    private Lazy<MappedList<AdsType, ApplovinMaxAdsUnitId>> _unitIdsMapping;

    [SerializeField]
    private bool isTestAds;

    [SerializeField]
    private ApplovinMaxAdsEnabledAds enabledAds;

    [SerializeField]
    private ApplovinMaxAdsUnitId[] unitIds;

    [SerializeField]
    private ApplovinMaxAdsWaterfallUnitId[] interstitialWaterfallUnitIds = Array.Empty<ApplovinMaxAdsWaterfallUnitId>();

    [SerializeField]
    private ApplovinMaxAdsWaterfallUnitId[] rewardedWaterfallUnitIds = Array.Empty<ApplovinMaxAdsWaterfallUnitId>();

    [SerializeField]
    private ApplovinMaxAdsWaterfallUnitId[] rewardInterWaterfallUnitIds = Array.Empty<ApplovinMaxAdsWaterfallUnitId>();

    [SerializeField]
    private BannerAdsPosition bannerAdsDisplayPosition;

    [SerializeField]
    private bool bannerAutoSized = true;

    [SerializeField]
    private Color bannerBackgroundColor = Color.black;

    private MappedList<AdsType, ApplovinMaxAdsUnitId> ConstructMappedList()
        => new MappedList<AdsType, ApplovinMaxAdsUnitId>(
            unitIds, (uid) => uid.Type
        );

    public string[] GetSelectiveInitAdUnitIds()
        => GetAllConfiguredUnitIds()
            .Where((unitId) => !string.IsNullOrEmpty(unitId))
            .Distinct()
            .ToArray();

    private IEnumerable<string> GetAllConfiguredUnitIds()
    {
        foreach (var unitId in InterstitialUnitIds)
        {
            yield return unitId;
        }

        foreach (var unitId in RewardedUnitIds)
        {
            yield return unitId;
        }

        foreach (var unitId in RewardInterUnitIds)
        {
            yield return unitId;
        }

        if (unitIds == null)
        {
            yield break;
        }

        foreach (var unitId in unitIds)
        {
            if (unitId == null)
            {
                continue;
            }

            yield return unitId.UnitId;
        }
    }

    private string[] GetFullscreenUnitIds(ApplovinMaxAdsWaterfallUnitId[] configuredUnitIds, AdsType fallbackType)
    {
        var waterfallUnitIds = configuredUnitIds?
            .Select((unitId) => unitId?.UnitId)
            .Where((unitId) => !string.IsNullOrEmpty(unitId))
            .Distinct()
            .ToArray();

        if (waterfallUnitIds != null && waterfallUnitIds.Length > 0)
        {
            return waterfallUnitIds;
        }

        if (UnitIdsMapping.TryGetValue(fallbackType, out var mappedUnit) && !string.IsNullOrEmpty(mappedUnit.UnitId))
        {
            return new[] { mappedUnit.UnitId };
        }

        return Array.Empty<string>();
    }

    public string GetUnitId(AdsType type, bool isAndroid)
    {
        if (UnitIdsMapping != null && UnitIdsMapping.TryGetValue(type, out var unit) && unit != null)
        {
            string uid = unit.GetUnitId(isAndroid);
            if (!string.IsNullOrEmpty(uid)) return uid;
        }
        return "NOT CONFIGURED";
    }

    private string[] GetFullscreenUnitIds(ApplovinMaxAdsWaterfallUnitId[] configuredUnitIds, AdsType fallbackType, bool isAndroid)
    {
        var waterfallUnitIds = configuredUnitIds?
            .Select((unitId) => unitId?.GetUnitId(isAndroid))
            .Where((unitId) => !string.IsNullOrEmpty(unitId))
            .Distinct()
            .ToArray();

        if (waterfallUnitIds != null && waterfallUnitIds.Length > 0)
        {
            return waterfallUnitIds;
        }

        if (UnitIdsMapping != null && UnitIdsMapping.TryGetValue(fallbackType, out var mappedUnit) && mappedUnit != null)
        {
            string uid = mappedUnit.GetUnitId(isAndroid);
            if (!string.IsNullOrEmpty(uid))
            {
                return new[] { uid };
            }
        }

        return Array.Empty<string>();
    }
}
