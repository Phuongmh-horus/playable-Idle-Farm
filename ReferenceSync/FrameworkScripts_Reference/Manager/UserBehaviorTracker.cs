using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if ADJUST_SDK
using AdjustSdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


#endif
using UnityEngine;
using UnityEngine.Purchasing;

public static class UserBehaviorTracker
{

    /// <summary>
    /// Get specific price adjustment percentage based on currency code
    /// </summary>
    private static float GetPriceAdjustment(string currencyCode)
    {
#if UNITY_IOS
        // 15% adjustment for HKD, VND, USD, CAD, PHP
        List<string> tier1Currencies = new List<string> { "HKD", "VND", "USD", "CAD", "PHP" };
        if (tier1Currencies.Contains(currencyCode.ToUpper()))
        {
            return 0.85f;
        }

        // 20% adjustment for QAR, JPY, AED, THB, MYR, CHF, TWD, SGD, BRL, KRW, AUD, IDR
        List<string> tier2Currencies = new List<string> { "QAR", "JPY", "AED", "THB", "MYR", "CHF", "TWD", "SGD", "BRL", "KRW", "AUD", "IDR" };
        if (tier2Currencies.Contains(currencyCode.ToUpper()))
        {
            return 0.80f;
        }

        // 30% adjustment for all other currencies
        return 0.70f;
#else
        return FirebaseManager.Instance.GetRemoteNetIAPValue();
#endif
    }

    public static void SendIAPSuccessTracking(IAPTrackingData iapTrackingData)
    {
        if(TrackingEvent.IsBlockTracking()) return;
        iapTrackingData.NetIAPValue = GetPriceAdjustment(iapTrackingData.RegionPrice);

        var value = iapTrackingData.LocalizedPrice * iapTrackingData.NetIAPValue;
        SendIAPAdsjustTracking(value, iapTrackingData.RegionPrice, iapTrackingData.ProductId,
            iapTrackingData.TransactionId, iapTrackingData.PurchaseToken, DataManager.PlayerData.TransactionDataList.Count);

        SendIAPAppMetricaTracking(iapTrackingData);

        SendIAPFirebaseTracking(iapTrackingData, "success");

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackIapPurchaseSuccess(new IapPurchaseInfo
            {
                Placement = "shop",
                ShowType = "ingame",
                TriggerType = "user_initiated",
                PackName = iapTrackingData.ProductName,
                Price = iapTrackingData.RealPrice,
                Currency = "USD"
            });
        }
#endif
    }

    public static void SendIAPFailTracking(IAPTrackingData iapTrackingData, string reason)
    {
        if (TrackingEvent.IsBlockTracking())
        {
            return;
        }
        SendIAPFirebaseTracking(iapTrackingData, "failed", reason);

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackIapPurchaseFailed(new IapPurchaseFailedInfo
            {
                Placement = "shop",
                ShowType = "ingame",
                TriggerType = "user_initiated",
                PackName = iapTrackingData.ProductName,
                Price = iapTrackingData.RealPrice,
                Currency = "USD",
                FailReason = reason,
                ErrorCode = null
            });
        }
