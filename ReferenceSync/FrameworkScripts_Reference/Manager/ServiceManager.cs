#if APPSFLYER_SDK
using System;
using System.Collections.Generic;
#endif
#if ADJUST_SDK
using AdjustSdk;
#endif
using System;
using AdMod;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

/// <summary>
/// GameManager is a singleton MonoBehaviour responsible for coordinating initialization and event handling
/// between FirebaseManager, AdjustManager, and AppsFlyerManager. It listens for key events such as
/// Firebase initialization, Adjust attribution changes, deferred deeplink reception, and AppsFlyer conversion data.
/// The class ensures that attribution and conversion data are forwarded to FirebaseManager only after
/// Firebase is initialized, caching them if necessary until initialization completes.
/// </summary>
public class ServiceManager : MonoBehaviour
{
    #region Singleton
    private static ServiceManager _instance;
    public static ServiceManager Instance
    {
        get { return _instance; }
    }
    #endregion

    #region Fields

#if ADJUST_SDK || APPSFLYER_SDK
    private bool IsFirebaseInitialized;
#endif
#if ADJUST_SDK
    private AdjustSdk.AdjustAttribution attribution = null;
#endif

#if APPSFLYER_SDK
    private Dictionary<string, object> conversionData = null;
#endif

    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_instance != null)
        {
            DestroyImmediate(this.gameObject);
        }
        else
        {
            _instance = this;
            //DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.OnFirebaseInitialized += OnFirebaseInitialized;
        }

#if ADJUST_SDK
        if (AdjustManager.Instance != null)
        {
            AdjustManager.Instance.OnAttributionChanged += OnAttributionChanged;
            AdjustManager.Instance.OnDeferredDeeplinkReceived += OnDeferredDeeplinkReceived;
        }
#endif

#if APPSFLYER_SDK
        if (AppsFlyerManager.Instance != null)
        {
            AppsFlyerManager.Instance.OnConversionDataSuccess += OnConversionDataSuccess;
        }
#endif

        if (AdmobManager.Instance != null)
        {
            AdmobManager.Instance.OnAdInitialized += OnAdInitialized;
            AdmobManager.Instance.OnBackgroundAdImpression += FirebaseManager.Instance.OnAdImpression;
            AdmobManager.Instance.OnAdDisplay += FirebaseManager.Instance.OnAdDisplay;
            AdmobManager.Instance.OnAdDisplayTrigger += FirebaseManager.Instance.OnAdDisplayTrigger;
            AdmobManager.Instance.OnAdDisplayed += FirebaseManager.Instance.OnAdDisplayed;
            AdmobManager.Instance.OnAdDisplayFailed += FirebaseManager.Instance.OnAdDisplayFailed;
            AdmobManager.Instance.OnAdPaid += FirebaseManager.Instance.OnAdPaid;
            AdmobManager.Instance.OnAdClicked += FirebaseManager.Instance.OnAdClicked;
            AdmobManager.Instance.OnAdHidden += FirebaseManager.Instance.OnAdHidden;
            AdmobManager.Instance.OnAdRewardReceived += FirebaseManager.Instance.OnAdRewardReceived;
            AdmobManager.Instance.SendTrackingAdsEvent += FirebaseManager.Instance.SendTrackingAdsEvent;

#if APPMETRICA_SDK
            if (AppMetricaManager.Instance != null)
            {
                AdmobManager.Instance.OnBackgroundAdImpression += AppMetricaManager.Instance.OnAdImpression;
            }
#endif

#if ADJUST_SDK
            if (AdjustManager.Instance != null)
            {
                AdmobManager.Instance.OnBackgroundAdImpression += AdjustManager.Instance.OnAdImpression;
            }
#endif

#if APPSFLYER_SDK
            if (AppsFlyerManager.Instance != null)
            {
                AdmobManager.Instance.OnBackgroundAdImpression += AppsFlyerManager.Instance.OnAdImpression;
            }
#endif

#if DATABUCKETS_SDK
            if (DatabucketsManager.Instance != null)
            {
                AdmobManager.Instance.OnBackgroundAdImpression += DatabucketsManager.Instance.OnAdImpression;
            }
#endif
        }

        Initialize(OnSuccess, OnError);
    }

    private void OnDestroy()
    {
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.OnFirebaseInitialized -= OnFirebaseInitialized;
        }

#if ADJUST_SDK
        if (AdjustManager.Instance != null)
        {
            AdjustManager.Instance.OnAttributionChanged -= OnAttributionChanged;
            AdjustManager.Instance.OnDeferredDeeplinkReceived -= OnDeferredDeeplinkReceived;
        }
