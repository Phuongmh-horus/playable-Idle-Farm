#if ADJUST_SDK
using AdjustSdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Manages Adjust SDK integration for analytics and attribution tracking.
/// Handles initialization, event tracking, revenue tracking, and attribution data.
/// 
/// # Adjust SDK Installation Guide (Unity Package Manager)
///
/// ## Steps to Install Adjust SDK
/// 1. Open Unity Editor.
/// 2. Go to Window -> Package Manager.
/// 3. In the Package Manager window, click the + button (top left) and select "Add package from git URL..."
/// 4. Enter the following URL:
///    https://github.com/adjust/unity_sdk.git?path=Assets/Adjust
/// 5. Click Add to start the installation.
///
/// ## Notes
/// - After installation, the Adjust SDK will be available in your project under <b>Assets/Adjust</b>.
/// - Make sure to configure your Adjust App Token in the AdjustConfigSO scriptable object for each platform.
///
/// ## Reference
/// - Official Adjust Unity SDK documentation: https://github.com/adjust/unity_sdk
/// </summary>
public class AdjustManager : MonoBehaviour
{
    #region Singleton

    private static AdjustManager _instance;

    /// <summary>
    /// Gets the singleton instance of AdjustManager.
    /// </summary>
    public static AdjustManager Instance
    {
        get { return _instance; }
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Configuration scriptable object containing Adjust SDK settings.
    /// </summary>
    [SerializeField]
    private AdjustConfigSO Configuration;

    /// <summary>
    /// Adjust SDK configuration instance created from Configuration.
    /// </summary>
    private AdjustConfig _config;

    #endregion

    #region Initialization State

    /// <summary>
    /// Indicates whether the Adjust SDK has been successfully initialized.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Counter for initialization retry attempts (max 3).
    /// </summary>
    private int _initializationAttempts = 0;

    #endregion

    #region Attribution Data

    /// <summary>
    /// Campaign attribution data from Adjust.
    /// Static field accessible globally for campaign tracking.
    /// </summary>
    public static string CAMPAIGN;

    #endregion

    #region Events

    /// <summary>
    /// Event triggered when attribution data changes.
    /// Subscribe to this event to receive attribution updates (campaign, network, creative, etc.).
    /// </summary>
    public Action<AdjustAttribution> OnAttributionChanged;

    /// <summary>
    /// Event triggered when a deferred deeplink is received.
    /// Subscribe to this event to handle deferred deep linking scenarios.
    /// </summary>
    public Action<string> OnDeferredDeeplinkReceived;

    #endregion

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

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the Adjust SDK with configuration settings.
    /// Sets up callbacks for session events, attribution changes, and deferred deeplinks.
    /// In the Unity Editor, initialization is skipped and marked as successful.
    /// </summary>
    public void InitAdjustApp()
    {
        // Create Adjust configuration with app token and environment from scriptable object
        _config = new AdjustConfig(
            Configuration.AppToken,
            (AdjustSdk.AdjustEnvironment)Configuration.Environment
        );

        // Enable background session tracking
        _config.IsSendingInBackgroundEnabled = true;

        // Set log level for debugging
        _config.LogLevel = (AdjustSdk.AdjustLogLevel?)Configuration.LogLevel;

        // Register callbacks for session and attribution events
        _config.SessionSuccessDelegate = HandleAdjustSessionSuccess;
        _config.SessionFailureDelegate = HandleAdjustSessionFailure;
        _config.AttributionChangedDelegate = HandleAttributionChanged;
        _config.DeferredDeeplinkDelegate = HandleDeferredDeeplink;

#if UNITY_EDITOR
        // Skip actual initialization in Unity Editor
        IsInitialized = true;
#else
        InitializeInternal();
#endif
    }

    /// <summary>
    /// Internal method to initialize the Adjust SDK.
    /// Retries up to 3 times if initialization fails.
    /// </summary>
    private void InitializeInternal()
    {
        // Limit retry attempts to prevent infinite loops
        if (_initializationAttempts++ >= 3)
        {
            Debug.LogError("Adjust SDK initialization failed after multiple attempts.");
            return;
        }

        Debug.Log("Start Initializing Adjust SDK");

        try
        {
            // Call TrackThirdPartySharing BEFORE InitSdk as per Adjust docs:
            // https://dev.adjust.com/en/sdk/unity/features/privacy
            AdjustThirdPartySharing adjustThirdPartySharing = new AdjustThirdPartySharing(true);
            Adjust.TrackThirdPartySharing(adjustThirdPartySharing);

            Adjust.InitSdk(_config);
            Debug.Log("[AdjustManager] Adjust.InitSdk() called successfully - Waiting for callback...");

            // Note: SDK starts enabled by default after InitSdk(), so Enable() is NOT needed here.
            // Adjust.Enable() is only for re-enabling after a Disable() call.
            // See: https://dev.adjust.com/en/sdk/unity/configuration#enable-the-sdk
        }
        catch (Exception ex)
        {
            Debug.LogError($"Adjust SDK initialization error: {ex.Message}");
            InitializeInternal();
        }
    }

    /// <summary>
    /// Callback invoked when Adjust session initialization fails.
    /// Attempts to reinitialize on the main thread.
    /// </summary>
    /// <param name="failure">Session failure information</param>
    private void HandleAdjustSessionFailure(AdjustSessionFailure failure)
    {
        IsInitialized = false;
        Debug.Log($"Adjust session initialization failed: {failure.Message} (Adid: {failure.Adid})");

        // Retry initialization on the main thread
        GameTimeManager.ExecuteOnMainThread(InitializeInternal);
    }

    /// <summary>
    /// Callback invoked when Adjust session initialization succeeds.
    /// Requests advertising ID and attribution data on the main thread.
    /// </summary>
    /// <param name="success">Session success information</param>
    private void HandleAdjustSessionSuccess(AdjustSessionSuccess success)
    {
        IsInitialized = true;

        // Request advertising ID and attribution data on main thread
        GameTimeManager.ExecuteOnMainThread(RequireAdvertisingId);
        GameTimeManager.ExecuteOnMainThread(RequireAttributions);

        Debug.Log($"Adjust session initialized successfully (Adid: {success.Adid})");
    }

    /// <summary>
    /// Internal callback for attribution data acquisition.
    /// Processes and registers attribution data on the main thread.
    /// </summary>
    /// <param name="attribution">Attribution information from Adjust</param>
    private void AcquireAttributionCallback(AdjustAttribution attribution)
    {
        Debug.Log("Acquired Attribution Info");

        if (attribution == null)
        {
            Debug.LogWarning("Attribution info received is null");
            return;
        }

        // Process attribution data on main thread
        GameTimeManager.ExecuteOnMainThread(() => RegisterAttributionData(attribution));
    }

    /// <summary>
    /// Handles attribution data changes.
    /// Updates campaign information and triggers attribution changed event.
    /// </summary>
    /// <param name="attributionData">Updated attribution data</param>
    public void HandleAttributionChanged(AdjustAttribution attributionData)
    {
        GameTimeManager.ExecuteOnMainThread(() =>
        {
            CAMPAIGN = attributionData.Campaign.NullReplace("Unknown");

            // Trigger the attribution changed event
            OnAttributionChanged?.Invoke(attributionData);
        });
    }

    /// <summary>
    /// Handles deferred deeplink received from Adjust.
    /// </summary>
    /// <param name="deeplinkURL">The deferred deeplink URL</param>
    private void HandleDeferredDeeplink(string deeplinkURL)
    {
        GameTimeManager.ExecuteOnMainThread(() =>
        {
            // Trigger the deferred deeplink received event
            OnDeferredDeeplinkReceived?.Invoke(deeplinkURL);
        });
    }

    /// <summary>
    /// Requests the advertising ID from the device.
    /// - Android: Gets Google Ad ID (GAID)
    /// - iOS: Gets Identifier for Advertisers (IDFA)
    /// </summary>
    public void RequireAdvertisingId()
    {
        try
        {
            Debug.Log("Acquiring Advertising ID");
#if !UNITY_EDITOR
            Adjust.GetAdid(AcquireAdidCallback);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            Adjust.GetGoogleAdId(AcquireAdvertisingIdCallback);
#endif

#if UNITY_IOS && !UNITY_EDITOR
            Adjust.GetIdfa(AcquireAdvertisingIdCallback);
#endif
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("Failed to acquire Advertising ID: {0}", ex.Message);
        }
    }

#if !UNITY_EDITOR
    private void AcquireAdidCallback(string adid)
    {
        if (!string.IsNullOrEmpty(adid))
        {
            GameTimeManager.ExecuteOnMainThread(() =>
            {
                FirebaseManager.SetUserProperty(FirebaseEventName.ADJUST_ID, adid);
                PlayerPrefs.SetString(FirebaseEventName.ADJUST_ID, adid);
                PlayerPrefs.Save();
            });
        }
    }
    
    /// <summary>
    /// Callback invoked when advertising ID is acquired.
    /// Saves the advertising ID to PlayerPrefs on the main thread.
    /// </summary>
    /// <param name="adid">The advertising ID</param>
    private void AcquireAdvertisingIdCallback(string adid)
    {
        Debug.LogFormat("Acquired Advertising ID: {0}", adid);

        if (!string.IsNullOrEmpty(adid))
        {
            GameTimeManager.ExecuteOnMainThread(() =>
            {
                PlayerPrefs.SetString("advertising_id", adid);
                PlayerPrefs.Save();
            });
        }
    }
#endif

    /// <summary>
    /// Requests attribution information from Adjust.
    /// Attribution data includes campaign, network, creative, and other marketing details.
    /// </summary>
    public void RequireAttributions()
    {
        try
        {
            Debug.Log("Acquiring Attribution Info");
            Adjust.GetAttribution(AcquireAttributionCallback);
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("Failed to acquire Attribution Info: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Registers attribution data (placeholder for future implementation).
    /// Override this method to implement custom attribution data processing.
    /// </summary>
    /// <param name="attribution">Attribution data to register</param>
    private void RegisterAttributionData(AdjustAttribution attribution)
    {
        HandleAttributionChanged(attribution);
    }

    #endregion

    /// <summary>
    /// Tracks in-app purchase revenue to Adjust.
    /// Sends purchase details including price, product ID, transaction ID, and receipt.
    /// </summary>
    /// <param name="price">Purchase price amount</param>
    /// <param name="priceName">Currency code (e.g., "USD")</param>
    /// <param name="productId">Product identifier</param>
    /// <param name="transactionId">Transaction identifier</param>
    /// <param name="purchaseToken">Purchase receipt/token</param>
    /// <param name="quantity">Purchase quantity</param>
    public void TrackPurchaseRevenue(double price, string priceName, string productId, string transactionId, string purchaseToken, int quantity)
    {
        try
        {
            // Skip tracking if blocked by tracking settings
            if (TrackingEvent.IsBlockTracking())
            {
                return;
            }

            // Create Adjust event for IAP tracking
            AdjustEvent adjustEvent = new AdjustEvent(Configuration.IapEventName);
            adjustEvent.SetRevenue(price, priceName);
            adjustEvent.ProductId = productId;
            adjustEvent.TransactionId = transactionId;
            adjustEvent.PurchaseToken = purchaseToken;
            adjustEvent.AddCallbackParameter(FirebaseEventName.QUANTITY, quantity.ToString("F2"));

            // Track the event
            Adjust.TrackEvent(adjustEvent);

            Debug.LogFormat(
                "Logged purchase revenue to Adjust: {0} {1} for product {2} (Transaction ID: {3})",
                adjustEvent.Revenue, adjustEvent.Currency, adjustEvent.ProductId, adjustEvent.TransactionId
            );
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("Failed to log purchase revenue to Adjust: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Tracks a custom event to Adjust.
    /// Use this method to send any custom analytics event to Adjust.
    /// </summary>
    /// <param name="adjustEvent">The Adjust event to track</param>
    public void TrackEvent(AdjustEvent adjustEvent)
    {
        try
        {
            // Skip tracking if blocked by tracking settings
            if (TrackingEvent.IsBlockTracking() && Configuration.Environment == AdjustEnvironment.Production)
            {
                return;
            }
            Adjust.TrackEvent(adjustEvent);
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("Failed to log event to Adjust: {0}", ex.Message);
        }
    }

    #region Ad Revenue Tracking

    /// <summary>
    /// Tracks ad impression revenue to Adjust.
    /// Supports both AppLovin MAX and IronSource SDKs.
    /// Executes on the main thread to ensure thread safety.
    /// </summary>
    /// <param name="impressionData">Ad impression data including revenue, network, and placement info</param>
    public void OnAdImpression(AdImpressionData impressionData)
    {
        try
        {
            // Skip tracking if blocked by tracking settings
            if (TrackingEvent.IsBlockTracking() && Configuration.Environment == AdjustEnvironment.Production)
            {
                return;
            }

            // Create ad revenue object based on the ad SDK being used
#if MAX_SDK
            var adRevenue = new AdjustAdRevenue("applovin_max_sdk");
#elif IS_SDK
            var adRevenue = new AdjustAdRevenue("ironsource_sdk");
#endif
            // Set revenue and impression details
            adRevenue.SetRevenue(impressionData.Revenue, "USD");
            adRevenue.AdRevenueNetwork = impressionData.NetworkName;
            adRevenue.AdRevenueUnit = impressionData.AdUnitIdentifier;
            adRevenue.AdRevenuePlacement = impressionData.Placement;

            // Track ad revenue on main thread
            Adjust.TrackAdRevenue(adRevenue);
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("Failed to log ad revenue to Adjust: {0}", ex.Message);
        }
    }

    #endregion

    #region verify iap
    public void VerifyPurchase(Product product, string purchaseToken, Action<bool> callback)
    {
#if UNITY_EDITOR
        callback?.Invoke(true);
        return;
#endif
        try
        {
#if UNITY_ANDROID
            var purchase = new AdjustPlayStorePurchase(product.definition.id, purchaseToken);
            Adjust.VerifyPlayStorePurchase(purchase, (verificationResult) =>
            {
                Debug.Log($"[Adjust] Verification status: {verificationResult.VerificationStatus}");
                Debug.Log($"[Adjust] Code: {verificationResult.Code}");
                Debug.Log($"[Adjust] Message: {verificationResult.Message}");
                Debug.Log($"[Adjust] productId: {product.definition.id}");
                Debug.Log($"[Adjust] purchaseToken: {purchaseToken}");

                if (verificationResult.VerificationStatus == "passed" ||
                    verificationResult.VerificationStatus == "success")
                {
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
#elif UNITY_IOS
            var purchase = new AdjustAppStorePurchase(product.definition.id, product.transactionID);
            Adjust.VerifyAppStorePurchase(purchase, (verificationResult) =>
            {
                Debug.Log($"[Adjust] Verification status: {verificationResult.VerificationStatus}");
                Debug.Log($"[Adjust] Code: {verificationResult.Code}");
                Debug.Log($"[Adjust] Message: {verificationResult.Message}");
                Debug.Log($"[Adjust] productId: {product.definition.id}");

                if (verificationResult.VerificationStatus == "passed" ||
                    verificationResult.VerificationStatus == "success")
                {
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
#else
                callback?.Invoke(true);
#endif           
        }
        catch (Exception ex)
        {
            Debug.LogError($"Verify Exception: {ex.Message}");
            callback?.Invoke(false);
        }
    }
    #endregion

    public string GetAppToken()
    {
        return Configuration.AppToken;
    }

    public string GetIAPToken()
    {
        return Configuration.IapEventName;
    }

    public AdjustEnvironment GetEnvironment()
    {
        return Configuration.Environment;
    }
}

#endif