#endif
    }

    private static void SendIAPFirebaseTracking(IAPTrackingData iapTrackingData, string status, string failureReason = "")
    {
        try
        {
            var time_local = DateTime.Now.ToDefaultString();
            var time_utc = DateTime.UtcNow.ToDefaultString();

            TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.Firebase_IAP);
            trackingEvent.AddParameter(FirebaseEventName.TRANSACTION_ID, iapTrackingData.TransactionId);
            trackingEvent.AddParameter(FirebaseEventName.PRODUCT_ID, iapTrackingData.ProductId);
            trackingEvent.AddParameter(FirebaseEventName.PRODUCT_NAME, iapTrackingData.ProductName);
            trackingEvent.AddParameter(FirebaseEventName.QUANTITY, iapTrackingData.Quantity);
            trackingEvent.AddParameter(FirebaseEventName.PRICE, iapTrackingData.RealPrice);
            trackingEvent.AddParameter(FirebaseEventName.CURRENCY, "USD");
            trackingEvent.AddParameter(FirebaseEventName.VALUE, iapTrackingData.RealPrice);
            trackingEvent.AddParameter(FirebaseEventName.PAYMENT_GATEWAY, iapTrackingData.PaymentGateway);
            trackingEvent.AddParameter(FirebaseEventName.PAYMENT_METHOD, iapTrackingData.PaymentMethod);
            trackingEvent.AddParameter(FirebaseEventName.STATUS, status);
            trackingEvent.AddParameter(FirebaseEventName.FAILURE_REASON, failureReason);
            trackingEvent.AddParameter(FirebaseEventName.ORDER_ID, GetOrderId());
            trackingEvent.AddParameter(FirebaseEventName.STAGE_ID, DataManager.PlayerData?.LevelDisplay ?? 0);
            trackingEvent.AddParameter(FirebaseEventName.STAGE_NUMBER, DataManager.PlayerData?.LevelDisplay ?? 0);
            trackingEvent.TrackEventFireBase();

            string GetOrderId()
            {
#if UNITY_ANDROID
                try
                {
                    if (string.IsNullOrEmpty(iapTrackingData.Receipt))
                        return null;

                    // Parse the receipt
                    var receiptRoot = JsonConvert.DeserializeObject<JObject>(iapTrackingData.Receipt);
                    if (receiptRoot == null)
                        return null;

                    string payloadJson;
                    
                    // Check if this is a Unity IAP wrapper with "Payload" field
                    var payloadToken = receiptRoot["Payload"];
                    if (payloadToken != null)
                    {
                        // Unity IAP format: {"Store":"GooglePlay", "TransactionID":"...", "Payload":"..."}
                        payloadJson = payloadToken.ToString();
                    }
                    else
                    {
                        // Direct Google Play format: {"json":"...", "signature":"...", "skuDetails":[...]}
                        payloadJson = iapTrackingData.Receipt;
                    }
                    
                    // Use the existing ReceiptAndroid class to parse the payload
                    var receiptAndroid = new ReceiptAndroid(payloadJson);
                    
                    // Return the orderId from the parsed data
                    return receiptAndroid?.JsonData?.orderId;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to extract orderId from receipt: {ex.Message}");
                    return null;
                }
#elif UNITY_IOS
                return iapTrackingData.TransactionId;
#else
                return null;
#endif
            }
        }
        catch { }
    }

    public class ReceiptAndroid
    {
        public ReceiptAndroidJsonData JsonData;

        // Json Fields, ! Case-sensetive
        //public string signature;
        public string json;

        public ReceiptAndroid(string payload)
        {
            // Fix invalid JSON escape sequences (e.g., \₫, \đ, etc.)
            // Replace backslash followed by non-standard escape characters with just the character
            string sanitizedPayload = Regex.Replace(payload, @"\\([^""\\/bfnrtu])", "$1");

            var receipt = JsonConvert.DeserializeObject<JObject>(sanitizedPayload);
            if (receipt == null)
            {
                throw new ArgumentException("Invalid receipt payload: deserialization returned null", nameof(payload));
            }

            var jsonToken = receipt["json"];
            if (jsonToken == null)
            {
                throw new ArgumentException("Invalid receipt payload: missing 'json' field", nameof(payload));
            }

            this.json = jsonToken.ToString();
            this.JsonData = JsonConvert.DeserializeObject<ReceiptAndroidJsonData>(this.json)
                ?? throw new ArgumentException("Invalid receipt JSON data", nameof(payload));
        }

    }

    public class ReceiptAndroidJsonData
    {
        public string orderId;
        public string packageName;
        public string productId;
        public long purchaseTime;
        public int purchaseState;
        public string purchaseToken;
    }

    private static void SendIAPAdsjustTracking(double price, string priceName, string productId, string transactionId, string purchaseToken, int quantity)
    {
#if ADJUST_SDK
        if (AdjustManager.Instance == null)
        {
            Debug.Log("AdjustManager is not initialized.");
            return;
        }
        AdjustManager.Instance.TrackPurchaseRevenue(price, priceName, productId, transactionId, purchaseToken, quantity);
#endif
    }

    private static void SendIAPAppMetricaTracking(IAPTrackingData iAPTracking)
    {
#if APPMETRICA_SDK
        if (AppMetricaManager.Instance == null)
        {
            Debug.Log("AppMetricaManager is not initialized.");
            return;
        }
        AppMetricaManager.Instance.TrackPurchaseRevenue(new PurchaseTrackingInfo
        {
            Currency = iAPTracking?.RegionPrice ?? "USD",
            Price = (double)(iAPTracking?.LocalizedPrice ?? 0),
            ProductId = iAPTracking?.ProductId,
            ReceiptRaw = iAPTracking?.Receipt,
            TransactionId = iAPTracking?.TransactionId
        });
#endif
    }



    #region Login Load Step

    public static void SendLoginTracking()
    {
        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.LOGIN);
        trackingEvent.TrackEventWithoutParam();
    }

    public static void SendLoginLoadStepTracking(string stepName, int statusCode = 200)
    {
        Debug.Log("SendLoginLoadStepTracking: " + stepName);
        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.LOGIN_LOADING_STEP);
        trackingEvent.AddParameter(FirebaseEventName.LOGIN_LOADING_STEP_NAME, stepName);
        trackingEvent.AddParameter(FirebaseEventName.STATUS_CODE, statusCode.ToString());
        trackingEvent.TrackEventFireBase();
    }

    public static void SendNewAccountTracking()
    {
        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.NEW_ACCOUNT);
        // trackingEvent.AddParameter(FirebaseEventName.DEVICE_ID, GameConfig.DeviceId);
        trackingEvent.TrackEventFireBase();
    }

    public static void SendSceneLoadTracking(string sceneName)
    {
        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.SCENE_LOAD);
        trackingEvent.AddParameter(FirebaseEventName.SCENE_NAME, sceneName);
        trackingEvent.TrackEventFireBase();
    }


    #endregion

    #region Tutorial

    public static void SendTutorialTracking(string tutName, int stepNum, string stepName)
    {
        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.TUTORIAL_STEP);
        trackingEvent.AddParameter(FirebaseEventName.TUT_NAME, tutName);
        trackingEvent.AddParameter(FirebaseEventName.STEP_NUMBER, stepNum);
        trackingEvent.AddParameter(FirebaseEventName.STEP_NAME, stepName);
        trackingEvent.TrackEventFireBase();

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackTutorialAction(new TutorialActionInfo
            {
                ActionCate = "click",
                ActionName = stepName,
                ActionIndex = stepNum
            });
        }