#endif

#if APPSFLYER_SDK
        if (AppsFlyerManager.Instance != null)
        {
            AppsFlyerManager.Instance.OnConversionDataSuccess -= OnConversionDataSuccess;
        }
#endif

        if (AdmobManager.Instance != null)
        {
            AdmobManager.Instance.OnAdInitialized -= OnAdInitialized;
            AdmobManager.Instance.OnBackgroundAdImpression -= FirebaseManager.Instance.OnAdImpression;
            AdmobManager.Instance.OnAdDisplay -= FirebaseManager.Instance.OnAdDisplay;
            AdmobManager.Instance.OnAdDisplayTrigger -= FirebaseManager.Instance.OnAdDisplayTrigger;
            AdmobManager.Instance.OnAdDisplayed -= FirebaseManager.Instance.OnAdDisplayed;
            AdmobManager.Instance.OnAdDisplayFailed -= FirebaseManager.Instance.OnAdDisplayFailed;
            AdmobManager.Instance.OnAdPaid -= FirebaseManager.Instance.OnAdPaid;
            AdmobManager.Instance.OnAdClicked -= FirebaseManager.Instance.OnAdClicked;
            AdmobManager.Instance.OnAdHidden -= FirebaseManager.Instance.OnAdHidden;
            AdmobManager.Instance.OnAdRewardReceived -= FirebaseManager.Instance.OnAdRewardReceived;

#if APPMETRICA_SDK
            if (AppMetricaManager.Instance != null)
            {
                AdmobManager.Instance.OnBackgroundAdImpression -= AppMetricaManager.Instance.OnAdImpression;
            }
#endif

#if ADJUST_SDK
            if (AdjustManager.Instance != null)
            {
                AdmobManager.Instance.OnBackgroundAdImpression -= AdjustManager.Instance.OnAdImpression;
            }
#endif

#if APPSFLYER_SDK
            if (AppsFlyerManager.Instance != null)
            {
                AdmobManager.Instance.OnBackgroundAdImpression -= AppsFlyerManager.Instance.OnAdImpression;
            }
#endif

#if DATABUCKETS_SDK
            if (DatabucketsManager.Instance != null)
            {
                AdmobManager.Instance.OnBackgroundAdImpression -= DatabucketsManager.Instance.OnAdImpression;
            }
#endif
        }
    }
    #endregion

    #region InitializeGamingServices
    void Initialize(Action onSuccess, Action<string> onError)
    {
        try
        {
            var options = new InitializationOptions().SetEnvironmentName("production");

            UnityServices.InitializeAsync(options).ContinueWith(task => onSuccess());
        }
        catch (Exception exception)
        {
            onError(exception.Message);
        }
    }

    void OnSuccess()
    {

    }

    void OnError(string message)
    {

    }
    #endregion

    #region Event Handlers

    private void OnFirebaseInitialized()
    {
#if ADJUST_SDK || APPSFLYER_SDK
        IsFirebaseInitialized = true;
#endif

        if (FirebaseManager.Instance != null)
        {
#if ADJUST_SDK
            FirebaseManager.Instance.OnAttributionChanged(this.attribution);
#endif
#if APPSFLYER_SDK
            FirebaseManager.Instance.OnConversionDataSuccess(this.conversionData);
#endif
        }
    }

#if ADJUST_SDK
    private void OnAttributionChanged(AdjustSdk.AdjustAttribution attribution)
    {
        if (IsFirebaseInitialized)
        {
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.OnAttributionChanged(attribution);
            }
        }
        else
        {
            this.attribution = attribution;
        }
    }

    private void OnDeferredDeeplinkReceived(string deeplinkURL)
    {
        // Handle deferred deeplink if needed
    }
#endif

#if APPSFLYER_SDK
    private void OnConversionDataSuccess(Dictionary<string, object> conversionData)
    {
        if (IsFirebaseInitialized)
        {
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.OnConversionDataSuccess(conversionData);
            }
        }
        else
        {
            this.conversionData = conversionData;
        }
    }
#endif

    private void OnAdInitialized()
    {
#if APPMETRICA_SDK
        if (AppMetricaManager.Instance != null)
        {
            AppMetricaManager.Instance.Initialize();
        }
#endif

#if ADJUST_SDK
        if (AdjustManager.Instance != null)
        {
            AdjustManager.Instance.InitAdjustApp();
        }
#endif

#if APPSFLYER_SDK
        if (AppsFlyerManager.Instance != null)
        {
            AppsFlyerManager.Instance.InitAppsflyerApp();
        }
#endif
    }

    #endregion
}
