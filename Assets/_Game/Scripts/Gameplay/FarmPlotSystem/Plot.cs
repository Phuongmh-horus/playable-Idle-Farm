using System;
using UnityEngine;

public sealed class Plot : MonoBehaviour
{
    [SerializeField] private StaticSelectable plantRoot;
    [SerializeField] private PlotVisual plotVisual;
    [SerializeField] private Vector3 upgradeIconOffset = new Vector3(0, 1.2f, 0);

    private int _plotId;
    private int _ringIndex;

    private FarmPlotSystem _slotSystem;
    private FarmPlotData _plotData;
    private Plant _spawnedPlant;

    private bool _isShowingUpgradeIcon;
    private bool _isGoldSubscribed;

    public int PlotId => _plotId;
    public int RingIndex => _ringIndex;
    public FarmPlotData PlotData => _plotData;
    public FarmPlotState State => _plotData?.State ?? FarmPlotState.RingLocked;
    public bool IsUnlocked => _plotData != null && _plotData.IsUnlocked;
    public Plant CurrentPlant => _spawnedPlant != null ? _spawnedPlant : GetComponentInChildren<Plant>();

    public Vector3 PlantPosition => _spawnedPlant != null
        ? _spawnedPlant.transform.position
        : (plantRoot != null ? plantRoot.transform.position : transform.position);

    private void Awake()
    {
        EnsureVisual();
        if (plantRoot != null)
        {
            plantRoot.OnTapped += HandlePlantRootTapped;
        }
    }

    private void Start()
    {
        SubscribeGoldEvent();
        UpdateUpgradeIconState();
    }

    private void OnEnable()
    {
        SubscribeGoldEvent();
        UpdateUpgradeIconState();
    }

    private void OnDisable()
    {
        UnsubscribeGoldEvent();
        HideUpgradeIcon();
    }

    private void OnDestroy()
    {
        if (plantRoot != null)
        {
            plantRoot.OnTapped -= HandlePlantRootTapped;
        }
        UnsubscribeFromPlotData();
        UnsubscribeGoldEvent();
        HideUpgradeIcon();
        Transform popupTarget = plantRoot != null ? plantRoot.transform : transform;
        if (WorldLayerManager.Instance != null)
        {
            WorldLayerManager.Instance.HideAllByTarget(popupTarget);
        }
        ClearSpawnedPlant();
    }

    public void ResetForPool()
    {
        UnsubscribeFromPlotData();
        UnsubscribeGoldEvent();
        HideUpgradeIcon();
        Transform popupTarget = plantRoot != null ? plantRoot.transform : transform;
        if (WorldLayerManager.Instance != null)
        {
            WorldLayerManager.Instance.HideAllByTarget(popupTarget);
        }
        ClearSpawnedPlant();
        _plotData = null;
        _slotSystem = null;
    }

    private void OnValidate()
    {
        EnsureVisual();
    }

    private void SubscribeToPlotData()
    {
        if (_plotData != null)
        {
            _plotData.OnStateChanged += HandlePlotStateChanged;
        }
    }

    private void UnsubscribeFromPlotData()
    {
        if (_plotData != null)
        {
            _plotData.OnStateChanged -= HandlePlotStateChanged;
        }
    }

    public void Bind(FarmPlotSystem owner, FarmPlotData farmPlot)
    {
        UnsubscribeFromPlotData();
        _slotSystem = owner;
        _plotData = farmPlot;
        SubscribeToPlotData();

        ApplyPlotData(farmPlot);

        if (farmPlot != null)
        {
            UpdateVisualByState(farmPlot.State);
            UpdatePlantInstance();
        }

        UpStatePlot();
        UpdateUpgradeIconState();
    }

    public void UpStatePlot()
    {
        if (_plotData == null || WorldLayerManager.Instance == null) return;

        Transform popupTarget = plantRoot != null ? plantRoot.transform : transform;

        // Chá»‰ hiá»‡n popup giÃ¡ náº¿u Ã´ Ä‘áº¥t Ä‘ang Locked (chÆ°a má»Ÿ khÃ³a)
        if (_plotData.State == FarmPlotState.Locked)
        {
            double cost = _plotData.Config != null ? _plotData.Config.unlockCost : 10;

            WorldLayerManager.Instance.ShowPopupPlantPriceLayer(new PlantPriceData
            {
                PlantId = -1,
                Price = cost,
                TargetPlot = this,
                OnBuyClicked = OnBuyPlotClicked
            }, popupTarget);
        }
        else
        {
            // Náº¿u Ã´ Ä‘áº¥t Ä‘Ã£ Ä‘Æ°á»£c má»Ÿ khÃ³a hoáº·c á»Ÿ tráº¡ng thÃ¡i khÃ¡c, áº©n popup giÃ¡ náº¿u Ä‘ang hiá»ƒn thá»‹
            WorldLayerManager.Instance.HidePlantPriceLayer(popupTarget);
        }
    }

