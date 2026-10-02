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

    public static void UpdatePlotData(int plotId, PlotState state, int plantId, int level)
    {
        PlotSaveData plot = GetOrCreatePlotSaveData(plotId, state, plantId, level);
        plot.state = state;
        plot.plantedPlantId = plantId;
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

    public static int InitialPlotCount => CurrentScenario != null
        ? Mathf.Max(1, CurrentScenario.initialPlotCount)
        : 1;

    public static void Prepare(ScenePlayableSO scenario)
    {
        CurrentScenario = scenario;
        // Currency is an inspector-authored scenario value; all later income
        // and spending remains session-only in PlayableFarmState.
        PlayableFarmState.ResetPlayableState(scenario != null ? scenario.startingCurrency : 0d);
        int initialPlotCount = InitialPlotCount;
        for (int plotId = 0; plotId < initialPlotCount; plotId++)
        {
            int plantId = scenario != null ? GetInitialPlantId(scenario, plotId) : -1;
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

        int plantId = GetInitialPlantId(CurrentScenario, plotId);
        data = new PlotSaveData
        {
            plotId = plotId,
            state = plantId >= 0 ? PlotState.Planted : PlotState.Empty,
            plantedPlantId = plantId,
            currentLevel = 1
        };
        return true;
    }

    private static int GetInitialPlantId(ScenePlayableSO scenario, int plotId)
    {
        if (scenario.initialPlantIds != null && plotId < scenario.initialPlantIds.Count)
        {
            return scenario.initialPlantIds[plotId];
        }
        return -1;
    }
}
