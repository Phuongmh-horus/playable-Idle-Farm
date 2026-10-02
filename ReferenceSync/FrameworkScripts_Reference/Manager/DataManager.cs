using AdMod;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public partial class DataManager
{
    /// <summary>
    /// Gets the pack appearance data. Loads it if not already loaded.
    /// </summary>
    public static readonly string IOSAppId = ""; // Điền App ID của bạn từ App Store Connect
    
    public static PlayerData PlayerData;
    public static bool ExistPlayerData;
    public static long SessionPreviousLogoutUnixSeconds;
    public static string InitDataLocalKey = "InitDataLocalKey_FN";
    public static string RemoveAdsKey = "RemoveAdsKey";
    public static string ConsumeHeartForLastKey = "ConsumeHeartForLastKey";
    public static string CheckVersion = "CheckVersion";
    public static string LoseStreakKey = "LoseStreakKey";
    public static PackAppearanceSaveData PackAppearanceData => PlayerData?.PackAppearanceData;

    #region Init Data

    /// <summary>
    /// ⚠️ CẢNH BÁO: hàm này xoá toàn bộ PlayerPrefs và persistentDataPath mỗi khi đổi version.
    /// Bật nó lên trong bản release đồng nghĩa mọi user mất sạch tiến trình sau mỗi lần update.
    /// Chỉ dùng cho build test nội bộ, KHÔNG gọi từ <see cref="InitData"/>.
    /// </summary>
    public static void CheckVersionApp()
    {
        var lastVersion = PlayerPrefs.GetString(CheckVersion, "");
        if(!lastVersion.IsNullOrEmpty() && Application.version == lastVersion) return;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetString(CheckVersion, Application.version);
        PlayerPrefs.Save();
        LocalDatabaseManager.ResetAllData();
    }
    public static void InitData()
    {
        //CheckVersionApp();
        
        InitPlayerDataLocal();
        InitGameData();
    }

    /// <summary>
    /// True khi máy đã từng có save nhưng lần khởi động này đọc không được.
    /// Khi cờ này bật, mọi đường ghi đè (local lẫn server) đều bị chặn để không
    /// biến một lỗi đọc tạm thời thành mất data vĩnh viễn.
    /// </summary>
    public static bool LocalLoadFailed { get; private set; }

    private static void InitPlayerDataLocal()
    {
        PlayerData = LocalDatabaseManager.LoadPlayerData();
        SessionPreviousLogoutUnixSeconds = PlayerData != null ? PlayerData.LastLogoutUnixSeconds : 0;
        LocalLoadFailed = LocalDatabaseManager.LastLoadStatus == LoadDataStatus.Failed;

        if (LocalLoadFailed)
        {
            // Giữ nguyên mọi file trên đĩa: có thể lần mở sau đọc lại được (storage đầy,
            // file đang bị khoá, sandbox chưa mount xong...). Chỉ dựng data tạm trong RAM.
            Debug.LogError("[DataManager] Local save unreadable - keeping files intact, will restore from server if available.");
        }
        else if (PlayerData == null && PlayerPrefs.HasKey(InitDataLocalKey))
        {
            // Không còn file nào trên đĩa, nhưng InitDataLocalKey tồn tại nghĩa là máy này
            // ĐÃ TỪNG có account (bao gồm user dính bug xoá data ở các bản trước).
            // Xoá đúng một key này để GetPlayerData được phép chạy và kéo data về từ server,
            // thay vì PlayerPrefs.DeleteAll() — DeleteAll làm mất luôn device_id và session link,
            // khiến chính lệnh tra cứu server đó trả 404.
            Debug.LogWarning("[DataManager] Local save missing but account marker exists - allowing server restore.");
            SaveDiagnostics.Report(SaveDiagnostics.StageLoadMissingMarker,
                storage: LocalDatabaseManager.GetStorageSnapshot());
            PlayerPrefs.DeleteKey(InitDataLocalKey);
            PlayerPrefs.Save();
            LocalLoadFailed = true;
        }

        // Tính SAU khối if/else: nhánh "mất file nhưng còn marker" có bật LocalLoadFailed,
        // nếu tính trước sẽ sinh tổ hợp ExistPlayerData=false + LocalLoadFailed=true.
        // Tổ hợp đó khiến TryLoginAccount rơi vào nhánh "không có data local" và bỏ qua
        // toàn bộ nhánh xử lý LocalLoadFailed phía dưới.
        //
        // ExistPlayerData chỉ được false khi máy CHƯA TỪNG có data. Trước đây nó cũng false
        // khi đọc lỗi, và nhánh xử lý sẽ xoá sạch persistentDataPath (gồm cả .bak/.tmp)
        // + PlayerPrefs -> user level 100 tụt về level 1 không cứu được.
        ExistPlayerData = PlayerData != null || LocalLoadFailed;

        PlayerData ??= LocalDatabaseManager.CreateNewAcc();
        ValidatePlayerData();
    }
    private static void InitGameData()
    {
        GameSettingData.InitData();
    }

    #endregion

    #region Validate Data

    /*private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && IsPlaying && MeshCountClick > 0)
        {
            NoteConsumeHeart();
        }
    }*/

    public static void NoteConsumeHeart()
    {
        var currentTime = DateTime.UtcNow;
        if(PlayerData.CurrentHeart >= 5) PlayerPrefs.SetString(HeartSystem.LastConsumeHeartKey, currentTime.ToDefaultString());
        PlayerPrefs.SetInt(ConsumeHeartForLastKey, 1);
        PlayerPrefs.Save();
    }
    
    private static void ValidateConsumeHeartForLast()
    {
        if (PlayerPrefs.GetInt(ConsumeHeartForLastKey,0) > 0)
        {
            Debug.Log("ValidatePlayerData ConsumeHeart");
            var newHeart = PlayerData.CurrentHeart - 1;
            PlayerData.CurrentHeart = Mathf.Clamp(newHeart, 0, PlayerData.MaxHeart);
            Debug.Log($"ValidatePlayerData CurrentHeart: {PlayerData.CurrentHeart}");
            PlayerPrefs.DeleteKey(ConsumeHeartForLastKey);
        }
    }
    private static void ValidateLastLogin()
    {
        if (PlayerData == null) return;

        var lastLogin = PlayerData.LastLoginTimeString.ToDefaultDateTime();
        if (lastLogin.Day != DateTime.UtcNow.Day || lastLogin.Month != DateTime.UtcNow.Month ||
            lastLogin.Year != DateTime.UtcNow.Year)
        {
            PlayerData.LastLoginTimeString = DateTime.UtcNow.ToDefaultString();
            if (DateTime.UtcNow >= lastLogin)
            {
                PlayerData.DayPlayCount += 1;
                GameEventBus.OnDailyLoginEvent?.Invoke(1);
            }
        }
    }

    public static Action OnValidatePlayerData;
    private static void ValidatePlayerData()
    {
        PlayerData.TransactionDataList ??= new();
        PlayerData.TutorialDataList ??= new();
        PlayerData.AvatarOwnedList ??= new();
        PlayerData.FrameOwnedList ??= new();
        PlayerData.PackAppearanceData ??= new();
        PlayerData.NewAvatarOwnedList  ??= new();
        PlayerData.NewFrameOwnedList ??= new();
        PlayerData.BoosterData ??= new();
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.plots ??= new();
        PlayerData.GardenData.sprinkler ??= new();
        PlayerData.GardenData.harvester ??= new();
        PlayerData.GardenData.transport ??= new();
        PlayerData.GardenData.specialPlots ??= new();
        PlayerData.GardenData.specialSprinkler ??= new();
        PlayerData.GardenData.specialHarvester ??= new();
        PlayerData.CollectionData ??= new();
        PlayerData.GardenData.mainQuest ??= new();
        PlayerData.GardenData.mainQuest.completedQuestIds ??= new();
        PlayerData.DailyWeeklyQuestData ??= new();
        PlayerData.DailyWeeklyQuestData.daily ??= new();
        PlayerData.DailyWeeklyQuestData.weekly ??= new();
        PlayerData.DailyWeeklyQuestData.daily.taskProgress ??= new();
        PlayerData.DailyWeeklyQuestData.weekly.taskProgress ??= new();
        PlayerData.DailyWeeklyQuestData.daily.claimedMilestones ??= new();
        PlayerData.DailyWeeklyQuestData.weekly.claimedMilestones ??= new();

        SyncCollectionFromPlots();
        ValidateConsumeHeartForLast();
        ActivePackStarter();
        ValidateLastLogin();
        OnValidatePlayerData?.Invoke();
    }
    private static void ActivePackStarter()
    {
        if (GetPackAppearanceDataInfo(IAPCategory.StarterPack) == null)
        {
            SetPackCooldownStartTime(IAPCategory.StarterPack);
        }
    }
    public static void ValidateMaxHeart(int maxHeart)
    {
        if (PlayerData.MaxHeart >= maxHeart)
        {
            return;
        }
        Debug.Log($"ValidateMaxHeart {maxHeart}");

        PlayerData.MaxHeart = maxHeart;
        PlayerData.CurrentHeart = maxHeart;
    }

    #endregion

    #region IAP

    public static bool HadRemoveImperativeAds()
    {
        return PlayerPrefs.GetInt(RemoveAdsKey, 0) == 1 || PlayerData?.RemoveImperativeAds == true;
    }
    public static bool HasPurchaseAll(HashSet<string> productID)
    {
        if (productID == null || productID.Count == 0)
            return true;

        return ProductIDPurchased.IsSupersetOf(productID);
    }
    public static bool HasPurchaseAny(HashSet<string> productID)
    {
        if (productID == null || productID.Count == 0)
            return false;

        return ProductIDPurchased.Overlaps(productID);
    }

    protected static HashSet<string> _productIDPurchased;
    public static HashSet<string> ProductIDPurchased
    {
        get
        {
            if (_productIDPurchased == null ||
                PlayerData?.TransactionDataList == null ||
                _productIDPurchased?.Count != PlayerData?.TransactionDataList?.Count)
            {
                _productIDPurchased = new HashSet<string>();
                if (PlayerData?.TransactionDataList != null)
                {
                    // Lấy toàn bộ giao dịch thực tế
                    HashSet<string> rawPurchasedIds = new HashSet<string>();
                    foreach (var transaction in PlayerData.TransactionDataList)
                    {
                        rawPurchasedIds.Add(transaction.ProductId);
                    }

                    // Đồng bộ hóa: Nếu mua gói gốc thì tính là mua gói sale và ngược lại
                    var allProducts = GameEventBus.GetAllIAPProducts?.Invoke();
                    if (allProducts != null)
                    {
                        foreach (var product in allProducts)
                        {
                            if (product == null) continue;
                            
                            _productIDPurchased.Add(product.ProductID); // Tạm thêm để kiểm tra, sẽ xóa nếu chưa mua

                            bool isBaseOwned = rawPurchasedIds.Contains(product.ProductID);
                            bool isSaleOwned = false;

                            IAPProduct saleProd = null;
                            if (product.SaleCategory != IAPCategory.None)
                            {
                                saleProd = allProducts.Find(x => x.Category == product.SaleCategory);
                                if (saleProd != null)
                                {
                                    isSaleOwned = rawPurchasedIds.Contains(saleProd.ProductID);
                                }
                            }

                            if (isBaseOwned || isSaleOwned)
                            {
                                _productIDPurchased.Add(product.ProductID);
                                if (saleProd != null) _productIDPurchased.Add(saleProd.ProductID);
                            }
                            else
                            {
                                _productIDPurchased.Remove(product.ProductID);
                            }
                        }
                    }
                    else
                    {
                        // Fallback nếu chưa có dữ liệu cấu hình
                        foreach (var id in rawPurchasedIds) _productIDPurchased.Add(id);
                    }
                }
            }
            return _productIDPurchased;
        }
    }

    public static List<string> PackNotPurchased(List<string> productID)
    {
        var transactions = PlayerData.TransactionDataList;
        if (transactions == null || transactions.Count == 0)
            return productID; // nếu chưa có giao dịch nào, coi như chưa mua gì

        // Tạo danh sách kết quả chứa các sản phẩm chưa mua
        var notPurchased = new List<string>();
        for (int i = 0; i < productID.Count; i++)
        {
            var id = productID[i];
            if (!ProductIDPurchased.Contains(id))
            {
                notPurchased.Add(id);
            }
        }

        return notPurchased;
    }

    public static void OnBuyPurchaseSuccess(Product product, IAPProduct iapProduct, string receipt, string transactionId, Action onDone = null)
    {
        if (PlayerData == null || iapProduct == null || product == null || product.definition == null)
        {
            Debug.LogError($"OnBuyPurchaseSuccess failed: PlayerData is null: {PlayerData == null}, iapProduct is null: {iapProduct == null}, product is null: {product == null}");
            return;
        }
        PlayerData.TransactionDataList ??= new ();
        PlayerData.TransactionDataList.Add(new TransactionData()
        {
            ProductId = product.definition.id,
            Receipt = receipt,
            TransactionId = transactionId,
        });
        
        Debug.Log($"OnBuyPurchaseSuccess {iapProduct.ProductID}");

        if (FirebaseManager.Instance.GetWeekendSaleTypeValue() == 2 && FirebaseManager.Instance.IsWeekendSaleActive())
        {
            // Kiểm tra nếu Pack vừa mua là gói Sale (Dựa vào SaleRate > 0)
            if (iapProduct.SaleRate > 0)
            {
                PlayerData.LastWeekBoughtWeekendSale = FirebaseManager.Instance.GetCurrentWeekId();
                Debug.Log($"[WeekendSale] Đã mua gói Sale {iapProduct.ProductID}, kết thúc đợt sale tuần này.");
            }
        }

        if (iapProduct.Rewards is not { Count: > 0 }) return;
        var isCashPack = iapProduct.Category.ToString().Contains("Cash");
        var type = isCashPack ? RewardItemType.CashPack : RewardItemType.BundlePack;
        ClaimReward(iapProduct.Rewards, onDone, "purchase_iap", type);
    }
    
    #endregion

    #region Heart

    public static void ChangeHeart(int heartAmount, bool isSave = true)
    {
        if (PlayerData == null) return;

        Debug.Log($"ChangeHeart {heartAmount}");
        var newHeart = PlayerData.CurrentHeart + heartAmount;
        PlayerData.CurrentHeart = Mathf.Clamp(newHeart, 0, PlayerData.MaxHeart);
        try
        {
            GameEventBus.OnHeartDataChange?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        if (isSave) SavePlayerData().Forget();
    }

    public static void ChangeInfiniteHeartTime(DateTime infiniteHeartTime, bool isSave = true)
    {
        Debug.Log($"ChangeInfiniteHeartTime {infiniteHeartTime}");
        PlayerData.InfiniteHeartTimeString = infiniteHeartTime.ToDefaultString();
        if (isSave) SavePlayerData().Forget();
    }
    public static void ValidateHeartRecover(int heartRecover, bool isSave = false)
    {
        if (heartRecover <= 0) return;
        ChangeHeart(heartRecover, isSave);
    }
    #endregion

    #region Offer Pack

    public static PackAppearanceData GetPackAppearanceDataInfo(IAPCategory category)
    {
        try
        {
            return PackAppearanceData.Data.TryGetValue(category.ToString(T_Utility.EN_Culture), out var data) ? data : null;
        }
        catch (Exception e)
        {
            Debug.LogError($"GetPackAppearanceDataInfo error: {e}");
        }

        return null;
    }

    /// <summary>
    /// Tracks when a pack is displayed and updates its appearance data
    /// </summary>
    /// <param name="productId">The ID of the pack being displayed</param>
    public static void TrackPackAppearance(IAPCategory iapCategory)
    {
        try
        {
            var category = iapCategory.ToString(T_Utility.EN_Culture);
            if (string.IsNullOrEmpty(category)) return;

            if (!PackAppearanceData.Data.TryGetValue(category, out var packData))
            {
                packData = new PackAppearanceData(category);
                PackAppearanceData.Data[category] = packData;
            }

            packData.DisplayCount++;
            packData.LastDisplayTime = DateTime.UtcNow.ToDefaultString();
            SavePlayerData().Forget();
        }
        catch (Exception e)
        {
            Debug.LogError($"TrackPackAppearance error: {e}");
        }
    }
    /// <summary>
    /// Thiết lập thời gian bắt đầu cooldown cho một pack
    /// </summary>
    /// <param name="productId">ID của pack cần thiết lập cooldown</param>
    /// <param name="startTime">Thời điểm bắt đầu cooldown (mặc định là thời điểm hiện tại)</param>
    public static void SetPackCooldownStartTime(IAPCategory iapCategory, DateTime? startTime = null)
    {
        var category = iapCategory.ToString(T_Utility.EN_Culture);
        if (string.IsNullOrEmpty(category))
        {
            Debug.LogWarning("[SetPackCooldownStartTime] ProductId không được để trống");
            return;
        }

        try
        {
            var packData = PackAppearanceData.Data.GetValueOrDefault(category);
            packData ??= new PackAppearanceData(category);
            PackAppearanceData.Data[category] = packData;
            Debug.Log($"[SetPackCooldownStartTime] Đã tạo mới dữ liệu cho pack: {category}");

            // Thiết lập thời gian cooldown
            var cooldownTime = startTime ?? DateTime.UtcNow;
            packData.CooldownStartTime = cooldownTime.ToDefaultString();
            Debug.Log(
                $"[SetPackCooldownStartTime] Đã thiết lập cooldown cho pack {category} bắt đầu từ {cooldownTime}");

            // Lưu dữ liệu
            SavePlayerData().Forget();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SetPackCooldownStartTime] Lỗi khi thiết lập cooldown cho pack {category}: {e}");
        }
    }
    public static void SetIgnoreLastPackAll()
    {
        if (PackAppearanceData == null || PackAppearanceData.Data == null) return;
        Debug.Log("DataManager SetIgnoreLastPackAll");
        foreach (var packAppearanceData in PackAppearanceData.Data.Values)
        {
            packAppearanceData.IsIgnoredLastTime = true;
        }
        SavePlayerData().Forget();
    }
    public static void SetIgnoreLastPack(IAPCategory category)
    {
        var packAppearanceData = GetPackAppearanceDataInfo(category);
        packAppearanceData.IsIgnoredLastTime = true;
        GameEventBus.IgnoreLastPack?.Invoke();
        SavePlayerData().Forget();
    }
    #endregion

    #region Reward

    public static void ClaimReward(List<RewardItem> rewards, Action onDone = null, string source = null, RewardItemType rewardItemType = RewardItemType.Other)
    {
        Action claimAction = OnClaimReward(rewards, source) + onDone;
        LayerManager
            .Instance
            .ShowPopupRewardLayer(new()
            {
                RewardItems = rewards,
                CloseAction = claimAction,
                RewardItemType = rewardItemType
            });
    }

    public static Action OnClaimReward(List<RewardItem> rewards, string source = null, string productId = null, string placement = null)
    {
        Action result = null;
        foreach (var reward in rewards)
        {
            switch (reward.RewardType)
            {
                case RewardType.Cash:
                    var cashBefore = PlayerData.Cash;
                    ChangeCash(reward.Quantity, false, false);
                    result += () =>
                    {
                        GameEventBus.OnCashChange?.Invoke();
                    };

                    UserBehaviorTracker.SendEarnVirtualCurrencyTracking("Cash", reward.Quantity, cashBefore,
                        PlayerData.Cash, source);
                    break;

                case RewardType.Gem:
                    var gemBefore = PlayerData.Gem;
                    ChangeGem(reward.Quantity, false, false);
                    result += () =>
                    {
                        GameEventBus.OnGemChange?.Invoke();
                    };

                    UserBehaviorTracker.SendEarnVirtualCurrencyTracking("Gem", reward.Quantity, gemBefore,
                        PlayerData.Gem, source);
                    break;

                case RewardType.AdTicket:
                    var ticketBefore = PlayerData.AdTicket;
                    ChangeAdTicket((int)reward.Quantity, false, false);
                    result += () =>
                    {
                        GameEventBus.OnAdTicketChange?.Invoke();
                    };

                    UserBehaviorTracker.SendEarnVirtualCurrencyTracking("AdTicket", reward.Quantity, ticketBefore,
                        PlayerData.AdTicket, source);
                    break;

                case RewardType.RemoveAds:
                    PlayerData.RemoveImperativeAds = true;
                    PlayerPrefs.SetInt(RemoveAdsKey, 1);
                    AdmobManager.Instance.HideBannerAd();
                    break;

                case RewardType.InfiniteHeart:
                    HeartSystem.Instance.BuyHeartInfinite((int)reward.Quantity, false);
                    result += () => { HeartSystem.Instance.UpdateCoolDownHeart(); };
                    break;
                case RewardType.Frame:
                    PlayerData.NewFrameOwnedList.Add(reward.ItemId);
                    PlayerData.FrameOwnedList.Add(reward.ItemId);
                    break;
                case RewardType.Avatar:
                    PlayerData.AvatarOwnedList.Add(reward.ItemId);
                    PlayerData.NewAvatarOwnedList.Add(reward.ItemId);
                    break;
                case RewardType.None:
                default:
                    Debug.LogWarning($"RewardType {reward.RewardType} not support");
                    break;
            }
        }

        SavePlayerData().Forget();
        return result;
    }

    public static void ChangeCash(double amount, bool activeEvent = true, bool isSave = true)
    {
        Debug.Log($"ChangeCash {amount}");
        PlayerData.Cash = Math.Max(PlayerData.Cash + amount, 0.0);
        if (activeEvent)
        {
            GameEventBus.OnCashChange?.Invoke();
        }

#if DATABUCKETS_SDK
        if (DatabucketsManager.Instance != null && DatabucketsManager.Instance.IsInitialized)
        {
            DatabucketsManager.Instance.SetBalanceCoin(PlayerData.Cash);
        }
#endif

        if (isSave) SavePlayerData().Forget();
    }

    public static void ChangeGem(double amount, bool activeEvent = true, bool isSave = true)
    {
        Debug.Log($"ChangeGem {amount}");
        PlayerData.Gem = Math.Max(PlayerData.Gem + amount, 0.0);
        if (activeEvent)
        {
            GameEventBus.OnGemChange?.Invoke();
        }

        if (isSave) SavePlayerData().Forget();
    }

    public static void ChangeAdTicket(int amount, bool activeEvent = true, bool isSave = true)
    {
        if (PlayerData == null) return;
        Debug.Log($"ChangeAdTicket {amount}");
        PlayerData.AdTicket = Math.Max(PlayerData.AdTicket + amount, 0);
        if (activeEvent)
        {
            GameEventBus.OnAdTicketChange?.Invoke();
        }

        if (isSave) SavePlayerData().Forget();
    }

    /// <summary>
    /// Đánh dấu người chơi đã rate → chặn không hiện popup rating lại nữa.
    /// </summary>
    public static void MarkRated()
    {
        if (PlayerData == null) return;
        PlayerData.RateUsValue = 1;
        SavePlayerData().Forget();
    }
    
    public static void ChangeBooster(RewardType type, int amount, bool activeEvent = true, bool isSave = true)
    {
        Debug.Log($"ChangeBooster {type.ToString()}: {amount}");
        PlayerData.BoosterData ??= new BoosterData();
        switch (type)
        {
            case RewardType.SampleBooster:
                PlayerData.BoosterData.SampleBooster = Mathf.Max(PlayerData.BoosterData.SampleBooster + amount, 0);
                break;
        }

#if DATABUCKETS_SDK
        UpdateDatabucketsBoosterBalance(type);
#endif

        if (activeEvent)
        {
            GameEventBus.OnBoosterDataChanged?.Invoke(type);
        }

        if (isSave) SavePlayerData().Forget();
    }

#if DATABUCKETS_SDK
    private static void UpdateDatabucketsBoosterBalance(RewardType type)
    {
        if (DatabucketsManager.Instance == null || PlayerData?.BoosterData == null) return;

        string boosterName = type switch
        {
            RewardType.SampleBooster => "sample",
            _ => null
        };

        if (boosterName == null) return;

        int balance = type switch
        {
            RewardType.SampleBooster => PlayerData.BoosterData.SampleBooster,
            _ => 0
        };

        DatabucketsManager.Instance.SetBalanceBooster(boosterName, balance);
    }
#endif
    
    public static void UpdateClaimWinFree(bool isSave = true)
    {
        PlayerData.ClaimWinFree++;
        if(isSave) SavePlayerData().Forget();
    }
    
    #endregion

    #region Profile

    public static void SelectAvatar(int avatarId)
    {
        if (PlayerData.NewAvatarOwnedList.Remove(avatarId))
        {
            SavePlayerData().Forget();
        }
    }

    public static void SelectFrame(int frameId)
    {
        if (PlayerData.NewFrameOwnedList.Remove(frameId))
        {
            SavePlayerData().Forget();
        }
    }

    public static void PurchaseAvatar(int id)
    {
        PlayerData.AvatarOwnedList ??= new();
        PlayerData.AvatarOwnedList.Add(id);
        PlayerData.AvatarId = id;
        GameEventBus.OnAvatarChange?.Invoke();
        SavePlayerData().Forget();
    }

    public static void EquipAvatar(int id)
    {
        PlayerData.AvatarId = id;
        GameEventBus.OnAvatarChange?.Invoke();
        SavePlayerData().Forget();
    }

    public static void PurchaseFrame(int id)
    {
        PlayerData.FrameOwnedList ??= new();
        PlayerData.FrameOwnedList.Add(id);
        PlayerData.FrameId = id;
        GameEventBus.OnFrameChange?.Invoke();
        SavePlayerData().Forget();
    }
    public static void EquipFrame(int id)
    {
        PlayerData.FrameId = id;
        GameEventBus.OnFrameChange?.Invoke();
        SavePlayerData().Forget();
    }

    public static void ChangeName(string name)
    {
        Debug.Log($"ChangeName {name}");
        PlayerData.NickName = name;
        GameEventBus.OnChangeNameSuccess?.Invoke();
        SavePlayerData().Forget();
    }
    #endregion

    #region PlayerInfo

    public static void ChangeLevel(int levelUp, bool isSave = true)
    {
        Debug.Log($"ChangeLevel {levelUp}");
        
        if (ConfigHolder.Instance)
        {
            var levelLoop = PlayerData.LevelProgress + levelUp;
            var levelStartLoop = FirebaseManager.Instance.GetLevelStartLoopCampaignValue();
            var levelEndLoop = ConfigHolder.Instance.LevelConfigSO.LevelConfigDataList.Count;
            if (PlayerData.LevelProgress < PlayerData.LevelDisplay && levelLoop < levelStartLoop)
            {
                PlayerData.LevelProgress = levelEndLoop - (levelStartLoop - levelLoop - 1);
            }

            else if (levelLoop > levelEndLoop)
            {
                PlayerData.LevelProgress = levelStartLoop + (levelLoop - levelEndLoop - 1);
            }
            else
            {
                PlayerData.LevelProgress = levelLoop;
            }
        }
        PlayerData.LevelDisplay += levelUp;
        if(isSave) SavePlayerData().Forget();
    }

    #endregion

    #region Farm & Garden Data

    public static bool HasEnoughCash(double amount)
    {
        return PlayerData != null && PlayerData.Cash >= amount;
    }

    public static bool ConsumeCash(double amount, bool activeEvent = true, bool isSave = true)
    {
        if (PlayerData == null || PlayerData.Cash < amount) return false;
        ChangeCash(-amount, activeEvent, isSave);
        return true;
    }

    public static bool HasEnoughGem(double amount)
    {
        return PlayerData != null && PlayerData.Gem >= amount;
    }

    public static bool ConsumeGem(double amount, bool activeEvent = true, bool isSave = true)
    {
        if (PlayerData == null || PlayerData.Gem < amount) return false;
        ChangeGem(-amount, activeEvent, isSave);
        return true;
    }

    public static bool HasEnoughAdTicket(int amount)
    {
        return PlayerData != null && PlayerData.AdTicket >= amount;
    }

    public static bool ConsumeAdTicket(int amount, bool activeEvent = true, bool isSave = true, string source = "")
    {
        if (PlayerData == null || PlayerData.AdTicket < amount) return false;
        var ticketBefore = PlayerData.AdTicket;
        ChangeAdTicket(-amount, activeEvent, isSave);
        if (!string.IsNullOrEmpty(source))
        {
            UserBehaviorTracker.SendSpendVirtualCurrencyTracking("AdTicket", amount, ticketBefore, PlayerData.AdTicket, source);
        }
        return true;
    }

    public static PlotSaveData GetPlotSaveData(int plotId)
    {
        if (PlayerData?.GardenData == null) return null;
        PlayerData.GardenData.plots ??= new();
        return PlayerData.GardenData.plots.Find(p => p.plotId == plotId);
    }

    public static PlotSaveData GetOrCreatePlotSaveData(int plotId, PlotState defaultState = PlotState.Locked, int defaultPlantId = -1, int defaultLevel = 1)
    {
        if (PlayerData == null) 
            return new PlotSaveData { plotId = plotId, plantedPlantId = defaultPlantId, state = defaultState, currentLevel = defaultLevel };

        PlayerData.GardenData ??= new();
        PlayerData.GardenData.plots ??= new();

        var plot = PlayerData.GardenData.plots.Find(p => p.plotId == plotId);
        if (plot == null)
        {
            plot = new PlotSaveData
            {
                plotId = plotId,
                state = defaultState,
                currentLevel = defaultLevel,
                plantedPlantId = defaultPlantId
            };
            PlayerData.GardenData.plots.Add(plot);
            SavePlayerData().Forget();
        }
        return plot;
    }

    public static void UpdatePlotData(int plotId, PlotState state, int plantId, int level)
    {
        if (PlayerData == null) return;
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.plots ??= new();

        var plot = PlayerData.GardenData.plots.Find(p => p.plotId == plotId);
        if (plot == null)
        {
            plot = new PlotSaveData
            {
                plotId = plotId
            };
            PlayerData.GardenData.plots.Add(plot);
        }

        plot.state = state;
        plot.plantedPlantId = plantId;
        plot.currentLevel = level;

        if (state == PlotState.Planted && plantId >= 0 && level > 0)
        {
            UnlockCollectionPlant(plantId, level);
        }

        SavePlayerData().Forget();
    }

    public static bool IsSectorMerged(int sectorIndex)
    {
        if (PlayerData?.GardenData?.mergedSectors == null) return false;
        return PlayerData.GardenData.mergedSectors.Contains(sectorIndex);
    }

    public static void SetSectorMerged(int sectorIndex)
    {
        if (PlayerData == null) return;
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.mergedSectors ??= new();
        if (!PlayerData.GardenData.mergedSectors.Contains(sectorIndex))
        {
            PlayerData.GardenData.mergedSectors.Add(sectorIndex);
            SavePlayerData().Forget();
        }
    }

    public static List<int> GetMergedSectors()
    {
        if (PlayerData?.GardenData?.mergedSectors == null) return new List<int>();
        return PlayerData.GardenData.mergedSectors;
    }

    public static SprinklerSaveData GetSprinklerSaveData()
    {
        if (PlayerData == null) return new SprinklerSaveData();
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.sprinkler ??= new();
        return PlayerData.GardenData.sprinkler;
    }

    public static void UpdateSprinklerLevel(int level)
    {
        if (PlayerData == null) return;
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.sprinkler ??= new();
        PlayerData.GardenData.sprinkler.currentLevel = level;
        SavePlayerData().Forget();
    }

    public static HarvesterSaveData GetHarvesterSaveData()
    {
        if (PlayerData == null) return new HarvesterSaveData();
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.harvester ??= new();
        return PlayerData.GardenData.harvester;
    }

    public static void UpdateHarvesterLevel(int level)
    {
        if (PlayerData == null) return;
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.harvester ??= new();
        PlayerData.GardenData.harvester.currentLevel = level;
        SavePlayerData().Forget();
    }

    public static TransportSaveData GetTransportSaveData()
    {
        if (PlayerData == null) return new TransportSaveData();
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.transport ??= new();
        return PlayerData.GardenData.transport;
    }

    public static void UpdateTransportLevel(int level)
    {
        if (PlayerData == null) return;
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.transport ??= new();
        PlayerData.GardenData.transport.currentLevel = level;
        SavePlayerData().Forget();
    }

    #endregion

    #region Value Upgrade

    public static ValueUpgradeSaveData GetValueUpgradeSaveData()
    {
        if (PlayerData == null) return new ValueUpgradeSaveData();
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.valueUpgrades ??= new();
        return PlayerData.GardenData.valueUpgrades;
    }

    public static int GetValueUpgradeLevel(ValueUpgradeType type, int targetId)
    {
        return GetValueUpgradeSaveData().GetLevel((int)type, targetId);
    }

    public static void UpdateValueUpgradeLevel(ValueUpgradeType type, int targetId, int level)
    {
        if (PlayerData == null) return;
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.valueUpgrades ??= new();
        PlayerData.GardenData.valueUpgrades.SetLevel((int)type, targetId, level);
        SavePlayerData().Forget();
    }

    #endregion

    #region Collection

    public static CollectionSaveData GetCollectionSaveData()
    {
        if (PlayerData == null) return new CollectionSaveData();
        PlayerData.CollectionData ??= new();
        return PlayerData.CollectionData;
    }

    public static CollectionItemState GetCollectionItemState(int plantId, int level)
    {
        return GetCollectionSaveData().GetItemState(plantId, level);
    }

    public static bool UnlockCollectionPlant(int plantId, int level)
    {
        if (PlayerData == null || plantId < 0 || level <= 0) return false;
        PlayerData.CollectionData ??= new();
        bool isNew = PlayerData.CollectionData.UnlockItem(plantId, level);
        if (isNew)
        {
            SavePlayerData().Forget();
            GameEventBus.OnCollectionPlantUnlocked?.Invoke(plantId, level);
        }
        return isNew;
    }

    public static bool ClaimCollectionPlant(int plantId, int level)
    {
        if (PlayerData == null || plantId < 0 || level <= 0) return false;
        PlayerData.CollectionData ??= new();
        bool success = PlayerData.CollectionData.ClaimItem(plantId, level);
        if (success)
        {
            SavePlayerData().Forget();
            GameEventBus.OnCollectionPlantClaimed?.Invoke(plantId, level, PlayerData.CollectionData.currentClaimedCount);
        }
        return success;
    }

    public static bool ClaimCollectionReward()
    {
        if (PlayerData == null) return false;
        var config = ConfigHolder.Instance?.CollectionConfigSO;
        int target = config != null ? config.targetCount : 16;
        double rewardGem = config != null ? config.rewardGem : 5000;

        PlayerData.CollectionData ??= new();
        if (!PlayerData.CollectionData.CanClaimReward(target)) return false;

        if (PlayerData.CollectionData.ConsumeReward(target))
        {
            ChangeGem(rewardGem, true, false);
            SavePlayerData().Forget();
            GameEventBus.OnGainGem?.Invoke();
            GameEventBus.OnCollectionRewardClaimed?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Đồng bộ lại tiến trình mở khóa từ các ô đất hiện có trong vườn (dành cho người chơi cũ hoặc khi load)
    /// </summary>
    public static void SyncCollectionFromPlots()
    {
        if (PlayerData?.GardenData?.plots == null) return;
        bool anyNew = false;
        PlayerData.CollectionData ??= new();
        foreach (var plot in PlayerData.GardenData.plots)
        {
            if (plot.state == PlotState.Planted && plot.plantedPlantId >= 0)
            {
                int maxLvl = Mathf.Max(1, plot.currentLevel);
                for (int l = 1; l <= maxLvl; l++)
                {
                    if (PlayerData.CollectionData.UnlockItem(plot.plantedPlantId, l))
                    {
                        anyNew = true;
                    }
                }
            }
        }
        if (anyNew)
        {
            SavePlayerData().Forget();
        }
    }

    #endregion

    #region Main Quest

    public static MainQuestSaveData GetMainQuestSaveData()
    {
        if (PlayerData == null) return new MainQuestSaveData();
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.mainQuest ??= new();
        return PlayerData.GardenData.mainQuest;
    }

    public static void UpdateMainQuestProgress(long progress, bool isSave = true)
    {
        if (PlayerData == null) return;
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.mainQuest ??= new();
        PlayerData.GardenData.mainQuest.currentProgress = progress;
        if (isSave) SavePlayerData().Forget();
    }

    public static void AddCompletedMainQuestId(int id, bool isSave = true)
    {
        if (PlayerData == null) return;
        PlayerData.GardenData ??= new();
        PlayerData.GardenData.mainQuest ??= new();
        PlayerData.GardenData.mainQuest.completedQuestIds ??= new();
        if (!PlayerData.GardenData.mainQuest.completedQuestIds.Contains(id))
        {
            PlayerData.GardenData.mainQuest.completedQuestIds.Add(id);
        }
        PlayerData.GardenData.mainQuest.currentProgress = 0;
        if (isSave) SavePlayerData().Forget();
    }

    #endregion
}