#endif
    }

    #endregion

    #region InGame

    public static void SendStartGameTracking(string playId, string gameMode)
    {
        var level = DataManager.PlayerData.LevelDisplay;
        PlayerPrefs.SetInt(FirebaseEventName.HIGHEST_LEVEL_START, level);
        FirebaseManager.SetUserProperty(FirebaseEventName.HIGHEST_LEVEL_START, level.ToString());

        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.GAME_START);
        trackingEvent.AddParameter(FirebaseEventName.PLAY_ID, playId);
        trackingEvent.AddParameter(FirebaseEventName.GAME_MODE, gameMode);
        trackingEvent.AddParameter(FirebaseEventName.STAGE_ID, DataManager.PlayerData.LevelDisplay);
        // trackingEvent.AddParameter("currency_value", DataManager.PlayerData.Cash);
        trackingEvent.TrackEventFireBase();

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackLevelStart(new LevelStartInfo
            {
                Level = level,
                LoopBy = 0,
                PlayType = gameMode ?? "home",
                PlayIndex = 1,
                LoseIndex = 0,
                DurationTotalStart = 0,
                Mode = gameMode ?? "normal"
            });
        }
#endif
    }

    public static void SendEndGameTracking(string playId, string gameMode, string result, int timePlayed)
    {
        var level = DataManager.PlayerData.LevelDisplay;
        PlayerPrefs.SetInt(FirebaseEventName.HIGHEST_LEVEL_WIN, level);
        FirebaseManager.SetUserProperty(FirebaseEventName.HIGHEST_LEVEL_WIN, level.ToString());

        PlayerPrefs.DeleteKey(DataManager.ConsumeHeartForLastKey);
        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.GAME_END);
        trackingEvent.AddParameter(FirebaseEventName.PLAY_ID, playId);
        trackingEvent.AddParameter(FirebaseEventName.GAME_MODE, gameMode);
        trackingEvent.AddParameter(FirebaseEventName.STAGE_ID, DataManager.PlayerData.LevelDisplay);
        trackingEvent.AddParameter(FirebaseEventName.GAME_RESULT, result);
        trackingEvent.AddParameter(FirebaseEventName.INGAME_TIME, timePlayed);
        // trackingEvent.AddParameter("currency_value", DataManager.PlayerData.Cash);
        trackingEvent.TrackEventFireBase();

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackLevelEnd(new LevelEndInfo
            {
                Level = level,
                LoopBy = 0,
                PlayType = gameMode ?? "home",
                Mode = gameMode ?? "normal",
                PlayIndex = 1,
                LoseIndex = 0,
                DurationTotalStart = 0,
                DurationTotalEnd = 0,
                DurationRemain = 0,
                DurationPlay = timePlayed,
                ItemsTotal = 0,
                ItemsCleared = 0,
                ActionSeq = string.Empty,
                Result = result ?? string.Empty,
                LoseBy = null
            });
        }
