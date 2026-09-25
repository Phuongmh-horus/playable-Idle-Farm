using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CollectionItemLevelConfig
{
    [Tooltip("Level tương ứng của cây trong game (bắt đầu từ 1)")]
    public int level = 1;

    [Tooltip("Nhãn hiển thị phía trên ô (ví dụ: Lv.1, Lv.5, MEGA...)")]
    public string levelLabel = "Lv.1";

    [Tooltip("Sprite của cây ở mốc level này")]
    public Sprite plantSprite;
}

[Serializable]
public class CollectionPlantConfig
{
    [Tooltip("Loại cây (khớp với PlantConfig trong game)")]
    public PlantType plantType;
    
    [Tooltip("Danh sách các mốc level của cây này")]
    public List<CollectionItemLevelConfig> levels = new List<CollectionItemLevelConfig>();

    public int PlantId => (int)plantType;
}

[CreateAssetMenu(fileName = "CollectionConfigSO", menuName = "Farm/CollectionConfigSO")]
public class CollectionConfigSO : ScriptableObject
{
    [Header("Milestone Progress & Reward")]
    [Tooltip("Số lượng cây cần đếm để nhận thưởng (mặc định 16)")]
    public int targetCount = 16;

    [Tooltip("Số lượng Gem nhận được khi hoàn thành mốc")]
    public double rewardGem = 5000;
    
    [Header("Plant Collections")]
    [Tooltip("Danh sách các giống cây trong bộ sưu tập")]
    public List<CollectionPlantConfig> plantCollections = new List<CollectionPlantConfig>();

    public CollectionPlantConfig GetPlantConfig(PlantType plantType)
    {
        return plantCollections.Find(p => p.plantType == plantType);
    }

    public CollectionPlantConfig GetPlantConfig(int plantId)
    {
        return plantCollections.Find(p => (int)p.plantType == plantId);
    }

    public CollectionItemLevelConfig GetLevelConfig(int plantId, int level)
    {
        var plant = GetPlantConfig(plantId);
        return plant?.levels.Find(l => l.level == level);
    }
}
