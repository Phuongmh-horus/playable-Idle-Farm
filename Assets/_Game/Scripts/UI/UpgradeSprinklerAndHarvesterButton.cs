using UnityEngine;

public class UpgradeSprinklerAndHarvesterButton : StaticSelectableButtonBase
{
    [Header("Upgrade Icon Settings")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 iconOffset = new Vector3(0, 1.5f, 0);

    private bool _isShowingIcon;
    private bool _isGoldSubscribed;
    private bool _isIrrigationSubscribed;
    private bool _isHarvestSubscribed;

    private Transform Target => targetTransform != null ? targetTransform : transform;
    private FarmIrrigationSystem IrrigationSystem => FarmSystem.Instance != null ? FarmSystem.Instance.IrrigationSystem : null;
    private FarmHarvestSystem HarvestSystem => FarmSystem.Instance != null ? FarmSystem.Instance.HarvestSystem : null;

    private void Start()
    {
        SubscribeEvents();
        UpdateUpgradeIconState();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        UpdateUpgradeIconState();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        HideIcon();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        HideIcon();
    }

    protected override void OnButtonClicked(StaticSelectable selectable)
    {
        ShowAvailableUpgrade();
    }

    private void SubscribeEvents()
    {
        if (!_isGoldSubscribed)
        {
            GameEventBus.OnGoldChange += OnGoldChanged;
            _isGoldSubscribed = true;
        }

        var farm = FarmSystem.Instance;
        if (farm != null)
        {
            if (!_isIrrigationSubscribed && farm.IrrigationSystem != null)
            {
                farm.IrrigationSystem.OnSprinklerLevelChanged += OnSprinklerLevelChanged;
                _isIrrigationSubscribed = true;
            }

            if (!_isHarvestSubscribed && farm.HarvestSystem != null)
            {
                farm.HarvestSystem.OnHarvesterLevelChanged += OnHarvesterLevelChanged;
                _isHarvestSubscribed = true;
            }
        }
    }

    private void UnsubscribeEvents()
    {
        if (_isGoldSubscribed)
        {
            GameEventBus.OnGoldChange -= OnGoldChanged;
            _isGoldSubscribed = false;
        }

        var farm = FarmSystem.Instance;
        if (farm != null)
        {
            if (_isIrrigationSubscribed && farm.IrrigationSystem != null)
            {
                farm.IrrigationSystem.OnSprinklerLevelChanged -= OnSprinklerLevelChanged;
                _isIrrigationSubscribed = false;
            }

            if (!_isHarvestSubscribed && farm.HarvestSystem != null)
            {
                farm.HarvestSystem.OnHarvesterLevelChanged -= OnHarvesterLevelChanged;
                _isHarvestSubscribed = false;
            }
        }
        else
        {
            _isIrrigationSubscribed = false;
            _isHarvestSubscribed = false;
        }
    }

    private void OnGoldChanged()
    {
        UpdateUpgradeIconState();
    }

    private void OnSprinklerLevelChanged(int newLevel)
    {
        UpdateUpgradeIconState();
    }

    private void OnHarvesterLevelChanged(int newLevel)
    {
        UpdateUpgradeIconState();
    }

    public void UpdateUpgradeIconState()
    {
        SubscribeEvents();

        bool canUpgrade = CanUpgradeAny();
        if (canUpgrade)
        {
            ShowIcon();
        }
        else
        {
            HideIcon();
        }
    }

    public bool CanUpgradeAny()
    {
        return CanUpgradeSprinkler() || CanUpgradeHarvester();
    }

    public bool CanUpgradeSprinkler()
    {
        if (IrrigationSystem != null)
        {
            return IrrigationSystem.CanUpgrade;
        }

        var config = ConfigHolder.Instance?.FarmConfig?.sprinkler;
        int currentLevel = PlayableFarmState.GetSprinklerSaveData()?.currentLevel ?? 1;
        int maxLevel = (config != null && config.maxLevel > 0) ? config.maxLevel : 30;
        if (currentLevel >= maxLevel) return false;

        double cost = config != null ? config.GetUpgradeCost(currentLevel) : 0;
        return cost <= 0 || PlayableFarmState.HasEnoughGold(cost);
    }

    public bool CanUpgradeHarvester()
    {
        if (HarvestSystem != null)
        {
            return HarvestSystem.CanUpgrade;
        }

        var config = ConfigHolder.Instance?.FarmConfig?.harvester;
        int currentLevel = PlayableFarmState.GetHarvesterSaveData()?.currentLevel ?? 1;
        int maxLevel = (config != null && config.maxLevel > 0) ? config.maxLevel : 9;
        if (currentLevel >= maxLevel) return false;

        double cost = config != null ? config.GetUpgradeCost(currentLevel) : 0;
        return cost <= 0 || PlayableFarmState.HasEnoughGold(cost);
    }

    private void ShowIcon()
    {
        if (_isShowingIcon) return;
        if (WorldLayerManager.Instance == null) return;

        WorldLayerManager.Instance.ShowIconUpgradeLayer(Target, iconOffset, OnUpgradeIconClicked);
        _isShowingIcon = true;
    }

    private void OnUpgradeIconClicked()
    {
        ShowAvailableUpgrade();
    }

    // This proxy represents both systems in the current scene. Prefer the
    // sprinkler when both are affordable; otherwise open the harvester popup.
    private void ShowAvailableUpgrade()
    {
        if (CanUpgradeSprinkler()) LunaUIManager.Instance?.ShowIrrigationUpgrade();
        else if (CanUpgradeHarvester()) LunaUIManager.Instance?.ShowHarvestUpgrade();
    }

    private void HideIcon()
    {
        if (!_isShowingIcon) return;
        if (WorldLayerManager.Instance != null)
        {
            WorldLayerManager.Instance.HideIconUpgradeLayer(Target);
        }
        _isShowingIcon = false;
    }
}
