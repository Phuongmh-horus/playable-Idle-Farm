using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SupportPackConfig", menuName = "ScriptableObjects/SupportPackConfig")]
public class SupportPackConfigSO : ScriptableObject
{
    [SerializeField] private List<SupportPackEntry> supportPacks = new List<SupportPackEntry>();

    private Dictionary<IAPCategory, SupportPackBase> _packCache;

    private void OnEnable()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        _packCache = new Dictionary<IAPCategory, SupportPackBase>();
        foreach (var entry in supportPacks)
        {
            if (entry.PackPrefab != null && !_packCache.ContainsKey(entry.Category))
            {
                _packCache[entry.Category] = entry.PackPrefab;
            }
        }
    }

    public SupportPackBase GetSupportPack(IAPCategory category)
    {
        if (_packCache == null || _packCache.Count == 0)
        {
            BuildCache();
        }

        return _packCache.TryGetValue(category, out var prefab) ? prefab : null;
    }

    public List<SupportPackEntry> GetAllSupportPacks()
    {
        return supportPacks;
    }

    public void RefreshCache()
    {
        BuildCache();
    }
}

[Serializable]
public class SupportPackEntry
{
    public IAPCategory Category;
    public SupportPackBase PackPrefab;
}
