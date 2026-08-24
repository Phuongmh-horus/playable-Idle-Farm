// using System;
// using System.Collections.Generic;
// using System.Net.Mime;
// using UnityEngine;

// public partial class LayerManager
// {
//     private ShowLayerGroupData _showMainMenuLayerData;

//     public void ShowMainMenuLayer(System.Action<LayerGroup> onDone = null)
//     {
//         if (_showMainMenuLayerData == null)
//         {
//             _showMainMenuLayerData = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.MainMenuLayer);
//             _showMainMenuLayerData.AddLayer(LayerType.NavigationBarLayer);
//             _showMainMenuLayerData.AddLayer(LayerType.CurrencyLayer);
//             _showMainMenuLayerData.AddLayer(LayerType.HeartLayer);
//             _showMainMenuLayerData.AddLayer(LayerType.MaintainInternetLayer);
//         }

//         _showMainMenuLayerData.OnInitData = SetupDataMainMenuLayer;
//         _showMainMenuLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showMainMenuLayerData);
//         return;

//         void SetupDataMainMenuLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.MainMenuLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }

//             if (layerGroup.GetLayerBase(LayerType.CurrencyLayer, out var layerBase2))
//             {
//                 layerBase2.InitData();
//             }

//             if (layerGroup.GetLayerBase(LayerType.HeartLayer, out var layerBase3))
//             {
//                 layerBase3.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showSelectAccountLayerData;

//     public void ShowSelectAccountLayer(LayerGroupType layerGroupType, System.Action<LayerGroup> onDone = null)
//     {
//         _showSelectAccountLayerData ??= LayerGroupBuilder.Build(layerGroupType, LayerType.SelectAccountLayer);
//         _showSelectAccountLayerData.OnInitData = SetupDataSelectAccountLayer;
//         _showSelectAccountLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showSelectAccountLayerData);
//         return;

//         void SetupDataSelectAccountLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.SelectAccountLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     public void ShowShopCashMore()
//     {
//         if (GameStateManager.Instance.CurrentGameState == GameState.MainMenu)
//         {
//             ShowShopLayer((group) => { GameEventBus.SetBottomBarTabActive?.Invoke(BottomBarTabType.Shop, false); });
//         }
//         else
//         {
//             ShowShopInGameLayer();
//         }
//     }

//     private ShowLayerGroupData _showShopLayerData;

//     public void ShowShopLayer(System.Action<LayerGroup> onDone = null)
//     {
//         if (_showShopLayerData == null)
//         {
//             _showShopLayerData = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.ShopLayer);
//             _showShopLayerData.AddLayer(LayerType.NavigationBarLayer);
//             _showShopLayerData.AddLayer(LayerType.CurrencyLayer);
//             _showShopLayerData.AddLayer(LayerType.HeartLayer);
//         }

//         _showShopLayerData.OnInitData = SetupDataShopLayer;
//         _showShopLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showShopLayerData);
//         return;

//         void SetupDataShopLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             var data = new UIShopData() { ShopFullScreen = false };
//             if (layerGroup.GetLayerBase(LayerType.ShopLayer, out var layerBase))
//             {
//                 var shopLayer = layerBase as UIShopLayer;
//                 shopLayer?.InitData(data, true);
//             }

//             if (layerGroup.GetLayerBase(LayerType.CurrencyLayer, out var layerBase2))
//             {
//                 layerBase2.InitData();
//             }

//             if (layerGroup.GetLayerBase(LayerType.HeartLayer, out var layerBase3))
//             {
//                 layerBase3.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showShopInGameLayerData;

//     public void ShowShopInGameLayer(System.Action<LayerGroup> onDone = null)
//     {
//         if (_showShopInGameLayerData == null)
//         {
//             _showShopInGameLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.ShopLayer);
//             _showShopInGameLayerData.AddLayer(LayerType.CurrencyLayer);
//             _showShopInGameLayerData.AddLayer(LayerType.HeartLayer);
//         }

//         _showShopInGameLayerData.OnInitData = SetupDataShopLayer;
//         _showShopInGameLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showShopInGameLayerData);
//         return;