    private void OnBuyPlotClicked(PlantPriceData priceData)
    {
        PurchasePlot(priceData.Price);
    }

    /// <summary>
    /// Mua má»Ÿ khÃ³a Ã´ Ä‘áº¥t: trá»« tiá»n, chuyá»ƒn Ã´ Ä‘áº¥t sang tráº¡ng thÃ¡i Empty (Ä‘áº¥t trá»‘ng)
    /// </summary>
    public bool PurchasePlot(double price)
    {
        if (price > 0)
        {
            if (!PlayableFarmState.ConsumeGold(price))
            {
                Debug.LogWarning($"[Plot] Not enough coins to purchase plot {PlotId}");
                return false;
            }
        }

        // Má»Ÿ khÃ³a Ã´ Ä‘áº¥t -> chuyá»ƒn sang Empty
        bool success = UnlockPlot();
        if (success)
        {
            Transform popupTarget = plantRoot != null ? plantRoot.transform : transform;
            if (WorldLayerManager.Instance != null)
            {
                WorldLayerManager.Instance.HidePlantPriceLayer(popupTarget);
            }
        }

        return success;
    }

    public bool UnlockPlot()
    {
        return _slotSystem != null && _slotSystem.UnlockPlot(_plotId);
    }

    public bool TryOccupy(int plantId)
    {
        return _slotSystem != null && _slotSystem.TryOccupyPlot(_plotId, plantId);
    }

    public void Release()
    {
        _slotSystem?.ReleasePlot(_plotId);
    }

    public void SetPlantLevel(int level)
    {
        _plotData?.SetPlantLevel(level);
        if (_spawnedPlant != null)
        {
            _spawnedPlant.SetLevel(level);
        }
        UpdateUpgradeIconState();
    }

    private void HandlePlotStateChanged(FarmPlotData plotData, FarmPlotState newState)
    {
        UpdateVisualByState(newState);
        UpdatePlantInstance();
        UpStatePlot();
        UpdateUpgradeIconState();
    }

    /// <summary>
    public void UpdatePlantInstance()
    {
        if (_plotData == null)
            return;

        if (_plotData.State == FarmPlotState.Occupied && _plotData.PlantId >= 0)
        {
            if (_spawnedPlant == null || _spawnedPlant.PlantId != _plotData.PlantId)
            {
                ClearSpawnedPlant();

                Transform parent = plantRoot != null ? plantRoot.transform : transform;
                int ringIndex = _plotData != null ? _plotData.RingIndex : _ringIndex;
                _spawnedPlant = PlantSpawner.SpawnPlant(_plotData.PlantId, _plotData.PlantLevel, parent, Vector3.zero, ringIndex);
            }
            else if (_spawnedPlant != null && _spawnedPlant.PlantLevel != _plotData.PlantLevel)
            {
                _spawnedPlant.SetLevel(_plotData.PlantLevel);
            }
        }
        else
        {
            ClearSpawnedPlant();
        }
    }

    private void ClearSpawnedPlant()
    {
        if (_spawnedPlant != null)
        {
            PlantSpawner.DespawnPlant(_spawnedPlant);
            _spawnedPlant = null;
        }
    }

    /// <summary>
    /// Cáº­p nháº­t Sprite hiá»ƒn thá»‹ theo tráº¡ng thÃ¡i hiá»‡n táº¡i cá»§a Ã´ Ä‘áº¥t
    /// </summary>
    public void UpdateVisualByState(FarmPlotState state)
    {
        EnsureVisual();
        if (plotVisual != null)
        {
            plotVisual.SetState(state);
        }
    }

    private void EnsureVisual()
    {
        if (plotVisual == null)
        {
            plotVisual = GetComponent<PlotVisual>();
        }
    }

