using System;
using UnityEngine;

public partial class WorldLayerManager
{
    // Các hàm Show/Hide cụ thể cho từng WorldLayer sẽ được sinh tự động tại đây thông qua Tool Generate Layer

    public PopupUpgradePlantLayer ShowPopupUpgradePlantLayer(Plot plot, Transform target, Vector3 offset = default)
    {
        var layer = ShowWorldLayer<PopupUpgradePlantLayer>(WorldLayerType.PopupUpgradePlantLayer, target, offset);
        if (layer != null)
        {
            layer.InitData(plot);
        }
        return layer;
    }

    public PopupUpgradePlantLayer ShowPopupUpgradePlantLayer(Plot plot, Vector3 position)
    {
        var layer = ShowWorldLayer<PopupUpgradePlantLayer>(WorldLayerType.PopupUpgradePlantLayer, position);
        if (layer != null)
        {
            layer.InitData(plot);
        }
        return layer;
    }

    public PopupUpgradePlantLayer ShowPopupUpgradePlantLayer(Transform target, Vector3 offset = default)
    {
        Plot plot = target != null ? target.GetComponentInParent<Plot>() : null;
        return ShowPopupUpgradePlantLayer(plot, target, offset);
    }

    public PopupUpgradePlantLayer ShowPopupUpgradePlantLayer(Vector3 position)
    {
        return ShowPopupUpgradePlantLayer((Plot)null, position);
    }

    public void HideBuyPlant(Transform target)
    {
        HideWorldLayer(WorldLayerType.PopupUpgradePlantLayer, target);
    }

    public PopupPlantPriceLayer ShowPopupPlantPriceLayer(PlantPriceData plantPriceData, Transform target, Vector3 offset = default)
    {
        var layer = ShowWorldLayer<PopupPlantPriceLayer>(WorldLayerType.PopupPlantPriceLayer, target, offset);
        if (layer != null)
        {
            layer.InitData(plantPriceData) ;
        }
        return layer;
    }

    public PopupPlantPriceLayer ShowPopupPlantPriceLayer(PlantPriceData plantPriceData, Vector3 position)
    {
        var layer = ShowWorldLayer<PopupPlantPriceLayer>(WorldLayerType.PopupPlantPriceLayer, position);
        if (layer != null)
        {
            layer.InitData(plantPriceData);
        }
        return layer;
    }

    public void HidePlantPriceLayer(Transform target)
    {
        HideWorldLayer(WorldLayerType.PopupPlantPriceLayer, target);
    }

    public WaterRequiredLayer ShowWaterRequiredLayer(Transform target, Vector3 offset = default)
    {
        return ShowWorldLayer<WaterRequiredLayer>(WorldLayerType.WaterRequiredLayer, target, offset);
    }

    public WaterRequiredLayer ShowWaterRequiredLayer(Vector3 position)
    {
        return ShowWorldLayer<WaterRequiredLayer>(WorldLayerType.WaterRequiredLayer, position);
    }

    public void HideWaterRequiredLayer(Transform target)
    {
        HideWorldLayer(WorldLayerType.WaterRequiredLayer, target);
    }

    public PopupBuyPlotLayer ShowPopupBuyPlotLayer(Plot plot, Transform target, Vector3 offset = default)
    {
        var layer = ShowWorldLayer<PopupBuyPlotLayer>(WorldLayerType.PopupBuyPlotLayer, target, offset);
        if (layer != null)
        {
            layer.InitData(plot);
        }
        return layer;
    }

    public PopupBuyPlotLayer ShowPopupBuyPlotLayer(BuyPlotData buyPlotData, Transform target, Vector3 offset = default)
    {
        var layer = ShowWorldLayer<PopupBuyPlotLayer>(WorldLayerType.PopupBuyPlotLayer, target, offset);
        if (layer != null)
        {
            layer.InitData(buyPlotData);
        }
        return layer;
    }

    public PopupBuyPlotLayer ShowPopupBuyPlotLayer(Plot plot, Vector3 position)
    {
        var layer = ShowWorldLayer<PopupBuyPlotLayer>(WorldLayerType.PopupBuyPlotLayer, position);
        if (layer != null)
        {
            layer.InitData(plot);
        }
        return layer;
    }

