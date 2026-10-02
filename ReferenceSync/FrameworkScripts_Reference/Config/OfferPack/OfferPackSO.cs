using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OfferPackData", menuName = "Data/Offer Pack Data")]
public class OfferPackSO : ScriptableObject
{
    [Header("Danh sách các gói ưu đãi")]
    [SerializeField] private List<OfferPackData> offerPacks = new List<OfferPackData>();

    private Dictionary<IAPCategory, OfferPackData> _categoryCache;

    /// <summary>
    /// Lấy danh sách các gói ưu đãi phù hợp với điều kiện hiện tại
    /// </summary>
    /// <param name="currentLevel">Level hiện tại của người chơi</param>
    /// <param name="currentSceneType">Loại scene hiện tại (MainMenu/InGame)</param>
    /// <returns>Danh sách các gói phù hợp</returns>
    public List<OfferPackData> GetValidOfferPacks()
    {
        List<OfferPackData> validPacks = new List<OfferPackData>();

        foreach (var pack in offerPacks)
        {
            if (pack.CanDisplay())
            {
                validPacks.Add(pack);
            }
        }
        validPacks.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        return validPacks;
    }

    public OfferPackData GetOfferPackDataByCategory(IAPCategory category)
    {
        if (_categoryCache == null)
        {
            BuildCategoryCache();
        }

        _categoryCache.TryGetValue(category, out var pack);
        return pack;
    }

    private void BuildCategoryCache()
    {
        _categoryCache = new Dictionary<IAPCategory, OfferPackData>();
        foreach (var pack in offerPacks)
        {
            if (!_categoryCache.ContainsKey(pack.IAPCategory))
            {
                _categoryCache[pack.IAPCategory] = pack;
            }
        }
    }

    private void OnValidate()
    {
        _categoryCache = null;
    }
}
