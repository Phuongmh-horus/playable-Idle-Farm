using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Session-only farm state for the playable ad. It deliberately replaces the
/// original account, disk-save, cloud-sync, IAP and analytics data layer.
/// </summary>
[Serializable]
public sealed class PlayableFarmRuntimeData
{
    public double Gold;
    public GardenSaveData GardenData = new GardenSaveData();
    public List<PlantLevelRecord> UnlockedPlantLevels = new List<PlantLevelRecord>();
}

[Serializable]
public sealed class PlantLevelRecord
{
    public int PlantId;
    public int Level;
}

/// <summary>
/// Legacy gameplay entry points backed by the playable's real in-memory state.
/// Keep gameplay callers focused on farm mutations while scenario setup owns
/// the initial values. Do not add account or persistence responsibilities here.
/// </summary>
public static class PlayableFarmState
{
    private static PlayableFarmRuntimeData _playerData;

    public static PlayableFarmRuntimeData PlayerData
    {
        get
        {
            if (_playerData == null)
            {
                ResetPlayableState(0d);
            }
            return _playerData;
        }
    }

    public static void ResetPlayableState(double startingGold)
    {
        _playerData = new PlayableFarmRuntimeData();
        _playerData.Gold = Math.Max(0d, startingGold);
        EnsureGardenData();
    }

    public static bool HasEnoughGold(double amount)
    {
        return amount <= 0d || PlayerData.Gold >= amount;
    }

    public static bool ConsumeGold(double amount)
    {
        if (amount <= 0d)
        {
            return true;
        }

        if (!HasEnoughGold(amount))
        {
            return false;
        }

        ChangeGold(-amount);
        return true;
    }

    public static void ChangeGold(double amount)
    {
        PlayerData.Gold = Math.Max(0d, PlayerData.Gold + amount);
        if (GameEventBus.OnGoldChange != null)
        {
            GameEventBus.OnGoldChange();
        }
    }

    public static PlotSaveData GetPlotSaveData(int plotId)
    {
        EnsureGardenData();
        return PlayerData.GardenData.plots.Find(plot => plot != null && plot.plotId == plotId);
    }

    public static PlotSaveData GetOrCreatePlotSaveData(int plotId, PlotState defaultState, int defaultPlantId, int defaultLevel)
    {
        PlotSaveData plot = GetPlotSaveData(plotId);
        if (plot != null)
        {
            return plot;
        }

        plot = new PlotSaveData
        {
            plotId = plotId,
            state = defaultState,
            plantedPlantId = defaultPlantId,
            currentLevel = Mathf.Max(1, defaultLevel)
        };
        PlayerData.GardenData.plots.Add(plot);
        return plot;
    }

    public static void UpdatePlotData(int plotId, PlotState state, int plantId, int level, int visualPlantId = -1)
    {
        PlotSaveData plot = GetOrCreatePlotSaveData(plotId, state, plantId, level);
        plot.state = state;
        plot.plantedPlantId = plantId;
        plot.visualPlantId = state == PlotState.Planted ? visualPlantId : -1;
        plot.currentLevel = Mathf.Max(1, level);

        if (state == PlotState.Planted && plantId >= 0)
        {
            UnlockCollectionPlant(plantId, plot.currentLevel);
        }
    }

    public static SprinklerSaveData GetSprinklerSaveData()
    {
        EnsureGardenData();
        return PlayerData.GardenData.sprinkler;
    }

    public static void UpdateSprinklerLevel(int level)
    {
        GetSprinklerSaveData().currentLevel = Mathf.Max(1, level);
    }

    public static HarvesterSaveData GetHarvesterSaveData()
    {
        EnsureGardenData();
        return PlayerData.GardenData.harvester;
    }

    public static void UpdateHarvesterLevel(int level)
    {
        GetHarvesterSaveData().currentLevel = Mathf.Max(1, level);
    }

    public static TransportSaveData GetTransportSaveData()
    {
        EnsureGardenData();
        return PlayerData.GardenData.transport;
    }