    public void HidePopupBuyPlotLayer(Transform target)
    {
        HideWorldLayer(WorldLayerType.PopupBuyPlotLayer, target);
    }

    public PopupPlantTreeLayer ShowPopupPlantTreeLayer(Plot plot, Transform target, Vector3 offset = default)
    {
        var layer = ShowWorldLayer<PopupPlantTreeLayer>(WorldLayerType.PopupPlantTreeLayer, target, offset);
        if (layer != null)
        {
            layer.InitData(plot);
        }
        return layer;
    }

    public PopupPlantTreeLayer ShowPopupPlantTreeLayer(PlantTreeData plantTreeData, Transform target, Vector3 offset = default)
    {
        var layer = ShowWorldLayer<PopupPlantTreeLayer>(WorldLayerType.PopupPlantTreeLayer, target, offset);
        if (layer != null)
        {
            layer.InitData(plantTreeData);
        }
        return layer;
    }

    public PopupPlantTreeLayer ShowPopupPlantTreeLayer(Plot plot, Vector3 position)
    {
        var layer = ShowWorldLayer<PopupPlantTreeLayer>(WorldLayerType.PopupPlantTreeLayer, position);
        if (layer != null)
        {
            layer.InitData(plot);
        }
        return layer;
    }

    public void HidePopupPlantTreeLayer(Transform target)
    {
        HideWorldLayer(WorldLayerType.PopupPlantTreeLayer, target);
    }

    public IconUpgradeLayer ShowIconUpgradeLayer(Transform target, Vector3 offset = default, Action onClick = null)
    {
        var layer = ShowWorldLayer<IconUpgradeLayer>(WorldLayerType.IconUpgradeLayer, target, offset);
        if (layer != null)
        {
            layer.InitData(onClick);
        }
        return layer;
    }

    public IconUpgradeLayer ShowIconUpgradeLayer(Vector3 position, Action onClick = null)
    {
        var layer = ShowWorldLayer<IconUpgradeLayer>(WorldLayerType.IconUpgradeLayer, position);
        if (layer != null)
        {
            layer.InitData(onClick);
        }
        return layer;
    }

    public void HideIconUpgradeLayer(Transform target)
    {
        HideWorldLayer(WorldLayerType.IconUpgradeLayer, target);
    }

    public CapacityFruitTransport ShowCapacityFruitTransport(Transform target, Vector3 offset = default)
    {
        var layer = ShowWorldLayer<CapacityFruitTransport>(WorldLayerType.CapacityFruitTransport, target, offset);
        if (layer != null)
        {
            layer.InitData();
        }
        return layer;
    }

    public CapacityFruitTransport ShowCapacityFruitTransport(Transform target, Vector3 offset, int current, int max)
    {
        var layer = ShowWorldLayer<CapacityFruitTransport>(WorldLayerType.CapacityFruitTransport, target, offset);
        if (layer != null)
        {
            layer.UpdateCapacity(current, max);
        }
        return layer;
    }

    public CapacityFruitTransport ShowCapacityFruitTransport(Vector3 position)
    {
        var layer = ShowWorldLayer<CapacityFruitTransport>(WorldLayerType.CapacityFruitTransport, position);
        if (layer != null)
        {
            layer.InitData();
        }
        return layer;
    }

    public void HideCapacityFruitTransport(Transform target)
    {
        HideWorldLayer(WorldLayerType.CapacityFruitTransport, target);
    }

    public FloatingCurrencyLayer ShowFloatingCurrencyLayer(Transform target, Vector3 offset, double amount)
    {
        var layer = ShowWorldLayer<FloatingCurrencyLayer>(WorldLayerType.FloatingCurrencyLayer, target, offset);
        if (layer != null)
        {
            layer.ShowAmount(amount);
        }
        return layer;
    }

    public FloatingCurrencyLayer ShowFloatingCurrencyLayer(Vector3 position, double amount)
    {
        var layer = ShowWorldLayer<FloatingCurrencyLayer>(WorldLayerType.FloatingCurrencyLayer, position);
        if (layer != null)
        {
            layer.ShowAmount(amount);
        }
        return layer;
    }

    public void HideFloatingCurrencyLayer(Transform target)
    {
        HideWorldLayer(WorldLayerType.FloatingCurrencyLayer, target);
    }
}