#endif
    }

    public static void SendInGameDeathTracking(string playId, int preLoseCount, int progressComplete)
    {
        TrackingEvent trackingEvent = new TrackingEvent("ingame_death");
        trackingEvent.AddParameter("play_id", playId);
        trackingEvent.AddParameter("stage_id", DataManager.PlayerData.LevelDisplay);
        trackingEvent.AddParameter("prelose_count", preLoseCount);
        trackingEvent.AddParameter("progress", progressComplete);
        trackingEvent.TrackEventFireBase();

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackEvent("ingame_death", new Dictionary<string, object>
            {
                { "play_id", playId },
                { "pre_lose_count", preLoseCount },
                { "progress_complete", progressComplete }
            });
        }
#endif
    }

    public static void SendInGameReviveTracking(string playId, int reviveCount, int progressComplete)
    {
        TrackingEvent trackingEvent = new TrackingEvent("ingame_revive");
        trackingEvent.AddParameter("play_id", playId);
        trackingEvent.AddParameter("stage_id", DataManager.PlayerData.LevelDisplay);
        trackingEvent.AddParameter("revive_count", reviveCount);
        trackingEvent.AddParameter("progress", progressComplete);
        trackingEvent.TrackEventFireBase();

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackEvent("ingame_revive", new Dictionary<string, object>
            {
                { "play_id", playId },
                { "revive_count", reviveCount },
                { "progress_complete", progressComplete }
            });
        }
#endif
    }
    #endregion

    #region Booster
    public static void SendUseBoosterTracking(string playId, string itemName, int amount, int progress)
    {
        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.BOOSTER_USE);
        var timePlayed = (int)PlaytimeTracker.Instance.GetTimeGamePlay();
        trackingEvent.AddParameter(FirebaseEventName.PLAY_ID, playId);
        trackingEvent.AddParameter(FirebaseEventName.STAGE_ID, DataManager.PlayerData.LevelDisplay);
        trackingEvent.AddParameter(FirebaseEventName.BOOSTER_NAME, itemName);
        trackingEvent.AddParameter(FirebaseEventName.PROGRESS, progress.ToString());
        trackingEvent.AddParameter(FirebaseEventName.INGAME_TIME, timePlayed.ToString());
        trackingEvent.AddParameter(FirebaseEventName.QUANTITY, amount.ToString());
        trackingEvent.TrackEventFireBase();

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackResourceSpend(new ResourceSpendInfo
            {
                ResourceType = "booster",
                ResourceName = itemName,
                ResourceAmount = amount,
                ResourceBalance = 0,
                Reason = "use",
                Placement = "use_booster",
                PlacementDetail = $"use_booster_{progress}"
            });
        }
