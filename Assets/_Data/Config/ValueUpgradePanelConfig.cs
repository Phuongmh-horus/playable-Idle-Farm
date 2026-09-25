using System;
using System.Collections.Generic;
using UnityEngine;

public enum ValueUpgradeType
{
    Vegetables = 0,
    Sprinkler = 1
}

[Serializable]
public class ValueUpgradeEntryConfig
{
    [Header("Upgrade Info")]
    [Tooltip("Loại nâng cấp (0: Vegetables, 1: Sprinkler)")]
    public ValueUpgradeType upgradeType;

    [Tooltip("ID của đối tượng (1-10 cho Vegetables, 0 hoặc 1 cho Sprinkler)")]
    public int targetId;

    [Tooltip("Tên đối tượng (ví dụ: Wheat, Corn, Sprinkler)")]
    public string targetName;

    [Header("Level Settings")]
    [Tooltip("Số bậc nâng cấp tối đa (mặc định 10 bậc)")]
    public int maxLevel = 10;

    [Header("Cost Settings")]
    [Tooltip("Giá khởi điểm tại cấp 1 (firstCost)")]
    public double firstCost = 50;

    [Tooltip("Hệ số nhân giá nâng cấp theo cấp (mặc định 3.0x)")]
    public float costMultiplier = 3.0f;

    [Header("Effect Settings")]
    [Tooltip("Hiệu ứng tăng mỗi cấp (ví dụ: 0.08 = 8%, 0.02 = 2%)")]
    public float upgradeEffect = 0.08f;

    [Tooltip("Mô tả hiệu ứng")]
    public string effectDescription = "Tăng 8% Coin sản lượng gốc";

    public int MaxLevel => maxLevel > 0 ? maxLevel : 10;
    public bool IsMaxLevel(int currentLevel) => currentLevel >= MaxLevel;

    /// <summary>
    /// Tính chi phí nâng cấp từ currentLevel lên currentLevel + 1.
    /// Cấp 0 -> 1: firstCost
    /// Cấp n -> n+1: firstCost * costMultiplier^n
    /// </summary>
    public double GetUpgradeCost(int currentLevel)
    {
        if (IsMaxLevel(currentLevel)) return 0;
        if (currentLevel <= 0) return firstCost;
        return firstCost * Math.Pow(costMultiplier, currentLevel);
    }

    /// <summary>
    /// Tính tổng chỉ số hiệu ứng cộng dồn tại level hiện tại
    /// </summary>
    public float GetTotalEffect(int currentLevel)
    {
        int clampedLevel = Mathf.Clamp(currentLevel, 0, MaxLevel);
        return clampedLevel * upgradeEffect;
    }

    /// <summary>
    /// Tính tổng % hiệu ứng tăng thêm (ví dụ: 8% -> trả về 8 * level)
    /// </summary>
    public float GetTotalEffectPercent(int currentLevel)
    {
        return GetTotalEffect(currentLevel) * 100f;
    }

    /// <summary>
    /// Kiểm tra xem đối tượng tương ứng đã được mở khóa trong game chưa
    /// </summary>
    public bool IsUnlocked()
    {
        if (upgradeType == ValueUpgradeType.Sprinkler)
        {
            // Sprinkler luôn mở khóa cùng nông trại
            return true;
        }

        if (upgradeType == ValueUpgradeType.Vegetables)
        {
            int plantId = targetId;

            // 1. Kiểm tra qua FarmPlotSystem trên Scene
            if (FarmSystem.Instance?.PlotSystem != null)
            {
                foreach (var plot in FarmSystem.Instance.PlotSystem.Plots)
                {
                    if (plot != null && plot.IsUnlocked)
                    {
                        if (plot.PlantId == plantId || (plot.Config != null && plot.Config.defaultPlantId == plantId))
                            return true;
                    }
                }
            }

            // 2. Kiểm tra qua PlayableFarmState (dữ liệu lưu trữ)
            if (PlayableFarmState.PlayerData?.GardenData?.plots != null)
            {
                var farmConfig = ConfigHolder.Instance?.FarmConfig;
                foreach (var plotSave in PlayableFarmState.PlayerData.GardenData.plots)
                {
                    if (plotSave != null && plotSave.state != PlotState.Locked)
                    {
                        if (plotSave.plantedPlantId == plantId)
                            return true;

                        var plotCfg = farmConfig?.GetPlotConfig(plotSave.plotId);
                        if (plotCfg != null && plotCfg.defaultPlantId == plantId)
                            return true;
                    }
                }
            }

            // 3. Fallback: Kiểm tra plot mặc định được mở khóa ban đầu
            var pCfg = ConfigHolder.Instance?.FarmConfig?.plots;
            if (pCfg != null)
            {
                foreach (var p in pCfg)
                {
                    if (p.isUnlockedByDefault && p.defaultPlantId == plantId)
                        return true;
                }
            }
        }

        return false;
    }
}

[CreateAssetMenu(fileName = "ValueUpgradePanelConfig", menuName = "Farm/ValueUpgradePanelConfig")]
public class ValueUpgradePanelConfig : ScriptableObject
{
    [Header("Upgrade Config List")]
    public List<ValueUpgradeEntryConfig> upgradeEntries = new List<ValueUpgradeEntryConfig>();

    /// <summary>
    /// Lấy config theo loại và ID đối tượng
    /// </summary>
    public ValueUpgradeEntryConfig GetEntry(ValueUpgradeType type, int targetId = 0)
    {
        return upgradeEntries.Find(e => e.upgradeType == type && (type != ValueUpgradeType.Vegetables || e.targetId == targetId));
    }

    /// <summary>
    /// Lấy config nâng cấp cho cây trồng (0-based ID: 0 = Wheat, 1 = Corn, ...)
    /// </summary>
    public ValueUpgradeEntryConfig GetPlantEntry(int plantId)
    {
        return upgradeEntries.Find(e => e.upgradeType == ValueUpgradeType.Vegetables && e.targetId == plantId);
    }

    /// <summary>
    /// Lấy config nâng cấp cho Sprinkler
    /// </summary>
    public ValueUpgradeEntryConfig GetSprinklerEntry()
    {
        return upgradeEntries.Find(e => e.upgradeType == ValueUpgradeType.Sprinkler);
    }

    /// <summary>
    /// Lấy toàn bộ danh sách nâng cấp theo loại
    /// </summary>
    public List<ValueUpgradeEntryConfig> GetEntriesByType(ValueUpgradeType type)
    {
        return upgradeEntries.FindAll(e => e.upgradeType == type);
    }

    /// <summary>
    /// Lấy chi phí nâng cấp
    /// </summary>
    public double GetUpgradeCost(ValueUpgradeType type, int targetId, int currentLevel)
    {
        var entry = GetEntry(type, targetId);
        return entry != null ? entry.GetUpgradeCost(currentLevel) : 0;
    }

    /// <summary>
    /// Lấy tổng giá trị hiệu ứng cộng thêm
    /// </summary>
    public float GetTotalEffect(ValueUpgradeType type, int targetId, int currentLevel)
    {
        var entry = GetEntry(type, targetId);
        return entry != null ? entry.GetTotalEffect(currentLevel) : 0f;
    }
}
