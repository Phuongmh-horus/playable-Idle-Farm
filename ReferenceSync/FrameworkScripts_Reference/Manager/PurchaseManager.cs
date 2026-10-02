using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Firebase.Analytics;
using Firebase.Extensions;
using I2.Loc;
using UnityEngine;
using UnityEngine.Purchasing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static UserBehaviorTracker;

public class PurchaseManager : MonoSingleton<PurchaseManager>
{
    #region Fields
    [SerializeField]
    private IAPProductSO iapProductSO;

    private StoreController m_StoreController;

    private bool isPurchasePending = false;
    private readonly HashSet<string> m_ProcessingTransactions = new();
    private bool _isRestoring = false;
    private bool? _isUnsafeDeviceCache = null;
    #endregion

    #region Main Methods

    protected override void Awake()
    {
        base.Awake();
        GameEventBus.GetIAPProductByCategory = GetIAPProductByCategory;
        GameEventBus.GetIAPProductById = GetIAPProductById;
        GameEventBus.GetAllIAPProducts = () => iapProductSO.IAPProductList;
        GameEventBus.GetProductPriceByProductCategory = GetProductPriceByProductCategory;
        GameEventBus.OnBuyProduct = BuyProduct;
        GameEventBus.OnRestorePurchases = RestorePurchases;
    }

    private void OnValidate()
    {
        if (iapProductSO == null)
        {
            var configs = Resources.FindObjectsOfTypeAll<IAPProductSO>();
            if (configs.Length > 0) iapProductSO = configs[0];
        }
    }

