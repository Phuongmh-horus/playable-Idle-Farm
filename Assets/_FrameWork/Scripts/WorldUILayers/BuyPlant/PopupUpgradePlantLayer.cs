using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupUpgradePlantLayer : WorldLayerBase
{
    [Header("UI Elements")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;

    private Plot _currentPlot;
    private Plant _currentPlant;

    protected override void Awake()
    {
        base.Awake();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseOnClick);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(UpgradeButtonOnClick);
        }
    }

    protected override void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseOnClick);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(UpgradeButtonOnClick);
        }

        base.OnDestroy();
    }

    public void InitData(Plot plot)
    {
        _currentPlot = plot;
        _currentPlant = plot != null ? plot.CurrentPlant : null;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_currentPlant == null)
        {
            if (_currentPlot != null)
            {
                _currentPlant = _currentPlot.CurrentPlant;
            }
        }

        if (_currentPlant == null)
        {
            CloseOnClick();
            return;
        }

        int currentLevel = _currentPlant.PlantLevel;
        int maxLevel = _currentPlant.MaxLevel;

        if (levelText != null)
        {
            levelText.text = $"Lv. {currentLevel}/{maxLevel}";
        }

        if (currentLevel >= maxLevel)
        {
            if (costText != null) costText.text = "MAX";
            if (upgradeButton != null) upgradeButton.interactable = false;
        }
        else
        {
            double upgradeCost = _currentPlant.UpgradeCost;
            if (costText != null)
            {
                costText.text = upgradeCost > 0 ? $"${T_Utility.FormatGold(upgradeCost)}" : "UPGRADE";
            }
            if (upgradeButton != null)
            {
                bool canAfford = PlayableFarmState.HasEnoughGold(upgradeCost);
                upgradeButton.interactable = canAfford;
            }
        }
    }

    private void UpgradeButtonOnClick()
    {
        if (_currentPlot == null || _currentPlant == null)
            return;

        if (_currentPlant.IsPlantMaxLevel)
            return;

        double upgradeCost = _currentPlant.UpgradeCost;

        if (upgradeCost > 0)
        {
            if (!PlayableFarmState.ConsumeGold(upgradeCost))
            {
                Debug.LogWarning("[PopupUpgradePlantLayer] Not enough coins to upgrade plant!");
                return;
            }
        }

        bool upgraded = _currentPlant.TryUpgrade();
        if (upgraded)
        {
            if (_currentPlot != null)
            {
                _currentPlot.SetPlantLevel(_currentPlant.PlantLevel);
            }

            // Cáº­p nháº­t vÃ  lÆ°u láº¡i vÃ o SaveData
            PlayableFarmState.UpdatePlotData(
                _currentPlot.PlotId,
                PlotState.Planted,
                _currentPlant.PlantId,
                _currentPlant.PlantLevel
            );
            PlayableFarmState.UnlockCollectionPlant(_currentPlant.PlantId, _currentPlant.PlantLevel);

            GameEventBus.OnCropUpgradedEvent?.Invoke(_currentPlant.PlantId, _currentPlant.PlantLevel);

            RefreshUI();
        }
    }

    public void CloseOnClick()
    {
        WorldLayerManager.Instance.Hide(this);
    }
}

