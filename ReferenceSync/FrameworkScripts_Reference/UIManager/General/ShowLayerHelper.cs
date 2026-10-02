using System;
using System.Collections.Generic;
using System.Net.Mime;

public partial class LayerManager
{
    private ShowLayerGroupData _showMainMenuLayerData;

    public void ShowMainMenuLayer(System.Action<LayerGroup> onDone = null)
    {
        if (_showMainMenuLayerData == null)
        {
            _showMainMenuLayerData = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.MainMenuLayer);
            _showMainMenuLayerData.AddLayer(LayerType.NavigationBarLayer);
            _showMainMenuLayerData.AddLayer(LayerType.CurrencyLayer);
            _showMainMenuLayerData.AddLayer(LayerType.MaintainInternetLayer);
        }

        _showMainMenuLayerData.OnInitData = SetupDataMainMenuLayer;
        _showMainMenuLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showMainMenuLayerData);
        return;

        void SetupDataMainMenuLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.MainMenuLayer, out var layerBase))
            {
                layerBase.InitData();
            }

            if (layerGroup.GetLayerBase(LayerType.CurrencyLayer, out var layerBase2))
            {
                layerBase2.InitData();
            }
        }
    }

    private ShowLayerGroupData _showSelectAccountLayerData;

    public void ShowSelectAccountLayer(LayerGroupType layerGroupType, System.Action<LayerGroup> onDone = null)
    {
        _showSelectAccountLayerData ??= LayerGroupBuilder.Build(layerGroupType, LayerType.SelectAccountLayer);
        _showSelectAccountLayerData.OnInitData = SetupDataSelectAccountLayer;
        _showSelectAccountLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showSelectAccountLayerData);
        return;

        void SetupDataSelectAccountLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.SelectAccountLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    public void ShowShopCashMore()
    {
        if (GameStateManager.Instance.CurrentGameState == GameState.MainMenu)
        {
            ShowShopLayer((group) => { GameEventBus.SetBottomBarTabActive?.Invoke(BottomBarTabType.Shop, false); });
        }
        else
        {
            ShowShopInGameLayer();
        }
    }

    private ShowLayerGroupData _showShopLayerData;

    public void ShowShopLayer(System.Action<LayerGroup> onDone = null)
    {
        if (_showShopLayerData == null)
        {
            _showShopLayerData = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.ShopLayer);
            _showShopLayerData.AddLayer(LayerType.NavigationBarLayer);
            _showShopLayerData.AddLayer(LayerType.CurrencyLayer);
            _showShopLayerData.AddLayer(LayerType.HeartLayer);
        }

        _showShopLayerData.OnInitData = SetupDataShopLayer;
        _showShopLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showShopLayerData);
        return;

        void SetupDataShopLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            var data = new UIShopData() { ShopFullScreen = false };
            if (layerGroup.GetLayerBase(LayerType.ShopLayer, out var layerBase))
            {
                var shopLayer = layerBase as UIShopLayer;
                shopLayer?.InitData(data, true);
            }

            if (layerGroup.GetLayerBase(LayerType.CurrencyLayer, out var layerBase2))
            {
                layerBase2.InitData();
            }

            if (layerGroup.GetLayerBase(LayerType.HeartLayer, out var layerBase3))
            {
                layerBase3.InitData();
            }
        }
    }

    private ShowLayerGroupData _showShopInGameLayerData;

    public void ShowShopInGameLayer(System.Action<LayerGroup> onDone = null)
    {
        if (_showShopInGameLayerData == null)
        {
            _showShopInGameLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.ShopLayer);
            _showShopInGameLayerData.AddLayer(LayerType.CurrencyLayer);
            _showShopInGameLayerData.AddLayer(LayerType.GemLayer);
            _showShopInGameLayerData.AddLayer(LayerType.AdTicketLayer);
        }

        _showShopInGameLayerData.OnInitData = SetupDataShopLayer;
        _showShopInGameLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showShopInGameLayerData);
        return;

        void SetupDataShopLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            var data = new UIShopData() { ShopFullScreen = true };
            if (layerGroup.GetLayerBase(LayerType.ShopLayer, out var layerBase))
            {
                var shopLayer = layerBase as UIShopLayer;
                shopLayer?.InitData(data, false);
            }
            if (layerGroup.GetLayerBase(LayerType.CurrencyLayer, out var layerBase2))
            {
                layerBase2.InitData();
            }
            if (layerGroup.GetLayerBase(LayerType.GemLayer, out var layerBase3))
            {
                layerBase3.InitData();
            }
            if (layerGroup.GetLayerBase(LayerType.AdTicketLayer, out var layerBase4))
            {
                layerBase4.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupFeedbackLayerData;

    public void ShowPopupFeedbackLayer(PopupFeedbackData popupFeedbackData, System.Action<LayerGroup> onDone = null)
    {
        _showPopupFeedbackLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Notify, LayerType.PopupFeedbackLayer);
        _showPopupFeedbackLayerData.OnInitData = SetupDataPopupFeedbackLayer;
        _showPopupFeedbackLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupFeedbackLayerData);
        return;

        void SetupDataPopupFeedbackLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupFeedbackLayer, out var layerBase))
            {
                var popupFeedBack = layerBase as UIPopupFeedbackLayer;
                popupFeedBack?.InitData(popupFeedbackData);
            }
        }
    }

    public void ShowPopupNoAds()
    {
        ShowPopupFeedbackLayer(TextUtility.GetI2("ui_no_ads"));
    }

    public void ShowPopupFeedbackLayer(string content, string title = "", System.Action<LayerGroup> onDone = null)
    {
        var popupFeedbackData = new PopupFeedbackData()
        {
            Title = title,
            Content = content,
        };
        ShowPopupFeedbackLayer(popupFeedbackData, onDone);
    }

    public void ShowPopupNoInternet()
    {
        ShowPopupFeedbackLayer(TextUtility.GetI2("ui_no_internet"));
    }

    private ShowLayerGroupData _showPopupConfirmLayerData;

    public void ShowPopupConfirmLayer(PopupConfirmLayerData data, System.Action<LayerGroup> onDone = null)
    {
        _showPopupConfirmLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupConfirmLayer);
        _showPopupConfirmLayerData.OnInitData = SetupDataPopupConfirmLayer;
        _showPopupConfirmLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupConfirmLayerData);
        return;

        void SetupDataPopupConfirmLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupConfirmLayer, out var layerBase))
            {
                var popupConfirmLayer = layerBase as UIPopupConfirmLayer;
                popupConfirmLayer?.InitData(data);
            }
        }
    }

    #region Popup Notify

    public void ShowPopupNotifyLayer(string content, Action<LayerGroup> onDone = null)
    {
        var okData = new PopupConfirmLayerData()
        {
            Title = TextUtility.GetI2("ui_popup_notify_title"),
            Content = content,
            IsHideCancelButton = true,
            OkText = TextUtility.GetI2("btn_ok"),
        };
        ShowPopupConfirmLayer(okData, onDone);
    }

    #endregion

    public void ShowPopupLastChange(Action okAction = null, Action cancelAction = null)
    {
        var okData = new PopupConfirmLayerData()
        {
            Title = TextUtility.GetI2("ui_popup_notify_title"),
            Content = TextUtility.GetI2("ui_popup_noti_last_chance"),
            OnOkClick = okAction,
            OnCancelClick = cancelAction
        };
        ShowPopupConfirmLayer(okData);
    }

    private ShowLayerGroupData _showBannerAdsLayerData;

    public void ShowBannerAdsLayer(System.Action<LayerGroup> onDone = null)
    {
        _showBannerAdsLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Fixed, LayerType.BannerAdsLayer);
        _showBannerAdsLayerData.OnInitData = SetupDataBannerAdsLayer;
        _showBannerAdsLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showBannerAdsLayerData);
        return;

        void SetupDataBannerAdsLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.BannerAdsLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupRewardLayerData;

    public void ShowPopupRewardLayer(PopupRewardLayerData data, System.Action<LayerGroup> onDone = null)
    {
        _showPopupRewardLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupRewardLayer);
        _showPopupRewardLayerData.OnInitData = SetupDataPopupRewardLayer;
        _showPopupRewardLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupRewardLayerData);
        return;

        void SetupDataPopupRewardLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupRewardLayer, out var layerBase))
            {
                var popupRewardLayer = layerBase as UIPopupRewardLayer;
                popupRewardLayer?.InitData(data);
            }
        }
    }

    protected SupportPackLayerData _popupRefillHeartSupportData =
        new SupportPackLayerData()
        {
            SupportPacks =
                new List<IAPCategory>()
                    { IAPCategory.TinyPack, IAPCategory.SmallPack, IAPCategory.GreatPack, IAPCategory.EpicPack }
        };

    private ShowLayerGroupData _showPopupMoreHeartLayerData;

    public void ShowPopupRefillHeartLayer(PopupRefillHeartData data, System.Action<LayerGroup> onDone = null)
    {
        if (_showPopupMoreHeartLayerData == null)
        {
            _showPopupMoreHeartLayerData =
                LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupRefillHeartLayer);
            _showPopupMoreHeartLayerData.AddLayer(LayerType.SupportPackLayer);
        }

        _showPopupMoreHeartLayerData.OnInitData = SetupDataPopupMoreHeartLayer;
        _showPopupMoreHeartLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupMoreHeartLayerData);
        return;

        void SetupDataPopupMoreHeartLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupRefillHeartLayer, out var layerBase))
            {
                var moreHeartLayer = layerBase as UIPopupRefillHeartLayer;
                moreHeartLayer?.InitData(data);
            }

            if (layerGroup.GetLayerBase(LayerType.SupportPackLayer, out var layerBase2))
            {
                var supportPackLayer = layerBase2 as UISupportPackLayer;
                supportPackLayer?.InitData(_popupRefillHeartSupportData);
            }
        }
    }

    private ShowLayerGroupData _showSettingMainMenuLayerData;

    public void ShowSettingMainMenuLayer(Action<LayerGroup> onDone = null)
    {
        _showSettingMainMenuLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.SettingMainMenuLayer);
        _showSettingMainMenuLayerData.OnInitData = SetupDataSettingMainMenuLayer;
        _showSettingMainMenuLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showSettingMainMenuLayerData);
        return;

        void SetupDataSettingMainMenuLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.SettingMainMenuLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showSettingLanguageLayerData;

    public void ShowSettingLanguageLayer(System.Action<LayerGroup> onDone = null)
    {
        _showSettingLanguageLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.SettingLanguageLayer);
        _showSettingLanguageLayerData.OnInitData = SetupDataSettingLanguageLayer;
        _showSettingLanguageLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showSettingLanguageLayerData);
        return;

        void SetupDataSettingLanguageLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.SettingLanguageLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showSettingIngameLayerData;

    public void ShowSettingInGameLayer(System.Action<LayerGroup> onDone = null)
    {
        _showSettingIngameLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.SettingIngameLayer);
        _showSettingIngameLayerData.OnInitData = SetupDataSettingIngameLayer;
        _showSettingIngameLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showSettingIngameLayerData);
        return;

        void SetupDataSettingIngameLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.SettingIngameLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupRemoveAdsData;

    public void ShowPopupRemoveAds(System.Action<LayerGroup> onDone = null)
    {
        _showPopupRemoveAdsData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupRemoveAds);
        _showPopupRemoveAdsData.OnInitData = SetupDataPopupRemoveAds;
        _showPopupRemoveAdsData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupRemoveAdsData);
        return;

        void SetupDataPopupRemoveAds(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupRemoveAds, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showBannerSeparateAdsData;

    public void ShowBannerSeparateAds(LayerGroupType layerGroupType, System.Action<LayerGroup> onDone = null)
    {
        _showBannerSeparateAdsData ??= LayerGroupBuilder.Build(layerGroupType, LayerType.BannerSeparateAds);
        _showBannerSeparateAdsData.OnInitData = SetupDataBannerSeperateAds;
        _showBannerSeparateAdsData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showBannerSeparateAdsData);
        return;

        void SetupDataBannerSeperateAds(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.BannerSeparateAds, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showMaintainInternetLayerData;

    public void ShowMaintainInternetLayer(LayerGroupType layerGroupType, System.Action<LayerGroup> onDone = null)
    {
        _showMaintainInternetLayerData ??= LayerGroupBuilder.Build(layerGroupType, LayerType.MaintainInternetLayer);
        _showMaintainInternetLayerData.OnInitData = SetupDataMaintainInternetLayer;
        _showMaintainInternetLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showMaintainInternetLayerData);
        return;

        void SetupDataMaintainInternetLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.MaintainInternetLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupStarterPackData;

    public void ShowPopupStarterPack(Action onClosePopup = null, Action<LayerGroup> onDone = null)
    {
        _showPopupStarterPackData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupStarterPack);
        _showPopupStarterPackData.OnInitData = SetupDataPopupStarterPack;
        _showPopupStarterPackData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupStarterPackData);
        return;

        void SetupDataPopupStarterPack(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupStarterPack, out var layerBase))
            {
                var popup = layerBase as UIPopupStarterPack;
                popup?.InitLayer(onClosePopup);
            }
        }
    }

    private ShowLayerGroupData _showPopupDailyGiftData;

    public void ShowPopupDailyGift(System.Action<LayerGroup> onDone = null)
    {
        _showPopupDailyGiftData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupDailyGift);
        _showPopupDailyGiftData.OnInitData = SetupDataPopupDailyGift;
        _showPopupDailyGiftData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupDailyGiftData);
        return;

        void SetupDataPopupDailyGift(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupDailyGift, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupProfileLayerData;

    public void ShowPopupProfileLayer(System.Action<LayerGroup> onDone = null)
    {
        _showPopupProfileLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupProfileLayer);
        _showPopupProfileLayerData.OnInitData = SetupDataPopupProfileLayer;
        _showPopupProfileLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupProfileLayerData);
        return;

        void SetupDataPopupProfileLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupProfileLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupEditNickNameData;

    public void ShowPopupEditNickName(System.Action<LayerGroup> onDone = null)
    {
        _showPopupEditNickNameData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupEditNickName);
        _showPopupEditNickNameData.OnInitData = SetupDataPopupEditNickName;
        _showPopupEditNickNameData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupEditNickNameData);
        return;

        void SetupDataPopupEditNickName(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupEditNickName, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupRatingLayerData;

    public void ShowPopupRatingLayer(System.Action<LayerGroup> onDone = null)
    {
        _showPopupRatingLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupRatingLayer);
        _showPopupRatingLayerData.OnInitData = SetupDataPopupRatingLayer;
        _showPopupRatingLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupRatingLayerData);
        return;

        void SetupDataPopupRatingLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupRatingLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showGamePlayLayerData;

    public void ShowGamePlayLayer(System.Action<LayerGroup> onDone = null)
    {
        if (_showGamePlayLayerData == null)
        {
            _showGamePlayLayerData = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.GamePlayLayer);
            _showGamePlayLayerData.AddLayer(LayerType.MaintainInternetLayer);
            _showGamePlayLayerData.AddLayer(LayerType.CurrencyLayer);
            _showGamePlayLayerData.AddLayer(LayerType.GemLayer);
            _showGamePlayLayerData.AddLayer(LayerType.AdTicketLayer);
        }

        _showGamePlayLayerData.OnInitData = SetupDataGamePlayLayer;
        _showGamePlayLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showGamePlayLayerData);
        return;

        void SetupDataGamePlayLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.GamePlayLayer, out var layerBase))
            {
                layerBase.InitData();
            }
            
            if (layerGroup.GetLayerBase(LayerType.CurrencyLayer, out var layerBase2))
            {
                layerBase2.InitData();
            }
            
            if (layerGroup.GetLayerBase(LayerType.GemLayer, out var layerBase3))
            {
                layerBase3.InitData();
            }
            if (layerGroup.GetLayerBase(LayerType.AdTicketLayer, out var layerBase4))
            {
                layerBase4.InitData();
            }
        }
    }

    private ShowLayerGroupData _showEndGameLayerData;

    public void ShowEndGameLayer(UIEndGameData data, System.Action<LayerGroup> onDone = null)
    {
        if (_showEndGameLayerData == null)
        {
            _showEndGameLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.EndGameLayer);
            _showEndGameLayerData.AddLayer(LayerType.CurrencyLayer);
            _showEndGameLayerData.AddLayer(LayerType.HeartLayer);
        }

        _showEndGameLayerData.OnInitData = SetupDataEndGameLayer;
        _showEndGameLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showEndGameLayerData);
        return;

        void SetupDataEndGameLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.EndGameLayer, out var layerBase))
            {
                var endgame = layerBase as UIEndGameLayer;
                endgame?.InitData(data);
            }

            if (layerGroup.GetLayerBase(LayerType.CurrencyLayer, out var layerBase2))
            {
                layerBase2.InitData();
            }

            if (layerGroup.GetLayerBase(LayerType.HeartLayer, out var layerBase3))
            {
                layerBase3.InitData();
            }
        }
    }

    private ShowLayerGroupData _showDifficultyWarningLayerData;

    public void ShowDifficultyWarningLayer(System.Action<LayerGroup> onDone = null)
    {
        if (_showDifficultyWarningLayerData == null)
        {
            _showDifficultyWarningLayerData =
                LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.DifficultyWarningLayer);
        }

        _showDifficultyWarningLayerData.OnInitData = SetupDataDifficultyWarningLayer;
        _showDifficultyWarningLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showDifficultyWarningLayerData);
        return;

        void SetupDataDifficultyWarningLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.DifficultyWarningLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showKeepPlayingLayerData;

    public void ShowKeepPlayingLayer(PopupKeepPlayingData data, System.Action<LayerGroup> onDone = null)
    {
        if (_showKeepPlayingLayerData == null)
        {
            _showKeepPlayingLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.KeepPlayingLayer);
        }

        _showKeepPlayingLayerData.OnInitData = SetupDataKeepPlayingLayer;
        _showKeepPlayingLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showKeepPlayingLayerData);
        return;

        void SetupDataKeepPlayingLayer(LayerGroup layerGroup)
        {
            if (layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.KeepPlayingLayer, out var layerBase))
            {
                var popupKeepPlaying = layerBase as UIKeepPlayingLayer;
                popupKeepPlaying?.InitData(data);
            }
        }
    }

    protected SupportPackLayerData _popupPreLoseSupportData =
        new SupportPackLayerData()
        {
            SupportPacks =
                new List<IAPCategory>()
                    { IAPCategory.StarterPack, IAPCategory.HelperPack }
        };
    private ShowLayerGroupData _showPopupPreLoseData;
    public void ShowPopupPreLose(System.Action<LayerGroup> onDone = null)
    {
        if(_showPopupPreLoseData == null)
        {
           _showPopupPreLoseData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupPreLose);
           _showPopupPreLoseData.AddLayer(LayerType.SupportPackLayer);
        }
        _showPopupPreLoseData.OnInitData = SetupDataPopupPreLose;
        _showPopupPreLoseData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupPreLoseData);
        return;

        void SetupDataPopupPreLose(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupPreLose, out var layerBase))
            {
                layerBase.InitData();
            }
            if (layerGroup.GetLayerBase(LayerType.SupportPackLayer, out var layerBase2))
            {
                var supportPackLayer = layerBase2 as UISupportPackLayer;
                supportPackLayer?.InitData(_popupPreLoseSupportData);
            }
        }
    }

    private ShowLayerGroupData _showPopupOfferLayerData;
    public void ShowPopupOfferLayer(PopupOfferLayerData data, System.Action<LayerGroup> onDone = null)
    {
        if(_showPopupOfferLayerData == null)
        {
           _showPopupOfferLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupOfferLayer);
        }
        _showPopupOfferLayerData.OnInitData = SetupDataPopupOfferLayer;
        _showPopupOfferLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupOfferLayerData);
        return;

        void SetupDataPopupOfferLayer(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupOfferLayer, out var layerBase))
            {
                var offerPackLayer = layerBase as UIPopupOfferUICoreLayer;
                offerPackLayer?.SetupLayerData(data);
            }
        }
    }



    private ShowLayerGroupData _showCheckUpdateLayerData;
    public void ShowCheckUpdateLayer(System.Action<LayerGroup> onDone = null)
    {
        if(_showCheckUpdateLayerData == null)
        {
           _showCheckUpdateLayerData = LayerGroupBuilder.Build(LayerGroupType.Custom, LayerType.CheckUpdateLayer);
        }
        _showCheckUpdateLayerData.OnInitData = SetupDataCheckUpdateLayer;
        _showCheckUpdateLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showCheckUpdateLayerData);
        return;

        void SetupDataCheckUpdateLayer(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.CheckUpdateLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupSprinklerAndHarvesterUpgradeLayerData;
    public void ShowPopupSprinklerAndHarvesterUpgradeLayer(System.Action<LayerGroup> onDone = null)
    {
        if(_showPopupSprinklerAndHarvesterUpgradeLayerData == null)
        {
           _showPopupSprinklerAndHarvesterUpgradeLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupSprinklerAndHarvesterUpgradeLayer);
        }
        _showPopupSprinklerAndHarvesterUpgradeLayerData.OnInitData = SetupDataPopupSprinklerAndHarvesterUpgradeLayer;
        _showPopupSprinklerAndHarvesterUpgradeLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupSprinklerAndHarvesterUpgradeLayerData);
        return;

        void SetupDataPopupSprinklerAndHarvesterUpgradeLayer(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupSprinklerAndHarvesterUpgradeLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupFruitTransportStationUpgradeLayerData;
    public void ShowPopupFruitTransportStationUpgradeLayer(System.Action<LayerGroup> onDone = null)
    {
        if(_showPopupFruitTransportStationUpgradeLayerData == null)
        {
           _showPopupFruitTransportStationUpgradeLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupFruitTransportStationUpgradeLayer);
        }
        _showPopupFruitTransportStationUpgradeLayerData.OnInitData = SetupDataPopupFruitTransportStationUpgradeLayer;
        _showPopupFruitTransportStationUpgradeLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupFruitTransportStationUpgradeLayerData);
        return;

        void SetupDataPopupFruitTransportStationUpgradeLayer(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupFruitTransportStationUpgradeLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showGemLayerData;
    public void ShowGemLayer(System.Action<LayerGroup> onDone = null)
    {
        if(_showGemLayerData == null)
        {
           _showGemLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.GemLayer);
        }
        _showGemLayerData.OnInitData = SetupDataGemLayer;
        _showGemLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showGemLayerData);
        return;

        void SetupDataGemLayer(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.GemLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupUpgradeValueData;
    public void ShowPopupUpgradeValue(System.Action<LayerGroup> onDone = null)
    {
        if(_showPopupUpgradeValueData == null)
        {
           _showPopupUpgradeValueData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupUpgradeValue);
        }
        _showPopupUpgradeValueData.OnInitData = SetupDataPopupUpgradeValue;
        _showPopupUpgradeValueData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupUpgradeValueData);
        return;

        void SetupDataPopupUpgradeValue(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupUpgradeValue, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupCollectionLayerData;
    public void ShowPopupCollectionLayer(System.Action<LayerGroup> onDone = null)
    {
        if(_showPopupCollectionLayerData == null)
        {
           _showPopupCollectionLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupCollectionLayer);
        }
        _showPopupCollectionLayerData.OnInitData = SetupDataPopupCollectionLayer;
        _showPopupCollectionLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupCollectionLayerData);
        return;

        void SetupDataPopupCollectionLayer(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupCollectionLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupDailyWeeklyQuestLayerData;
    public void ShowPopupDailyWeeklyQuestLayer(System.Action<LayerGroup> onDone = null)
    {
        if(_showPopupDailyWeeklyQuestLayerData == null)
        {
           _showPopupDailyWeeklyQuestLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupDailyWeeklyQuestLayer);
        }
        _showPopupDailyWeeklyQuestLayerData.OnInitData = SetupDataPopupDailyWeeklyQuestLayer;
        _showPopupDailyWeeklyQuestLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupDailyWeeklyQuestLayerData);
        return;

        void SetupDataPopupDailyWeeklyQuestLayer(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupDailyWeeklyQuestLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupLuckyWheelLayerData;
    public void ShowPopupLuckyWheelLayer(System.Action<LayerGroup> onDone = null)
    {
        if(_showPopupLuckyWheelLayerData == null)
        {
           _showPopupLuckyWheelLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupLuckyWheelLayer);
        }
        _showPopupLuckyWheelLayerData.OnInitData = SetupDataPopupLuckyWheelLayer;
        _showPopupLuckyWheelLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupLuckyWheelLayerData);
        return;

        void SetupDataPopupLuckyWheelLayer(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupLuckyWheelLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupInfoPlantLayerData;
    public void ShowPopupInfoPlantLayer(PopupInfoPlantLayerData data, System.Action<LayerGroup> onDone = null)
    {
        if(_showPopupInfoPlantLayerData == null)
        {
           _showPopupInfoPlantLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupInfoPlantLayer);
        }
        _showPopupInfoPlantLayerData.OnInitData = SetupDataPopupInfoPlantLayer;
        _showPopupInfoPlantLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupInfoPlantLayerData);
        return;

        void SetupDataPopupInfoPlantLayer(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupInfoPlantLayer, out var layerBase))
            {
                if (layerBase is UIPopupInfoPlantLayer popupInfoPlantLayer)
                {
                    popupInfoPlantLayer.InitData(data);
                }
            }
        }
    }

    private ShowLayerGroupData _showAdTicketLayerData;
    public void ShowAdTicketLayer(System.Action<LayerGroup> onDone = null)
    {
        if(_showAdTicketLayerData == null)
        {
           _showAdTicketLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.AdTicketLayer);
        }
        _showAdTicketLayerData.OnInitData = SetupDataAdTicketLayer;
        _showAdTicketLayerData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showAdTicketLayerData);
        return;

        void SetupDataAdTicketLayer(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.AdTicketLayer, out var layerBase))
            {
                layerBase.InitData();
            }
        }
    }

    private ShowLayerGroupData _showPopupOfflineEarningsData;
    public void ShowPopupOfflineEarnings(PopupOfflineEarningsData data = null, System.Action<LayerGroup> onDone = null)
    {
        if(_showPopupOfflineEarningsData == null)
        {
           _showPopupOfflineEarningsData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupOfflineEarnings);
        }
        _showPopupOfflineEarningsData.OnInitData = SetupDataPopupOfflineEarnings;
        _showPopupOfflineEarningsData.OnShowComplete = onDone;
        ShowGroupLayerAsync(_showPopupOfflineEarningsData);
        return;

        void SetupDataPopupOfflineEarnings(LayerGroup layerGroup)
        {
            if(layerGroup == null) return;
            if (layerGroup.GetLayerBase(LayerType.PopupOfflineEarnings, out var layerBase))
            {
                if (layerBase is UIPopupOfflineEarnings popup)
                {
                    if (data != null)
                        popup.InitData(data);
                    else
                        popup.InitData();
                }
                else
                {
                    layerBase.InitData();
                }
            }
        }
    }
}
