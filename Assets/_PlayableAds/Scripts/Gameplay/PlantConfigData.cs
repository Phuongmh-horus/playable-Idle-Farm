// using System;
// using System.Collections.Generic;
// using UnityEngine;

// [Serializable]
// public class PlantConfigData
// {
//     [Header("Identity")]
//     public int id;
//     public string plantName;

//     [Header("Fruit Visuals")]
//     public string fruitName;
//     public Sprite fruitIcon;
//     public GameObject fruitPrefab;

//     [Header("Juice Settings")]
//     [Tooltip("Màu hiển thị của nước ép")]
//     public Color juiceColor = Color.white;

//     [Tooltip("Lượng nước ép cơ bản bơm vào vòng (0 - 1) khi thu hoạch")]
//     public float defaultJuiceAmount = 0.05f;

//     [Header("Positions & Levels")]
//     public List<Vector2> fruitPositions = new();
//     public List<PlantLevelConfig> plantLevelConfigs = new();

//     #region Query Methods

//     /// <summary>
//     /// Lấy lượng nước ép theo level
//     /// </summary>
//     public float GetJuiceAmount(int level)
//     {
//         PlantLevelConfig levelConfig = GetLevelConfig(level);
//         return levelConfig != null ? levelConfig.GetStat(StatType.JuiceAmount, defaultJuiceAmount) : defaultJuiceAmount;
//     }

//     /// <summary>
//     /// Lấy cấu hình theo level cụ thể (level >= 1)
//     /// </summary>
//     public PlantLevelConfig GetLevelConfig(int level)
//     {
//         return plantLevelConfigs.Find(c => c.level == level);
//     }

//     /// <summary>
//     /// Lấy cấp độ tối đa hiện có của cây
//     /// </summary>
//     public int GetMaxLevel()
//     {
//         if (plantLevelConfigs == null || plantLevelConfigs.Count == 0)
//             return 0;

//         int max = 0;
//         foreach (var config in plantLevelConfigs)
//         {
//             if (config.level > max)
//                 max = config.level;
//         }
//         return max;
//     }

//     /// <summary>
//     /// Lấy prefab mô hình theo level
//     /// </summary>
//     public GameObject GetPrefab(int level)
//     {
//         return GetLevelConfig(level)?.prefab;
//     }

//     /// <summary>
//     /// Lấy sprite icon theo level
//     /// </summary>
//     public Sprite GetIcon(int level)
//     {
//         return GetLevelConfig(level)?.icon;
//     }

//     /// <summary>
//     /// Lấy giá trị của một chỉ số theo level
//     /// </summary>
//     public float GetStatValue(int level, StatType statType, float defaultValue = 0f)
//     {
//         PlantLevelConfig levelConfig = GetLevelConfig(level);
//         return levelConfig != null ? levelConfig.GetStat(statType, defaultValue) : defaultValue;
//     }

//     #endregion
// }

// [Serializable]
// public class PlantLevelConfig
// {
//     public int level;
    
//     public Sprite icon;
    
//     public GameObject prefab;
    
//     public List<StatData> stats = new(); // Các chỉ số tương ứng với level này

//     #region Query Methods

//     /// <summary>
//     /// Lấy giá trị của một chỉ số (WaterRequirement, UpgradeCost, FruitPrice)
//     /// </summary>
//     public float GetStat(StatType type, float defaultValue = 0f)
//     {
//         if (stats == null)
//             return defaultValue;

//         StatData stat = stats.Find(s => s.type == type);
//         return stat != null ? stat.value : defaultValue;
//     }

//     /// <summary>
//     /// Thử lấy giá trị chỉ số nếu tồn tại
//     /// </summary>
//     public bool TryGetStat(StatType type, out float value)
//     {
//         if (stats != null)
//         {
//             StatData stat = stats.Find(s => s.type == type);
//             if (stat != null)
//             {
//                 value = stat.value;
//                 return true;
//             }
//         }

//         value = 0f;
//         return false;
//     }

//     #endregion
// }
// [Serializable]
// public class StatData
// {
//     public StatType type;
    
//     public float value;
// }

// public enum StatType
// {
//     WaterRequirement,
//     UpgradeCost,
//     FruitPrice,
//     JuiceAmount,
// }