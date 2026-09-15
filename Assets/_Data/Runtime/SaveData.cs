using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// SAVE / PERSISTENT DATA LAYER (Dữ liệu lưu trạng thái người chơi lâu dài)
// ============================================================================

public enum PlotState
{
    Locked, // Chưa mua (Tốn unlockCost để mua)
    Empty, // Đã mua nhưng chưa trồng cây
    Planted // Đã trồng cây (Trồng miễn phí, nâng cấp level tốn tiền)
}

[Serializable]
public class PlotSaveData
{
    public int plotId;
    public PlotState state;
    public int currentLevel;
    public int plantedPlantId; // ID loại cây được trồng riêng trên ô đất này
}

[Serializable]
public class SpecialPlotSaveData
{
    public int plotId;
    public PlotState state;
    public int currentLevel;
    public int plantedPlantId; // ID loại cây đặc biệt do người chơi chọn trồng
}

[Serializable]
public class SprinklerSaveData
{
    public int currentLevel; // Level hiện tại của vòi phun nước
}

[Serializable]
public class HarvesterSaveData
{
    public int currentLevel; // Level hiện tại của hệ thống máy gặt
}

[Serializable]
public class ConveyorSaveData
{
    public int currentLevel; // Level hiện tại của băng chuyền
}

[Serializable]
public class GardenSaveData
{
    public int gardenId;
    public List<PlotSaveData> plots = new List<PlotSaveData>();
    public SprinklerSaveData sprinkler = new SprinklerSaveData(); // Lưu tiến trình vòi phun nước thường
    public HarvesterSaveData harvester = new HarvesterSaveData(); // Lưu tiến trình máy gặt thường
    public ConveyorSaveData conveyor = new ConveyorSaveData(); // Lưu tiến trình băng chuyền

    // tiến trình 2 ô đất đặc biệt
    public List<SpecialPlotSaveData> specialPlots = new List<SpecialPlotSaveData>();

    // Tiến trình vòi phun nước đặc biệt duy nhất của vườn
    public SprinklerSaveData specialSprinkler = new SprinklerSaveData();

    // Tiến trình máy gặt đặc biệt duy nhất của vườn
    public HarvesterSaveData specialHarvester = new HarvesterSaveData();
}

[Serializable]
public class UserSaveData
{
    public string farmName;
    public long coins;
    public List<GardenSaveData> gardens = new List<GardenSaveData>();
}
