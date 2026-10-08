using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlantType
{
    Tomato = 0,
    Potato = 1,
    Lettuce = 2,
    Carrot = 3,
    Eggplant = 4,
    Pumpkin = 5,
    Wheat = 6,
    ChiliPepper = 7,
    Radish = 8,
    BellPepper = 9
}

[CreateAssetMenu(fileName = "PlantConfig", menuName = "Farm/PlantConfig")]
public class PlantConfig : ScriptableObject
{
    public List<PlantConfigData> plants = new List<PlantConfigData>();

    #region Query Methods

    public PlantConfigData GetPlant(PlantType plantType)
    {
        return plants.Find(p => p.plantType == plantType);
    }

    public PlantConfigData GetPlant(int plantId)
    {
        return plants.Find(p => (int)p.plantType == plantId);
    }

    /// <summary>
    /// Gets the plant ID at the configured list position. The list order is
    /// intentionally separate from the stable PlantType/ID value.
    /// </summary>
    public int GetPlantIdAtOrder(int orderIndex)
    {
        if (plants == null || orderIndex < 0 || orderIndex >= plants.Count || plants[orderIndex] == null)
        {
            return -1;
        }

        return plants[orderIndex].ID;
    }

    #endregion
}

[Serializable]
public class PlantRingStats
{
    [Tooltip("Chi phí nâng cấp cơ bản tại Level 1 cho ring này")]
    public double baseUpgradeCost = 100;

    [Tooltip("Giá bán quả cơ bản tại Level 1 cho ring này")]
    public double baseFruitPrice = 10;

    [Tooltip("Lượng nước yêu cầu cho mỗi slot quả ở ring này")]
    public int waterRequirement = 10;

    [Tooltip("Chi phí nâng cấp tăng thêm mỗi level ở ring này")]
    public double levelUpCostSumPerLevel = 25;

    [Tooltip("Giá bán quả tăng thêm mỗi level ở ring này")]
    public double priceSumPerLevel = 5;
}

[Serializable]
public class PlantConfigData
{
    [Header("Identity")]
    public PlantType plantType;

    [Header("Visual Set (ScriptableObject)")]
    public PlantVisualSO visual;

    [Header("Playable Progression")]
    [Min(1)] public int maxLevel = 5;
    [Min(1)] public int baseFruitCount = 1;
    [Min(0)] public int fruitCountPerLevel = 1;

    [Header("Ring Stats (Cây ở 3 ring đầu: có 3 phần tử cho Ring 0, 1, 2. Cây ở Ring 4 hoặc cây dùng chung: chỉ cần 1 phần tử)")]
    public List<PlantRingStats> ringStats = new List<PlantRingStats>();

    #region Properties

    public int ID => (int)plantType;
    public string PlantName => GetDisplayName();
    public string FruitName => GetDisplayName();
    public Sprite plantIcon => visual != null ? visual.plantIcon : null;
    public Sprite fruitIcon => visual != null ? visual.fruitIcon : null;
    public Sprite giantSkin => visual != null ? visual.giantSkin : null;
    public Plant plantPrefab => GetPrefab();
    public List<Sprite> plantSprites => visual != null ? visual.plantSprites : null;

    public string GetDisplayName()
    {
        return plantType.ToString();
    }

    #endregion

    #region Query Methods

    public PlantRingStats GetRingStats(int ringIndex = 0)
    {
        if (ringStats == null || ringStats.Count == 0)
            return null;

        if (ringStats.Count == 1)
            return ringStats[0];

        int clampedIndex = Mathf.Clamp(ringIndex, 0, ringStats.Count - 1);
        return ringStats[clampedIndex];
    }

    public Sprite GetPlantSprite(int fruitCount = 0)
    {
        return visual != null ? visual.GetPlantSprite(fruitCount) : null;
    }

    public List<Vector2> GetFruitPositions()
    {
        return new List<Vector2>();
    }

    public int GetMaxLevel()
    {
        return Mathf.Max(1, maxLevel);
    }

    public int GetFruitCount(int level)
    {
        int clampedLevel = Mathf.Clamp(level, 1, GetMaxLevel());
        return Mathf.Max(1, baseFruitCount + (clampedLevel - 1) * fruitCountPerLevel);
    }

    public Plant GetPrefab()
    {
        return visual != null ? visual.plantPrefab : null;
    }

    public Sprite GetIcon(int level = 1)
    {
        return GetPlantSprite(level);
    }

    public Sprite GetPlantStoreIcon()
    {
        return visual != null ? visual.GetPlantStoreIcon() : (plantIcon != null ? plantIcon : fruitIcon);
    }

    /// <summary>
    /// Artwork for harvested fruit and transport cargo. This deliberately stays
    /// separate from the picker icon, which uses the plant's giant skin.
    /// </summary>
    public Sprite GetHarvestIcon()
    {
        return fruitIcon != null ? fruitIcon : (plantIcon != null ? plantIcon : GetIcon());
    }

    public int GetWaterRequirement(int ringIndex = 0)
    {
        PlantRingStats stats = GetRingStats(ringIndex);
        return stats?.waterRequirement ?? 10;
    }

    /// <summary>
    /// Lấy chi phí nâng cấp lên level tiếp theo cho ring cụ thể.
    /// Nếu LevelUpCostSumPerLevel > 0, tính theo: BaseCost + (level - 1) * LevelUpCostSumPerLevel
    /// </summary>
    public double GetUpgradeCost(int level, int ringIndex = 0)
    {
        PlantRingStats stats = GetRingStats(ringIndex);
        double baseCost = stats != null ? stats.baseUpgradeCost : 100;
        double sumPerLevel = stats != null ? stats.levelUpCostSumPerLevel : 25;

        if (sumPerLevel > 0)
        {
            return baseCost + (Mathf.Max(1, level) - 1) * sumPerLevel;
        }

        return baseCost;
    }

    /// <summary>
    /// Lấy giá bán trái cây theo level của cây trồng cho ring cụ thể.
    /// Nếu PriceSumPerLevel > 0, tính theo: BasePrice + (level - 1) * PriceSumPerLevel
    /// </summary>
    public double GetFruitPrice(int level, int ringIndex = 0)
    {
        PlantRingStats stats = GetRingStats(ringIndex);
        double basePrice = stats != null ? stats.baseFruitPrice : 10;
        double sumPerLevel = stats != null ? stats.priceSumPerLevel : 5;

        double price = basePrice;
        if (sumPerLevel > 0)
        {
            price = basePrice + (Mathf.Max(1, level) - 1) * sumPerLevel;
        }

        return price;
    }

    /// <summary>
    /// Tính tổng chi phí nâng cấp từ fromLevel đến toLevel cho ring cụ thể
    /// </summary>
    public double GetTotalUpgradeCost(int fromLevel, int toLevel, int ringIndex = 0)
    {
        double total = 0;
        for (int l = fromLevel; l < toLevel; l++)
        {
            total += GetUpgradeCost(l, ringIndex);
        }
        return total;
    }

    public double GetStatValue(int level, StatType statType, double defaultValue = 0, int ringIndex = 0)
    {
        switch (statType)
        {
            case StatType.UpgradeCost:
                return GetUpgradeCost(level, ringIndex);
            case StatType.FruitPrice:
                return GetFruitPrice(level, ringIndex);
            case StatType.WaterRequirement:
                return GetWaterRequirement(ringIndex);
            default:
                return defaultValue;
        }
    }

    #endregion
}
