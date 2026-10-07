using System;
using System.Collections.Generic;
using UnityEngine;

public partial class WorldLayerManager : MonoSingleton<WorldLayerManager>
{
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private RectTransform worldLayerParent;
    [SerializeField] private WorldLayerReferenceSO worldLayerReferenceSO;

    private readonly Dictionary<WorldLayerType, Queue<WorldLayerBase>> _poolDic = new Dictionary<WorldLayerType, Queue<WorldLayerBase>>();
    private readonly List<WorldLayerBase> _activeLayers = new List<WorldLayerBase>();

    private void Reset()
    {
        if (!worldCanvas) worldCanvas = GetComponentInChildren<Canvas>();
        if (!worldLayerParent) worldLayerParent = transform as RectTransform;
    }

    protected override void Awake()
    {
        base.Awake();
        RegisterPreAttachedLayers();

        if (!TryGetComponent<WorldSinglePopupAutoCloseHandler>(out _))
        {
            gameObject.AddComponent<WorldSinglePopupAutoCloseHandler>();
        }
    }

    private void RegisterPreAttachedLayers()
    {
        WorldLayerBase[] sceneLayers = GetComponentsInChildren<WorldLayerBase>(true);
        for (int i = 0; i < sceneLayers.Length; i++)
        {
            WorldLayerBase layer = sceneLayers[i];
            if (layer == null) continue;
            layer.LayerType = GetLayerType(layer);
            layer.CleanupBeforeHide();
            layer.gameObject.SetActive(false);
            GetOrCreatePool(layer.LayerType).Enqueue(layer);
        }
    }

    private static WorldLayerType GetLayerType(WorldLayerBase layer)
    {
        if (layer is PopupUpgradePlantLayer) return WorldLayerType.PopupUpgradePlantLayer;
        if (layer is PopupPlantPriceLayer) return WorldLayerType.PopupPlantPriceLayer;
        if (layer is WaterRequiredLayer) return WorldLayerType.WaterRequiredLayer;
        if (layer is PopupBuyPlotLayer) return WorldLayerType.PopupBuyPlotLayer;
        if (layer is PopupPlantTreeLayer) return WorldLayerType.PopupPlantTreeLayer;
        if (layer is IconUpgradeLayer) return WorldLayerType.IconUpgradeLayer;
        if (layer is CapacityFruitTransport) return WorldLayerType.CapacityFruitTransport;
        if (layer is FloatingCurrencyLayer) return WorldLayerType.FloatingCurrencyLayer;
        return layer.LayerType;
    }

    private Queue<WorldLayerBase> GetOrCreatePool(WorldLayerType layerType)
    {
        if (!_poolDic.TryGetValue(layerType, out var pool))
        {
            pool = new Queue<WorldLayerBase>();
            _poolDic[layerType] = pool;
        }
        return pool;
    }

    public T ShowWorldLayer<T>(WorldLayerType layerType, Transform target, Vector3 offset = default) where T : WorldLayerBase
    {
        var layer = Spawn(layerType);
        if (layer is T typedLayer)
        {
            typedLayer.SetupFollow(target, offset);
            return typedLayer;
        }
        return null;
    }

    public T ShowWorldLayer<T>(WorldLayerType layerType, Vector3 position) where T : WorldLayerBase
    {
        var layer = Spawn(layerType);
        if (layer is T typedLayer)
        {
            typedLayer.SetupPosition(position);
            return typedLayer;
        }
        return null;
    }

    private WorldLayerBase Spawn(WorldLayerType layerType)
    {
        WorldLayerSpawnMode spawnMode = worldLayerReferenceSO != null
            ? worldLayerReferenceSO.GetSpawnMode(layerType)
            : WorldLayerSpawnMode.Multiple;

        // Náº¿u lÃ  cháº¿ Ä‘á»™ Single: Chá»‰ cho phÃ©p 1 cÃ¡i duy nháº¥t hiá»ƒn thá»‹
        // Táº¯t cÃ¡i cÅ© Ä‘ang hiá»ƒn thá»‹ vÃ  tÃ¡i sá»­ dá»¥ng luÃ´n
        if (spawnMode == WorldLayerSpawnMode.Single)
        {
            for (int i = _activeLayers.Count - 1; i >= 0; i--)
            {
                var activeLayer = _activeLayers[i];
                if (activeLayer != null && activeLayer.LayerType == layerType)
                {
                    ReleaseToPool(activeLayer); // Táº¯t ngay láº­p tá»©c vÃ  Ä‘áº©y vá» Pool
                    break;
                }
            }
        }

        var pool = GetOrCreatePool(layerType);
        WorldLayerBase layer = null;

        while (pool.Count > 0)
        {
            layer = pool.Dequeue();
            if (layer != null) break;
        }

        if (layer == null)
        {
            Debug.LogError("[WorldLayerManager] No pre-attached layer is available for " + layerType + ". Add an inactive instance under Gameplay World Canvas.");
            return null;
        }

        if (!_activeLayers.Contains(layer))
        {
            _activeLayers.Add(layer);
        }

        return layer;
    }

    public void Hide(WorldLayerBase layer, bool animate = true)
    {
        if (layer == null) return;

        if (animate)
        {
            layer.Hide(true);
        }
        else
        {
            ReleaseToPool(layer);
        }
    }

    public void ReleaseToPool(WorldLayerBase layer)
    {
        if (layer == null) return;

        _activeLayers.Remove(layer);
        layer.CleanupBeforeHide();
        layer.gameObject.SetActive(false);
        GetOrCreatePool(layer.LayerType).Enqueue(layer);
    }

    public void HideWorldLayer(WorldLayerType layerType, Transform target, bool animate = true)
    {
        if (target == null) return;

        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            var layer = _activeLayers[i];
            if (layer.LayerType == layerType && layer.FollowTarget == target)
            {
                Hide(layer, animate);
            }
        }
    }

    public void HideAllByTarget(Transform target, bool animate = true)
    {
        if (target == null) return;

        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            var layer = _activeLayers[i];
            if (layer.FollowTarget == target)
            {
                Hide(layer, animate);
            }
        }
    }

    public void HideAllByType(WorldLayerType layerType, bool animate = true)
    {
        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            var layer = _activeLayers[i];
            if (layer.LayerType == layerType)
            {
                Hide(layer, animate);
            }
        }
    }

    public void HideAllSingleLayers(bool animate = true)
    {
        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            if (i >= _activeLayers.Count) continue;
            var layer = _activeLayers[i];
            if (layer == null || layer.IsHiding) continue;

            WorldLayerSpawnMode spawnMode = worldLayerReferenceSO != null
                ? worldLayerReferenceSO.GetSpawnMode(layer.LayerType)
                : WorldLayerSpawnMode.Multiple;

            if (spawnMode == WorldLayerSpawnMode.Single)
            {
                Hide(layer, animate);
            }
        }
    }

    public void HideAll(bool animate = false)
    {
        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            Hide(_activeLayers[i], animate);
        }
    }

    // --- Merged from ShowWorldLayerHelper ---

    // CÃ¡c hÃ m Show/Hide cá»¥ thá»ƒ cho tá»«ng WorldLayer sáº½ Ä‘Æ°á»£c sinh tá»± Ä‘á»™ng táº¡i Ä‘Ã¢y thÃ´ng qua Tool Generate Layer

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

