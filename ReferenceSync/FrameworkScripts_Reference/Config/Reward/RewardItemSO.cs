using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RewardItemSO", menuName = "ScriptableObjects/RewardItemSO", order = 1)]
public class RewardItemSO : ScriptableObject
{
    [SerializeField] private List<RewardItemSOData> rewardItems;

    // Cache dictionary for O(1) lookup
    private Dictionary<(RewardType rewardType, int itemId), RewardItemSOData> _rewardItemCache;

    /// <summary>
    /// Get reward item by type and id with optimized O(1) lookup
    /// </summary>
    public RewardItemSOData GetRewardItem(RewardType rewardType, int itemId = 0)
    {
        // Lazy initialization - build cache only when needed
        if (_rewardItemCache == null)
        {
            BuildCache();
        }

        _rewardItemCache ??= new();
        // O(1) lookup using Dictionary
        _rewardItemCache.TryGetValue((rewardType, itemId), out var rewardItem);
        return rewardItem;
    }

    /// <summary>
    /// Build cache dictionary from list. Call this if rewardItems list is modified at runtime.
    /// </summary>
    public void RebuildCache()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        _rewardItemCache = new Dictionary<(RewardType, int), RewardItemSOData>();

        if (rewardItems == null) return;

        foreach (var item in rewardItems)
        {
            var key = (item.RewardType, item.ItemId);

            // Handle duplicates - keep first occurrence
            if (!_rewardItemCache.ContainsKey(key))
            {
                _rewardItemCache[key] = item;
            }
            else
            {
                Debug.LogWarning($"[RewardItemSO] Duplicate reward item found: Type={item.RewardType}, ID={item.ItemId}. Keeping first occurrence.");
            }
        }
    }
}
[Serializable]
public class RewardItemSOData
{
    public int ItemId;
    public RewardType RewardType;
    public Sprite Icon;
}
