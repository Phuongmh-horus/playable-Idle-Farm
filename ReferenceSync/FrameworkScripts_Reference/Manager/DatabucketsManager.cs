#if DATABUCKETS_SDK
using Databuckets.Analytics;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DatabucketsManager : MonoBehaviour
{
    #region Singleton

    private static DatabucketsManager _instance;

    public static DatabucketsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DatabucketsManager>();
            }

            if (_instance == null)
            {
                GameObject go = new GameObject(nameof(DatabucketsManager) + "(MonoSingleton)");
                _instance = go.AddComponent<DatabucketsManager>();
            }

            return _instance;
        }
    }

    #endregion

    public bool IsInitialized { get; private set; }

    [SerializeField] private DatabucketsTrackingConfig Configuration;

    #region Unity Lifecycle

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DestroyImmediate(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    #endregion

    #region Initialization

    public void Initialize()
    {
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        await UniTask.NextFrame(destroyCancellationToken);

        try
        {
            if (Configuration == null)
            {
                Debug.LogError("[Databuckets] Configuration is null. Skipping initialization.");
                return;
            }

            string endpoint = Configuration.ApiEndpoint;
            string apiKey = Configuration.ApiKey;

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[Databuckets] Endpoint or API key is empty. Skipping initialization.");
                return;
            }

            DatabucketsTracker.Init(endpoint, apiKey);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("Failed to initialize Databuckets: {0}", ex.Message);
            return;
        }

        await UniTask.DelayFrame(2, cancellationToken: destroyCancellationToken);

        IsInitialized = true;

        if (DatabucketsTracker.TryGetUserPseudoId(out string userPseudoId))
        {
            Debug.Log($"[Databuckets] Initialized — user_pseudo_id: {userPseudoId}");
        }
        else
        {
            Debug.LogWarning("[Databuckets] Initialized but unable to read user_pseudo_id.");
        }

        if (DatabucketsTracker.TryGetCountry(out string country))
        {
            Debug.Log($"[Databuckets] Country resolved: {country}");
        }

        InitializeDefaultUserProperties();
    }

    private void CleanUp()
    {
        IsInitialized = false;
    }

    #endregion

    #region Tracking — Generic

    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning($"[Databuckets] TrackEvent skipped (not initialized): {eventName}");
            return;
        }

        try
        {
            DatabucketsTracker.Record(eventName, parameters);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] TrackEvent: {eventName}");
