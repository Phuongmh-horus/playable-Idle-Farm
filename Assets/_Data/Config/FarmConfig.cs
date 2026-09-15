using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FarmConfig", menuName = "Farm/FarmConfig")]
public class FarmConfig : ScriptableObject
{
    public string farmName = "Idle Farm";

    public List<PlotConfig> plots = new List<PlotConfig>();

    public SprinklerConfig sprinkler;

    public HarvesterConfig harvester;

    public ConveyorConfig conveyor;

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

    [Header("Plant Placement")]
    [Tooltip("Vị trí đặt cây (local offset so với ô đất)")]
    public Vector2 plantPosition;

    [Header("Gameplay")]
    public int defaultPlantId = -1;
    public long unlockCost;
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
    JuiceAmount,
}

#endregion

#region Sprinkler

[Serializable]
public class SprinklerConfig
{
    public float rotationSpeed;
    
    public List<SprinklerLevelConfig> levels = new List<SprinklerLevelConfig>();
}

[Serializable]
public class SprinklerLevelConfig
{
    public int level;

    public long upgradeCost;
    
    public float waterAmount;
    
    public int armLength; // Độ dài cánh tay vòi phun đang nâng cấp ở level này (cánh tay mới bắt đầu từ 1)
    
    public int nozzleCount; // Số lượng cánh tay SprinklerArm trong nông trại
}

#endregion

#region HarvesterConfig

[Serializable]
public class HarvesterConfig
{
    public float rotationSpeed = 90f;
    
    public List<HarvesterLevelConfig> levels = new List<HarvesterLevelConfig>();
}

[Serializable]
public class HarvesterLevelConfig
{
    public int level;
    
    public long upgradeCost;
    
    public int bladeCount; // Số lượng blade (lưỡi gặt) của cánh tay đang nâng cấp ở level này (cánh tay mới bắt đầu từ 1)
    
    public int harvesterCount; // Số lượng cánh tay HarvesterArm trong nông trại
}

#endregion

#region ConveyorConfig

[Serializable]
public class ConveyorConfig
{
    public float moveSpeed = 2f;
    
    public List<ConveyorLevelConfig> levels = new List<ConveyorLevelConfig>();
}

[Serializable]
public class ConveyorLevelConfig
{
    public int level;
    
    public long upgradeCost;
    
    public int maxBottles; // Tổng số chai tối đa trên băng chuyền ở level này
}

#endregion
