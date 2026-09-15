using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// RUNTIME SESSION DATA (Dữ liệu trạng thái tạm thời khi đang chơi)
// ============================================================================

[Serializable]
public class PlotRuntimeData
{
    public int plotId;

    public float currentWater; // Lượng nước hiện tại đang có trên ô đất / cây này
    public int currentFruitCount; // Số lượng quả hiện đang có trên cây (chờ thu hoạch)
}

[Serializable]
public class SpecialPlotRuntimeData
{
    public int plotId;

    public float currentWater; // Lượng nước hiện tại đang có trên ô đất đặc biệt
    public int currentFruitCount; // Số lượng quả hiện đang có trên cây đặc biệt
}

[Serializable]
public class FruitItemData
{
    public int plantId;    // ID loại cây (để biết quả loại gì)
    public long sellPrice; // Giá tiền bán của quả này khi bán thành công
}

[Serializable]
public class FruitTubeRuntimeData
{
    // Danh sách các quả thu hoạch đang xếp hàng nằm trong ống chứa
    public List<FruitItemData> fruits = new List<FruitItemData>();

    public int CurrentFruitCount => fruits != null ? fruits.Count : 0;
}

[Serializable]
public class BottleRuntimeData
{
    public bool isFilled;           // Chai đã chứa quả chưa?
    public FruitItemData fruitItem; // Thông tin quả đang nằm trong chai này
}

[Serializable]
public class ConveyorRuntimeData
{
    // Danh sách các chai chạy trên băng chuyền
    public List<BottleRuntimeData> bottles = new List<BottleRuntimeData>();
}

[Serializable]
public class GardenRuntimeData
{
    public int gardenId;
    public List<PlotRuntimeData> plots = new List<PlotRuntimeData>();
    public List<SpecialPlotRuntimeData> specialPlots = new  List<SpecialPlotRuntimeData>(); // Trạng thái 2 ô đất đặc biệt
}
