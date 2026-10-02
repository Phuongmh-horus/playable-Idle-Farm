using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

[Serializable]
public class IAPProduct
{
    public IAPCategory Category;
    public string Name;
    public string ProductID;
    public IAPProductType ProductType;

    public List<RewardItem> Rewards;
    public float RealPrice; // usd
    public float SaleRate;
    public IAPCategory SaleCategory = IAPCategory.None;
    public ProductType StoreProductType = UnityEngine.Purchasing.ProductType.Consumable;
}

public enum IAPProductType
{
    RemoveAds = 1,
    Cash = 2,
    HelperPack = 3,
    StarterPack = 4,
    BundlePack = 5,
    AdTicketPack = 6,
}

public enum IAPCategory
{
    None = 0,
    //RemoveAds
    RemoveAds = 101,
    RemoveAdsBundle = 102,

    //Cash
    FreeCash = 201,
    Cash300 = 202,
    Cash500 = 203,
    Cash900 = 204,
    Cash2000 = 205,
    Cash5000 = 206,
    Cash10000 = 207,

    //Pack bundle
    StarterPack = 301,
    HelperPack = 302,
    TinyPack = 303,
    SmallPack = 304,
    GreatPack = 305,
    EpicPack = 306,
    
    //AdTicket
    AdTicket1 = 401,
    AdTicket2 = 402,
    AdTicket3 = 403,
    AdTicket4 = 404,
    AdTicket5 = 405,
    AdTicket6 = 406,
    
    //Gem
    FreeGem = 501,
    Gem2 = 502,
    Gem3 = 503,
    Gem4 = 504,
    Gem5 = 505,
    Gem6 = 506,
    Gem7 = 507,
}