//         void SetupDataShopLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             var data = new UIShopData() { ShopFullScreen = true };
//             if (layerGroup.GetLayerBase(LayerType.ShopLayer, out var layerBase))
//             {
//                 var shopLayer = layerBase as UIShopLayer;
//                 shopLayer?.InitData(data, false);
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupFeedbackLayerData;

//     public void ShowPopupFeedbackLayer(PopupFeedbackData popupFeedbackData, System.Action<LayerGroup> onDone = null)
//     {
//         _showPopupFeedbackLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Notify, LayerType.PopupFeedbackLayer);
//         _showPopupFeedbackLayerData.OnInitData = SetupDataPopupFeedbackLayer;
//         _showPopupFeedbackLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupFeedbackLayerData);
//         return;

//         void SetupDataPopupFeedbackLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupFeedbackLayer, out var layerBase))
//             {
//                 var popupFeedBack = layerBase as UIPopupFeedbackLayer;
//                 popupFeedBack?.InitData(popupFeedbackData);
//             }
//         }
//     }

//     public void ShowPopupNoAds()
//     {
//         ShowPopupFeedbackLayer(TextUtility.GetI2("ui_no_ads"));
//     }

//     public void ShowPopupFeedbackLayer(string content, string title = "", System.Action<LayerGroup> onDone = null)
//     {
//         var popupFeedbackData = new PopupFeedbackData()
//         {
//             Title = title,
//             Content = content,
//         };
//         ShowPopupFeedbackLayer(popupFeedbackData, onDone);
//     }

//     public void ShowPopupNoInternet()
//     {
//         ShowPopupFeedbackLayer(TextUtility.GetI2("ui_no_internet"));
//     }

//     private ShowLayerGroupData _showPopupConfirmLayerData;

//     public void ShowPopupConfirmLayer(PopupConfirmLayerData data, System.Action<LayerGroup> onDone = null)
//     {
//         _showPopupConfirmLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupConfirmLayer);
//         _showPopupConfirmLayerData.OnInitData = SetupDataPopupConfirmLayer;
//         _showPopupConfirmLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupConfirmLayerData);
//         return;

//         void SetupDataPopupConfirmLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupConfirmLayer, out var layerBase))
//             {
//                 var popupConfirmLayer = layerBase as UIPopupConfirmLayer;
//                 popupConfirmLayer?.InitData(data);
//             }
//         }
//     }

//     #region Popup Notify

//     public void ShowPopupNotifyLayer(string content, Action<LayerGroup> onDone = null)
//     {
//         var okData = new PopupConfirmLayerData()
//         {
//             Title = TextUtility.GetI2("ui_popup_notify_title"),
//             Content = content,
//             IsHideCancelButton = true,
//             OkText = TextUtility.GetI2("btn_ok"),
//         };
//         ShowPopupConfirmLayer(okData, onDone);
//     }

//     #endregion

//     public void ShowPopupLastChange(Action okAction = null, Action cancelAction = null)
//     {
//         var okData = new PopupConfirmLayerData()
//         {
//             Title = TextUtility.GetI2("ui_popup_notify_title"),
//             Content = TextUtility.GetI2("ui_popup_noti_last_chance"),
//             OnOkClick = okAction,
//             OnCancelClick = cancelAction
//         };
//         ShowPopupConfirmLayer(okData);
//     }

//     private ShowLayerGroupData _showBannerAdsLayerData;

//     public void ShowBannerAdsLayer(System.Action<LayerGroup> onDone = null)
//     {
//         _showBannerAdsLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Fixed, LayerType.BannerAdsLayer);
//         _showBannerAdsLayerData.OnInitData = SetupDataBannerAdsLayer;
//         _showBannerAdsLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showBannerAdsLayerData);
//         return;

//         void SetupDataBannerAdsLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.BannerAdsLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupRewardLayerData;