    public static void UpdateTransportLevel(int level)
    {
        TransportConfig config = ConfigHolder.Instance?.FarmConfig?.transport;
        int maxLevel = config != null ? config.MaxLevel : 20;
        GetTransportSaveData().currentLevel = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        if (GameEventBus.OnUpdateTransportStation != null)
        {
            GameEventBus.OnUpdateTransportStation();
        }
    }

    public static bool UnlockCollectionPlant(int plantId, int level)
    {
        if (plantId < 0 || level <= 0)
        {
            return false;
        }

        List<PlantLevelRecord> unlocked = PlayerData.UnlockedPlantLevels;
        for (int i = 0; i < unlocked.Count; i++)
        {
            if (unlocked[i].PlantId == plantId && unlocked[i].Level == level)
            {
                return false;
            }
        }

        unlocked.Add(new PlantLevelRecord { PlantId = plantId, Level = level });
        return true;
    }

    private static void EnsureGardenData()
    {
        if (_playerData == null)
        {
            _playerData = new PlayableFarmRuntimeData();
        }

        if (_playerData.GardenData == null)
        {
            _playerData.GardenData = new GardenSaveData();
        }
        if (_playerData.GardenData.plots == null)
        {
            _playerData.GardenData.plots = new List<PlotSaveData>();
        }
        if (_playerData.GardenData.sprinkler == null)
        {
            _playerData.GardenData.sprinkler = new SprinklerSaveData();
        }
        if (_playerData.GardenData.harvester == null)
        {
            _playerData.GardenData.harvester = new HarvesterSaveData();
        }
        if (_playerData.GardenData.transport == null)
        {
            _playerData.GardenData.transport = new TransportSaveData();
        }
    }
}

/// <summary>
/// Applies a ScenePlayableSO once per playable session. This is intentionally
/// separate from PlayableFarmState so scenario authoring remains declarative in SOs.
/// </summary>
public static class PlayableScenarioRuntime
{
    public static ScenePlayableSO CurrentScenario { get; private set; }
    private static readonly List<int> _configuredVisualPlantOrder = new List<int>();

    public static int InitialPlotCount => CurrentScenario != null
        ? Mathf.Max(1, CurrentScenario.initialPlotCount)
        : 1;

    public static void Prepare(ScenePlayableSO scenario)
    {
        CurrentScenario = scenario;
        BuildConfiguredVisualOrder();
        // Currency is an inspector-authored scenario value; all later income
        // and spending remains session-only in PlayableFarmState.
        PlayableFarmState.ResetPlayableState(scenario != null ? scenario.startingCurrency : 0d);
        int initialPlotCount = InitialPlotCount;
        for (int plotId = 0; plotId < initialPlotCount; plotId++)
        {
            int plantId = scenario != null ? GetInitialPlotPlantId(plotId) : -1;
            PlotState state = plantId >= 0 ? PlotState.Planted : PlotState.Empty;
            PlayableFarmState.UpdatePlotData(plotId, state, plantId, 1);
        }
    }

    public static bool TryGetInitialPlotData(int plotId, out PlotSaveData data)
    {
        data = null;
        if (CurrentScenario == null)
        {
            return false;
        }

        if (plotId < 0 || plotId >= InitialPlotCount)
        {
            return false;
        }

        int plantId = GetInitialPlotPlantId(plotId);
        data = new PlotSaveData
        {
            plotId = plotId,
            state = plantId >= 0 ? PlotState.Planted : PlotState.Empty,
            plantedPlantId = plantId,
            currentLevel = 1
        };
        return true;
    }

    public static int GetInitialFruitCount(int plotId)
    {
        if (CurrentScenario == null || plotId < 0 || plotId >= InitialPlotCount)
        {
            return 0;
        }

        return Mathf.Max(0, CurrentScenario.initialFruitCount);
    }

    public static int GetConfiguredVisualPlantId(int plotId)
    {
        EnsureConfiguredVisualOrder();
        if (plotId >= 0 && plotId < _configuredVisualPlantOrder.Count && _configuredVisualPlantOrder[plotId] >= 0)
        {
            return _configuredVisualPlantOrder[plotId];
        }

        return CurrentScenario != null ? ResolveConfiguredGameplayPlantId(CurrentScenario, plotId) : -1;
    }