    private void HandlePlantRootTapped(StaticSelectable selectable)
    {
        if (_plotData == null)
            return;

        Transform popupTarget = plantRoot != null ? plantRoot.transform : transform;

        switch (_plotData.State)
        {
            case FarmPlotState.Locked:
                UpStatePlot();
                break;

            case FarmPlotState.Empty:
                if (WorldLayerManager.Instance != null)
                {
                    WorldLayerManager.Instance.ShowPopupPlantTreeLayer(this, popupTarget);
                }
                break;

            case FarmPlotState.Occupied:
                LunaUIManager.Instance?.ShowPlantUpgrade(this);
                break;

            case FarmPlotState.RingLocked:
            default:
                break;
        }
    }

    private void ApplyPlotData(FarmPlotData farmPlot)
    {
        if (farmPlot == null)
            return;

        _plotId = farmPlot.Id;
        _ringIndex = farmPlot.RingIndex;

        // Scene-authored Plot/PlantRoot transforms are authoritative for the
        // playable. Do not apply the original game's per-slot config offset:
        // it moves a plant back toward the obsolete circular layout.
    }

    #region Upgrade Icon

    private void SubscribeGoldEvent()
    {
        if (!_isGoldSubscribed)
        {
            GameEventBus.OnGoldChange += HandleGoldChanged;
            _isGoldSubscribed = true;
        }
    }

    private void UnsubscribeGoldEvent()
    {
        if (_isGoldSubscribed)
        {
            GameEventBus.OnGoldChange -= HandleGoldChanged;
            _isGoldSubscribed = false;
        }
    }

    private void HandleGoldChanged()
    {
        UpdateUpgradeIconState();
    }

    public void UpdateUpgradeIconState()
    {
        ScenePlayableSO scenario = PlayableScenarioRuntime.CurrentScenario;
        if (scenario != null && !scenario.showUpgrade)
        {
            HideUpgradeIcon();
            return;
        }

        if (CanUpgradePlant())
        {
            ShowUpgradeIcon();
        }
        else
        {
            HideUpgradeIcon();
        }
    }

    public bool CanUpgradePlant()
    {
        if (_plotData == null || _plotData.State != FarmPlotState.Occupied)
            return false;

        Plant plant = CurrentPlant;
        if (plant != null)
        {
            if (plant.IsPlantMaxLevel)
                return false;

            double cost = plant.UpgradeCost;
            return cost <= 0 || PlayableFarmState.HasEnoughGold(cost);
        }

        if (_plotData.PlantId >= 0)
        {
            var config = ConfigHolder.Instance != null && ConfigHolder.Instance.PlantConfig != null
                ? ConfigHolder.Instance.PlantConfig.GetPlant(_plotData.PlantId)
                : null;

            if (config != null)
            {
                int currentLevel = _plotData.PlantLevel;
                int maxLevel = config.GetMaxLevel();

                if (currentLevel >= maxLevel)
                    return false;

                int ringIndex = _plotData != null ? _plotData.RingIndex : _ringIndex;
                double cost = config.GetUpgradeCost(currentLevel, ringIndex);
                return cost <= 0 || PlayableFarmState.HasEnoughGold(cost);
            }
        }

        return false;
    }

    private void ShowUpgradeIcon()
    {
        if (_isShowingUpgradeIcon) return;
        if (WorldLayerManager.Instance == null) return;

        Transform popupTarget = plantRoot != null ? plantRoot.transform : transform;
        WorldLayerManager.Instance.ShowIconUpgradeLayer(popupTarget, upgradeIconOffset, OnUpgradeIconClicked);
        _isShowingUpgradeIcon = true;

        // Tutorial milestone: the first visible affordable upgrade is the
        // authoritative signal until scenario-specific upgrade rules exist.
        PlayableTutorialFlow.Instance?.ReportFirstPlantUpgradeAffordable();
    }

    private void OnUpgradeIconClicked()
    {
        LunaUIManager.Instance?.ShowPlantUpgrade(this);
    }

    private void HideUpgradeIcon()
    {
        if (!_isShowingUpgradeIcon) return;
        if (WorldLayerManager.Instance != null)
        {
            Transform popupTarget = plantRoot != null ? plantRoot.transform : transform;
            WorldLayerManager.Instance.HideIconUpgradeLayer(popupTarget);
        }
        _isShowingUpgradeIcon = false;
    }

    #endregion
}