//     public void ShowPopupRewardLayer(PopupRewardLayerData data, System.Action<LayerGroup> onDone = null)
//     {
//         _showPopupRewardLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupRewardLayer);
//         _showPopupRewardLayerData.OnInitData = SetupDataPopupRewardLayer;
//         _showPopupRewardLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupRewardLayerData);
//         return;

//         void SetupDataPopupRewardLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupRewardLayer, out var layerBase))
//             {
//                 var popupRewardLayer = layerBase as UIPopupRewardLayer;
//                 popupRewardLayer?.InitData(data);
//             }
//         }
//     }

//     protected SupportPackLayerData _popupRefillHeartSupportData =
//         new SupportPackLayerData()
//         {
//             SupportPacks =
//                 new List<IAPCategory>()
//                     { IAPCategory.TinyPack, IAPCategory.SmallPack, IAPCategory.GreatPack, IAPCategory.EpicPack }
//         };

//     private ShowLayerGroupData _showPopupMoreHeartLayerData;

//     public void ShowPopupRefillHeartLayer(PopupRefillHeartData data, System.Action<LayerGroup> onDone = null)
//     {
//         if (_showPopupMoreHeartLayerData == null)
//         {
//             _showPopupMoreHeartLayerData =
//                 LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupRefillHeartLayer);
//             _showPopupMoreHeartLayerData.AddLayer(LayerType.SupportPackLayer);
//         }

//         _showPopupMoreHeartLayerData.OnInitData = SetupDataPopupMoreHeartLayer;
//         _showPopupMoreHeartLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupMoreHeartLayerData);
//         return;

//         void SetupDataPopupMoreHeartLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupRefillHeartLayer, out var layerBase))
//             {
//                 var moreHeartLayer = layerBase as UIPopupRefillHeartLayer;
//                 moreHeartLayer?.InitData(data);
//             }

//             if (layerGroup.GetLayerBase(LayerType.SupportPackLayer, out var layerBase2))
//             {
//                 var supportPackLayer = layerBase2 as UISupportPackLayer;
//                 supportPackLayer?.InitData(_popupRefillHeartSupportData);
//             }
//         }
//     }

//     private ShowLayerGroupData _showSettingMainMenuLayerData;

//     public void ShowSettingMainMenuLayer(Action<LayerGroup> onDone = null)
//     {
//         _showSettingMainMenuLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.SettingMainMenuLayer);
//         _showSettingMainMenuLayerData.OnInitData = SetupDataSettingMainMenuLayer;
//         _showSettingMainMenuLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showSettingMainMenuLayerData);
//         return;

//         void SetupDataSettingMainMenuLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.SettingMainMenuLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showSettingLanguageLayerData;

//     public void ShowSettingLanguageLayer(System.Action<LayerGroup> onDone = null)
//     {
//         _showSettingLanguageLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.SettingLanguageLayer);
//         _showSettingLanguageLayerData.OnInitData = SetupDataSettingLanguageLayer;
//         _showSettingLanguageLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showSettingLanguageLayerData);
//         return;

//         void SetupDataSettingLanguageLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.SettingLanguageLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showSettingIngameLayerData;

//     public void ShowSettingInGameLayer(System.Action<LayerGroup> onDone = null)
//     {
//         _showSettingIngameLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.SettingIngameLayer);
//         _showSettingIngameLayerData.OnInitData = SetupDataSettingIngameLayer;
//         _showSettingIngameLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showSettingIngameLayerData);
//         return;

//         void SetupDataSettingIngameLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.SettingIngameLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupRemoveAdsData;

//     public void ShowPopupRemoveAds(System.Action<LayerGroup> onDone = null)
//     {
//         _showPopupRemoveAdsData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupRemoveAds);
//         _showPopupRemoveAdsData.OnInitData = SetupDataPopupRemoveAds;
//         _showPopupRemoveAdsData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupRemoveAdsData);
//         return;

//         void SetupDataPopupRemoveAds(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupRemoveAds, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showBannerSeparateAdsData;

