using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FarmConfig", menuName = "Farm/FarmConfig")]
public class FarmConfig : ScriptableObject
{
    public string farmName = "Idle Farm";

    public List<PlotConfig> plots = new List<PlotConfig>();

    [Header("Initial Plant Order")]
    [Tooltip("Plant IDs assigned to the first plots in order. Reorder this list to change which plant is built first.")]
    public List<int> initialPlantOrder = new List<int>();

    public SprinklerConfig sprinkler;

    public HarvesterConfig harvester;

    public TransportConfig transport;

    /// <summary>
    /// Lấy PlotConfig theo plotId
    /// </summary>
    public PlotConfig GetPlotConfig(int plotId)
    {
        return plots.Find(p => p.id == plotId);
    }

    /// <summary>
    /// Lấy PlotConfig theo RingIndex và SectorIndex
    /// </summary>
    public PlotConfig GetPlotConfig(int ringIndex, int sectorIndex)
    {
        return plots.Find(p => p.ringIndex == ringIndex && p.sectorIndex == sectorIndex);
    }

    /// <summary>
    /// Lấy tất cả các plot theo loại (Normal hoặc Special)
    /// </summary>
    public List<PlotConfig> GetPlotsByType(PlotType plotType)
    {
        return plots.FindAll(p => p.plotType == plotType);
    }

    public bool HasInitialPlantOrder
    {
        get { return initialPlantOrder != null && initialPlantOrder.Count > 0; }
    }

    public int GetInitialPlantId(int plotId)
    {
        if (initialPlantOrder != null && plotId >= 0 && plotId < initialPlantOrder.Count)
        {
            return initialPlantOrder[plotId];
        }

        PlotConfig plot = GetPlotConfig(plotId);
        return plot != null ? plot.defaultPlantId : -1;
    }

    /// <summary>
    /// Resolves the first plant for a plot. An explicit FarmConfig order wins;
    /// otherwise the order of PlantConfig.plants is used, while the legacy
    /// per-plot default remains a fallback when no plant list is available.
    /// </summary>
    public int GetInitialPlantId(int plotId, PlantConfig plantConfig)
    {
        if (initialPlantOrder != null && plotId >= 0 && plotId < initialPlantOrder.Count)
        {
            return initialPlantOrder[plotId];
        }

        if (plantConfig != null)
        {
            int orderedPlantId = plantConfig.GetPlantIdAtOrder(plotId);
            if (orderedPlantId >= 0)
            {
                return orderedPlantId;
            }
        }

        PlotConfig plot = GetPlotConfig(plotId);
        return plot != null ? plot.defaultPlantId : -1;
    }
}

#region PlotConfig

public enum PlotType
{
    Normal = 0,
    Special = 1
}

[Serializable]
public class PlotConfig
{
    [Header("Identity & Layout")]
    public int id;
    public int ringIndex;
    public int sectorIndex;
    public PlotType plotType = PlotType.Normal;

    [Header("Gameplay")]
    public int defaultPlantId = -1;
    public double unlockCost;
    public bool isUnlockedByDefault;
}

[Serializable]
public class StatData
{
    public StatType type;

    public float value;
}

public enum StatType
{
    WaterRequirement,
    UpgradeCost,
    FruitPrice,
}

#endregion

#region Sprinkler

[Serializable]
public class SprinklerConfig
{
    [Header("General Settings")]
    [Tooltip("Cấp độ tối đa của vòi phun")]
    public int maxLevel = 20;

    [Tooltip("Chu kỳ quay 1 vòng 360 độ (giây/vòng)")]
    public float rotationDuration = 10f;

    [Tooltip("Seconds removed from one revolution at every 5-level milestone.")]
    [Min(0f)] public float rotationDurationReductionPerMilestone = 2f;

    [Tooltip("Shortest allowed revolution duration.")]
    [Min(0.1f)] public float minimumRotationDuration = 2f;

    public float GetRotationDuration(int level)
    {
        int clampedLevel = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        int milestoneCount = clampedLevel / 5;
        return Mathf.Max(minimumRotationDuration, rotationDuration - milestoneCount * rotationDurationReductionPerMilestone);
    }

    public float GetRotationSpeed(int level)
    {
        float duration = GetRotationDuration(level);
        return duration > 0f ? 360f / duration : 0f;
    }

    [Header("Upgrade Cost Formula Settings")]
    [Tooltip("Chi phí nâng cấp tại Level 1")]
    public double level1Cost = 19;

