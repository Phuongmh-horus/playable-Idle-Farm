using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupMergePlant : WorldLayerBase
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Button mergeButton;
    [SerializeField] private Image oldPlantIcon; // sprite index 7 (đủ 6 quả)
    [SerializeField] private Image newPlantIcon; // sprite đủ nước của cây merge
    [SerializeField] private TextMeshProUGUI gemAmount;

    private Plot _currentPlot;
    private int _sectorIndex = -1;
    private int _plantId = -1;
    private bool _isMerging;

    protected override void Awake()
    {
        base.Awake();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseOnClick);
        }

        if (mergeButton != null)
        {
            mergeButton.onClick.AddListener(MergeButtonOnClick);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _isMerging = false;
        SetInteractionsBlocked(false);

        GameEventBus.OnGemChange += RefreshUI;
        GameEventBus.OnSectorMergedEvent += HandleSectorMerged;
        GameEventBus.OnCropPlantedEvent += HandleCropPlanted;
        GameEventBus.OnCropUpgradedEvent += HandleCropUpgraded;

        RefreshUI();
    }

    protected override void OnDisable()
    {
        _isMerging = false;
        SetInteractionsBlocked(false);

        GameEventBus.OnGemChange -= RefreshUI;
        GameEventBus.OnSectorMergedEvent -= HandleSectorMerged;
        GameEventBus.OnCropPlantedEvent -= HandleCropPlanted;
        GameEventBus.OnCropUpgradedEvent -= HandleCropUpgraded;

        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        GameEventBus.OnGemChange -= RefreshUI;
        GameEventBus.OnSectorMergedEvent -= HandleSectorMerged;
        GameEventBus.OnCropPlantedEvent -= HandleCropPlanted;
        GameEventBus.OnCropUpgradedEvent -= HandleCropUpgraded;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseOnClick);
        }

        if (mergeButton != null)
        {
            mergeButton.onClick.RemoveListener(MergeButtonOnClick);
        }

        base.OnDestroy();
    }

    public void InitData(Plot plot)
    {
        _currentPlot = plot;
        if (plot != null && plot.PlotData != null)
        {
            _sectorIndex = plot.PlotData.SectorIndex;
            _plantId = plot.CurrentPlant != null ? plot.CurrentPlant.PlantId : plot.PlotData.PlantId;
        }
        else
        {
            _sectorIndex = -1;
            _plantId = -1;
        }

        RefreshUI();
    }

    private void HandleSectorMerged(int sectorIndex, int plantId)
    {
        if (_sectorIndex == sectorIndex)
        {
            CloseOnClick();
        }
        else
        {
            RefreshUI();
        }
    }

    private void HandleCropPlanted(int plotId, int plantId) => RefreshUI();
    private void HandleCropUpgraded(int plantId, int level) => RefreshUI();

    public void RefreshUI()
    {
        if (_isMerging)
            return;

        if (_currentPlot == null || _currentPlot.PlotData == null)
        {
            return;
        }

        _sectorIndex = _currentPlot.PlotData.SectorIndex;
        _plantId = _currentPlot.CurrentPlant != null ? _currentPlot.CurrentPlant.PlantId : _currentPlot.PlotData.PlantId;

        PlantConfigData plantConfig = ConfigHolder.Instance?.PlantConfig?.GetPlant(_plantId);
        if (plantConfig == null)
        {
            return;
        }

        // 1. Update Old Plant Icon (Sprite index 7 / max fruit stage)
        if (oldPlantIcon != null)
        {
            int maxFruits = plantConfig.GetMaxFruitSlots(plantConfig.GetMaxLevel());
            Sprite oldSprite = plantConfig.GetPlantSprite(maxFruits);
            if (oldSprite != null)
            {
                oldPlantIcon.sprite = oldSprite;
            }
        }

        // 2. Update New Plant Icon (Mega watered sprite)
        if (newPlantIcon != null)
        {
            Sprite megaSprite = plantConfig.GetMegaSprite(true);
            if (megaSprite != null)
            {
                newPlantIcon.sprite = megaSprite;
            }
        }

        // 3. Update Gem Cost & Merge Button state
        double mergeCost = plantConfig.GetMegaMergeCost();
        if (gemAmount != null)
        {
            gemAmount.text = T_Utility.FormatGem(mergeCost);
        }

        FarmPlotSystem plotSystem = FarmSystem.Instance?.PlotSystem;
        bool canMergeSector = plotSystem != null && _sectorIndex >= 0 && plotSystem.CanMergeSector(_sectorIndex);
        bool hasEnoughGem = DataManager.HasEnoughGem(mergeCost);

        if (mergeButton != null)
        {
            mergeButton.interactable = canMergeSector && hasEnoughGem;
        }
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

        if (mergeButton != null && blocked)
        {
            mergeButton.interactable = false;
        }
    }

    private void MergeButtonOnClick()
    {
        if (_isMerging)
            return;

        if (_sectorIndex < 0 || _currentPlot == null)
        {
            return;
        }

        FarmPlotSystem plotSystem = FarmSystem.Instance?.PlotSystem;
        if (plotSystem == null || !plotSystem.CanMergeSector(_sectorIndex))
        {
            Debug.LogWarning($"[PopupMergePlant] Sector {_sectorIndex} is not ready to merge!");
            return;
        }

        PlantConfigData plantConfig = ConfigHolder.Instance?.PlantConfig?.GetPlant(_plantId);
        double mergeCost = plantConfig != null ? plantConfig.GetMegaMergeCost() : 0;

        if (mergeCost > 0 && !DataManager.HasEnoughGem(mergeCost))
        {
            Debug.LogWarning($"[PopupMergePlant] Not enough gem to merge sector {_sectorIndex}. Required: {mergeCost}");
            return;
        }

        int targetSector = _sectorIndex;
        Vector3 focusPos = _currentPlot != null ? _currentPlot.PlantPosition : transform.position;

        _isMerging = true;
        SetInteractionsBlocked(true);

        bool success = plotSystem.MergeSector(targetSector);
        if (success)
        {
            if (FXManager.Instance != null && mergeButton != null)
            {
                FXManager.Instance.PlayCashUIParticle(mergeButton.transform);
            }

            MegaPlot megaPlot = plotSystem.GetMegaPlot(targetSector);
            if (megaPlot != null)
            {
                focusPos = megaPlot.PlantPosition;
            }

            // Kích hoạt Zoom Camera chiêm ngưỡng cây Mega sau khi ghép thành công
            GameEventBus.RequestCameraZoomInAndBack?.Invoke(
                focusPos,
                1.2f,   // Mức zoom gần
                0.45f,  // Thời gian zoom in
                1.0f,   // Thời gian dừng ngắm nhìn cây Mega
                0.45f,  // Thời gian zoom out về lại vị trí ban đầu
                null,   // Offset
                true,   // Khóa thao tác tay lúc đang diễn hoạt
                () =>
                {
                    // Khi camera zoom in đến nơi -> phát hiệu ứng diễn hoạt bung lớn của cây Mega
                    MegaPlot targetMega = plotSystem.GetMegaPlot(targetSector);
                    if (targetMega != null && targetMega.CurrentPlant != null)
                    {
                        targetMega.CurrentPlant.FillAllFruits();
                    }
                },
                null,   // Callback onComplete
                0.25f   // Delay 0.25s sau khi đóng popup trước khi camera bắt đầu zoom
            );

            CloseOnClick();
        }
        else
        {
            _isMerging = false;
            SetInteractionsBlocked(false);
            RefreshUI();
        }
    }

    private void CloseOnClick()
    {
        if (_isMerging)
        {
            _isMerging = false;
            SetInteractionsBlocked(false);
        }

        WorldLayerManager.Instance.Hide(this);
    }
}
