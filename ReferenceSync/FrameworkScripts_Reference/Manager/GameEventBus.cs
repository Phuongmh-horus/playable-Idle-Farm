using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameEventBus
{
    #region Profile

    public static Action<int> OnSelectAvatarCell;
    public static Action<int> OnSelectFrameCell;
    public static Action OnAvatarChange;
    public static Action OnFrameChange;
    public static Action OnChangeNameSuccess;
    public static Action UpdateProfileNotify;

    /// <summary>
    /// Toàn bộ PlayerData vừa bị thay bằng account khác (chọn account ở popup,
    /// link Google/Apple giữa game). Mọi UI đang hiển thị số liệu người chơi phải vẽ lại.
    ///
    /// Khác với các event lẻ ở trên: những event kia báo MỘT giá trị đổi, event này báo
    /// ĐỔI CẢ TÀI KHOẢN. Màn hình nào chỉ đọc data lúc khởi tạo (bản đồ level chẳng hạn)
    /// thì phải nghe event này, vì nó sẽ không được khởi tạo lại.
    /// </summary>
    public static Action OnPlayerDataReplaced;
    #endregion
    
    #region BottomBarLayer

    public static Action<BottomBarTabType, bool> SetBottomBarTabActive;

    #endregion

    #region IAP

    public static Action OnRestorePurchases;
    public static Action<IAPProduct> OnPurchaseSuccess;
    public static Action<string> OnBuyProduct;
    public static Func<IAPCategory, IAPProduct> GetIAPProductByCategory;
    public static Func<string, IAPProduct> GetIAPProductById;
    public static Func<List<IAPProduct>> GetAllIAPProducts;
    public static Func<IAPCategory, string> GetProductPriceByProductCategory;
    public static Action<string> OnReceivePackInfoFromStore;
    public static Action OnBuyWeekendSaleThisWeek;

    #endregion

    #region Heart

    public static Action OnHeartDataChange;
    public static Action UpdateOffHeartListen;

    #endregion

    #region LayerManager

    public static ActionSealed<ShowLayerGroupData> OnCloseLayerGroup = new();

    #endregion

    #region Setting

    public static ActionSealed<string> OnLanguageChanged = new();
    public static Action<string> OnLanguageSelected;
    #endregion

    #region Shop

    public static Action CancelIgnoreLastPackShop;

    #endregion

    #region Offer Pack

    public static Action OnOfferPackClosed;
    public static Action<IAPCategory> OnOfferPackOpened;
    public static Action IgnoreLastPack;
    public static Action OnShowPopupOffer;
    #endregion

    #region Reward

    public static Action OnGainCash; // Hiệu ứng tiền bay
    public static Action OnCashChange; // Hiệu ứng tiền thay đổi
    public static Action OnGainGem; // Hiệu ứng gem bay
    public static Action OnGemChange; // Hiệu ứng gem thay đổi
    public static Action OnGainAdTicket; // Hiệu ứng vé bay
    public static Action OnAdTicketChange; // Hiệu ứng vé thay đổi
    public static Action<RewardType> OnBoosterDataChanged;
    #endregion

    #region New Level

    public static Action OnNewLevel;

    #endregion

    #region SoundManager

    public static Action<float> OnChangeSound;
    public static Action<float> OnChangeSoundFx;

    #endregion

    #region HelperPack

    public static Action ActiveHelperItem;

    #endregion

    #region Game Play
    public static Action OnLoadLevelDone;
    public static Action OnUseSampleBooster;
    public static Action<Action> OnReceiveBooster;
    public static Action<RewardType, Vector3> OnReceiveBoosterFromBundle;
    public static Action<RewardType, Vector3, Action> OnReceiveBoosterFromPopUpBuyMore;
    #endregion

    #region Camera

    public static Action OnCameraAutoFocusStart;
    public static Action OnCameraDragStart;
    public static Func<bool> GetCameraIsPanning;
    public static Func<bool> GetCameraIsPinching;
    public static Func<bool> GetCameraIsAnimating;
    public static Action<Vector3, float?, float?, Vector3?> RequestCameraFocusAndZoom;

    /// <summary>
    /// Parameters: targetPosition, targetNormalizedZoom, zoomInDuration, holdDuration, zoomOutDuration, offset, lockInput, onTargetReached, onComplete, delayBeforeStart
    /// </summary>
    public static Action<Vector3, float?, float?, float?, float?, Vector3?, bool, Action, Action, float?> RequestCameraZoomInAndBack;

    /// <summary>
    /// Parameters: targetPosition, targetNormalizedZoom, duration, offset, lockInput, onComplete
    /// </summary>
    public static Action<Vector3, float?, float?, Vector3?, bool, Action> RequestCameraFlyTo;

    /// <summary>
    /// Parameters: targetPositions, targetNormalizedZoom, travelDurationPerTarget, holdDurationPerTarget, returnDuration, offset, lockInput, onComplete
    /// </summary>
    public static Action<IList<Vector3>, float?, float?, float?, float?, Vector3?, bool, Action> RequestCameraTour;

    /// <summary>
    /// Parameters: duration, magnitude, frequency
    /// </summary>
    public static Action<float, float, float> RequestCameraShake;

    public static Action RequestCancelCameraAnimation;
    
    #endregion

    #region Fruit Transport Station

    public static Action OnUpdateTransportStation;

    #endregion

    #region Value Upgrade

    public static Action OnUpdateValueUpgrade;

    #endregion

    #region Selection & Input

    public static Action OnNothingClicked;

    #endregion

    #region Collection

    public static Action<int, int> OnCollectionPlantUnlocked; // (plantId, level)
    public static Action<int, int, int> OnCollectionPlantClaimed; // (plantId, level, currentClaimedCount)
    public static Action OnCollectionRewardClaimed;

    #endregion

    #region Main Quest & Farm Economy

    public static Action OnMainQuestUpdated;
    public static Action<MainQuestData> OnMainQuestCompleted;
    public static Action<MainQuestData> OnMainQuestClaimed;
    public static Action<int> OnPlotUnlockedEvent;
    public static Action<int, int> OnCropPlantedEvent;
    public static Action<int, int> OnCropUpgradedEvent;
    public static Action<int, int> OnSectorMergedEvent;
    public static Action<int> OnSprinklerUpgradedEvent;
    public static Action<int> OnHarvesterUpgradedEvent;
    public static Action<int, int> OnCropHarvestedEvent;
    public static Action<double> OnRevenueEarnedEvent;
    public static Action<int> OnCrowChasedEvent;
    public static Action<int> OnOrderCompletedEvent;
    public static Action<int> OnFruitSoldEvent; // (saleCount)
    public static Action<long> OnOnlineDurationTickEvent; // (minutes)
    public static Action<int, int> OnMegaCropHarvestedEvent; // (plantId, count)
    public static Action OnMiniSprinklerUsedEvent;
    public static Action OnShopPurchasedEvent;
    public static Action<int> OnDailyLoginEvent;

    /// <summary>
    /// Kích hoạt mỗi khi có bất kỳ thay đổi nào ảnh hưởng tới nền kinh tế / Idle CPS của Nông trại:
    /// (Trồng cây mới, nâng cấp cây, mở ô đất, gộp cụm Mega, nâng cấp Vòi tưới / Máy gặt / Xe tải, nâng cấp Value Upgrade).
    /// </summary>
    public static Action OnFarmEconomyChanged;

    #endregion

    #region Daily / Weekly Quest

    public static Action<QuestPeriodType> OnDailyWeeklyQuestUpdated;
    // A newly paid and verified transaction only. Restore purchases must not invoke this event.
    public static Action<IAPProduct, string> OnVerifiedIAPPurchased;

    #endregion
}
