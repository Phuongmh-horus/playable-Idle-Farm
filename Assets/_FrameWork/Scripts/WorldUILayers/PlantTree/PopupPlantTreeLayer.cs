using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupPlantTreeLayer : WorldLayerBase
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text plantNameText;
    [SerializeField] private Image plantIcon;
    [SerializeField] private Button plantButton;
    [SerializeField] private Button closeButton;

    private Plot _currentPlot;
    private PlantTreeData _data;

    public event Action<Plot> OnPlantSuccess;

    protected override void Awake()
    {
        base.Awake();
        AutoFindUIElements();

        if (plantButton != null)
        {
            plantButton.onClick.AddListener(PlantButtonOnClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    protected override void OnDestroy()
    {
        if (plantButton != null)
        {
            plantButton.onClick.RemoveListener(PlantButtonOnClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }

        base.OnDestroy();
    }

    private void AutoFindUIElements()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string bName = btn.gameObject.name.ToLower();
            var tmp = btn.GetComponentInChildren<TMP_Text>(true);
            string tText = tmp != null ? tmp.text.ToLower() : "";

            if (closeButton == null && (bName.Contains("close") || tText.Contains("close") || bName.Contains("exit")))
            {
                closeButton = btn;
            }
            else if (plantButton == null && (bName.Contains("plant") || bName.Contains("seed") || bName.Contains("button")))
            {
                plantButton = btn;
            }
        }

        if (plantNameText == null)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (closeButton == null || t.transform.parent != closeButton.transform)
                {
                    plantNameText = t;
                    break;
                }
            }
        }

        if (plantIcon == null)
        {
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name.ToLower().Contains("icon") || img.gameObject.name.ToLower().Contains("plant"))
                {
                    plantIcon = img;
                    break;
                }
            }
        }
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
        string plantName = plantConfig != null && !string.IsNullOrEmpty(plantConfig.PlantName) 
            ? plantConfig.PlantName 
            : $"Plant #{plantId + 1}";

        if (plantNameText != null)
        {
            plantNameText.text = plantName;
        }

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
            FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
            int configPlantId = farmConfig != null
                ? farmConfig.GetInitialPlantId(_currentPlot.PlotId, ConfigHolder.Instance?.PlantConfig)
                : _currentPlot.PlotData.Config.defaultPlantId;
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
            PlayableFarmState.UnlockCollectionPlant(plantId, 1);
            Plot plantedPlot = _currentPlot;
            OnPlantSuccess?.Invoke(plantedPlot);
        }

        Close();
    }

    public void Close()
    {
        WorldLayerManager.Instance.Hide(this);
    }

    public override void CleanupBeforeHide()
    {
        _currentPlot = null;
        _data = null;
        base.CleanupBeforeHide();
    }
}

public class PlantTreeData
{
    public Plot TargetPlot;
    public int PlantId;
    public Action<PlantTreeData> OnPlantClicked;
}
