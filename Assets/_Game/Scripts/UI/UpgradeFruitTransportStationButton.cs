using UnityEngine;
using UnityEngine.UI;

public class UpgradeFruitTransportStationButton : StaticSelectableButtonBase
{
    [Header("Upgrade Icon Settings")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 iconOffset = new Vector3(0, 1.5f, 0);

    private bool _isShowingIcon;
    private bool _isGoldSubscribed;
    private bool _isTransportSubscribed;

    private Transform Target => targetTransform != null ? targetTransform : transform;
    private TransportConfig Config => ConfigHolder.Instance != null && ConfigHolder.Instance.FarmConfig != null
        ? ConfigHolder.Instance.FarmConfig.transport
        : null;

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
        LunaUIManager.Instance?.ShowTransportUpgrade();
    }

    private void SubscribeEvents()
    {
        if (!_isGoldSubscribed)
        {
            GameEventBus.OnGoldChange += OnGoldChanged;
            _isGoldSubscribed = true;
        }

        if (!_isTransportSubscribed)
        {
            GameEventBus.OnUpdateTransportStation += OnTransportStationUpdated;
            _isTransportSubscribed = true;
        }
    }

    private void UnsubscribeEvents()
    {
        if (_isGoldSubscribed)
        {
            GameEventBus.OnGoldChange -= OnGoldChanged;
            _isGoldSubscribed = false;
        }

        if (_isTransportSubscribed)
        {
            GameEventBus.OnUpdateTransportStation -= OnTransportStationUpdated;
            _isTransportSubscribed = false;
        }
    }

    private void OnGoldChanged()
    {
        UpdateUpgradeIconState();
    }

    private void OnTransportStationUpdated()
    {
        UpdateUpgradeIconState();
    }

    public void UpdateUpgradeIconState()
    {
        SubscribeEvents();

        bool canUpgrade = CanUpgrade();
        if (canUpgrade)
        {
            ShowIcon();
        }
        else
        {
            HideIcon();
        }
    }

    public bool CanUpgrade()
    {
        var config = Config;
        int currentLevel = PlayableFarmState.GetTransportSaveData()?.currentLevel ?? 1;
        int maxLevel = config != null ? config.MaxLevel : 30;
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
        LunaUIManager.Instance?.ShowTransportUpgrade();
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
