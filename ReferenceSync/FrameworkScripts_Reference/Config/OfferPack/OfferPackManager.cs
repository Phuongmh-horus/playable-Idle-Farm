using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OfferPackManager : MonoSingleton<OfferPackManager>
{
    [SerializeField] private OfferPackSO offerPackSO;

    private void OnValidate()
    {
        if (offerPackSO == null)
        {
            var configs = Resources.FindObjectsOfTypeAll<OfferPackSO>();
            if(configs.Length > 0) offerPackSO = configs[0];
        }
    }

    public OfferPackData GetOfferPackData(IAPCategory category)
    {
        return offerPackSO?.GetOfferPackDataByCategory(category);
    }

    public List<OfferPackData> GetValidOfferPacks()
    {
        var result = offerPackSO?.GetValidOfferPacks();
        return result;
    }

    public bool CanShowPackBundle(IAPCategory category)
    {
        var pack = offerPackSO?.GetOfferPackDataByCategory(category);
        if (pack == null) return false;
        return pack.CanDisplay();
    }
    public bool IsShowLastTime(IAPCategory category)
    {
        var pack = offerPackSO?.GetOfferPackDataByCategory(category);
        if (pack == null) return false;
        return pack.IsShowLastTime();
    }

    private Queue<OfferPackData> OfferValidPacks = new();
    private List<OfferPackData> ShownOfferPacks = new List<OfferPackData>();
    public void ShowOfferPack(bool isReset = true)
    {
        if (isReset) ShownOfferPacks.Clear();
        OfferValidPacks = new Queue<OfferPackData>(GetValidOfferPacks().Except(ShownOfferPacks));
        if (OfferValidPacks.Count <= 0)
        {
            GameEventBus.OnOfferPackClosed?.Invoke();
            return;
        }

        var offerPack = OfferValidPacks.Dequeue();
        var packData = GameEventBus.GetIAPProductByCategory?.Invoke(offerPack.IAPCategory);
        if (packData == null)
        {
            Debug.LogError($"OfferPackManager: Không tìm thấy gói ưu đãi với ID {offerPack.IAPCategory}");
            return;
        }
        switch (packData.ProductType)
        {
            case IAPProductType.StarterPack:
                if (DataManager.GetPackAppearanceDataInfo(IAPCategory.StarterPack) != null)
                {
                    //UserBehaviorTracker.SendTrackingShowPopupStarter();
                }
                else
                {
                    DataManager.SetPackCooldownStartTime(IAPCategory.StarterPack);
                }
                LayerManager.Instance.ShowPopupStarterPack(()=>ShowOfferPack(false));
                break;
        }
        ShownOfferPacks.Add(offerPack);
        GameEventBus.OnOfferPackOpened?.Invoke(offerPack.IAPCategory);
        DataManager.TrackPackAppearance(offerPack.IAPCategory);
    }
}
