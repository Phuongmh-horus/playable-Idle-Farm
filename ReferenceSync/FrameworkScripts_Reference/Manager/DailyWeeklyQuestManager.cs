using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum DailyWeeklyQuestProgressState
{
    InProgress = 0,
    Claimable = 1,
    Claimed = 2
}

public class DailyWeeklyQuestManager : MonoSingleton<DailyWeeklyQuestManager>
{
    private const float OnlineSaveIntervalSeconds = 10f;

    private float _onlineSecondAccumulator;
    private float _onlineSaveAccumulator;
    private float _resetCheckAccumulator;
    private bool _hasApplicationFocus = true;
    private bool _isApplicationPaused;
    private bool _dirty;
    private bool _showingMilestoneReward;

    public DailyWeeklyQuestConfig Config => ConfigHolder.Instance?.DailyWeeklyQuestConfig;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<DailyWeeklyQuestManager>() != null) return;
        new GameObject(nameof(DailyWeeklyQuestManager)).AddComponent<DailyWeeklyQuestManager>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        DontDestroyOnLoad(gameObject);
        SubscribeEvents();
        EnsureAllPeriodsCurrent(DateTime.UtcNow, false);
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        GameEventBus.OnCropHarvestedEvent += HandleCropHarvested;
        GameEventBus.OnCropUpgradedEvent += HandleCropUpgraded;
        GameEventBus.OnVerifiedIAPPurchased += HandleVerifiedIAPPurchased;
        GameEventBus.OnPlayerDataReplaced += HandlePlayerDataReplaced;
    }

    private void UnsubscribeEvents()
    {
        GameEventBus.OnCropHarvestedEvent -= HandleCropHarvested;
        GameEventBus.OnCropUpgradedEvent -= HandleCropUpgraded;
        GameEventBus.OnVerifiedIAPPurchased -= HandleVerifiedIAPPurchased;
        GameEventBus.OnPlayerDataReplaced -= HandlePlayerDataReplaced;
    }

    private void Update()
    {
        if (DataManager.PlayerData == null) return;

        var delta = Time.unscaledDeltaTime;
        _resetCheckAccumulator += delta;
        if (_resetCheckAccumulator >= 1f)
        {
            _resetCheckAccumulator %= 1f;
            EnsureAllPeriodsCurrent(DateTime.UtcNow, true);
        }

        if (!_hasApplicationFocus || _isApplicationPaused) return;

        _onlineSecondAccumulator += delta;
        if (_onlineSecondAccumulator >= 1f)
        {
            long elapsedSeconds = Mathf.FloorToInt(_onlineSecondAccumulator);
            _onlineSecondAccumulator -= elapsedSeconds;
            AdvanceQuest(DailyWeeklyQuestType.OnlineDuration, elapsedSeconds, false);
        }

        if (!_dirty) return;
        _onlineSaveAccumulator += delta;
        if (_onlineSaveAccumulator >= OnlineSaveIntervalSeconds)
            FlushSave();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        _hasApplicationFocus = hasFocus;
        if (hasFocus)
            EnsureAllPeriodsCurrent(DateTime.UtcNow, true);
        else
            FlushSave();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        _isApplicationPaused = pauseStatus;
        if (pauseStatus)
            FlushSave();
        else
            EnsureAllPeriodsCurrent(DateTime.UtcNow, true);
    }

    private void OnApplicationQuit()
    {
        FlushSave();
    }

    private void HandleCropHarvested(int plantId, int count)
    {
        if (count > 0)
            AdvanceQuest(DailyWeeklyQuestType.HarvestAnyFruit, count, true);
    }

    private void HandleCropUpgraded(int plantId, int level)
    {
        AdvanceQuest(DailyWeeklyQuestType.UpgradeAnyPlant, 1, true);
    }

    private void HandleVerifiedIAPPurchased(IAPProduct product, string transactionId)
    {
        AdvanceQuest(DailyWeeklyQuestType.PurchaseAnyIAP, 1, true);
    }

    private void HandlePlayerDataReplaced()
    {
        _dirty = false;
        _onlineSecondAccumulator = 0f;
        EnsureAllPeriodsCurrent(DateTime.UtcNow, true);
        NotifyUpdated(QuestPeriodType.Daily);
        NotifyUpdated(QuestPeriodType.Weekly);
    }

    public void EnsureCurrentCycles()
    {
        EnsureAllPeriodsCurrent(DateTime.UtcNow, true);
    }

    public IReadOnlyList<DailyWeeklyQuestConfigData> GetQuests(QuestPeriodType period)
    {
        EnsurePeriodCurrent(period, DateTime.UtcNow, true);
        return Config?.GetQuests(period) ?? Array.Empty<DailyWeeklyQuestConfigData>();
    }

    public long GetProgress(QuestPeriodType period, string questId)
    {
        EnsurePeriodCurrent(period, DateTime.UtcNow, true);
        var progress = FindProgress(GetPeriodSave(period), questId);
        return progress?.progress ?? 0;
    }

    public int GetStars(QuestPeriodType period)
    {
        EnsurePeriodCurrent(period, DateTime.UtcNow, true);
        return GetPeriodSave(period)?.totalStars ?? 0;
    }

    public int GetMaxStars()
    {
        return Mathf.Max(1, Config?.maxStars ?? 100);
    }

    public DailyWeeklyQuestProgressState GetQuestState(QuestPeriodType period, DailyWeeklyQuestConfigData quest)
    {
        if (quest == null) return DailyWeeklyQuestProgressState.InProgress;
        var progress = FindProgress(GetPeriodSave(period), quest.id);
        if (progress != null && progress.claimed) return DailyWeeklyQuestProgressState.Claimed;
        return (progress?.progress ?? 0) >= Math.Max(1, quest.targetAmount)
            ? DailyWeeklyQuestProgressState.Claimable
            : DailyWeeklyQuestProgressState.InProgress;
    }

    public DateTime GetNextResetUtc(QuestPeriodType period)
    {
        var now = DateTime.UtcNow;
        if (period == QuestPeriodType.Daily)
            return now.Date.AddDays(1);

        int daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
        DateTime currentMonday = now.Date.AddDays(-daysSinceMonday);
        return currentMonday.AddDays(7);
    }

    public bool ClaimTask(QuestPeriodType period, string questId)
    {
        EnsurePeriodCurrent(period, DateTime.UtcNow, true);

        var quest = Config?.GetQuest(period, questId);
        var periodSave = GetPeriodSave(period);
        if (quest == null || periodSave == null) return false;

        var progress = FindProgress(periodSave, quest.id);
        if (progress == null || progress.claimed || progress.progress < Math.Max(1, quest.targetAmount))
            return false;

        progress.claimed = true;
        periodSave.totalStars = Mathf.Clamp(
            periodSave.totalStars + Mathf.Max(0, quest.starReward),
            0,
            GetMaxStars());

        _dirty = true;
        FlushSave();
        NotifyUpdated(period);
        TryShowPendingMilestoneRewards(period);
        return true;
    }

    public void TryShowPendingMilestoneRewards(QuestPeriodType period)
    {
        if (_showingMilestoneReward) return;
        EnsurePeriodCurrent(period, DateTime.UtcNow, true);

        var periodSave = GetPeriodSave(period);
        var milestones = Config?.GetMilestones(period);
        if (periodSave == null || milestones == null) return;

        DailyWeeklyQuestMilestoneData next = null;
        for (int i = 0; i < milestones.Count; i++)
        {
            var milestone = milestones[i];
            if (milestone == null || milestone.requiredStars <= 0) continue;
            if (periodSave.totalStars < milestone.requiredStars) continue;
            if (periodSave.claimedMilestones.Contains(milestone.requiredStars)) continue;
            if (next == null || milestone.requiredStars < next.requiredStars)
                next = milestone;
        }

        if (next == null) return;

        periodSave.claimedMilestones.Add(next.requiredStars);
        _dirty = true;
        FlushSave();
        NotifyUpdated(period);

        if (next.rewards == null || next.rewards.Count == 0)
        {
            TryShowPendingMilestoneRewards(period);
            return;
        }

        _showingMilestoneReward = true;
        string source = period == QuestPeriodType.Daily
            ? $"daily_quest_{next.requiredStars}"
            : $"weekly_quest_{next.requiredStars}";

        DataManager.ClaimReward(next.rewards, () =>
        {
            _showingMilestoneReward = false;
            TryShowPendingMilestoneRewards(period);
        }, source);
    }

    public bool IsMilestoneClaimed(QuestPeriodType period, int requiredStars)
    {
        var save = GetPeriodSave(period);
        return save?.claimedMilestones?.Contains(requiredStars) == true;
    }

    private void AdvanceQuest(DailyWeeklyQuestType type, long amount, bool saveImmediately)
    {
        if (amount <= 0 || type == DailyWeeklyQuestType.None || Config == null) return;
        EnsureAllPeriodsCurrent(DateTime.UtcNow, true);

        bool dailyChanged = AdvancePeriod(QuestPeriodType.Daily, type, amount);
        bool weeklyChanged = AdvancePeriod(QuestPeriodType.Weekly, type, amount);
        if (!dailyChanged && !weeklyChanged) return;

        _dirty = true;
        if (saveImmediately)
            FlushSave();

        if (dailyChanged) NotifyUpdated(QuestPeriodType.Daily);
        if (weeklyChanged) NotifyUpdated(QuestPeriodType.Weekly);
    }

    private bool AdvancePeriod(QuestPeriodType period, DailyWeeklyQuestType type, long amount)
    {
        var quests = Config.GetQuests(period);
        var save = GetPeriodSave(period);
        if (quests == null || save == null) return false;

        bool changed = false;
        for (int i = 0; i < quests.Count; i++)
        {
            var quest = quests[i];
            if (quest == null || quest.questType != type || string.IsNullOrWhiteSpace(quest.id)) continue;

            var progress = GetOrCreateProgress(save, quest.id);
            long target = Math.Max(1, quest.targetAmount);
            if (progress.claimed || progress.progress >= target) continue;

            progress.progress = Math.Min(target, progress.progress + amount);
            changed = true;
        }
        return changed;
    }

    private bool EnsureAllPeriodsCurrent(DateTime utcNow, bool saveIfReset)
    {
        bool dailyReset = EnsurePeriodCurrent(QuestPeriodType.Daily, utcNow, false);
        bool weeklyReset = EnsurePeriodCurrent(QuestPeriodType.Weekly, utcNow, false);
        if (!dailyReset && !weeklyReset) return false;

        _dirty = true;
        if (saveIfReset) FlushSave();
        return true;
    }

    private bool EnsurePeriodCurrent(QuestPeriodType period, DateTime utcNow, bool saveIfReset)
    {
        var periodSave = GetPeriodSave(period);
        if (periodSave == null) return false;

        string currentKey = GetCycleKey(period, utcNow);
        if (periodSave.cycleKey == currentKey) return false;

        periodSave.cycleKey = currentKey;
        periodSave.totalStars = 0;
        periodSave.taskProgress.Clear();
        periodSave.claimedMilestones.Clear();
        _dirty = true;

        if (saveIfReset) FlushSave();
        NotifyUpdated(period);
        return true;
    }

    private static string GetCycleKey(QuestPeriodType period, DateTime utcNow)
    {
        if (period == QuestPeriodType.Daily)
            return utcNow.Date.ToString("yyyy-MM-dd");

        int daysSinceMonday = ((int)utcNow.DayOfWeek + 6) % 7;
        return utcNow.Date.AddDays(-daysSinceMonday).ToString("yyyy-MM-dd");
    }

    private static QuestPeriodSaveData GetPeriodSave(QuestPeriodType period)
    {
        var playerData = DataManager.PlayerData;
        if (playerData == null) return null;

        playerData.DailyWeeklyQuestData ??= new DailyWeeklyQuestSaveData();
        playerData.DailyWeeklyQuestData.daily ??= new QuestPeriodSaveData();
        playerData.DailyWeeklyQuestData.weekly ??= new QuestPeriodSaveData();

        var result = period == QuestPeriodType.Daily
            ? playerData.DailyWeeklyQuestData.daily
            : playerData.DailyWeeklyQuestData.weekly;
        result.taskProgress ??= new List<DailyWeeklyQuestProgressSaveData>();
        result.claimedMilestones ??= new List<int>();
        return result;
    }

    private static DailyWeeklyQuestProgressSaveData FindProgress(QuestPeriodSaveData save, string questId)
    {
        return save?.taskProgress?.Find(p => p != null && p.questId == questId);
    }

    private static DailyWeeklyQuestProgressSaveData GetOrCreateProgress(QuestPeriodSaveData save, string questId)
    {
        var progress = FindProgress(save, questId);
        if (progress != null) return progress;

        progress = new DailyWeeklyQuestProgressSaveData { questId = questId };
        save.taskProgress.Add(progress);
        return progress;
    }

    private void FlushSave()
    {
        if (!_dirty || DataManager.PlayerData == null) return;
        _dirty = false;
        _onlineSaveAccumulator = 0f;
        DataManager.SavePlayerData().Forget();
    }

    private static void NotifyUpdated(QuestPeriodType period)
    {
        GameEventBus.OnDailyWeeklyQuestUpdated?.Invoke(period);
    }
}