//     public void ShowBannerSeparateAds(LayerGroupType layerGroupType, System.Action<LayerGroup> onDone = null)
//     {
//         _showBannerSeparateAdsData ??= LayerGroupBuilder.Build(layerGroupType, LayerType.BannerSeparateAds);
//         _showBannerSeparateAdsData.OnInitData = SetupDataBannerSeperateAds;
//         _showBannerSeparateAdsData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showBannerSeparateAdsData);
//         return;

//         void SetupDataBannerSeperateAds(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.BannerSeparateAds, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showMaintainInternetLayerData;

//     public void ShowMaintainInternetLayer(LayerGroupType layerGroupType, System.Action<LayerGroup> onDone = null)
//     {
//         _showMaintainInternetLayerData ??= LayerGroupBuilder.Build(layerGroupType, LayerType.MaintainInternetLayer);
//         _showMaintainInternetLayerData.OnInitData = SetupDataMaintainInternetLayer;
//         _showMaintainInternetLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showMaintainInternetLayerData);
//         return;

//         void SetupDataMaintainInternetLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.MaintainInternetLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupStarterPackData;

//     public void ShowPopupStarterPack(Action onClosePopup = null, Action<LayerGroup> onDone = null)
//     {
//         _showPopupStarterPackData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupStarterPack);
//         _showPopupStarterPackData.OnInitData = SetupDataPopupStarterPack;
//         _showPopupStarterPackData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupStarterPackData);
//         return;

//         void SetupDataPopupStarterPack(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupStarterPack, out var layerBase))
//             {
//                 var popup = layerBase as UIPopupStarterPack;
//                 popup?.InitLayer(onClosePopup);
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupDailyGiftData;

//     public void ShowPopupDailyGift(System.Action<LayerGroup> onDone = null)
//     {
//         _showPopupDailyGiftData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupDailyGift);
//         _showPopupDailyGiftData.OnInitData = SetupDataPopupDailyGift;
//         _showPopupDailyGiftData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupDailyGiftData);
//         return;

//         void SetupDataPopupDailyGift(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupDailyGift, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupProfileLayerData;

//     public void ShowPopupProfileLayer(System.Action<LayerGroup> onDone = null)
//     {
//         _showPopupProfileLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupProfileLayer);
//         _showPopupProfileLayerData.OnInitData = SetupDataPopupProfileLayer;
//         _showPopupProfileLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupProfileLayerData);
//         return;

//         void SetupDataPopupProfileLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupProfileLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupEditNickNameData;

//     public void ShowPopupEditNickName(System.Action<LayerGroup> onDone = null)
//     {
//         _showPopupEditNickNameData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupEditNickName);
//         _showPopupEditNickNameData.OnInitData = SetupDataPopupEditNickName;
//         _showPopupEditNickNameData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupEditNickNameData);
//         return;

//         void SetupDataPopupEditNickName(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupEditNickName, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupRatingLayerData;

//     public void ShowPopupRatingLayer(System.Action<LayerGroup> onDone = null)
//     {
//         _showPopupRatingLayerData ??= LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupRatingLayer);
//         _showPopupRatingLayerData.OnInitData = SetupDataPopupRatingLayer;
//         _showPopupRatingLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupRatingLayerData);
//         return;

//         void SetupDataPopupRatingLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupRatingLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showGamePlayLayerData;

//     public void ShowGamePlayLayer(System.Action<LayerGroup> onDone = null)
//     {
//         if (_showGamePlayLayerData == null)
//         {
//             _showGamePlayLayerData = LayerGroupBuilder.Build(LayerGroupType.Root, LayerType.GamePlayLayer);
//             _showGamePlayLayerData.AddLayer(LayerType.MaintainInternetLayer);
//         }

//         _showGamePlayLayerData.OnInitData = SetupDataGamePlayLayer;
//         _showGamePlayLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showGamePlayLayerData);
//         return;

//         void SetupDataGamePlayLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.GamePlayLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showEndGameLayerData;

//     public void ShowEndGameLayer(UIEndGameData data, System.Action<LayerGroup> onDone = null)
//     {
//         if (_showEndGameLayerData == null)
//         {
//             _showEndGameLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.EndGameLayer);
//         }
        