    public static bool SwapConfiguredVisualOrder(int plotId, int selectedPlantId)
    {
        if (plotId < 0 || selectedPlantId < 0)
        {
            return false;
        }

        EnsureConfiguredVisualOrder();
        EnsureConfiguredVisualOrderSize(plotId + 1);
        int configuredPlantId = GetConfiguredVisualPlantId(plotId);
        if (configuredPlantId < 0 || configuredPlantId == selectedPlantId)
        {
            return configuredPlantId == selectedPlantId;
        }

        int selectedIndex = _configuredVisualPlantOrder.IndexOf(selectedPlantId);
        if (selectedIndex < 0)
        {
            return false;
        }

        _configuredVisualPlantOrder[selectedIndex] = configuredPlantId;
        _configuredVisualPlantOrder[plotId] = selectedPlantId;
        return true;
    }

    public static bool CanSwapConfiguredVisualOrder(int plotId, int selectedPlantId)
    {
        if (plotId < 0 || selectedPlantId < 0)
        {
            return false;
        }

        EnsureConfiguredVisualOrder();
        EnsureConfiguredVisualOrderSize(plotId + 1);
        int configuredPlantId = GetConfiguredVisualPlantId(plotId);
        return configuredPlantId == selectedPlantId ||
            (configuredPlantId >= 0 && _configuredVisualPlantOrder.IndexOf(selectedPlantId) >= 0);
    }

    private static void BuildConfiguredVisualOrder()
    {
        _configuredVisualPlantOrder.Clear();

        FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        PlantConfig plantConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.PlantConfig : null;
        if (farmConfig != null && farmConfig.initialPlantOrder != null && farmConfig.initialPlantOrder.Count > 0)
        {
            _configuredVisualPlantOrder.AddRange(farmConfig.initialPlantOrder);
        }
        else if (plantConfig != null && plantConfig.plants != null)
        {
            for (int i = 0; i < plantConfig.plants.Count; i++)
            {
                if (plantConfig.plants[i] != null)
                {
                    _configuredVisualPlantOrder.Add(plantConfig.plants[i].ID);
                }
            }
        }
        else if (CurrentScenario != null && CurrentScenario.initialPlantIds != null)
        {
            _configuredVisualPlantOrder.AddRange(CurrentScenario.initialPlantIds);
        }
    }

    private static void EnsureConfiguredVisualOrder()
    {
        if (_configuredVisualPlantOrder.Count > 0 || CurrentScenario == null)
        {
            return;
        }

        // LunaUIManager prepares the scenario before ConfigHolder's Awake.
        // Defer the authored order until those inspector references exist.
        if (ConfigHolder.Instance != null)
        {
            BuildConfiguredVisualOrder();
        }
    }

    private static void EnsureConfiguredVisualOrderSize(int size)
    {
        while (_configuredVisualPlantOrder.Count < size)
        {
            int nextPlotId = _configuredVisualPlantOrder.Count;
            int plantId = CurrentScenario != null ? ResolveConfiguredGameplayPlantId(CurrentScenario, nextPlotId) : -1;
            _configuredVisualPlantOrder.Add(plantId);
        }
    }

    private static int GetInitialPlotPlantId(int plotId)
    {
        if (CurrentScenario != null && CurrentScenario.UsesPlantPicker && plotId == 0)
        {
            return -1;
        }

        return CurrentScenario != null ? ResolveConfiguredGameplayPlantId(CurrentScenario, plotId) : -1;
    }

    private static int ResolveConfiguredGameplayPlantId(ScenePlayableSO scenario, int plotId)
    {
        FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        PlantConfig plantConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.PlantConfig : null;
        if (farmConfig != null && farmConfig.HasInitialPlantOrder &&
            plotId >= 0 && plotId < farmConfig.initialPlantOrder.Count)
        {
            return farmConfig.initialPlantOrder[plotId];
        }

        if (farmConfig != null && plantConfig != null)
        {
            int orderedPlantId = farmConfig.GetInitialPlantId(plotId, plantConfig);
            if (orderedPlantId >= 0)
            {
                return orderedPlantId;
            }
        }

        if (scenario.initialPlantIds != null && plotId < scenario.initialPlantIds.Count)
        {
            return scenario.initialPlantIds[plotId];
        }
        return -1;
    }
}
