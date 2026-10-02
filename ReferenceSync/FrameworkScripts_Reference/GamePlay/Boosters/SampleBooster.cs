using System;
using System.Collections;
using System.Collections.Generic;
using AdMod;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class SampleBooster : BaseBooster
{
    [SerializeField]
    private Canvas          boosterCanvas;
    [SerializeField]
    private Sprite            sampleIcon;
    [SerializeField]
    private TextMeshProUGUI   sampleAmount;
    [SerializeField]
    private GameObject        plusIcon;
    [SerializeField]
    private ParticleSystem boosterParticles;

    private bool _isInteractable = false;
    private int  _levelOpenSampleBooster = 5;


    private void Start()
    {
        _levelOpenSampleBooster = BoosterData.LevelOpen;
        sampleIcon = BoosterData.BoosterIcon;
        SetupAmount(RewardType.SampleBooster);
        _isInteractable                        =  true;
        GameEventBus.OnBoosterDataChanged += SetupAmount;
        GameEventBus.OnLoadLevelDone                += ChangeLockStateBooster;
        GameEventBus.OnLanguageChanged.Register(ChangeLockText);
        GameEventBus.OnUseSampleBooster += OnUseSampleBooster;
        ChangeLockStateBooster();
    }
    
    protected void OnDestroy()
    {
        GameEventBus.OnBoosterDataChanged -= SetupAmount;
        GameEventBus.OnLanguageChanged.UnRegister(ChangeLockText);
        GameEventBus.OnLoadLevelDone -= ChangeLockStateBooster;
        GameEventBus.OnUseSampleBooster -= OnUseSampleBooster;
    }
    
    public void OnLockButtonClicked()
    {
        LayerManager.Instance.ShowPopupFeedbackLayer(TextUtility.GetI2("ui_popup_locked_booster"));
    }

    private void OnUseSampleBooster()
    {
        boosterParticles?.Play();
    }

    public override void ChangeLockStateBooster()
    {
        var currentLevel = DataManager.PlayerData.LevelDisplay;
        boosterLockGO.SetActive(currentLevel < _levelOpenSampleBooster);
        boosterGO.SetActive(currentLevel >= _levelOpenSampleBooster);
        ChangeLockText();
    }
    
    private void ChangeLockText(string language="")
    {
        var levelText = TextUtility.GetI2("level");
        if (levelText == "level") levelText = "Level";
        boosterLockText.text = levelText + " " + _levelOpenSampleBooster;
    }
    

    public override void OnButtonClick()
    {
        if (DataManager.PlayerData.BoosterData.SampleBooster <= 0)
        {
            BuyBooster();
            return;
        }

        ExecuteBooster();
    }

    public override void ExecuteBooster()
    {
        button.interactable = false;
        //TO DO: Use Booster
        DataManager.ChangeBooster(RewardType.SampleBooster, -1);
        button.interactable = true;
    }
    public override void BuyBooster()
    {
        LayerManager
            .Instance
            .ShowPopupOfferLayer(new PopupOfferLayerData()
                {
                    ItemType = RewardType.SampleBooster,
                    Title = TextUtility.GetI2("ui_buy_booster_title"),
                    Content = TextUtility.GetI2("ui_booster_sample_name"),
                    CashAction = () => { OnBuyBoosterByCash(); },
                    AdsAction = () => { OnBuyBoosterByAds(); },
                    Price = new PriceType()
                    {
                        Cash = BoosterData.BoosterCost,
                        Ads = 1
                    },
                }
            );
        GameEventBus.OnShowPopupOffer?.Invoke();
    }

    protected override void OnBuyBoosterByCash()
    {
        if (DataManager.PlayerData.Cash < BoosterData.BoosterCost)
        {
            LayerManager.Instance.ShowShopInGameLayer();
            return;
        }
        DataManager.ChangeCash(-BoosterData.BoosterCost);
        DataManager.ChangeBooster(RewardType.SampleBooster,1);
        TrackingSpendCash();
        GameEventBus.OnReceiveBooster?.Invoke(LayerManager.Instance.CloseLastLayerGroup);
    }
    
    private void TrackingSpendCash()
    {
        UserBehaviorTracker.SendSpendVirtualCurrencyTracking("Cash",BoosterData.BoosterCost, DataManager.PlayerData.Cash + BoosterData.BoosterCost,
            DataManager.PlayerData.Cash, "buy_booster_" + BoosterData.BoosterType);
    }

    protected override void OnBuyBoosterByAds()
    {
#if NO_ADS
        OnVideoRewarded(1);
        return;
#endif
        if (GameConfig.NoInternet())
        {
            LayerManager
               .Instance
               .ShowPopupNoInternet();
            return;
        }
        if (!AdmobManager.Instance.IsRewardedVideoLoaded())
        {
            LayerManager.Instance.ShowPopupNoAds();
            return;
        }
        if (AdmobManager.Instance != null)
        {
            AdmobManager.Instance.OnVideoRewarded -= OnVideoRewarded;
            AdmobManager.Instance.OnVideoRewarded += OnVideoRewarded;
            AdmobManager.Instance.RequestShowRewardedVideo(AdsRewardedType.ADS_FREE_BOOSTER);
        }
    }

    private void OnVideoRewarded(double revenue)
    {
        if (AdmobManager.Instance != null) AdmobManager.Instance.OnVideoRewarded -= OnVideoRewarded;
        DataManager.ChangeBooster(RewardType.SampleBooster,1);
    }
    private void OnLockVacuumCleaner(bool isLock) { IsInteractable(!isLock); }

    private void IsInteractable(bool isInteractable)
    {
        sampleAmount.color = isInteractable ? Color.white : Color.gray;
        _isInteractable = isInteractable;
    }

    private void SetupAmount(RewardType type)
    {
        if (sampleAmount == null || type != RewardType.SampleBooster) return;
        DataManager.PlayerData.BoosterData ??= new BoosterData();
        sampleAmount.text = DataManager.PlayerData.BoosterData.SampleBooster.ToString();
        plusIcon.SetActive(DataManager.PlayerData.BoosterData.SampleBooster <= 0);
        sampleAmount.gameObject.SetActive(DataManager.PlayerData.BoosterData.SampleBooster > 0);
    }
}