//         if (!data.IsWinMatch)
//         {
//             _showEndGameLayerData.RemoveLayer(LayerType.CurrencyLayer);
//             _showEndGameLayerData.RemoveLayer(LayerType.HeartLayer);
//         }
//         else
//         {
//             _showEndGameLayerData.AddLayer(LayerType.CurrencyLayer);
//             _showEndGameLayerData.AddLayer(LayerType.HeartLayer);
//         }

//         _showEndGameLayerData.OnInitData = SetupDataEndGameLayer;
//         _showEndGameLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showEndGameLayerData);
//         return;

//         void SetupDataEndGameLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.EndGameLayer, out var layerBase))
//             {
//                 var endgame = layerBase as UIEndGameLayer;
//                 endgame?.InitData(data);
//             }

//             if (data.IsWinMatch)
//             {
//                 if (layerGroup.GetLayerBase(LayerType.CurrencyLayer, out var layerBase2))
//                 {
//                     // layerBase2.InitData();
//                 }

//                 if (layerGroup.GetLayerBase(LayerType.HeartLayer, out var layerBase3))
//                 {
//                     layerBase3.InitData();
//                 }
//             }
//         }
//     }

//     private ShowLayerGroupData _showDifficultyWarningLayerData;

//     public void ShowDifficultyWarningLayer(System.Action<LayerGroup> onDone = null)
//     {
//         if (_showDifficultyWarningLayerData == null)
//         {
//             _showDifficultyWarningLayerData =
//                 LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.DifficultyWarningLayer);
//         }

//         _showDifficultyWarningLayerData.OnInitData = SetupDataDifficultyWarningLayer;
//         _showDifficultyWarningLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showDifficultyWarningLayerData);
//         return;

//         void SetupDataDifficultyWarningLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.DifficultyWarningLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showKeepPlayingLayerData;

//     public void ShowKeepPlayingLayer(PopupKeepPlayingData data, System.Action<LayerGroup> onDone = null)
//     {
//         if (_showKeepPlayingLayerData == null)
//         {
//             _showKeepPlayingLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.KeepPlayingLayer);
//         }

//         _showKeepPlayingLayerData.OnInitData = SetupDataKeepPlayingLayer;
//         _showKeepPlayingLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showKeepPlayingLayerData);
//         return;

//         void SetupDataKeepPlayingLayer(LayerGroup layerGroup)
//         {
//             if (layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.KeepPlayingLayer, out var layerBase))
//             {
//                 var popupKeepPlaying = layerBase as UIKeepPlayingLayer;
//                 popupKeepPlaying?.InitData(data);
//             }
//         }
//     }

//     protected SupportPackLayerData _popupPreLoseSupportData =
//         new SupportPackLayerData()
//         {
//             SupportPacks =
//                 new List<IAPCategory>()
//                     { IAPCategory.StarterPack, IAPCategory.HelperPack }
//         };
//     private ShowLayerGroupData _showPopupPreLoseData;
//     public void ShowPopupPreLose(ELoseReason loseReason = ELoseReason.None, System.Action<LayerGroup> onDone = null)
//     {
//         if(_showPopupPreLoseData == null)
//         {
//            _showPopupPreLoseData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupPreLose);
           
//            _showPopupPreLoseData.AddLayer(LayerType.HeartLayer);
//            _showPopupPreLoseData.AddLayer(LayerType.CurrencyLayer);
//            _showPopupPreLoseData.AddLayer(LayerType.SupportPackLayer);
//         }
//         _showPopupPreLoseData.OnInitData = SetupDataPopupPreLose;
//         _showPopupPreLoseData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupPreLoseData);
//         return;

