using System;
using UnityEngine;

public enum FarmPlotState
{
    RingLocked = 0, // Vành đai chưa mở khóa (chưa thể mua lẻ)
    Locked = 1,     // Đã mở vành đai, ô đất đang khóa (sẵn sàng mua)
    Empty = 2,      // Đất trống đã mở khóa, sẵn sàng trồng
    Occupied = 3    // Đang có cây trồng
}

[Serializable]
public sealed class FarmPlotData
{
    public int Id { get; private set; }
    public int RingIndex { get; private set; }
    public int SectorIndex { get; private set; }
    public PlotType PlotType { get; private set; }
    public FarmPlotState State { get; private set; }
    public int PlantId { get; private set; } = -1;
    public int VisualPlantId { get; private set; } = -1;
    public int PlantLevel { get; private set; } = 1;
    public PlotConfig Config { get; private set; }

    public bool IsUnlocked => State == FarmPlotState.Empty || State == FarmPlotState.Occupied;
    public bool IsSpecial => PlotType == PlotType.Special;

    public event Action<FarmPlotData, FarmPlotState> OnStateChanged;

    public FarmPlotData(int id, int ringIndex, int sectorIndex, PlotType plotType = PlotType.Normal)
    {
        Id = id;
        RingIndex = ringIndex;
        SectorIndex = sectorIndex;
        PlotType = plotType;
        State = (ringIndex == 0 && sectorIndex == 0) ? FarmPlotState.Locked : FarmPlotState.RingLocked;
    }

    public FarmPlotData(PlotConfig config)
    {
        Config = config;
        Id = config.id;
        RingIndex = config.ringIndex;
        SectorIndex = config.sectorIndex;
        PlotType = config.plotType;
        PlantId = -1;
        PlantLevel = 1;

        if (config.isUnlockedByDefault)
        {
            if (config.defaultPlantId >= 0)
            {
                PlantId = config.defaultPlantId;
                State = FarmPlotState.Occupied;
            }
            else
            {
                State = FarmPlotState.Empty;
            }
        }
        else
        {
            State = (config.ringIndex == 0 && config.sectorIndex == 0) ? FarmPlotState.Locked : FarmPlotState.RingLocked;
        }
    }

    /// <summary>
    /// Đồng bộ trạng thái ô đất từ Config kết hợp SaveData
    /// </summary>
    public void LoadData(PlotConfig config, PlotSaveData save)
    {
        Config = config;
        if (config != null)
        {
            Id = config.id;
            RingIndex = config.ringIndex;
            SectorIndex = config.sectorIndex;
            PlotType = config.plotType;
        }

        if (save != null)
        {
            switch (save.state)
            {
                case PlotState.Planted:
                    State = FarmPlotState.Occupied;
                    PlantId = save.plantedPlantId;
                    VisualPlantId = save.visualPlantId;
                    PlantLevel = Math.Max(1, save.currentLevel);
                    break;
                case PlotState.Empty:
                    State = FarmPlotState.Empty;
                    PlantId = -1;
                    VisualPlantId = -1;
                    PlantLevel = 1;
                    break;
                case PlotState.Locked:
                    State = FarmPlotState.Locked;
                    PlantId = -1;
                    VisualPlantId = -1;
                    PlantLevel = 1;
                    break;
                default:
                    State = (RingIndex == 0 && SectorIndex == 0) ? FarmPlotState.Locked : FarmPlotState.RingLocked;
                    PlantId = -1;
                    VisualPlantId = -1;
                    PlantLevel = 1;
                    break;
            }
        }
        else
        {
            // Fallback khi chưa có save data (người chơi mới hoặc ô đất mới)
            if (config != null && config.isUnlockedByDefault)
            {
                if (config.defaultPlantId >= 0)
                {
                    State = FarmPlotState.Occupied;
                    PlantId = config.defaultPlantId;
                    VisualPlantId = -1;
                    PlantLevel = 1;
                }
                else
                {
                    State = FarmPlotState.Empty;
                    PlantId = -1;
                    VisualPlantId = -1;
                    PlantLevel = 1;
                }
            }
            else
            {
                State = (RingIndex == 0 && SectorIndex == 0) ? FarmPlotState.Locked : FarmPlotState.RingLocked;
                PlantId = -1;
                VisualPlantId = -1;
                PlantLevel = 1;
            }
        }
    }

    /// <summary>
    /// Mở khóa vành đai chứa ô này (chuyển từ RingLocked -> Locked)
    /// </summary>
    public void UnlockRing()
    {
        if (State == FarmPlotState.RingLocked)
        {
            SetState(FarmPlotState.Locked);
        }
    }

    public bool UnlockPlot()
    {
        if (State != FarmPlotState.Locked && State != FarmPlotState.RingLocked)
            return false;

        PlantId = -1;
        VisualPlantId = -1;
        PlantLevel = 1;
        SetState(FarmPlotState.Empty);
        return true;
    }

    public void ForceUnlock()
    {
        PlantId = -1;
        VisualPlantId = -1;
        PlantLevel = 1;
        SetState(FarmPlotState.Empty);
    }

    public bool Occupy(int plantId, int level = 1, int visualPlantId = -1)
    {
        if (State != FarmPlotState.Empty || plantId < 0)
            return false;

        PlantId = plantId;
        VisualPlantId = visualPlantId;
        PlantLevel = Math.Max(1, level);
        SetState(FarmPlotState.Occupied);
        return true;
    }

    public void SetPlantLevel(int level)
    {
        PlantLevel = Math.Max(1, level);
    }

    public bool Release()
    {
        if (State != FarmPlotState.Occupied)
            return false;

        PlantId = -1;
        VisualPlantId = -1;
        PlantLevel = 1;
        SetState(FarmPlotState.Empty);
        return true;
    }

    public void SetState(FarmPlotState newState)
    {
        if (State == newState)
            return;

        State = newState;
        OnStateChanged?.Invoke(this, newState);
    }
}