#endif
        }
        catch (Exception ex)
        {
            Debug.LogWarningFormat("Failed to log event {0} to Databuckets: {1}", eventName, ex.Message);
        }
    }

    #endregion

    #region Tracking — Level Analytics (Data Tracking Plan)

    public void TrackLevelStart(LevelStartInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackLevelStart skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("level_start", new Dictionary<string, object>
            {
                ["level"] = info.Level,
                ["loop_by"] = info.LoopBy,
                ["play_type"] = info.PlayType ?? string.Empty,
                ["play_index"] = info.PlayIndex,
                ["lose_index"] = info.LoseIndex,
                ["duration_total_start"] = info.DurationTotalStart,
                ["mode"] = info.Mode ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] level_start: level={info.Level}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log level_start: {0}", ex.Message); }
    }

    public void TrackLevelEnd(LevelEndInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackLevelEnd skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("level_end", new Dictionary<string, object>
            {
                ["level"] = info.Level,
                ["loop_by"] = info.LoopBy,
                ["play_type"] = info.PlayType ?? string.Empty,
                ["mode"] = info.Mode ?? string.Empty,
                ["play_index"] = info.PlayIndex,
                ["lose_index"] = info.LoseIndex,
                ["duration_total_start"] = info.DurationTotalStart,
                ["duration_total_end"] = info.DurationTotalEnd,
                ["duration_remain"] = info.DurationRemain,
                ["duration_play"] = info.DurationPlay,
                ["items_total"] = info.ItemsTotal,
                ["items_cleared"] = info.ItemsCleared,
                ["action_seq"] = info.ActionSeq ?? string.Empty,
                ["result"] = info.Result ?? string.Empty,
                ["lose_by"] = info.LoseBy ?? null
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] level_end: level={info.Level} result={info.Result}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log level_end: {0}", ex.Message); }
    }

    public void TrackLevelExit(LevelExitInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackLevelExit skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("level_exit", new Dictionary<string, object>
            {
                ["level"] = info.Level,
                ["loop_by"] = info.LoopBy,
                ["play_type"] = info.PlayType ?? string.Empty,
                ["mode"] = info.Mode ?? string.Empty,
                ["play_index"] = info.PlayIndex,
                ["lose_index"] = info.LoseIndex,
                ["exit_index"] = info.ExitIndex,
                ["duration_total_start"] = info.DurationTotalStart,
                ["duration_total_end"] = info.DurationTotalEnd,
                ["duration_remain"] = info.DurationRemain,
                ["duration_play"] = info.DurationPlay,
                ["items_total"] = info.ItemsTotal,
                ["items_cleared"] = info.ItemsCleared,
                ["action_seq"] = info.ActionSeq ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] level_exit: level={info.Level}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log level_exit: {0}", ex.Message); }
    }

    public void TrackLevelReopen(LevelReopenInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackLevelReopen skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("level_reopen", new Dictionary<string, object>
            {
                ["level"] = info.Level,
                ["loop_by"] = info.LoopBy,
                ["play_index"] = info.PlayIndex,
                ["lose_index"] = info.LoseIndex,
                ["duration_total_start"] = info.DurationTotalStart,
                ["mode"] = info.Mode ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] level_reopen: level={info.Level}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log level_reopen: {0}", ex.Message); }
    }

    #endregion

    #region Tracking — Resource Analytics (Data Tracking Plan)

    public void TrackResourceEarn(ResourceEarnInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackResourceEarn skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("resource_earn", new Dictionary<string, object>
            {
                ["resource_type"] = info.ResourceType ?? string.Empty,
                ["resource_name"] = info.ResourceName ?? string.Empty,
                ["resource_amount"] = info.ResourceAmount,
                ["resource_balance"] = info.ResourceBalance,
                ["placement"] = info.Placement ?? string.Empty,
                ["placement_detail"] = info.PlacementDetail ?? null,
                ["reason"] = info.Reason ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] resource_earn: {info.ResourceName} +{info.ResourceAmount}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log resource_earn: {0}", ex.Message); }
    }

    public void TrackResourceSpend(ResourceSpendInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackResourceSpend skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("resource_spend", new Dictionary<string, object>
            {
                ["resource_type"] = info.ResourceType ?? string.Empty,
                ["resource_name"] = info.ResourceName ?? string.Empty,
                ["resource_amount"] = info.ResourceAmount,
                ["resource_balance"] = info.ResourceBalance,
                ["reason"] = info.Reason ?? string.Empty,
                ["placement"] = info.Placement ?? string.Empty,
                ["placement_detail"] = info.PlacementDetail ?? null
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] resource_spend: {info.ResourceName} -{info.ResourceAmount}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log resource_spend: {0}", ex.Message); }
    }

    #endregion

    #region Tracking — IAP

    public void TrackIapShow(IapShowInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackIapShow skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("iap_show", new Dictionary<string, object>
            {
                ["placement"] = info.Placement ?? string.Empty,
                ["show_type"] = info.ShowType ?? string.Empty,
                ["trigger_type"] = info.TriggerType ?? string.Empty,
                ["pack_name"] = info.PackName
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] iap_show: placement={info.Placement}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log iap_show: {0}", ex.Message); }
    }

    public void TrackIapClick(IapClickInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackIapClick skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("iap_click", new Dictionary<string, object>
            {
                ["placement"] = info.Placement ?? string.Empty,
                ["show_type"] = info.ShowType ?? string.Empty,
                ["trigger_type"] = info.TriggerType ?? string.Empty,
                ["pack_name"] = info.PackName ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] iap_click: {info.PackName}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log iap_click: {0}", ex.Message); }
    }

    public void TrackIapPurchaseSuccess(IapPurchaseInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackIapPurchaseSuccess skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("iap_purchase_success", new Dictionary<string, object>
            {
                ["placement"] = info.Placement ?? string.Empty,
                ["show_type"] = info.ShowType ?? string.Empty,
                ["trigger_type"] = info.TriggerType ?? string.Empty,
                ["pack_name"] = info.PackName ?? string.Empty,
                ["price"] = info.Price,
                ["currency"] = info.Currency ?? "USD"
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] iap_purchase_success: {info.PackName} — {info.Price} {info.Currency}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log iap_purchase_success: {0}", ex.Message); }
    }

    public void TrackIapPurchaseFailed(IapPurchaseFailedInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackIapPurchaseFailed skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("iap_purchase_failed", new Dictionary<string, object>
            {
                ["placement"] = info.Placement ?? string.Empty,
                ["show_type"] = info.ShowType ?? string.Empty,
                ["trigger_type"] = info.TriggerType ?? string.Empty,
                ["pack_name"] = info.PackName ?? string.Empty,
                ["price"] = info.Price,
                ["currency"] = info.Currency ?? "USD",
                ["fail_reason"] = info.FailReason ?? string.Empty,
                ["error_code"] = info.ErrorCode ?? null
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] iap_purchase_failed: {info.PackName} reason={info.FailReason}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log iap_purchase_failed: {0}", ex.Message); }
    }

    public void TrackIapClose(IapCloseInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackIapClose skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("iap_close", new Dictionary<string, object>
            {
                ["placement"] = info.Placement ?? string.Empty,
                ["show_type"] = info.ShowType ?? string.Empty,
                ["trigger_type"] = info.TriggerType ?? string.Empty,
                ["pack_name"] = info.PackName,
                ["duration_iap"] = info.DurationIap
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] iap_close: placement={info.Placement}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log iap_close: {0}", ex.Message); }
    }

    #endregion

    #region Tracking — IAA / Ads (Data Tracking Plan)

    public void TrackAdRequest(AdRequestInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackAdRequest skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("ad_request", new Dictionary<string, object>
            {
                ["ad_format"] = info.AdFormat ?? string.Empty,
                ["ad_platform"] = info.AdPlatform ?? string.Empty,
                ["ad_network"] = info.AdNetwork ?? string.Empty,
                ["ad_unit_id"] = info.AdUnitId ?? string.Empty,
                ["placement"] = info.Placement ?? string.Empty,
                ["is_load"] = info.IsLoad ? 1 : 0,
                ["error_type"] = info.ErrorType ?? null,
                ["error_code"] = info.ErrorCode ?? null,
                ["load_time"] = info.LoadTime
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] ad_request: format={info.AdFormat} load={info.IsLoad}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log ad_request: {0}", ex.Message); }
    }

    public void TrackAdImpression(AdImpressionTrackingInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackAdImpression skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("ad_impression", new Dictionary<string, object>
            {
                ["ad_format"] = info.AdFormat ?? string.Empty,
                ["ad_platform"] = info.AdPlatform ?? string.Empty,
                ["ad_network"] = info.AdNetwork ?? string.Empty,
                ["ad_unit_id"] = info.AdUnitId ?? string.Empty,
                ["placement"] = info.Placement ?? string.Empty,
                ["value"] = info.Value,
                ["is_show"] = info.IsShow ? 1 : 0,
                ["error_type"] = info.ErrorType ?? null,
                ["error_code"] = info.ErrorCode ?? null
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] ad_impression: network={info.AdNetwork} placement={info.Placement}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log ad_impression: {0}", ex.Message); }
    }

    /// <summary>
    /// Legacy wrapper cho ad_impression từ AdImpressionData (dùng trong ServiceManager callback).
    /// </summary>
    public void OnAdImpression(AdImpressionData info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] AdImpression skipped (not initialized)"); return; }

        try
        {
            string adFormat = info.AdFormat ?? info.AdsType.ToString().ToLower();
            DatabucketsTracker.Record("ad_impression", new Dictionary<string, object>
            {
                ["ad_format"] = adFormat,
                ["ad_platform"] = info.Platform ?? "max",
                ["ad_network"] = info.NetworkName ?? string.Empty,
                ["ad_unit_id"] = info.AdUnitIdentifier ?? string.Empty,
                ["placement"] = info.Placement ?? string.Empty,
                ["value"] = info.Revenue,
                ["is_show"] = 1,
                ["error_type"] = null,
                ["error_code"] = null
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] ad_impression: {info.NetworkName} — {info.Placement}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log ad_impression: {0}", ex.Message); }
    }

    public void TrackAdOfferShown(AdOfferShowInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackAdOfferShown skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("ad_offer_shown", new Dictionary<string, object>
            {
                ["ad_format"] = info.AdFormat ?? string.Empty,
                ["placement"] = info.Placement ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] ad_offer_shown: format={info.AdFormat} placement={info.Placement}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log ad_offer_shown: {0}", ex.Message); }
    }

    public void TrackAdClick(AdClickInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackAdClick skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("ad_click", new Dictionary<string, object>
            {
                ["ad_format"] = info.AdFormat ?? string.Empty,
                ["ad_platform"] = info.AdPlatform ?? string.Empty,
                ["ad_network"] = info.AdNetwork ?? string.Empty,
                ["ad_unit_id"] = info.AdUnitId ?? string.Empty,
                ["placement"] = info.Placement ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] ad_click: network={info.AdNetwork}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log ad_click: {0}", ex.Message); }
    }

    public void TrackAdComplete(AdCompleteInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackAdComplete skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("ad_complete", new Dictionary<string, object>
            {
                ["ad_format"] = info.AdFormat ?? string.Empty,
                ["ad_platform"] = info.AdPlatform ?? string.Empty,
                ["ad_network"] = info.AdNetwork ?? string.Empty,
                ["ad_unit_id"] = info.AdUnitId ?? string.Empty,
                ["end_type"] = info.EndType ?? string.Empty,
                ["duration_ad"] = info.DurationAd,
                ["placement"] = info.Placement ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] ad_complete: format={info.AdFormat} end_type={info.EndType}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log ad_complete: {0}", ex.Message); }
    }

    #endregion

    #region Tracking — Notifications (Data Tracking Plan)

    public void TrackNotiSend(NotiInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackNotiSend skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("noti_send", new Dictionary<string, object>
            {
                ["noti_cate"] = info.NotiCate ?? string.Empty,
                ["noti_name"] = info.NotiName ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] noti_send: cate={info.NotiCate}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log noti_send: {0}", ex.Message); }
    }

    public void TrackNotiReceive(NotiInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackNotiReceive skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("noti_receive", new Dictionary<string, object>
            {
                ["noti_cate"] = info.NotiCate ?? string.Empty,
                ["noti_name"] = info.NotiName ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] noti_receive: cate={info.NotiCate}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log noti_receive: {0}", ex.Message); }
    }

    public void TrackNotiOpen(NotiInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackNotiOpen skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("noti_open", new Dictionary<string, object>
            {
                ["noti_cate"] = info.NotiCate ?? string.Empty,
                ["noti_name"] = info.NotiName ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] noti_open: cate={info.NotiCate}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log noti_open: {0}", ex.Message); }
    }

    #endregion

    #region Tracking — LiveOps / Features (Data Tracking Plan)

    public void TrackFeatureFirstShow(FeatureShowInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackFeatureFirstShow skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("feature_first_show", new Dictionary<string, object>
            {
                ["feature_name"] = info.FeatureName ?? string.Empty,
                ["feature_category"] = info.FeatureCategory ?? string.Empty,
                ["feature_schedule_type"] = info.FeatureScheduleType ?? string.Empty,
                ["feature_start_date"] = info.FeatureStartDate,
                ["feature_end_date"] = info.FeatureEndDate,
                ["placement"] = info.Placement ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] feature_first_show: {info.FeatureName}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log feature_first_show: {0}", ex.Message); }
    }

    public void TrackFeatureOpen(FeatureOpenInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackFeatureOpen skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("feature_open", new Dictionary<string, object>
            {
                ["feature_name"] = info.FeatureName ?? string.Empty,
                ["feature_category"] = info.FeatureCategory ?? string.Empty,
                ["feature_schedule_type"] = info.FeatureScheduleType ?? string.Empty,
                ["feature_start_date"] = info.FeatureStartDate,
                ["feature_end_date"] = info.FeatureEndDate,
                ["placement"] = info.Placement ?? string.Empty,
                ["open_type"] = info.OpenType ?? string.Empty,
                ["open_index"] = info.OpenIndex
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] feature_open: {info.FeatureName}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log feature_open: {0}", ex.Message); }
    }

    public void TrackFeatureClose(FeatureCloseInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackFeatureClose skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("feature_close", new Dictionary<string, object>
            {
                ["feature_name"] = info.FeatureName ?? string.Empty,
                ["feature_category"] = info.FeatureCategory ?? string.Empty,
                ["feature_schedule_type"] = info.FeatureScheduleType ?? string.Empty,
                ["feature_start_date"] = info.FeatureStartDate,
                ["feature_end_date"] = info.FeatureEndDate,
                ["placement"] = info.Placement ?? string.Empty,
                ["open_index"] = info.OpenIndex,
                ["duration_feature"] = info.DurationFeature
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] feature_close: {info.FeatureName}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log feature_close: {0}", ex.Message); }
    }

    public void TrackEventStart(IngameEventStartInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackEventStart skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("event_start", new Dictionary<string, object>
            {
                ["event_name_ingame"] = info.EventNameIngame ?? string.Empty,
                ["event_schedule_type"] = info.EventScheduleType ?? string.Empty,
                ["event_start_date"] = info.EventStartDate,
                ["event_end_date"] = info.EventEndDate,
                ["placement"] = info.Placement ?? string.Empty,
                ["event_start_index"] = info.EventStartIndex
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] event_start: {info.EventNameIngame}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log event_start: {0}", ex.Message); }
    }

    public void TrackEventEnd(IngameEventEndInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackEventEnd skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("event_end", new Dictionary<string, object>
            {
                ["event_name_ingame"] = info.EventNameIngame ?? string.Empty,
                ["event_schedule_type"] = info.EventScheduleType ?? string.Empty,
                ["event_start_date"] = info.EventStartDate,
                ["event_end_date"] = info.EventEndDate,
                ["placement"] = info.Placement ?? string.Empty,
                ["event_start_index"] = info.EventStartIndex,
                ["event_end_type"] = info.EventEndType ?? string.Empty,
                ["event_result"] = info.EventResult ?? null,
                ["event_progression"] = info.EventProgression,
                ["duration_event"] = info.DurationEvent
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] event_end: {info.EventNameIngame} type={info.EventEndType}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log event_end: {0}", ex.Message); }
    }

    #endregion

    #region Tracking — Other Metrics (Data Tracking Plan)

    public void TrackTutorialAction(TutorialActionInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackTutorialAction skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("tutorial_action", new Dictionary<string, object>
            {
                ["action_cate"] = info.ActionCate ?? string.Empty,
                ["action_name"] = info.ActionName ?? string.Empty,
                ["action_index"] = info.ActionIndex
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] tutorial_action: {info.ActionName} [{info.ActionIndex}]");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log tutorial_action: {0}", ex.Message); }
    }

    public void TrackButtonClick(ButtonClickInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackButtonClick skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("button_click", new Dictionary<string, object>
            {
                ["button_name"] = info.ButtonName ?? string.Empty,
                ["screen_name"] = info.ScreenName ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] button_click: {info.ButtonName} @ {info.ScreenName}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log button_click: {0}", ex.Message); }
    }

    public void TrackScreenShow(ScreenShowInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackScreenShow skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("screen_show", new Dictionary<string, object>
            {
                ["screen_name"] = info.ScreenName ?? string.Empty,
                ["button_name"] = info.ButtonName ?? string.Empty,
                ["prev_screen_name"] = info.PrevScreenName ?? string.Empty,
                ["duration_prev_screen"] = info.DurationPrevScreen
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] screen_show: {info.ScreenName}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log screen_show: {0}", ex.Message); }
    }

    public void TrackScreenExit(ScreenExitInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackScreenExit skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("screen_exit", new Dictionary<string, object>
            {
                ["prev_screen_name"] = info.PrevScreenName ?? string.Empty,
                ["duration_prev_screen"] = info.DurationPrevScreen
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] screen_exit: prev={info.PrevScreenName}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log screen_exit: {0}", ex.Message); }
    }

    #endregion

    #region Tracking — Technical Performance (Data Tracking Plan)

    public void TrackLoadingStart(LoadingStartInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackLoadingStart skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("loading_start", new Dictionary<string, object>
            {
                ["data_source"] = info.DataSource ?? string.Empty,
                ["resource_type"] = info.ResourceType ?? string.Empty,
                ["load_context"] = info.LoadContext ?? string.Empty,
                ["trigger_source"] = info.TriggerSource ?? string.Empty,
                ["load_id"] = info.LoadId,
                ["priority"] = info.Priority ?? string.Empty
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] loading_start: source={info.DataSource}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log loading_start: {0}", ex.Message); }
    }

    public void TrackLoadingFinish(LoadingFinishInfo info)
    {
        if (!IsInitialized) { Debug.LogWarning("[Databuckets] TrackLoadingFinish skipped (not initialized)"); return; }

        try
        {
            DatabucketsTracker.Record("loading_finish", new Dictionary<string, object>
            {
                ["data_source"] = info.DataSource ?? string.Empty,
                ["resource_type"] = info.ResourceType ?? string.Empty,
                ["load_context"] = info.LoadContext ?? string.Empty,
                ["trigger_source"] = info.TriggerSource ?? string.Empty,
                ["load_id"] = info.LoadId,
                ["priority"] = info.Priority ?? string.Empty,
                ["result"] = info.Result ?? string.Empty,
                ["load_time"] = info.LoadTime,
                ["error_code"] = info.ErrorCode ?? null,
                ["response_bytes"] = info.ResponseBytes,
                ["retry_count"] = info.RetryCount
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Databuckets Tracking] loading_finish: source={info.DataSource} result={info.Result}");
#endif
        }
        catch (Exception ex) { Debug.LogWarningFormat("Failed to log loading_finish: {0}", ex.Message); }
    }

    #endregion

    #region User Properties (Data Tracking Plan)

    public void InitializeDefaultUserProperties()
    {
        if (!IsInitialized) return;

        try
        {
            int level = DataManager.PlayerData?.LevelDisplay ?? 1;

            // current_level: level hiện tại của user
            SetCommonProperty("current_level", level);

            // balance properties: cần _n suffix để Databuckets convert string→number từ Firebase
            var cash = DataManager.PlayerData?.Cash ?? 0;
            SetCommonProperty("balance_coin_n", cash);

            // connection_type: wifi | mobile_data | offline
            string connectionType = Application.internetReachability == NetworkReachability.NotReachable
                ? "offline"
                : (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork ? "mobile_data" : "wifi");
            SetCommonProperty("connection_type", connectionType);

            // is_iap_user_n
            int iapCount = DataManager.PlayerData?.TransactionDataList?.Count ?? 0;
            SetCommonProperty("is_iap_user_n", iapCount > 0 ? 1 : 0);
            SetCommonProperty("iap_count_n", iapCount);

            // active_day_n
            int activeDay = PlayerPrefs.GetInt("DATABUCKETS_ACTIVE_DAY", 0);
            string lastActiveStr = PlayerPrefs.GetString("DATABUCKETS_LAST_ACTIVE_TIME", "");
            string currentDayStr = System.DateTime.Now.ToString("yyyy-MM-dd");

            if (lastActiveStr != currentDayStr)
            {
                activeDay++;
                PlayerPrefs.SetInt("DATABUCKETS_ACTIVE_DAY", activeDay);
                PlayerPrefs.SetString("DATABUCKETS_LAST_ACTIVE_TIME", currentDayStr);
                PlayerPrefs.Save();
            }
            SetCommonProperty("active_day_n", activeDay);

            // win_streak_n, lose_streak_n
            int winStreak = PlayerPrefs.GetInt("DATABUCKETS_WIN_STREAK", 0);
            SetCommonProperty("win_streak_n", winStreak);
            int loseStreak = PlayerPrefs.GetInt("DATABUCKETS_LOSE_STREAK", 0);
            SetCommonProperty("lose_streak_n", loseStreak);

            // current_mode: mặc định là normal nếu chưa set
            SetCommonProperty("current_mode", new object[] { "normal" });

            // current_event: null nếu chưa tham gia event nào
            SetCommonProperty("current_event", new object[] { "null" });

            // balance_* khởi tạo mặc định
            int sample = DataManager.PlayerData?.BoosterData?.SampleBooster ?? 0;
            SetCommonProperty("balance_sample_n", sample);

            Debug.Log("[Databuckets] Default user properties initialized.");
        }
        catch (Exception ex)
        {
            Debug.LogWarningFormat("Failed to initialize Databuckets user properties: {0}", ex.Message);
        }
    }

    // ── Convenience setters ──

    public void SetBalanceCoin(double balance)
    {
        SetCommonProperty("balance_coin_n", balance);
    }

    public void SetBalanceBooster(string boosterName, int balance)
    {
        SetCommonProperty($"balance_{boosterName}_n", balance);
    }

    public void SetCurrentLevel(int level)
    {
        SetCommonProperty("current_level", level);
    }

    public void SetCurrentMode(string[] modes)
    {
        SetCommonProperty("current_mode", modes);
    }

    public void SetCurrentEvent(string[] events)
    {
        SetCommonProperty("current_event", events);
    }

    public void SetIapUser(bool isIapUser)
    {
        SetCommonProperty("is_iap_user_n", isIapUser ? 1 : 0);
    }

    public void SetIapCount(int count)
    {
        SetCommonProperty("iap_count_n", count);
    }

    public void SetActiveDay(int days)
    {
        SetCommonProperty("active_day_n", days);
        PlayerPrefs.SetInt("DATABUCKETS_ACTIVE_DAY", days);
        PlayerPrefs.Save();
    }

    public void SetConnectionType(string connectionType)
    {
        SetCommonProperty("connection_type", connectionType);
    }

    public void SetWinStreak(int streak)
    {
        SetCommonProperty("win_streak_n", streak);
    }

    public void SetLoseStreak(int streak)
    {
        SetCommonProperty("lose_streak_n", streak);
    }

    public void SetAttributionProperties(string network, string campaign, string adgroup, string creative, string trackerName = null)
    {
        var props = new Dictionary<string, object>
        {
            ["ua_network"] = network ?? string.Empty,
            ["ua_campaign"] = campaign ?? string.Empty,
            ["ua_adgroup"] = adgroup ?? string.Empty,
            ["ua_creative"] = creative ?? string.Empty
        };

        if (!string.IsNullOrEmpty(trackerName))
            props["ua_tracker_name"] = trackerName;

        SetCommonProperties(props);
    }

    public void SetUserId(string userId)
    {
        SetCommonProperty("user_id", userId ?? string.Empty);
    }

    #endregion

    #region Common Properties

    public void SetCommonProperty(string key, object value)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning($"[Databuckets] SetCommonProperty skipped (not initialized): {key}");
            return;
        }

        try
        {
            DatabucketsTracker.SetCommonProperty(key, value);
        }
        catch (Exception ex)
        {
            Debug.LogWarningFormat("Failed to set common property {0} to Databuckets: {1}", key, ex.Message);
        }
    }

    public void SetCommonProperties(Dictionary<string, object> properties)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[Databuckets] SetCommonProperties skipped (not initialized)");
            return;
        }

        try
        {
            DatabucketsTracker.SetCommonProperties(properties);
        }
        catch (Exception ex)
        {
            Debug.LogWarningFormat("Failed to set common properties to Databuckets: {0}", ex.Message);
        }
    }

    #endregion

    #region Identity

    public bool TryGetUserPseudoId(out string userPseudoId)
    {
        try
        {
            return DatabucketsTracker.TryGetUserPseudoId(out userPseudoId);
        }
        catch (Exception ex)
        {
            Debug.LogWarningFormat("Failed to get user pseudo ID from Databuckets: {0}", ex.Message);
            userPseudoId = null;
            return false;
        }
    }

    public bool TryGetCountry(out string country)
    {
        try
        {
            return DatabucketsTracker.TryGetCountry(out country);
        }
        catch (Exception ex)
        {
            Debug.LogWarningFormat("Failed to get country from Databuckets: {0}", ex.Message);
            country = null;
            return false;
        }
    }

    #endregion
}

#region Tracking Info Structs — Level Analytics (Data Tracking Plan)

public struct LevelStartInfo
{
    public int Level;
    public int LoopBy;
    public string PlayType;
    public int PlayIndex;
    public int LoseIndex;
    public long DurationTotalStart;
    public string Mode;
}

public struct LevelEndInfo
{
    public int Level;
    public int LoopBy;
    public string PlayType;
    public string Mode;
    public int PlayIndex;
    public int LoseIndex;
    public long DurationTotalStart;
    public long DurationTotalEnd;
    public long DurationRemain;
    public long DurationPlay;
    public int ItemsTotal;
    public int ItemsCleared;
    public string ActionSeq;
    public string Result;
    public string LoseBy;
}

public struct LevelExitInfo
{
    public int Level;
    public int LoopBy;
    public string PlayType;
    public string Mode;
    public int PlayIndex;
    public int LoseIndex;
    public int ExitIndex;
    public long DurationTotalStart;
    public long DurationTotalEnd;
    public long DurationRemain;
    public long DurationPlay;
    public int ItemsTotal;
    public int ItemsCleared;
    public string ActionSeq;
}

public struct LevelReopenInfo
{
    public int Level;
    public int LoopBy;
    public int PlayIndex;
    public int LoseIndex;
    public long DurationTotalStart;
    public string Mode;
}

#endregion

#region Tracking Info Structs — Resource Analytics (Data Tracking Plan)

public struct ResourceEarnInfo
{
    public string ResourceType;
    public string ResourceName;
    public double ResourceAmount;
    public double ResourceBalance;
    public string Placement;
    public string PlacementDetail;
    public string Reason;
}

public struct ResourceSpendInfo
{
    public string ResourceType;
    public string ResourceName;
    public double ResourceAmount;
    public double ResourceBalance;
    public string Reason;
    public string Placement;
    public string PlacementDetail;
}

#endregion

#region Tracking Info Structs — IAP (Data Tracking Plan)

public struct IapShowInfo
{
    public string Placement;
    public string ShowType;
    public string TriggerType;
    /// <summary>null = show cả shop; string = tên pack; string[] = danh sách pack</summary>
    public object PackName;
}

public struct IapClickInfo
{
    public string Placement;
    public string ShowType;
    public string TriggerType;
    public string PackName;
}

public struct IapPurchaseInfo
{
    public string Placement;
    public string ShowType;
    public string TriggerType;
    public string PackName;
    public double Price;
    public string Currency;
}

public struct IapPurchaseFailedInfo
{
    public string Placement;
    public string ShowType;
    public string TriggerType;
    public string PackName;
    public double Price;
    public string Currency;
    public string FailReason;
    public string ErrorCode;
}

public struct IapCloseInfo
{
    public string Placement;
    public string ShowType;
    public string TriggerType;
    /// <summary>null = show cả shop; string = tên pack; string[] = danh sách pack</summary>
    public object PackName;
    public long DurationIap;
}

#endregion

#region Tracking Info Structs — IAA / Ads (Data Tracking Plan)

public struct AdRequestInfo
{
    public string AdFormat;
    public string AdPlatform;
    public string AdNetwork;
    public string AdUnitId;
    public string Placement;
    public bool IsLoad;
    public string ErrorType;
    public string ErrorCode;
    public long LoadTime;
}

public struct AdImpressionTrackingInfo
{
    public string AdFormat;
    public string AdPlatform;
    public string AdNetwork;
    public string AdUnitId;
    public string Placement;
    public double Value;
    public bool IsShow;
    public string ErrorType;
    public string ErrorCode;
}

public struct AdOfferShowInfo
{
    public string AdFormat;
    public string Placement;
}

public struct AdClickInfo
{
    public string AdFormat;
    public string AdPlatform;
    public string AdNetwork;
    public string AdUnitId;
    public string Placement;
}

public struct AdCompleteInfo
{
    public string AdFormat;
    public string AdPlatform;
    public string AdNetwork;
    public string AdUnitId;
    public string EndType;
    public long DurationAd;
    public string Placement;
}

#endregion

#region Tracking Info Structs — Notifications (Data Tracking Plan)

public struct NotiInfo
{
    public string NotiCate;
    public string NotiName;
}

#endregion

#region Tracking Info Structs — LiveOps / Features (Data Tracking Plan)

public struct FeatureShowInfo
{
    public string FeatureName;
    public string FeatureCategory;
    public string FeatureScheduleType;
    public long FeatureStartDate;
    public long FeatureEndDate;
    public string Placement;
}

public struct FeatureOpenInfo
{
    public string FeatureName;
    public string FeatureCategory;
    public string FeatureScheduleType;
    public long FeatureStartDate;
    public long FeatureEndDate;
    public string Placement;
    public string OpenType;
    public int OpenIndex;
}

public struct FeatureCloseInfo
{
    public string FeatureName;
    public string FeatureCategory;
    public string FeatureScheduleType;
    public long FeatureStartDate;
    public long FeatureEndDate;
    public string Placement;
    public int OpenIndex;
    public long DurationFeature;
}

public struct IngameEventStartInfo
{
    public string EventNameIngame;
    public string EventScheduleType;
    public long EventStartDate;
    public long EventEndDate;
    public string Placement;
    public int EventStartIndex;
}

public struct IngameEventEndInfo
{
    public string EventNameIngame;
    public string EventScheduleType;
    public long EventStartDate;
    public long EventEndDate;
    public string Placement;
    public int EventStartIndex;
    public string EventEndType;
    public string EventResult;
    public long EventProgression;
    public long DurationEvent;
}

#endregion

#region Tracking Info Structs — Other Metrics (Data Tracking Plan)

public struct TutorialActionInfo
{
    public string ActionCate;
    public string ActionName;
    public int ActionIndex;
}

public struct ButtonClickInfo
{
    public string ButtonName;
    public string ScreenName;
}

public struct ScreenShowInfo
{
    public string ScreenName;
    public string ButtonName;
    public string PrevScreenName;
    public long DurationPrevScreen;
}

public struct ScreenExitInfo
{
    public string PrevScreenName;
    public long DurationPrevScreen;
}

#endregion

#region Tracking Info Structs — Technical Performance (Data Tracking Plan)

public struct LoadingStartInfo
{
    public string DataSource;
    public string ResourceType;
    public string LoadContext;
    public string TriggerSource;
    public long LoadId;
    public string Priority;
}

public struct LoadingFinishInfo
{
    public string DataSource;
    public string ResourceType;
    public string LoadContext;
    public string TriggerSource;
    public long LoadId;
    public string Priority;
    public string Result;
    public long LoadTime;
    public string ErrorCode;
    public long ResponseBytes;
    public int RetryCount;
}

#endregion

#endif