//         void SetupDataPopupPreLose(LayerGroup layerGroup)
//         {
//             if(layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupPreLose, out var layerBase))
//             {
//                 var preloseLayer = layerBase as UIPopupPrelose;
//                 preloseLayer?.InitData(loseReason);
//             }
//             if (layerGroup.GetLayerBase(LayerType.SupportPackLayer, out var layerBase2))
//             {
//                 var supportPackLayer = layerBase2 as UISupportPackLayer;
//                 supportPackLayer?.InitData(_popupPreLoseSupportData);
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupOfferLayerData;
//     public void ShowPopupOfferLayer(PopupOfferLayerData data, System.Action<LayerGroup> onDone = null)
//     {
//         if(_showPopupOfferLayerData == null)
//         {
//            _showPopupOfferLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupOfferLayer);
//            if (!data.IsHideGoldButton) _showPopupOfferLayerData.AddLayer(LayerType.CurrencyLayer);
//         }
//         _showPopupOfferLayerData.OnInitData = SetupDataPopupOfferLayer;
//         _showPopupOfferLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupOfferLayerData);
//         return;

//         void SetupDataPopupOfferLayer(LayerGroup layerGroup)
//         {
//             if(layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupOfferLayer, out var layerBase))
//             {
//                 var offerPackLayer = layerBase as UIPopupOfferUICoreLayer;
//                 offerPackLayer?.SetupLayerData(data);
//             }
//         }
//     }



//     private ShowLayerGroupData _showCheckUpdateLayerData;
//     public void ShowCheckUpdateLayer(System.Action<LayerGroup> onDone = null)
//     {
//         if(_showCheckUpdateLayerData == null)
//         {
//            _showCheckUpdateLayerData = LayerGroupBuilder.Build(LayerGroupType.Custom, LayerType.CheckUpdateLayer);
//         }
//         _showCheckUpdateLayerData.OnInitData = SetupDataCheckUpdateLayer;
//         _showCheckUpdateLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showCheckUpdateLayerData);
//         return;

//         void SetupDataCheckUpdateLayer(LayerGroup layerGroup)
//         {
//             if(layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.CheckUpdateLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupWarningPreloseData;
//     public void ShowPopupWarningPrelose(System.Action<LayerGroup> onDone = null)
//     {
//         if(_showPopupWarningPreloseData == null)
//         {
//            _showPopupWarningPreloseData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupWarningPrelose);
//            _showPopupWarningPreloseData.AddLayer(LayerType.HeartLayer);
//            _showPopupWarningPreloseData.AddLayer(LayerType.CurrencyLayer);
//            _showPopupWarningPreloseData.AddLayer(LayerType.SupportPackLayer);
//         }
//         _showPopupWarningPreloseData.OnInitData = SetupDataPopupWarningPrelose;
//         _showPopupWarningPreloseData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupWarningPreloseData);
//         return;

//         void SetupDataPopupWarningPrelose(LayerGroup layerGroup)
//         {
//             if(layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupWarningPrelose, out var layerBase))
//             {
//                 layerBase.InitData();
                
//             }
//             if (layerGroup.GetLayerBase(LayerType.SupportPackLayer, out var layerBase2))
//             {
//                 var supportPackLayer = layerBase2 as UISupportPackLayer;
//                 supportPackLayer?.InitData(_popupPreLoseSupportData);
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupNewFeatureData;
//     public void ShowPopupNewFeature(MechanicType mechanicType, System.Action<LayerGroup> onDone = null)
//     {
//         if(_showPopupNewFeatureData == null)
//         {
//            _showPopupNewFeatureData = LayerGroupBuilder.Build(LayerGroupType.FullScreen, LayerType.PopupNewFeature);
//         }
//         _showPopupNewFeatureData.OnInitData = SetupDataPopupNewFeature;
//         _showPopupNewFeatureData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupNewFeatureData);
//         return;

//         void SetupDataPopupNewFeature(LayerGroup layerGroup)
//         {
//             if(layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupNewFeature, out var layerBase))
//             {
//                 var layer = layerBase as UIPopupNewFeature;
//                 layer?.InitData(mechanicType);
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showGamePlayTutorialLayerData;
//     public void ShowGamePlayTutorialLayer(Carrier targetCarrier = null, System.Action<LayerGroup> onDone = null, bool isFirstTut = true)
//     {
//         if(_showGamePlayTutorialLayerData == null)
//         {
//            _showGamePlayTutorialLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.GamePlayTutorialLayer);
//         }
//         _showGamePlayTutorialLayerData.OnInitData = SetupDataGamePlayTutorialLayer;
//         _showGamePlayTutorialLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showGamePlayTutorialLayerData);
//         return;