    [Tooltip("Giá cơ sở để tính chi phí nâng cấp sau level 1 (SprinklerBasePrice)")]
    public double sprinklerBasePrice = 190;

    [Tooltip("Hệ số tăng giá nâng cấp theo cấp (SprinklerUpdateScale)")]
    public float sprinklerUpdateScale = 2.25f;

    [Header("Water Amount Settings (Tính trên mỗi cánh tay)")]
    [Tooltip("Lượng nước phun gốc (Level 1)")]
    public float baseWaterAmount = 5f;

    [Tooltip("Lượng nước tăng thêm theo cấp (+3 nước / cấp)")]
    public float waterAmountSumPerLevel = 3f;

    public float GetWaterAmount(int level)
    {
        return baseWaterAmount + (Mathf.Max(1, level) - 1) * waterAmountSumPerLevel;
    }

    public double GetUpgradeCost(int level)
    {
        if (level >= maxLevel) return 0;
        if (level <= 1) return level1Cost;

        return sprinklerBasePrice * Math.Pow(sprinklerUpdateScale, level - 2);
    }

    public int GetNozzleCount(int level)
    {
        int clampedLevel = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        if (clampedLevel >= 20) return 4;
        if (clampedLevel >= 10) return 3;
        if (clampedLevel >= 2) return 2;
        return 1;
    }
}

#endregion

#region HarvesterConfig

[Serializable]
public class HarvesterConfig
{
    [Header("General Settings")]
    [Tooltip("Cấp độ tối đa của máy gặt")]
    public int maxLevel = 9;

    [Tooltip("Chu kỳ quay 1 vòng 360 độ (giây/vòng)")]
    public float rotationDuration = 45f;

    public float rotationSpeed => rotationDuration > 0f ? 360f / rotationDuration : 8f;

    [Header("Upgrade Cost Formula Settings")]
    [Tooltip("Chi phí nâng cấp tại Level 1")]
    public double level1Cost = 130;

    [Tooltip("Giá cơ sở để tính chi phí nâng cấp sau level 1 (HarvesterBasePrice)")]
    public double harvesterBasePrice = 9750;

    [Tooltip("Hệ số tăng giá nâng cấp theo cấp (HarvesterUpdateScale)")]
    public float harvesterUpdateScale = 3f;

    public double GetUpgradeCost(int level)
    {
        if (level >= maxLevel) return 0;
        if (level <= 1) return level1Cost;

        return harvesterBasePrice * Math.Pow(harvesterUpdateScale, level - 2);
    }

    public int GetHarvesterCount(int level)
    {
        int clampedLevel = Mathf.Clamp(level, 1, maxLevel);
        return (clampedLevel + 2) / 3;
    }

    public int GetBladeCount(int level, int armIndex)
    {
        int clampedLevel = Mathf.Clamp(level, 1, maxLevel);
        return Mathf.Clamp(clampedLevel - armIndex * 3, 1, 3);
    }

    public int GetTotalBlades(int level)
    {
        return Mathf.Clamp(level, 1, maxLevel);
    }
}

#endregion

#region TransportConfig

[Serializable]
public class TransportConfig
{
    [Header("General Settings")]
    [Tooltip("Cấp độ tối đa của xe tải")]
    public int maxLevel = 20;

    [Header("Capacity Settings")]
    [Tooltip("Sức chứa khởi đầu (Level 1)")]
    public int baseCapacity = 1;

    [Tooltip("Sức chứa tăng thêm mỗi level")]
    public int capacitySumPerLevel = 1;

    [Header("Upgrade Cost Formula Settings")]
    [Tooltip("Chi phí nâng cấp tại Level 1")]
    public double level1Cost = 19;

    [Tooltip("Giá cơ sở để tính chi phí nâng cấp sau level 1 (CargoBasePrice)")]
    public double cargoBasePrice = 190;

    [Tooltip("Hệ số tăng giá nâng cấp theo cấp (CargoUpdateScale)")]
    public float cargoUpdateScale = 2.25f;

    public int MaxLevel => maxLevel;

    public int GetCapacity(int level)
    {
        int clampedLevel = Mathf.Clamp(level, 1, maxLevel);
        return baseCapacity + (clampedLevel - 1) * capacitySumPerLevel;
    }

    public double GetUpgradeCost(int level)
    {
        if (level >= maxLevel) return 0;
        if (level <= 1) return level1Cost;

        return cargoBasePrice * Math.Pow(cargoUpdateScale, level - 2);
    }
}

#endregion
