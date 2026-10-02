using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DailyGiftConfig", menuName = "ScriptableObjects/DailyGiftConfig", order = 2)]
public class DailyGiftConfigSO : ScriptableObject
{
    public int LevelOpen = 1;
    [SerializeField] private List<WeeklyReward> weeklyRewards = new ();

    // Cache dictionaries for fast lookup
    private Dictionary<int, WeeklyReward> _weeklyRewardCache;
    private Dictionary<(int weekId, int dayNumber), DailyReward> _dailyRewardCache;
    private bool _isInitialized;

    /// <summary>
    /// Initialize cache dictionaries
    /// </summary>
    private void Initialize()
    {
        if (_isInitialized) return;

        _weeklyRewardCache = new Dictionary<int, WeeklyReward>();
        _dailyRewardCache = new Dictionary<(int, int), DailyReward>();

        foreach (var weeklyReward in weeklyRewards)
        {
            _weeklyRewardCache[weeklyReward.ID] = weeklyReward;

            foreach (var dailyReward in weeklyReward.DailyRewards)
            {
                _dailyRewardCache[(weeklyReward.ID, dailyReward.DayNumber)] = dailyReward;
            }
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Get all weekly rewards
    /// </summary>
    public List<WeeklyReward> GetAllWeeklyRewards()
    {
        return weeklyRewards;
    }

    /// <summary>
    /// Get weekly reward by ID
    /// </summary>
    public WeeklyReward GetWeeklyReward(int weekId)
    {
        Initialize();
        return _weeklyRewardCache.TryGetValue(weekId, out var reward) ? reward : null;
    }

    /// <summary>
    /// Get daily reward by week ID and day number
    /// </summary>
    public DailyReward GetDailyReward(int weekId, int dayNumber)
    {
        Initialize();
        return _dailyRewardCache.TryGetValue((weekId, dayNumber), out var reward) ? reward : null;
    }

    /// <summary>
    /// Get reward items for a specific week and day
    /// </summary>
    public List<RewardItem> GetRewardItems(int weekId, int dayNumber)
    {
        var dailyReward = GetDailyReward(weekId, dayNumber);
        return dailyReward?.RewardItems ?? new List<RewardItem>();
    }

    /// <summary>
    /// Get all daily rewards for a specific week
    /// </summary>
    public List<DailyReward> GetDailyRewardsForWeek(int weekId)
    {
        var weeklyReward = GetWeeklyReward(weekId);
        return weeklyReward?.DailyRewards ?? new List<DailyReward>();
    }

    /// <summary>
    /// Get total number of weeks configured
    /// </summary>
    public int GetTotalWeeks()
    {
        return weeklyRewards.Count;
    }

    /// <summary>
    /// Check if a specific week and day exists
    /// </summary>
    public bool HasDailyReward(int weekId, int dayNumber)
    {
        Initialize();
        return _dailyRewardCache.ContainsKey((weekId, dayNumber));
    }

    /// <summary>
    /// Reset cache (useful when data changes in editor)
    /// </summary>
    public void ResetCache()
    {
        _isInitialized = false;
        _weeklyRewardCache?.Clear();
        _dailyRewardCache?.Clear();
    }

    private void OnValidate()
    {
        // Reset cache when data changes in editor
        ResetCache();
    }
}

[Serializable]
public class WeeklyReward
{
    public int ID;
    public List<DailyReward> DailyRewards = new();
}

[Serializable]
public class DailyReward
{
    [Header("Day Configuration")]
    [Range(1, 7)]
    [Tooltip("Day number (1-7)")]
    public int DayNumber;

    [Header("Rewards")]
    [Tooltip("List of rewards for this day")]
    public List<RewardItem> RewardItems = new ();
}
