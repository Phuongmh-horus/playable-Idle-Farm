using UnityEngine;
using UnityEngine.UI;

public class UpgradeFruitTransportStationButton : StaticSelectableButtonBase
{
    [Header("Upgrade Icon Settings")]
    [SerializeField] private FruitTransportStation transportStation;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 iconOffset = new Vector3(0, 1.5f, 0);

    private IconUpgradeLayer _iconLayer;
    private bool _isGoldSubscribed;
    private bool _isTransportSubscribed;

    private Transform Target => targetTransform != null
        ? targetTransform
        : (transportStation != null ? transportStation.VehicleTransform : transform);

    private void OnValidate()
    {
        if (transportStation == null) transportStation = GetComponent<FruitTransportStation>();
        if (targetTransform == null && transportStation != null) targetTransform = transportStation.VehicleTransform;
    }

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
        ScenePlayableSO scenario = PlayableScenarioRuntime.CurrentScenario;
        if (scenario != null && (!scenario.showUpgrade || !scenario.showConveyorAndSell)) return;
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
            if (transportStation == null) transportStation = GetComponent<FruitTransportStation>();
            if (transportStation != null)
            {
                transportStation.OnVehicleLevelChanged += OnVehicleLevelChanged;
                transportStation.OnVehicleStateChanged += OnVehicleStateChanged;
            }
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
            if (transportStation != null)
            {
                transportStation.OnVehicleLevelChanged -= OnVehicleLevelChanged;
                transportStation.OnVehicleStateChanged -= OnVehicleStateChanged;
            }
            _isTransportSubscribed = false;
        }
    }

    private void OnGoldChanged()
    {
        UpdateUpgradeIconState();
    }

    private void OnVehicleLevelChanged(int level)
    {
        UpdateUpgradeIconState();
    }

    private void OnVehicleStateChanged(TransportVehicleState state)
    {
        UpdateUpgradeIconState();
    }

    public void UpdateUpgradeIconState()
    {
        SubscribeEvents();

        ScenePlayableSO scenario = PlayableScenarioRuntime.CurrentScenario;
        bool canUpgrade = (scenario == null || (scenario.showUpgrade && scenario.showConveyorAndSell)) && CanUpgrade();
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
        if (transportStation == null) transportStation = GetComponent<FruitTransportStation>();
        return transportStation != null && transportStation.IsIdleAtStation && transportStation.CanUpgrade;
    }

    private void ShowIcon()
    {
        if (_iconLayer != null && _iconLayer.gameObject.activeInHierarchy && _iconLayer.FollowTarget == Target) return;
        _iconLayer = null;
        if (WorldLayerManager.Instance == null) return;

        _iconLayer = WorldLayerManager.Instance.ShowIconUpgradeLayer(Target, iconOffset, OnUpgradeIconClicked);
    }

    private void OnUpgradeIconClicked()
    {
        ScenePlayableSO scenario = PlayableScenarioRuntime.CurrentScenario;
        if (scenario != null && (!scenario.showUpgrade || !scenario.showConveyorAndSell)) return;
        LunaUIManager.Instance?.ShowTransportUpgrade();
    }

    private void HideIcon()
    {
        if (_iconLayer != null && WorldLayerManager.Instance != null)
        {
            WorldLayerManager.Instance.Hide(_iconLayer, false);
        }
        _iconLayer = null;
    }
}
