using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public enum TransportVehicleState
{
    IdleAtStation = 0,
    Departing = 1, 
    SellingOffScreen = 2, 
    Returning = 3
}

public sealed class FruitTransportStation : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform idlePoint;
    [SerializeField] private Transform exitPoint;

    [SerializeField] private FruitTransportStationConfig config;
    [SerializeField] private TransportVehicle vehicle;
    [Header("Playable cargo UI")]
    [SerializeField] private Vector3 capacityLayerOffset = new Vector3(0f, 1.25f, 0f);

    private int _vehicleLevel;
    private int _loadedFruitCount;
    private int _arrivedFruitCount;
    private double _loadedRevenue;
    private readonly List<LoadedFruitItem> _loadedFruits = new List<LoadedFruitItem>();
    private readonly List<AnimatedFruit> _activeAnimatedFruits = new List<AnimatedFruit>();
    private TransportVehicleState _state = TransportVehicleState.IdleAtStation;
    private CapacityFruitTransport _capacityLayer;
    
    public bool IsFull => _loadedFruitCount >= GetMaxCapacity();
    public int LoadedFruitCount => _loadedFruitCount;
    public double LoadedRevenue => _loadedRevenue;
    public int Capacity => GetMaxCapacity();
    public bool IsIdleAtStation => _state == TransportVehicleState.IdleAtStation;
    public int Level => _vehicleLevel;
    public int MaxLevel => ConfigHolder.Instance?.FarmConfig?.transport?.MaxLevel ?? 1;
    public double NextUpgradeCost => _vehicleLevel < MaxLevel
        ? (ConfigHolder.Instance?.FarmConfig?.transport?.GetUpgradeCost(_vehicleLevel) ?? -1)
        : -1;
    public bool CanUpgrade => _vehicleLevel < MaxLevel && (NextUpgradeCost <= 0 || PlayableFarmState.HasEnoughGold(NextUpgradeCost));

    #region Events

    public event Action<int, int, double> OnFruitDelivered;
    public event Action<int, int> OnFruitLoaded;
    public event Action<TransportVehicleState> OnVehicleStateChanged;
    public event Action<int> OnVehicleLevelChanged;

    #endregion

    private void Awake()
    {
        if (vehicle != null && idlePoint != null)
        {
            vehicle.PlaceAt(idlePoint.position);
        }
        GameEventBus.OnUpdateTransportStation += UpdateFromSave;
    }

    private void Start()
    {
        UpdateFromSave();
        RefreshCapacityLayer();
    }

    private void OnDestroy()
    {
        GameEventBus.OnUpdateTransportStation -= UpdateFromSave;
        CancelActiveMotions();
        if (WorldLayerManager.Instance != null) WorldLayerManager.Instance.HideCapacityFruitTransport(transform);
    }

    private void UpdateFromSave()
    {
        if (PlayableFarmState.PlayerData != null)
        {
            var transportSave = PlayableFarmState.GetTransportSaveData();
            if (transportSave != null && transportSave.currentLevel > 0)
            {
                SetLevel(transportSave.currentLevel);
                return;
            }
        }
        SetLevel(1);
    }

    private void SetState(TransportVehicleState newState)
    {
        _state = newState;
        OnVehicleStateChanged?.Invoke(_state);

    }

    public void CollectFruitsWithAnimation(int plantId, int fruitCount, Vector3 startPos, int plantLevel, int ringIndex)
    {
        if (!IsIdleAtStation || IsFull)
            return;

        PlantConfigData plantConfig = ConfigHolder.Instance?.PlantConfig?.GetPlant(plantId);
        int spaceLeft = GetCapacityLeft();
        int acceptedCount = Mathf.Min(fruitCount, spaceLeft);

        if (acceptedCount > 0 && plantConfig != null)
        {
            double price = plantConfig.GetFruitPrice(plantLevel, ringIndex);
            double totalGained = price * acceptedCount;

            _loadedFruitCount += acceptedCount;
            _loadedRevenue += totalGained;

            LoadedFruitItem item = new LoadedFruitItem
            {
                PlantId = plantId,
                Count = acceptedCount,
                Revenue = totalGained
            };
            _loadedFruits.Add(item);
            RefreshCapacityLayer();
        }

        Transform targetCargo = vehicle != null && vehicle.CargoRoot != null ? vehicle.CargoRoot : (idlePoint != null ? idlePoint : transform);

        for (int i = 0; i < fruitCount; i++)
        {
            bool isAccepted = i < acceptedCount;

            AnimatedFruit animFruit = FruitSpawner.SpawnAnimatedFruit(transform, startPos);
            if (animFruit == null)
                continue;

            _activeAnimatedFruits.Add(animFruit);

            animFruit.Fly(
                startPosition: startPos,
                targetTransform: targetCargo,
                fruitSprite: plantConfig?.fruitIcon,
                onArrived: () =>
                {
                    _activeAnimatedFruits.Remove(animFruit);
                    if (isAccepted && vehicle)
                    {
                        vehicle.SpawnFruitVisuals(plantId, 1);
                    }
                    if (isAccepted)
                    {
                        _arrivedFruitCount++;
                        OnFruitLoaded?.Invoke(plantId, 1);
                        RefreshCapacityLayer();
                        if (_arrivedFruitCount == 1)
                        {
                            // First cargo arrival is the playable's sale-ready milestone.
                            PlayableTutorialFlow.Instance?.ReportJuiceDeliveredToBottles();
                        }
                    }
                    FruitSpawner.DespawnAnimatedFruit(animFruit);
                }
            );
        }
    }

    public int GetCapacityLeft()
    {
        return Mathf.Max(0, GetMaxCapacity() - _loadedFruitCount);
    }

    private int GetMaxCapacity()
    {
        return ConfigHolder.Instance?.FarmConfig?.transport?.GetCapacity(_vehicleLevel) ?? 10;
    }

    private void SetLevel(int level)
    {
        _vehicleLevel = Mathf.Max(1, level);
        OnVehicleLevelChanged?.Invoke(_vehicleLevel);
        RefreshCapacityLayer();
    }

    /// <summary>
    /// Starts a real sale only when the vehicle can depart. Tutorial phase 3
    /// is reported after that validation, never merely because a disabled or
    /// invalid Sell button was clicked.
    /// </summary>
    public bool TrySell()
    {
        if (!IsIdleAtStation || _loadedFruitCount <= 0)
            return false;

        if (!StartDeparture())
            return false;

        PlayableTutorialFlow.Instance?.ReportSellOutClicked();
        return true;
    }

    // Compatibility entry point for existing inspector button bindings.
    public void Sell() { TrySell(); }

    public bool TryUpgrade()
    {
        if (!CanUpgrade)
        {
            return false;
        }

        if (NextUpgradeCost > 0 && !PlayableFarmState.ConsumeGold(NextUpgradeCost))
        {
            return false;
        }

        PlayableFarmState.UpdateTransportLevel(_vehicleLevel + 1);
        return true;
    }

    private bool StartDeparture()
    {
        CancelActiveMotions();
        if (vehicle == null || exitPoint == null || config == null)
        {
            SetState(TransportVehicleState.IdleAtStation);
            return false;
        }

        SetState(TransportVehicleState.Departing);
        vehicle.MoveTo(vehicle.transform.position, exitPoint.position, config.DepartureDuration, Ease.InQuad, OnReachedOffScreen);
        return true;
    }

    private void OnReachedOffScreen()
    {
        SetState(TransportVehicleState.SellingOffScreen);
        vehicle.ClearFruitVisuals();

        double totalGoldGained = 0;
        foreach (var item in _loadedFruits)
        {
            OnFruitDelivered?.Invoke(item.PlantId, item.Count, item.Revenue);
            GameEventBus.OnRevenueEarnedEvent?.Invoke(item.Revenue);
            totalGoldGained += item.Revenue;
        }

        if (totalGoldGained > 0 && PlayableFarmState.PlayerData != null)
        {
            PlayableFarmState.ChangeGold(totalGoldGained);
        }

        _loadedFruits.Clear();
        _loadedFruitCount = 0;
        _loadedRevenue = 0;
        _arrivedFruitCount = 0;
        RefreshCapacityLayer();
        StartReturn();
    }

    private void StartReturn()
    {
        if (vehicle == null || startPoint == null || idlePoint == null || config == null)
        {
            SetState(TransportVehicleState.IdleAtStation);
            return;
        }

        SetState(TransportVehicleState.Returning);
        vehicle.MoveTo(startPoint.position, idlePoint.position, config.ReturnDuration, Ease.OutQuad,
            () => SetState(TransportVehicleState.IdleAtStation));
    }

    private void CancelActiveMotions()
    {
        if (vehicle != null)
        {
            vehicle.CancelMotions();
        }

        for (int i = _activeAnimatedFruits.Count - 1; i >= 0; i--)
        {
            var fruit = _activeAnimatedFruits[i];
            if (fruit != null)
            {
                FruitSpawner.DespawnAnimatedFruit(fruit);
            }
        }
        _activeAnimatedFruits.Clear();
        _arrivedFruitCount = _loadedFruitCount;
    }

    /// <summary>
    /// Reuses the compact capacity world-layer. The station remains the only
    /// source of truth for its loaded fruit and pending revenue.
    /// </summary>
    private void RefreshCapacityLayer()
    {
        if (_capacityLayer == null && WorldLayerManager.Instance != null)
        {
            _capacityLayer = WorldLayerManager.Instance.ShowCapacityFruitTransport(
                transform, capacityLayerOffset, _loadedFruitCount, GetMaxCapacity());
        }

        if (_capacityLayer != null)
        {
            _capacityLayer.UpdateCapacity(_loadedFruitCount, GetMaxCapacity());
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (idlePoint == null || startPoint == null || exitPoint == null)
            return;

        Vector3 center = idlePoint.position;
        Vector3 start = startPoint.position;
        Vector3 exit = exitPoint.position;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawLine(start, exit);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(start, 0.4f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.5f);

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(exit, 0.4f);

        UnityEditor.Handles.Label(center + Vector3.up * 0.7f, "Vehicle Station (Idle)");
        UnityEditor.Handles.Label(exit + Vector3.up * 0.7f, "Exit / Sell Point");
        UnityEditor.Handles.Label(start + Vector3.up * 0.7f, "Start / Return Point");
    }
#endif
}

public sealed class LoadedFruitItem
{
    public int PlantId;
    public int Count;
    public double Revenue;
}