    private string GetProductPriceByProductCategory(IAPCategory iapCategory)
    {
        var productConfig = GetIAPProductByCategory(iapCategory);
        // Lấy giá từ IAP và hiển thị
        var price = GetSalePriceByProductID(iapCategory);
        string showPrice = "";
        if (productConfig != null && productConfig.SaleRate > 0)
        {
            var originalPrice = GetOriginalPriceFromUnityIAP(iapCategory);
            showPrice += $"<size=70%><s>{originalPrice}</s></size>\n";
        }

        showPrice += price;
        return showPrice;
    }
    private string GetSalePriceByProductID(IAPCategory iapCategory)
    {
        var product = GetProductInitializeByCategory(iapCategory);
        var productConfig = iapProductSO.GetIAPProductByCategory(iapCategory);
        if (productConfig == null)
        {
            Debug.LogError("Product config not found for product " + iapCategory);
            return "???";
        }

        var price = productConfig.RealPrice;
        if (product != null)
        {
#if UNITY_EDITOR
            if (LocalizationManager.CurrentLanguage == LanguageName.Vietnam)
            {
                var cultureInfo = new CultureInfo("vi-VN");
                float exchangeRate = 25000;
                return string.Format(cultureInfo, "{0:C}", price * exchangeRate);
            }
            return price + "$";
#endif
            var result = product.metadata.localizedPriceString;
            if (!string.IsNullOrEmpty(result)) return result;
        }

        return price + "$";
    }
    private string GetOriginalPriceFromUnityIAP(IAPCategory iapCategory)
    {
        var product = GetProductInitializeByCategory(iapCategory);
        var productConfig = iapProductSO.GetIAPProductByCategory(iapCategory);
        var price = productConfig.RealPrice;
        float saleRate = productConfig.SaleRate;

        if (saleRate > 0)
        {
            price = productConfig.RealPrice * 100f / (100 - saleRate);
            price = (float)Math.Round(price, 2);
        }

        if (product != null)
        {
#if UNITY_EDITOR
            if (LocalizationManager.CurrentLanguage == LanguageName.Vietnam)
            {
                var cultureInfo = new CultureInfo("vi-VN");
                float exchangeRate = 25000;
                return string.Format(cultureInfo, "{0:C}", price * exchangeRate);
            }
            return price + "$";
#endif
            try
            {
                string localizedPriceString = product.metadata.localizedPriceString;

                // Regex tìm phần số có thể có dấu chấm, phẩy, khoảng trắng,...
                string priceFormat = Regex.Replace(localizedPriceString, @"[\d\s.,]+", "{0}");
                var salePrice = product.metadata.localizedPrice;

                float originalPrice = (float)salePrice;
                if (saleRate > 0)
                {
                    originalPrice = originalPrice * 100f / (100 - saleRate);
                    originalPrice = (float)Math.Round(originalPrice, 2);
                }

                // Format lại giá theo tiền tệ người dùng

                var result = string.Format(priceFormat, originalPrice.ToString("N", CultureInfo.CurrentCulture));
                if (!string.IsNullOrEmpty(result)) return result;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        return price + "$";
    }

    private Product GetProductInitializeByCategory(IAPCategory iapCategory)
    {
        var productConfig = iapProductSO.GetIAPProductByCategory(iapCategory);
        if (productConfig != null && InitializeProduct.TryGetValue(productConfig.ProductID, out var product))
        {
            return product;
        }

        return null;
    }

    private IAPProduct GetIAPProductById(string productId)
    {
        return iapProductSO.GetIAPProductByID(productId);
    }

    private IAPProduct GetIAPProductByCategory(IAPCategory category)
    {
        return iapProductSO.GetIAPProductByCategory(category);
    }

    public void StartInitIAPProduct()
    {
        InitializePurchasing().Forget();
    }

    public async UniTask InitializePurchasing()
    {
        if (IsStoreInitialized())
            return;

        m_StoreController = UnityIAPServices.StoreController();

        // Subscribe to purchase events
        m_StoreController.OnPurchasePending += OnPurchasePending;
        m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;

        try
        {
            // Connect to store services
            await m_StoreController.Connect();

            //Debug.Log("Unity IAP v5 initialization successful");

            // Fetch your products
            FetchProducts();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Store connection failed: {e}");
            // Your UI logic for showing initialization errors
            Debug.LogError("Failed to initialize store - check internet connection and store setup");
        }
    }

    private void FetchProducts()
    {
        m_StoreController.OnProductsFetched += OnProductsFetched;
        m_StoreController.OnPurchasesFetched += OnPurchasesFetched;

        var products = new List<ProductDefinition>();
        var iapProducts = iapProductSO.IAPProductList;
        foreach (var iapProduct in iapProducts)
        {
            if (iapProduct == null) continue;
#if UNITY_IOS
            products.Add(new ProductDefinition(iapProduct.ProductID, iapProduct.ProductID,
                iapProduct.StoreProductType));
#else
            products.Add(new ProductDefinition(iapProduct.ProductID, iapProduct.ProductID,
                ProductType.Consumable));
#endif
        }

        m_StoreController.FetchProducts(products);
        //Debug.Log("Products fetched and ready for purchase");
    }
    #endregion

    #region Purchase Event Handlers
    private void OnPurchasesFetched(Orders orders)
    {
        // Only process rewards during an explicit RestoreTransactions call.
        // OnPurchasesFetched also fires on FetchProducts() during init — skip that path.
        if (!_isRestoring) return;

        bool restoredAny = false;
        var toRestore = new List<(IAPProduct iapProduct, string receipt)>();

        foreach (var confirmedOrder in orders.ConfirmedOrders)
        {
            var product = confirmedOrder.CartOrdered?.Items().FirstOrDefault()?.Product;
            if (product == null) continue;
            if (product.definition.type != ProductType.NonConsumable) continue;

            var iapProduct = iapProductSO.GetIAPProductByProductID(product.definition.id);
            if (iapProduct == null) continue;

            var restoreTransactionId = "restore_" + product.definition.id;
            restoredAny = true;

            // Already rewarded in a previous session — skip silently.
            DataManager.PlayerData.TransactionDataList ??= new();
            if (DataManager.PlayerData.TransactionDataList.Exists(t => t.TransactionId == restoreTransactionId))
                continue;

            var orderReceipt = confirmedOrder.Info?.Receipt ?? string.Empty;

            // Mark as rewarded before showing popup to prevent duplicate on crash/re-open.
            DataManager.PlayerData.TransactionDataList.Add(new TransactionData
            {
                ProductId = iapProduct.ProductID,
                TransactionId = restoreTransactionId,
                Receipt = orderReceipt
            });
            DataManager.SavePlayerData().Forget();
            toRestore.Add((iapProduct, orderReceipt));
        }

        _isRestoring = false;

        if (GameStateManager.Instance.CurrentGameState == GameState.None) return;
        ShowRestoreRewardChain(toRestore, restoredAny);
    }

    private void ShowRestoreRewardChain(List<(IAPProduct iapProduct, string receipt)> toRestore, bool restoredAny)
    {
        if (toRestore.Count == 0)
        {
            var key = restoredAny ? "ui_notify_restoresuccess" : "ui_notify_restorefail";
            LayerManager.Instance.ShowPopupFeedbackLayer(TextUtility.GetI2(key));
            return;
        }

        // Chain reward popups: each closes into the next; last closes into feedback popup.
        Action chainedAction = () => LayerManager.Instance.ShowPopupFeedbackLayer(TextUtility.GetI2("ui_notify_restoresuccess"));

        for (int i = toRestore.Count - 1; i >= 0; i--)
        {
            var item = toRestore[i];
            var next = chainedAction;
            chainedAction = () =>
            {
                try
                {
                    GameEventBus.OnPurchaseSuccess?.Invoke(item.iapProduct);
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    Debug.LogError($"ShowRestoreRewardChain - OnPurchaseSuccess failed for {item.iapProduct.ProductID}: {e.Message}");
#endif
                }
                DataManager.ClaimReward(item.iapProduct.Rewards, next, "restore", RewardItemType.Other);
            };
        }
        chainedAction();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Right-click component in Inspector → "Simulate Restore Purchases"
    /// Clears restore transaction entries for non-consumables then runs the full reward chain.
    /// </summary>
    [ContextMenu("Simulate Restore Purchases")]
    private void SimulateRestorePurchases()
    {
        if (DataManager.PlayerData == null)
        {
            Debug.LogWarning("[SimulateRestore] PlayerData is null — run the game first.");
            return;
        }

        var nonConsumables = iapProductSO.IAPProductList
            .Where(p => p != null && p.StoreProductType == ProductType.NonConsumable)
            .ToList();

        int count = UnityEngine.Random.Range(1, Mathf.Min(4, nonConsumables.Count + 1));
        // Fisher-Yates shuffle then take first `count`
        for (int i = nonConsumables.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (nonConsumables[i], nonConsumables[j]) = (nonConsumables[j], nonConsumables[i]);
        }

        var toRestore = new List<(IAPProduct iapProduct, string receipt)>();
        foreach (var iapProduct in nonConsumables.Take(count))
        {
            var restoreTransactionId = "restore_" + iapProduct.ProductID;
            DataManager.PlayerData.TransactionDataList ??= new();
            DataManager.PlayerData.TransactionDataList.RemoveAll(t => t.TransactionId == restoreTransactionId);
            DataManager.PlayerData.TransactionDataList.Add(new TransactionData
            {
                ProductId = iapProduct.ProductID,
                TransactionId = restoreTransactionId,
                Receipt = "simulated_receipt"
            });
            toRestore.Add((iapProduct, "simulated_receipt"));
        }

        Debug.Log($"[SimulateRestore] Simulating restore for {toRestore.Count} non-consumable(s).");
        ShowRestoreRewardChain(toRestore, toRestore.Count > 0);
    }
#endif

    private void OnProductsFetched(List<Product> list)
    {
        foreach (var product in list)
        {
            InitializeProduct.TryAdd(product.definition.id, product);
            GameEventBus.OnReceivePackInfoFromStore?.Invoke(product.definition.id);
        }
    }

    private Dictionary<string, Product> InitializeProduct = new();
    private void OnPurchaseConfirmed(Order order)
    {
        switch (order)
        {
            case FailedOrder failedOrder:
                var failedProduct = failedOrder.CartOrdered.Items().First().Product.definition.id;
                OnPurchaseFailed(failedOrder.CartOrdered.Items().First().Product, failedOrder.FailureReason, failedOrder.Info?.Receipt, failedOrder.Info?.TransactionID);
                // Your UI logic for showing purchase errors
                Debug.LogError($"Purchase failed for {failedProduct}: {failedOrder.FailureReason}");
                break;

            case ConfirmedOrder confirmedOrder:
                //var confirmedProduct = order.CartOrdered.Items().First().Product;

                // Your UI logic for showing purchase success
                //Debug.Log($"Purchase successful: {confirmedProduct}");
#if UNITY_IOS
                LogApplePurchaseTransaction(confirmedOrder);
#endif
                break;
        }
    }

#if UNITY_IOS
    /// <summary>
    /// Log StoreKit 2 transaction to Firebase Analytics.
    /// For StoreKit 1 this is a no-op; for StoreKit 2 it logs the verified transaction.
    /// </summary>
    private void LogApplePurchaseTransaction(ConfirmedOrder confirmedOrder)
    {
        try
        {

            if (TrackingEvent.IsBlockTracking())
            {
                return;
            }

            var firebaseTransactionId = confirmedOrder?.Info?.Apple?.OriginalTransactionID ?? "";


            if (string.IsNullOrEmpty(firebaseTransactionId))
            {
                Debug.LogWarning(
                    "Firebase Analytics: Apple transaction ID is null or empty. Skipping logging.");
                return;
            }

            FirebaseAnalytics.LogAppleTransactionAsync(firebaseTransactionId)
                .ContinueWithOnMainThread(task =>
                {
                    try
                    {
                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Debug.LogError(
                                $"Failed to log Apple purchase transaction: {task.Exception}");
                        }
                        else
                        {
                            Debug.Log(
                                $"Firebase Analytics: Successfully logged Apple purchase transaction ({firebaseTransactionId})");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Debug.LogError(
                            $"Firebase Analytics: Exception in transaction callback: {innerEx}");
                    }
                });
        }
        catch (Exception ex)
        {
            Debug.LogError($"Firebase Analytics: Failed to log Apple purchase transaction: {ex}");
        }
    }
#endif

    private void OnPurchasePending(PendingOrder order)
    {
        //Debug.Log("Purchase pending - processing...");

        lock (m_ProcessingTransactions)
        {
            if (m_ProcessingTransactions.Contains(order.Info.TransactionID))
            {
                return;
            }
            m_ProcessingTransactions.Add(order.Info.TransactionID);
        }

        VerifyActionAsync(order).Forget();

    }

    private async UniTask VerifyActionAsync(PendingOrder order)
    {
        string transactionId = order.Info.TransactionID;
        Debug.Log($"VerifyActionAsync - Start: {transactionId}");

        try
        {
            await UniTask.SwitchToMainThread();
            Product purchasedProduct = order.CartOrdered.Items().First().Product;
            if (purchasedProduct == null) return;
            if (string.IsNullOrEmpty(purchasedProduct.definition?.id)) return;

            var iapProduct = iapProductSO.GetIAPProductByProductID(purchasedProduct.definition.id);

            if (iapProduct == null || string.IsNullOrEmpty(iapProduct.ProductID)) return;
            Debug.Log("VerifyActionAsync - 1");
            isPurchasePending = true;

            /* for Adjust verification, if you have Adjust integrated, otherwise skip this step
            var completionSource = new UniTaskCompletionSource<bool>();

            if (AdjustManager.Instance != null)
            {
                AdjustManager.Instance.VerifyPurchase(purchasedProduct, GetPurchaseToken(order.Info.Receipt), (success) =>
                {
                    completionSource.TrySetResult(success);
                });
            }
            else
            {
                completionSource.TrySetResult(true);
            }

            var verifySuccess = await completionSource.Task;
            */

#if !UNITY_EDITOR
            var reqData = new VerifyIAPRequestData()
            {
                Receipt = Newtonsoft.Json.JsonConvert.DeserializeObject<ReceiptClient>(order.Info.Receipt),
                ItemID = iapProduct.Category.GetHashCode(),
                ProductID = purchasedProduct.definition.id,
                AccountID = GameConfig.DeviceId,
                ProductType = iapProduct.StoreProductType.GetHashCode(),
#if UNITY_IOS
                JwsRepresentation = (order.Info != null && order.Info.Apple != null) ? order.Info.Apple.jwsRepresentation : ""
#endif
            };

            var verifySuccess = await DataManager.VerifyIAP(reqData);
            if (!verifySuccess.Item1)
            {
                if (verifySuccess.Item2 == 615)
                {
                    m_StoreController.ConfirmPurchase(order);
                }
                LayerManager.Instance?.ShowPopupFeedbackLayer(TextUtility.GetI2("ui_purchase_fail"));
                return;
            }
            else
            {
                // if (iapProduct.StoreProductType == ProductType.Subscription)
                // {
                //     DataManager.SaveSubscriptionData(
                //         iapProduct.ProductID,
                //         transactionId,
                //         reqData.Receipt?.store ?? string.Empty,
                //         reqData.Receipt?.payload ?? string.Empty,
                //         reqData.JwsRepresentation ?? string.Empty
                //     );
                // }

                // Reward the player BEFORE confirming purchase
                ProcessPurchaseReward(purchasedProduct, iapProduct, order);
                // Confirm the purchase (required)
                m_StoreController.ConfirmPurchase(order);

            }
#else
            // if (iapProduct.StoreProductType == ProductType.Subscription)
            // {
            //     var editorReceipt = Newtonsoft.Json.JsonConvert.DeserializeObject<ReceiptClient>(order.Info?.Receipt ?? string.Empty);
            //     DataManager.SaveSubscriptionData(
            //         iapProduct.ProductID,
            //         transactionId,
            //         editorReceipt?.store ?? string.Empty,
            //         editorReceipt?.payload ?? string.Empty,
            //         string.Empty
            //     );
            // }

            // Reward the player BEFORE confirming purchase
            var productId = order.CartOrdered.Items().First().Product.definition.id;
            ProcessPurchaseReward(order.CartOrdered.Items().First().Product, iapProduct, order);
            // Confirm the purchase (required)
            m_StoreController.ConfirmPurchase(order);
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in VerifyActionAsync: {e}");
        }
        finally
        {
            isPurchasePending = false;
            lock (m_ProcessingTransactions)
            {
                m_ProcessingTransactions.Remove(transactionId);
            }
            Debug.Log($"VerifyActionAsync - Complete: {transactionId}");
        }
    }
    #endregion

    #region Purchase Processing

    private void ProcessPurchaseReward(Product product, IAPProduct iapProduct, Order order, bool isVerified = true)
    {
        var receipt = order.Info?.Receipt;
        var transactionId = order.Info?.TransactionID;

        if (!IsUnsafeDevice() || isVerified)
        {
            if (false)
            {
                CheckShowRestorePack.CachedEvents.Add((onDone) =>
                {
                    DataManager.OnBuyPurchaseSuccess(product, iapProduct, receipt, transactionId, () =>
                    {
                        OnPurchaseComplete(iapProduct.ProductID);
                        onDone?.Invoke();
                    });
                });
            }
            else
            {
                DataManager.OnBuyPurchaseSuccess(product, iapProduct, receipt, transactionId, () => { OnPurchaseComplete(iapProduct.ProductID); });
            }
        }
        else
        {
            if (GameStateManager.Instance.CurrentGameState != GameState.None)
            {
                LayerManager.Instance?.ShowPopupFeedbackLayer(TextUtility.GetI2("ui_purchase_fail"));
            }
            Debug.LogWarning($"[PurchaseManager] Blocked reward on unsafe device because isVerified is false. Product: {iapProduct.ProductID}");
        }

        if (isVerified)
        {
            TrackingPurchase(product, iapProduct.RealPrice, order);
            GameEventBus.OnPurchaseSuccess?.Invoke(iapProduct);
            GameEventBus.OnVerifiedIAPPurchased?.Invoke(iapProduct, transactionId);
        }
    }

    private bool IsUnsafeDevice()
    {
        if (_isUnsafeDeviceCache.HasValue) return _isUnsafeDeviceCache.Value;

        bool isUnsafe = false;
#if UNITY_ANDROID && !UNITY_EDITOR
        isUnsafe = UserBlocker.IsAndroidRooted() || !UserBlocker.StoreInstallerChecker();
#elif (UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
        isUnsafe = JailbreakDetector.IsJailBroken();
#endif
        _isUnsafeDeviceCache = isUnsafe;
        return isUnsafe;
    }

    protected void TrackingPurchase(Product product, double realPrice, Order order)
    {
        var transactionID = order.Info?.TransactionID;
        var receipt = order.Info?.Receipt;
        var price = Convert.ToDouble(product.metadata.localizedPrice);
        var priceName = product.metadata.isoCurrencyCode;
        var productID = product.definition.id;
        Debug.Log("receiptClient:{Store: " +
#if UNITY_ANDROID
                  "\"Google Play\""
#else
                  "\"Appstore\""
#endif
                  + ",\n"
                  + "TransactionID: \"" + transactionID + "\",\n"
                  + "Payload : \"" + receipt + "\"\n"
                  + "}");

        UserBehaviorTracker.SendIAPSuccessTracking(new IAPTrackingData()
        {
            Receipt = receipt,
            TransactionId = transactionID,
            LocalizedPrice = price,
            RealPrice = realPrice,
            RegionPrice = priceName,
            ProductId = productID,
            PurchaseToken = GetPurchaseToken(receipt)
        });
    }

    string GetPurchaseToken(string receipt)
    {
#if UNITY_EDITOR
        // Trả về mã giả lập khi test trên Editor để tránh lỗi đỏ
        return "GPA.editor-fake-order-id";
#elif UNITY_ANDROID
        try
        {
            if (string.IsNullOrEmpty(receipt))
                return null;

            // Parse the receipt
            var receiptRoot = JsonConvert.DeserializeObject<JObject>(receipt);
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
                payloadJson = receipt;
            }

            // Use the existing ReceiptAndroid class to parse the payload
            var receiptAndroid = new ReceiptAndroid(payloadJson);

            // Return the orderId from the parsed data
            return receiptAndroid?.JsonData?.purchaseToken;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to extract orderId from receipt: {ex.Message}");
            return null;
        }
#else
        return "";
#endif
    }

    private void OnPurchaseComplete(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return;
        if (ActionRewardIAPComplete.TryGetValue(productId, out var action))
        {
            action?.Invoke();
        }
    }

    private Dictionary<string, Action> ActionRewardIAPComplete = new();

    public void RegisterActionRewardIAPComplete(string productID, Action action)
    {
        if (string.IsNullOrEmpty(productID) || action == null) return;
        if (!ActionRewardIAPComplete.TryAdd(productID, action))
        {
            ActionRewardIAPComplete[productID] += action;
        }
    }

    public void UnRegisterActionRewardIAPComplete(string productID, Action action)
    {
        if (string.IsNullOrEmpty(productID) || action == null) return;

        if (!ActionRewardIAPComplete.TryGetValue(productID, out var existingAction))
        {
            return;
        }

        existingAction -= action;
        if (existingAction == null)
        {
            ActionRewardIAPComplete.Remove(productID);
        }
        else
        {
            ActionRewardIAPComplete[productID] = existingAction;
        }
    }

    private void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason, string receipt, string transactionId)
    {
        if (this == null) return;

        if (failureReason != PurchaseFailureReason.UserCancelled && failureReason != PurchaseFailureReason.Unknown)
        {
            //todo: show popup notify fail purchase

            //var i2 = UITextContent.GetI2(failureReason.ToString());
            //Debug.LogError("PurchaseManager - failureReason :" + i2);
        }

        var price = Convert.ToDouble(product.metadata.localizedPrice);
        var priceName = product.metadata.isoCurrencyCode;
        var productID = product.definition.id;
        var iapProduct = iapProductSO.GetIAPProductByProductID(product.definition.id);
        UserBehaviorTracker.SendIAPFailTracking(new IAPTrackingData()
        {
            Receipt = receipt,
            TransactionId = transactionId,
            LocalizedPrice = price,
            RealPrice = iapProduct.RealPrice,
            RegionPrice = priceName,
            ProductId = productID,
        }, failureReason.ToString());
    }

    #endregion

    #region Public Purchase Methods
    public void BuyProduct(string productId)
    {
        if (GameConfig.NoInternet())
        {
            LayerManager.Instance.ShowPopupNoInternet();
            return;
        }

        if (!IsStoreInitialized())
        {
            //Debug.LogError("Store controller not initialized");
            return;
        }

        if (isPurchasePending)
        {
            //SocketO.ShowPopupOk.Publish(new(PopupContent.AlreadyBought, PopupOkAction.AlreadyPurchase));
            return;
        }

        var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == productId);

        if (product != null)
        {
            //Debug.Log($"Initiating purchase: {productId}");

#if DATABUCKETS_SDK
            if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
            {
                DatabucketsManager.Instance.TrackIapClick(new IapClickInfo
                {
                    Placement = "shop",
                    ShowType = "ingame",
                    TriggerType = "user_initiated",
                    PackName = productId
                });
            }
#endif

            m_StoreController.PurchaseProduct(product);
        }
        else
        {
            //Debug.LogError($"Product not found: {productId}");
        }
    }

    public void RestorePurchases()
    {
        if (!IsStoreInitialized()) return;
        _isRestoring = true;
        m_StoreController.RestoreTransactions(OnTransactionsRestored);
    }

    private void OnTransactionsRestored(bool success, string error)
    {
        // Success path: reward chain is handled in OnPurchasesFetched via _isRestoring flag.
        // Only handle explicit failure here (e.g. network error, store unavailable).
        if (!success)
        {
            _isRestoring = false;
            //Debug.LogError($"Transaction restoration failed: {error}");
            LayerManager.Instance.ShowPopupFeedbackLayer(TextUtility.GetI2("ui_notify_restorefail"));
        }
    }

    #endregion

    #region Helper Methods

    public bool IsStoreInitialized()
    {
        return m_StoreController != null;
    }

    public string GetProductPrice(string productId)
    {
        if (!IsStoreInitialized()) return "N/A";

        var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
        return product?.metadata.localizedPriceString ?? "N/A";
    }

    public bool IsProductOwned(string productId)
    {
        // Fallback: try to get controller from Unity IAP if proxy wasn't initialized properly
        if (!IsStoreInitialized())
        {
            return false;
        }

        var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
        return product != null && product.hasReceipt;

    }

    #endregion
}
