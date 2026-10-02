#if APPMETRICA_SDK
using Io.AppMetrica;
using Io.AppMetrica.Ecommerce;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppMetricaManager : MonoBehaviour
{
    #region Singleton

    private static AppMetricaManager _instance;

    /// <summary>
    /// Gets the singleton instance of AppMetricaManager.
    /// </summary>
    public static AppMetricaManager Instance
    {
        get { return _instance; }
    }

    #endregion

    private string AppMetricaFirstSessionMarkerKey => "APPMETRICA_FIRST_SESSION";

    public bool IsInitialized { get; private set; }
    public bool IsTrackingEnabled { get; private set; }
    public AppMetricaTrackingConfig Configuration { get; private set; }
    public int Priority => 1;

    public bool IsFirstSession
    {
        get
        {
            bool isFirstSession = !PlayerPrefs.HasKey(AppMetricaFirstSessionMarkerKey);
            if (isFirstSession)
            {
                PlayerPrefs.SetInt(AppMetricaFirstSessionMarkerKey, 1);
                PlayerPrefs.Save();
            }
            return isFirstSession;
        }
    }

    #region Unity Lifecycle

    /// <summary>
    /// Initializes the singleton instance and ensures only one AdjustManager exists.
    /// </summary>
    private void Awake()
    {
        if (_instance != null)
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


    public void Initialize()
    {
        try
        {
            AppMetrica.OnActivation += HandleAppMetricaAppActivated;
            AppMetrica.Activate(new AppMetricaConfig(Configuration.ApiKey)
            {
                FirstActivationAsUpdate = !IsFirstSession,
                Logs = true,
                CrashReporting = true,
            });
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat(
                "Failed to initialize AppMetrica: {0}",
                ex.Message
            );
        }
    }

    private void CleanUp()
    {
        AppMetrica.OnActivation -= HandleAppMetricaAppActivated;
        IsInitialized = false;
        IsTrackingEnabled = false;
    }


    //public void TrackAction(TrackingActionInfo info)
    //{
    //    try
    //    {
    //        AppMetrica.ReportEvent(info.ActionId, SerializeActionInfo(info));
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogWarningFormat(
    //            "Failed to log action {0} to AppMetrica: {1}",
    //            info.ActionId, ex.Message
    //        );
    //    }
    //}

    //private static string SerializeActionInfo(TrackingActionInfo info)
    //{
    //    return info.Parameters == null
    //        ? "{}"
    //        : JsonConvert.SerializeObject(
    //            info.Parameters,
    //            new JsonSerializerSettings
    //            {
    //                NullValueHandling = NullValueHandling.Ignore,
    //                DefaultValueHandling = DefaultValueHandling.Ignore
    //            }
    //    );
    //}

    public void TrackScreen(string screenId)
    {
        try
        {
            ECommerceScreen screen = new ECommerceScreen() { Name = screenId };
            AppMetrica.ReportECommerce(ECommerceEvent.ShowScreenEvent(screen));
        }
        catch (Exception ex)
        {
            Debug.LogWarningFormat(
                "Failed to log screen {0} to AppMetrica: {1}",
                screenId, ex.Message
            );
        }
    }

    public void OnAdImpression(AdImpressionData info)
    {
        try
        {
            var adrev = new AdRevenue(info.Revenue, "USD");
            adrev.AdNetwork = info.NetworkName;
            adrev.AdPlacementId = info.Placement;
            adrev.AdUnitId = info.AdUnitIdentifier;
            AppMetrica.ReportAdRevenue(adrev);
        }
        catch (Exception ex)
        {
            Debug.LogWarningFormat(
                "Failed to log ad revenue {0} to AppMetrica: {1}",
                info.AdUnitIdentifier, ex.Message
            );
        }
    }

    public void TrackPurchaseRevenue(PurchaseTrackingInfo info)
    {
        try
        {
            Revenue purrev = AcquireRevenueInfo(ref info);
            AppMetrica.ReportRevenue(purrev);
        }
        catch (Exception ex)
        {
            Debug.LogWarningFormat(
                "Failed to log purchase {0} to AppMetrica: {1}",
                info.ProductId, ex.Message
            );
        }
    }

    private Revenue AcquireRevenueInfo(ref PurchaseTrackingInfo info)
    {
        var purrev = new Revenue(
            (long)(info.Price * 1000000),
            info.Currency
        )
        {
            ProductID = info.ProductId,
        };

#if UNITY_ANDROID
        purrev.ReceiptValue = new Revenue.Receipt
        {
            Signature = info.Receipt.PayloadAndroid.Signature,
            Data = info.Receipt.PayloadAndroid.Json,
        };
#elif UNITY_IOS
                purrev.ReceiptValue = new Revenue.Receipt
                {
                    Data = info.ReceiptRaw,
                };
#endif
        return purrev;
    }

    private void HandleAppMetricaAppActivated(AppMetricaConfig config)
    {
        IsInitialized = true;
        IsTrackingEnabled = true;
        Debug.Log("[AppMetrica] Initialized Successfully");
    }
}
#endif