public enum WorldLayerType
{
    None = 0,
    
    PopupUpgradePlantLayer = 2,
    PopupPlantPriceLayer = 3,
    WaterRequiredLayer = 4,
    PopupBuyPlotLayer = 5,
    PopupPlantTreeLayer = 6,
    IconUpgradeLayer = 7,
    CapacityFruitTransport = 8,
    FloatingCurrencyLayer = 9,
}

public enum WorldLayerSpawnMode
{
    Multiple = 0, // Có thể spawn nhiều bản sao cùng lúc (Floating text, Bubble, Progress bar...)
    Single = 1    // Chỉ 1 bản sao duy nhất, spawn cái mới thì tự động tắt cái cũ và tái sử dụng (Exclusive)
}

public enum WorldLayerDepth
{
    Bottom = 100,       // Dưới cùng: Hiệu ứng sàn, vòng tròn chọn đất, bóng...
    StatusIcon = 200,   // Icon trạng thái thường trực: giọt nước, icon mũi tên upgrade...
    Popup = 500,        // Bảng tương tác, popup: chọn cây, mua đất, nâng cấp...
    TopOverlay = 1000   // Nổi hẳn trên cùng: text bay, thông báo, tooltip...
}

