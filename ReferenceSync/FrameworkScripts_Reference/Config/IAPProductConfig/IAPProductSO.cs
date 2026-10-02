using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "IAPProductSO", menuName = "ScriptableObjects/IAPProductSO", order = 1)]
public class IAPProductSO : ScriptableObject
{
    public List<IAPProduct> IAPProductList;

    private Dictionary<string, IAPProduct> _iapProductDictionary;

    public Dictionary<string, IAPProduct> IAPProductDictionary
    {
        get
        {
            if (_iapProductDictionary == null || _iapProductDictionary.Count == 0)
            {
                _iapProductDictionary = new Dictionary<string, IAPProduct>();
                foreach (var iapProduct in IAPProductList)
                {
                    _iapProductDictionary[iapProduct.ProductID] = iapProduct;
                }
            }
            return _iapProductDictionary;
        }
    }

    public IAPProduct GetIAPProductByID(string id)
    {
        return IAPProductDictionary.TryGetValue(id, out var iapProduct) ? iapProduct : null;
    }

    private Dictionary<IAPCategory, IAPProduct> _iapCategoryDictionary;

    public Dictionary<IAPCategory, IAPProduct> IAPCategoryDictionary
    {
        get
        {
            if (_iapCategoryDictionary == null || _iapCategoryDictionary.Count == 0)
            {
                _iapCategoryDictionary = new Dictionary<IAPCategory, IAPProduct>();
                foreach (var iapCategory in IAPProductList)
                {
                    _iapCategoryDictionary[iapCategory.Category] = iapCategory;
                }
            }
            return _iapCategoryDictionary;
        }
    }

    public IAPProduct GetIAPProductByCategory(IAPCategory category)
    {
        return IAPCategoryDictionary.TryGetValue(category, out var iapProduct) ? iapProduct : null;
    }

    public IAPProduct GetIAPProductByProductID(string id)
    {
        return IAPProductList.FirstOrDefault(x => x.ProductID == id);
    }
}
