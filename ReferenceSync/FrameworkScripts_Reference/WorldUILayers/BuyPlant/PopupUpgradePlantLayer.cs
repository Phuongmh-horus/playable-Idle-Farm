using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupUpgradePlantLayer : WorldLayerBase
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text plantName;
    [SerializeField] private TMP_Text profitTxt;
    [SerializeField] private TMP_Text waterRequiredTxt;
    [SerializeField] private SliderValidator levelProgressSlider;
    [SerializeField] private Image plantIcon;
    [SerializeField] private ImageStateHandle[] starStateHandles; // Từ 1 đến 5 sao (mốc level 5, 10,..25)

    private Plot _currentPlot;
    private Plant _currentPlant;
    private bool _isStarUpgrading;

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

    protected override void OnEnable()
    {
        base.OnEnable();
        _isStarUpgrading = false;
        SetInteractionsBlocked(false);

        GameEventBus.OnCashChange += OnCashChanged;
        GameEventBus.OnCropUpgradedEvent += OnCropUpgraded;
        RefreshUI();
    }

    protected override void OnDisable()
    {
        _isStarUpgrading = false;
        SetInteractionsBlocked(false);

        GameEventBus.OnCashChange -= OnCashChanged;
        GameEventBus.OnCropUpgradedEvent -= OnCropUpgraded;
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        GameEventBus.OnCashChange -= OnCashChanged;
        GameEventBus.OnCropUpgradedEvent -= OnCropUpgraded;

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
        if (_currentPlant == null && _currentPlot != null)
        {
            _currentPlant = _currentPlot.CurrentPlant;
        }

        if (_currentPlant == null)
        {
            CloseOnClick();
            return;
        }

        PlantConfigData config = _currentPlant.Config;
        int currentLevel = _currentPlant.PlantLevel;
        int maxLevel = _currentPlant.MaxLevel;
        int ringIndex = _currentPlot != null ? _currentPlot.RingIndex : 0;

        // 1. Plant Name
        if (plantName != null)
        {
            string displayName = config != null ? config.PlantName : "Plant";
            plantName.text = _currentPlant.IsMega ? $"Mega {displayName}" : displayName;
        }

        // 2. Plant / Fruit Icon
        if (plantIcon != null && config != null)
        {
            Sprite iconSprite;
            if (_currentPlant.IsMega)
            {
                iconSprite = config.GetMegaFruitIcon();
            }
            else
            {
                iconSprite = config.fruitIcon != null ? config.fruitIcon : config.GetIcon(currentLevel);
            }

            if (iconSprite != null)
            {
                plantIcon.sprite = iconSprite;
            }
        }

        // 3. Profit (Cash per harvest)
        if (profitTxt != null)
        {
            double price = _currentPlant.IsMega
                ? (config?.GetMegaFruitPrice() ?? 0)
                : (config?.GetFruitPrice(currentLevel, ringIndex) ?? 0);
            profitTxt.text = T_Utility.FormatCash(price);
        }

        // 4. Water Required
        if (waterRequiredTxt != null)
        {
            float waterReq = _currentPlant.WaterRequirement;
            waterRequiredTxt.text = Mathf.RoundToInt(waterReq).ToString();
        }

        // 5. Level Progress & Stars
        if (_currentPlant.IsMega)
        {
            if (levelText != null) levelText.text = "MEGA";
            if (levelProgressSlider != null) levelProgressSlider.Value = 1f;

            if (starStateHandles != null)
            {
                for (int i = 0; i < starStateHandles.Length; i++)
                {
                    if (starStateHandles[i] != null)
                    {
                        starStateHandles[i].SetupState(1);
                    }
                }
            }

            if (costText != null) costText.text = "MAX";
            if (upgradeButton != null) upgradeButton.interactable = false;
            return;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv. {currentLevel}/{maxLevel}";
        }

        if (levelProgressSlider != null)
        {
            float fill = maxLevel > 0 ? Mathf.Clamp01((float)currentLevel / maxLevel) : 1f;
            levelProgressSlider.SetValueSmooth(fill, 0.15f);
        }

        if (starStateHandles != null && starStateHandles.Length > 0)
        {
            int stageStep = config != null && config.levelsPerVisualStage > 0 ? config.levelsPerVisualStage : 5;
            for (int i = 0; i < starStateHandles.Length; i++)
            {
                if (starStateHandles[i] == null) continue;
                int milestoneLevel = (i + 1) * stageStep;
                bool isStarActive = currentLevel >= milestoneLevel;
                starStateHandles[i].SetupState(isStarActive ? 1 : 0);
            }
        }

        // 6. Upgrade / Max Button
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
                costText.text = upgradeCost > 0 ? $"${T_Utility.FormatCash(upgradeCost)}" : "FREE";
            }
            if (upgradeButton != null)
            {
                upgradeButton.interactable = DataManager.HasEnoughCash(upgradeCost);
            }
        }
    }

    public override void CleanupBeforeHide()
    {
        _isStarUpgrading = false;
        SetInteractionsBlocked(false);

        base.CleanupBeforeHide();
    }

    private void SetInteractionsBlocked(bool blocked)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = !blocked;
            canvasGroup.interactable = !blocked;
        }

        if (closeButton != null)
        {
            closeButton.interactable = !blocked;
        }

        if (upgradeButton != null && blocked)
        {
            upgradeButton.interactable = false;
        }
    }

    private void UpgradeButtonOnClick()
    {
        if (_isStarUpgrading)
            return;

        if (_currentPlot == null || _currentPlant == null)
            return;

        if (_currentPlant.IsMega)
            return;

        int currentLevel = _currentPlant.PlantLevel;
        int maxLevel = _currentPlant.MaxLevel;

        if (currentLevel >= maxLevel)
            return;

        double upgradeCost = _currentPlant.UpgradeCost;

        if (upgradeCost > 0)
        {
            if (!DataManager.ConsumeCash(upgradeCost))
            {
                Debug.LogWarning("[PopupUpgradePlantLayer] Not enough cash to upgrade plant!");
                RefreshUI();
                return;
            }
        }

        int prevLevel = _currentPlant.PlantLevel;
        int stageStep = _currentPlant.Config != null && _currentPlant.Config.levelsPerVisualStage > 0
            ? _currentPlant.Config.levelsPerVisualStage
            : 5;
        int prevStars = prevLevel / stageStep;

        bool upgraded = _currentPlant.TryUpgrade();
        if (upgraded)
        {
            if (FXManager.Instance != null && upgradeButton != null)
            {
                FXManager.Instance.PlayCashUIParticle(upgradeButton.transform);
            }

            if (_currentPlot != null)
            {
                _currentPlot.SetPlantLevel(_currentPlant.PlantLevel);
            }

            int newLevel = _currentPlant.PlantLevel;
            int newStars = newLevel / stageStep;
            bool reachedNewStar = newStars > prevStars;

            // Cập nhật và lưu lại vào SaveData
            DataManager.UpdatePlotData(
                _currentPlot.PlotId,
                PlotState.Planted,
                _currentPlant.PlantId,
                _currentPlant.PlantLevel
            );
            DataManager.UnlockCollectionPlant(_currentPlant.PlantId, _currentPlant.PlantLevel);

            GameEventBus.OnCropUpgradedEvent?.Invoke(_currentPlant.PlantId, _currentPlant.PlantLevel);
            GameEventBus.OnFarmEconomyChanged?.Invoke();

            if (reachedNewStar)
            {
                RefreshUI();
                HandleReachedNewStarAsync().Forget();
            }
            else
            {
                RefreshUI();
            }
        }
    }

    private async UniTaskVoid HandleReachedNewStarAsync()
    {
        _isStarUpgrading = true;
        SetInteractionsBlocked(true);

        Vector3 focusPos = _currentPlot != null ? _currentPlot.PlantPosition : _currentPlant.transform.position;
        Plant targetPlant = _currentPlant;

        try
        {
            await UniTask.WaitForSeconds(0.35f, cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        GameEventBus.RequestCameraZoomInAndBack?.Invoke(
            focusPos,
            1.2f,   // Mức zoom gần
            0.45f,  // Thời gian zoom in
            1.0f,   // Thời gian dừng ngắm nhìn cây đầy quả
            0.45f,  // Thời gian zoom out về lại vị trí ban đầu
            null,   // Offset
            true,   // Khóa thao tác tay lúc đang diễn hoạt
            () =>
            {
                // Khi camera zoom in đến nơi -> cây mới bung đầy quả kèm animation
                if (targetPlant != null)
                {
                    targetPlant.FillAllFruits(true);
                }
            },
            null,   // Callback onComplete
            0.25f   // Delay 0.25s sau khi đóng popup trước khi camera bắt đầu zoom
        );

        _isStarUpgrading = false;
        SetInteractionsBlocked(false);
        WorldLayerManager.Instance.Hide(this);
    }

    private void OnCashChanged()
    {
        if (_isStarUpgrading) return;
        RefreshUI();
    }

    private void OnCropUpgraded(int plantId, int level)
    {
        if (_isStarUpgrading) return;
        if (_currentPlant != null && _currentPlant.PlantId == plantId)
        {
            RefreshUI();
        }
    }

    private void CloseOnClick()
    {
        if (_isStarUpgrading) return;
        WorldLayerManager.Instance.Hide(this);
    }
}