//         void SetupDataGamePlayTutorialLayer(LayerGroup layerGroup)
//         {
//             if(layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.GamePlayTutorialLayer, out var layerBase))
//             {
//                 var layer = layerBase as UIGameplayTutorialLayer;
//                 layerBase.InitData();
//                 if (layer)
//                 {
//                     if (isFirstTut&& targetCarrier)
//                         layer.StartTutWithWorldPosition(targetCarrier.transform.position);
//                     else layer.StartFromThirdStep();
//                 }
//             }
//         }
//     }

//     private ShowLayerGroupData _showUndoBoosterTutorialLayerData;
//     public void ShowUndoBoosterTutorialLayer(System.Action<LayerGroup> onDone = null)
//     {
//         if(_showUndoBoosterTutorialLayerData == null)
//         {
//            _showUndoBoosterTutorialLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.UndoBoosterTutorialLayer);
//         }
//         _showUndoBoosterTutorialLayerData.OnInitData = SetupDataUndoBoosterTutorialLayer;
//         _showUndoBoosterTutorialLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showUndoBoosterTutorialLayerData);
//         return;

//         void SetupDataUndoBoosterTutorialLayer(LayerGroup layerGroup)
//         {
//             if(layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.UndoBoosterTutorialLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showExtraSlotBoosterLayerData;
//     public void ShowExtraSlotBoosterLayer(System.Action<LayerGroup> onDone = null)
//     {
//         if(_showExtraSlotBoosterLayerData == null)
//         {
//            _showExtraSlotBoosterLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.ExtraSlotBoosterLayer);
//         }
//         _showExtraSlotBoosterLayerData.OnInitData = SetupDataExtraSlotBoosterLayer;
//         _showExtraSlotBoosterLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showExtraSlotBoosterLayerData);
//         return;

//         void SetupDataExtraSlotBoosterLayer(LayerGroup layerGroup)
//         {
//             if(layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.ExtraSlotBoosterLayer, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showClawBoosterTutorialData;
//     public void ShowClawBoosterTutorial(System.Action<LayerGroup> onDone = null)
//     {
//         if(_showClawBoosterTutorialData == null)
//         {
//            _showClawBoosterTutorialData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.ClawBoosterTutorial);
//         }
//         _showClawBoosterTutorialData.OnInitData = SetupDataClawBoosterTutorial;
//         _showClawBoosterTutorialData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showClawBoosterTutorialData);
//         return;

//         void SetupDataClawBoosterTutorial(LayerGroup layerGroup)
//         {
//             if(layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.ClawBoosterTutorial, out var layerBase))
//             {
//                 layerBase.InitData();
//             }
//         }
//     }

//     private ShowLayerGroupData _showPopupRewardClaimedLayerData;
//     public void ShowPopupRewardClaimedLayer(PopupRewardLayerData data, System.Action<LayerGroup> onDone = null)
//     {
//         if(_showPopupRewardClaimedLayerData == null)
//         {
//            _showPopupRewardClaimedLayerData = LayerGroupBuilder.Build(LayerGroupType.Popup, LayerType.PopupRewardClaimedLayer);
//         }
//         _showPopupRewardClaimedLayerData.OnInitData = SetupDataPopupRewardClaimedLayer;
//         _showPopupRewardClaimedLayerData.OnShowComplete = onDone;
//         ShowGroupLayerAsync(_showPopupRewardClaimedLayerData);
//         return;

//         void SetupDataPopupRewardClaimedLayer(LayerGroup layerGroup)
//         {
//             if(layerGroup == null) return;
//             if (layerGroup.GetLayerBase(LayerType.PopupRewardClaimedLayer, out var layerBase))
//             {
//                 var popupRewardLayer = layerBase as UIPopupRewardClaimedLayer;
//                 popupRewardLayer?.InitData(data);
                
//             }
//         }
//     }
// }
