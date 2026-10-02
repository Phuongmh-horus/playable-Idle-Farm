using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MainQuestManager : MonoSingleton<MainQuestManager>
{
    public MainQuestConfig Config => ConfigHolder.Instance?.MainQuestConfig;
    private MainQuestData _dynamicLoopQuest;

    public MainQuestData CurrentQuest
    {
        get
        {
            if (Config == null || !Config.enableMainQuest || Config.quests == null) return null;
            var save = DataManager.GetMainQuestSaveData();
            save.completedQuestIds ??= new();

            // 1. Quét danh sách config theo thứ tự index (0 -> N-1) tìm quest đầu tiên chưa hoàn thành (theo ID)
            for (int i = 0; i < Config.quests.Count; i++)
            {
                var q = Config.quests[i];
                if (q != null && !save.completedQuestIds.Contains(q.id))
                {
                    return q; // Quest người chơi chưa từng hoàn thành
                }
            }

            // 2. Sau khi tất cả quest trong config đã hoàn thành: Kiểm tra có bật chế độ Loop không
            if (!Config.enableLoopQuest)
            {
                return null;
            }

            // Chuyển sang Loop Mode vô tận (Random 50/50 HarvestFruit / AccumulateRevenue)
            return GetOrCreateDynamicLoopQuest();
        }
    }

    /// <summary>
    /// Kiểm tra xem toàn bộ các nhiệm vụ có trong MainQuestConfig đã được hoàn thành chưa
    /// </summary>
    public bool IsAllConfigQuestsCompleted()
    {
        if (Config == null || Config.quests == null || Config.quests.Count == 0) return true;
        var save = DataManager.GetMainQuestSaveData();
        if (save.completedQuestIds == null) return false;

        for (int i = 0; i < Config.quests.Count; i++)
        {
            var q = Config.quests[i];
            if (q != null && !save.completedQuestIds.Contains(q.id))
            {
                return false;
            }
        }
        return true;
    }

    protected override void Awake()
    {
        base.Awake();
        MainQuestHandlerRegistry.InitializeHandlers();
        SubscribeEvents();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        MainQuestHandlerRegistry.SubscribeAll(HandleQuestProgressUpdated);

        GameEventBus.OnPlayerDataReplaced -= HandlePlayerDataReplaced;
        GameEventBus.OnPlayerDataReplaced += HandlePlayerDataReplaced;
    }

    private void UnsubscribeEvents()
    {
        MainQuestHandlerRegistry.UnsubscribeAll(HandleQuestProgressUpdated);

        GameEventBus.OnPlayerDataReplaced -= HandlePlayerDataReplaced;
    }

    private void HandleQuestProgressUpdated()
    {
        GameEventBus.OnMainQuestUpdated?.Invoke();
    }

    private void HandlePlayerDataReplaced()
    {
        GameEventBus.OnMainQuestUpdated?.Invoke();
    }

    /// <summary>
    /// Random thời lượng cơ bản theo tỉ lệ: 180s (50%), 300s (30%), 600s (20%)
    /// </summary>
    private int RandomBaseDuration()
    {
        var durations = Config?.baseDuration;

        // Tỉ lệ xác suất: index 0 (50%), index 1 (30%), index 2 (20%)
        float rand = UnityEngine.Random.Range(0f, 100f);
        int targetIndex = rand < 50f ? 0 : (rand < 80f ? 1 : 2);

        if (durations != null && durations.Count > 0)
        {
            if (targetIndex < durations.Count)
            {
                return durations[targetIndex];
            }
            return durations[UnityEngine.Random.Range(0, durations.Count)];
        }

        // Fallback mặc định theo tỉ lệ nếu config rỗng: 180s (50%), 300s (30%), 600s (20%)
        return targetIndex switch
        {
            0 => 180,
            1 => 300,
            _ => 600
        };
    }

    /// <summary>
    /// Lấy danh sách các plantId đang có trên nông trại (hoặc đã mở khóa trong config)
    /// </summary>
    private List<int> GetAvailablePlantIds()
    {
        var plantIds = new HashSet<int>();
        var plots = DataManager.PlayerData?.GardenData?.plots;
        if (plots != null)
        {
            for (int i = 0; i < plots.Count; i++)
            {
                var p = plots[i];
                if (p != null && p.state == PlotState.Planted && p.plantedPlantId >= 0)
                {
                    plantIds.Add(p.plantedPlantId);
                }
            }
        }

        // Nếu chưa có cây nào được trồng, lấy danh sách cây từ config
        if (plantIds.Count == 0)
        {
            var plantConfig = ConfigHolder.Instance?.PlantConfig;
            if (plantConfig?.plants != null)
            {
                for (int i = 0; i < plantConfig.plants.Count; i++)
                {
                    plantIds.Add((int)plantConfig.plants[i].plantType);
                }
            }
        }

        return new List<int>(plantIds);
    }

    /// <summary>
    /// Random một plantId nông sản không nằm trong 5 vòng loop gần nhất
    /// </summary>
    private int RandomPlantIdExcludingRecent()
    {
        var availablePlantIds = GetAvailablePlantIds();
        if (availablePlantIds == null || availablePlantIds.Count == 0)
            return 0; // Mặc định Tomato

        var save = DataManager.GetMainQuestSaveData();
        save.recentLoopPlantIds ??= new List<int>();

        // Lọc các plantId chưa xuất hiện trong 5 vòng gần nhất
        var candidates = new List<int>();
        for (int i = 0; i < availablePlantIds.Count; i++)
        {
            int pId = availablePlantIds[i];
            if (!save.recentLoopPlantIds.Contains(pId))
            {
                candidates.Add(pId);
            }
        }

        // Nếu tất cả plantId đều nằm trong recent (do người chơi mở <= 5 loại cây)
        if (candidates.Count == 0)
        {
            candidates = availablePlantIds;
        }

        int chosenId = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        // Cập nhật lại danh sách 5 vòng gần nhất
        save.recentLoopPlantIds.Add(chosenId);
        while (save.recentLoopPlantIds.Count > 5)
        {
            save.recentLoopPlantIds.RemoveAt(0);
        }

        return chosenId;
    }

    /// <summary>
    /// Sinh và duy trì Quest Loop ngẫu nhiên (50/50 giữa HarvestFruit và AccumulateRevenue) sau khi đã qua vòng đầu.
    /// </summary>
    private MainQuestData GetOrCreateDynamicLoopQuest()
    {
        var save = DataManager.GetMainQuestSaveData();

        // Nếu chưa khởi tạo cho lượt loop này, sinh ngẫu nhiên 50/50
        if (save.loopTargetAmount <= 0)
        {
            float rand = UnityEngine.Random.value;
            MainQuestType chosenType = rand < 0.5f ? MainQuestType.HarvestFruit : MainQuestType.AccumulateRevenue;
            save.currentLoopQuestType = (int)chosenType;

            double currentCps = FarmEconomyCalculator.CalculateCurrentIdleCps();
            if (currentCps <= 0.0) currentCps = 10.0; // Baseline fallback

            int baseDuration = RandomBaseDuration();
            double questValue = baseDuration * currentCps;

            if (chosenType == MainQuestType.HarvestFruit)
            {
                int plantId = RandomPlantIdExcludingRecent();
                save.loopTargetId = plantId;

                float fruitRate = FarmEconomyCalculator.CalculateCurrentPlantFruitRate(plantId);
                long requiredCount;
                if (fruitRate > 0f)
                {
                    requiredCount = (long)Math.Ceiling(baseDuration * fruitRate);
                }
                else
                {
                    requiredCount = Math.Max(5, (long)(baseDuration / 15f));
                }

                save.loopTargetAmount = Math.Max(1, requiredCount);
            }
            else // AccumulateRevenue
            {
                save.loopTargetId = -1;
                save.loopTargetAmount = Math.Max(10, (long)Math.Ceiling(questValue));
            }

            // Phần thưởng Cash scale theo questValue * loopRewardPercent %
            float rewardMul = Config != null ? Config.LoopRewardMultiplier : 0.5f;
            save.loopRewardAmount = Math.Max(100, Math.Round(questValue * rewardMul));

            DataManager.SavePlayerData().Forget();
        }

        if (_dynamicLoopQuest == null)
        {
            _dynamicLoopQuest = new MainQuestData();
        }

        _dynamicLoopQuest.questType = (MainQuestType)save.currentLoopQuestType;
        _dynamicLoopQuest.targetAmount = save.loopTargetAmount;
        _dynamicLoopQuest.targetId = save.loopTargetId;
        _dynamicLoopQuest.customDescription = string.Empty;
        _dynamicLoopQuest.rewardCashAmount = save.loopRewardAmount > 0 ? save.loopRewardAmount : 100;

        return _dynamicLoopQuest;
    }

    /// <summary>
    /// Tính toán tiến độ hiện tại của một nhiệm vụ (Ủy quyền cho Handler theo OCP)
    /// </summary>
    public long GetCurrentProgress(MainQuestData quest)
    {
        return MainQuestHandlerRegistry.GetCurrentProgress(quest);
    }

    /// <summary>
    /// Kiểm tra xem nhiệm vụ đã hoàn thành và sẵn sàng nhận thưởng chưa
    /// </summary>
    public bool IsQuestCompleted(MainQuestData quest)
    {
        if (quest == null) return false;
        return GetCurrentProgress(quest) >= quest.targetAmount;
    }

    /// <summary>
    /// Nhận thưởng nhiệm vụ hiện tại và chuyển sang nhiệm vụ tiếp theo
    /// </summary>
    public bool ClaimCurrentQuest(RectTransform flyTarget = null)
    {
        var quest = CurrentQuest;
        if (quest == null || !IsQuestCompleted(quest)) return false;

        double cashReward = quest.rewardCashAmount;

        // Trường hợp 1: Nhận thưởng trong chế độ Loop vô tận (Endless Loop Mode)
        if (IsAllConfigQuestsCompleted())
        {
            var save = DataManager.GetMainQuestSaveData();
            save.loopCount++;
            save.loopTargetAmount = 0;
            save.loopTargetId = -1;
            save.currentProgress = 0;
            DataManager.SavePlayerData().Forget();

            if (cashReward > 0)
            {
                var claimAction = DataManager.OnClaimReward(quest.rewards, "main_quest_loop");
                claimAction?.Invoke();

                TriggerCashRewardFlyEffects(flyTarget);
            }

            GameEventBus.OnMainQuestClaimed?.Invoke(quest);
            GameEventBus.OnMainQuestUpdated?.Invoke();
            return true;
        }

        // Trường hợp 2: Nhận thưởng nhiệm vụ thông thường trong chuỗi gốc
        if (cashReward > 0)
        {
            var claimAction = DataManager.OnClaimReward(quest.rewards, "main_quest");
            claimAction?.Invoke();

            TriggerCashRewardFlyEffects(flyTarget);
        }

        // Đánh dấu nhiệm vụ này đã hoàn thành theo ID và reset currentProgress
        DataManager.AddCompletedMainQuestId(quest.id);

        GameEventBus.OnMainQuestClaimed?.Invoke(quest);
        GameEventBus.OnMainQuestUpdated?.Invoke();
        return true;
    }

    private void TriggerCashRewardFlyEffects(RectTransform flyTarget)
    {
        if (flyTarget != null)
        {
            ObjectActionManager<RectTransform>.Instance?.Bind((int)RewardType.Cash, flyTarget);
        }
        GameEventBus.OnGainCash?.Invoke();
    }

    /// <summary>
    /// Điều hướng camera hoặc mở giao diện tương ứng với mục tiêu nhiệm vụ (Ủy quyền cho Handler theo OCP)
    /// </summary>
    public void FocusQuestTarget(MainQuestData quest)
    {
        MainQuestHandlerRegistry.FocusTarget(quest);
    }
}