#endif
    }

    public static void SendEarnBoosterTracking(string playId, string itemName, int amount, string source, string productId = "")
    {
        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.BOOSTER_EARN);
        trackingEvent.AddParameter(FirebaseEventName.PLAY_ID, playId);
        trackingEvent.AddParameter(FirebaseEventName.STAGE_ID, DataManager.PlayerData.LevelDisplay);
        trackingEvent.AddParameter(FirebaseEventName.SOURCE, source);
        trackingEvent.AddParameter(FirebaseEventName.PRODUCT_ID, productId);
        trackingEvent.AddParameter(FirebaseEventName.BOOSTER_NAME, itemName);
        trackingEvent.AddParameter(FirebaseEventName.QUANTITY, amount.ToString());
        trackingEvent.TrackEventFireBase();

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackResourceEarn(new ResourceEarnInfo
            {
                ResourceType = "booster",
                ResourceName = itemName,
                ResourceAmount = amount,
                ResourceBalance = 0,
                Placement = source ?? "unknown",
                PlacementDetail = null,
                Reason = string.IsNullOrEmpty(productId) ? "reward" : "purchase"
            });
        }
#endif
    }
    #endregion

    #region In/outcome

    #region Earn Virtual Currency

    public static void SendEarnVirtualCurrencyTracking(string currencyName, double amount, double currencyBefore, double currencyAfter, string source)
    {
        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.EARN_VIRTUAL_CURRENCY);
        trackingEvent.AddParameter(FirebaseEventName.VIRTUAL_CURRENCY_NAME, currencyName);
        trackingEvent.AddParameter(FirebaseEventName.VALUE, amount.ToString());
        trackingEvent.AddParameter(FirebaseEventName.PLACEMENT, source);
        trackingEvent.AddParameter(FirebaseEventName.BALANCE_BEFORE, currencyBefore);
        trackingEvent.AddParameter(FirebaseEventName.BALANCE_AFTER, currencyAfter);

        trackingEvent.TrackEventFireBase();

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackResourceEarn(new ResourceEarnInfo
            {
                ResourceType = "currency",
                ResourceName = currencyName,
                ResourceAmount = amount,
                ResourceBalance = currencyAfter,
                Placement = source ?? "unknown",
                PlacementDetail = null,
                Reason = "reward"
            });
        }
#endif
    }

    #endregion

    #region Spend Virtual Currency

    public static void SendSpendVirtualCurrencyTracking(string currencyName, double amount, double currencyBefore, double currencyAfter, string source)
    {
        TrackingEvent trackingEvent = new TrackingEvent(FirebaseEventName.SPEND_VIRTUAL_CURRENCY);
        trackingEvent.AddParameter(FirebaseEventName.VIRTUAL_CURRENCY_NAME, currencyName);
        trackingEvent.AddParameter(FirebaseEventName.VALUE, amount.ToString());
        trackingEvent.AddParameter(FirebaseEventName.PLACEMENT, source);
        trackingEvent.AddParameter(FirebaseEventName.BALANCE_BEFORE, currencyBefore);
        trackingEvent.AddParameter(FirebaseEventName.BALANCE_AFTER, currencyAfter);

        trackingEvent.TrackEventFireBase();

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.TrackResourceSpend(new ResourceSpendInfo
            {
                ResourceType = "currency",
                ResourceName = currencyName,
                ResourceAmount = amount,
                ResourceBalance = currencyAfter,
                Reason = "use",
                Placement = source ?? "unknown",
                PlacementDetail = null
            });
        }
#endif
    }

    #endregion

    #endregion
}

public class IAPTrackingData
{
    public string RegionPrice;
    public double LocalizedPrice;
    public double RealPrice;
    public double NetIAPValue;
    public string ProductId;
    public string ProductName;
    public string TransactionId;
    public string Receipt;
    public string PurchaseToken;
    public int Quantity = 1;
    public string PaymentGateway;
    public string PaymentMethod;
}

public struct PurchaseTrackingInfo
{
    public string TransactionId { get; set; }
    public string ProductId { get; set; }
    public double Price { get; set; }
    public string Currency { get; set; }
    public string ReceiptRaw { get; set; }

    public Receipt Receipt
    {
        get
        {
            try { return JsonConvert.DeserializeObject<Receipt>(ReceiptRaw); } catch { }
            return default;
        }
    }
}

[Serializable]
public struct Receipt
{
    public string Store;
    public string TransactionID;
    public string Payload;

    public PayloadAndroid PayloadAndroid
    {
        get
        {
            try { return JsonConvert.DeserializeObject<PayloadAndroid>(Payload); } catch { }
            return default;
        }
    }
}

[Serializable]
public struct PayloadAndroid
{
    public string Json;
    public string Signature;
}
