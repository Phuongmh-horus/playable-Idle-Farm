using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupPlantTreeLayer : WorldLayerBase
{
    [Header("UI Elements")]
    [SerializeField] private Image plantIcon;
    [SerializeField] private Button plantButton;

    private Plot _currentPlot;
    private PlantTreeData _data;

    public event Action<Plot> OnPlantSuccess;

    protected override void Awake()
    {
        base.Awake();

        if (plantButton != null)
        {
            plantButton.onClick.AddListener(PlantButtonOnClick);
        }
    }

    protected override void OnDestroy()
    {
        if (plantButton != null)
        {
            plantButton.onClick.RemoveListener(PlantButtonOnClick);
        }

        base.OnDestroy();
    }

    public void InitData(Plot plot)
    {
        _currentPlot = plot;
        _data = null;
        RefreshUI();
    }

    public void InitData(PlantTreeData data)
    {
        _data = data;
        _currentPlot = data != null ? data.TargetPlot : null;
        RefreshUI();
    }

    public void RefreshUI()
    {
        int plantId = GetPlantId();

        PlantConfigData plantConfig = ConfigHolder.Instance?.PlantConfig?.GetPlant(plantId);

        if (plantIcon != null && plantConfig != null)
        {
            Sprite iconSprite = plantConfig.fruitIcon != null ? plantConfig.fruitIcon : plantConfig.GetIcon(1);
            if (iconSprite != null)
            {
                plantIcon.sprite = iconSprite;
            }
        }

        if (plantButton != null)
        {
            plantButton.interactable = true;
        }
    }

    private int GetPlantId()
    {
        if (_data != null && _data.PlantId >= 0)
        {
            return _data.PlantId;
        }

        if (_currentPlot != null && _currentPlot.PlotData != null && _currentPlot.PlotData.Config != null)
        {
            int configPlantId = _currentPlot.PlotData.Config.defaultPlantId;
            return configPlantId >= 0 ? configPlantId : 0;
        }

        return 0;
    }

    public void PlantButtonOnClick()
    {
        if (_data != null && _data.OnPlantClicked != null)
        {
            _data.OnPlantClicked.Invoke(_data);
            Close();
            return;
        }

        if (_currentPlot == null || _currentPlot.State != FarmPlotState.Empty)
        {
            Close();
            return;
        }

        int plantId = GetPlantId();

        bool success = _currentPlot.TryOccupy(plantId);
        if (success)
        {
            DataManager.UpdatePlotData(_currentPlot.PlotId, PlotState.Planted, plantId, 1);
            DataManager.UnlockCollectionPlant(plantId, 1);
            GameEventBus.OnCropPlantedEvent?.Invoke(_currentPlot.PlotId, plantId);
            GameEventBus.OnFarmEconomyChanged?.Invoke();
            LayerManager.Instance?.ShowPopupInfoPlantLayer(new PopupInfoPlantLayerData
            {
                PlantId = plantId,
                PlantLevel = 1,
                RingIndex = _currentPlot.RingIndex
            });
            Plot plantedPlot = _currentPlot;
            OnPlantSuccess?.Invoke(plantedPlot);
        }

        Close();
    }

    public void Close()
    {
        WorldLayerManager.Instance.Hide(this);
    }
}

public class PlantTreeData
{
    public Plot TargetPlot;
    public int PlantId;
    public Action<PlantTreeData> OnPlantClicked;
}
