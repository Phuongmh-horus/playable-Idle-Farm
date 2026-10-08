using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class FarmPlotSystem : MonoBehaviour
{
    [SerializeField] private CircularFarmPlotLayoutHelper layoutHelper;
    [SerializeField] private Transform slotRoot;
    [SerializeField, Min(1)] private int maxSpawnedPlots = 10;
    [Tooltip("Use the Plot objects already authored under Slot Root. When disabled, runtime removes them, creates fresh prefab instances, then the Editor syncs equivalent prefab instances back to the scene after Play Mode stops.")]
    [FormerlySerializedAs("useSceneSlots")]
    [SerializeField] private bool useScenePlots = true;

    private readonly List<FarmPlotData> _plots = new List<FarmPlotData>();
    private readonly Dictionary<int, Plot> _plotComponents = new Dictionary<int, Plot>();
    private readonly List<Plot> _cachedPlotList = new List<Plot>();
    private readonly List<Plot> _runtimeSpawnedSlots = new List<Plot>();
    private readonly HashSet<int> _pendingVisualRefreshes = new HashSet<int>();

    public IReadOnlyList<FarmPlotData> Plots => _plots;
    public IReadOnlyList<Plot> SpawnedPlotComponents => _cachedPlotList;
    public IReadOnlyList<Plot> RuntimeSpawnedPlots => _runtimeSpawnedSlots;
    public bool UseScenePlots => useScenePlots;
    public Transform SlotRoot => GetSlotRoot();

    // public event Action<int> OnRingUnlocked;
    public event Action<FarmPlotData> OnPlotUnlocked;

    public int GetOwnedRingCount()
    {
        int maxRing = 0;
        for (int i = 0; i < _plots.Count; i++)
        {
            FarmPlotData p = _plots[i];
            if (p != null && p.IsUnlocked)
            {
                if (p.RingIndex > maxRing)
                {
                    maxRing = p.RingIndex;
                }
            }
        }
        return Mathf.Clamp(maxRing + 1, 1, 3);
    }

    public Plot GetPlotComponent(int plotId)
    {
        _plotComponents.TryGetValue(plotId, out Plot plot);
        return plot;
    }

    private void RegisterPlotComponent(Plot slot)
    {
        if (slot == null) return;
        _plotComponents[slot.PlotId] = slot;
        if (!_cachedPlotList.Contains(slot))
        {
            _cachedPlotList.Add(slot);
        }
    }

    private void RefreshBoundPlot(int plotId)
    {
        Plot plot;
        if (_plotComponents.TryGetValue(plotId, out plot) && plot != null)
        {
            // Do not depend on FarmPlotData delegate dispatch in Luna local web.
            plot.RefreshFromData(false);
        }
    }

    public CircularFarmPlotLayoutHelper LayoutHelper => layoutHelper;
    private int RingCount => layoutHelper != null ? layoutHelper.RingCount : 0;
    private int PlotsPerRing => layoutHelper != null ? layoutHelper.SectorCount : 0;
    private Plot[] PlotPrefabs => ConfigHolder.Instance?.FarmPlotConfigSO?.PlotPrefabs;

    public Plot GetPlotPrefabForRing(int ringIndex)
    {
        Plot[] prefabs = PlotPrefabs;
        return prefabs != null && ringIndex >= 0 && ringIndex < prefabs.Length
            ? prefabs[ringIndex]
            : null;
    }

    private Vector3 GetPlotPosition(int ringIndex, int sectorIndex)
    {
        return layoutHelper != null ? layoutHelper.GetPlotPointPosition(ringIndex, sectorIndex) : Vector3.zero;
    }

    public Vector3 GetPlotForward(int sectorIndex)
    {
        return layoutHelper != null ? layoutHelper.GetPlotPointForward(sectorIndex) : Vector3.up;
    }

    /// <summary>
    /// Returns the actual radial direction of a bound scene plot. Scene slots
    /// are the source of truth when authors reposition them in SampleScene.
    /// Layout data remains only as a fallback for runtime-instantiated slots.
    /// </summary>
    public Vector3 GetPlotDirection(int plotId)
    {
        return GetPlotDirection(plotId, transform.position);
    }

    /// <summary>
    /// Uses the visible scene plot/plant as the target. The supplied origin is
    /// the actual rotating arm pivot, so authored scene transforms remain
    /// correct even when FarmPlotSystem itself is not at the machine centre.
    /// </summary>
    public Vector3 GetPlotDirection(int plotId, Vector3 origin)
    {
        Plot plot = GetPlotComponent(plotId);
        if (plot != null)
        {
            Vector3 target = plot.CurrentPlant != null ? plot.PlantPosition : plot.transform.position;
            Vector3 direction = target - origin;
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }
        }

        FarmPlotData data = GetPlot(plotId);
        return data != null ? -GetPlotForward(data.SectorIndex) : Vector3.up;
    }

    private Quaternion GetPlotRotation(int sectorIndex)
    {
        return layoutHelper != null ? layoutHelper.GetPlotPointRotation(sectorIndex) : Quaternion.identity;
    }

    public void Initialize()
    {
        PlaceSlotPrefabs();
    }

    /// <summary>
    /// Guarantees runtime plot data is available without rebuilding a farm
    /// that has already started progressing.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_plots.Count == 0)
        {
            Initialize();
        }
    }

    private void PlaceSlotPrefabs()
    {
        _plots.Clear();
        ClearRuntimeSlots();
        _plotComponents.Clear();
        _pendingVisualRefreshes.Clear();
        _cachedPlotList.Clear();
        if (layoutHelper == null) return;
        CreatePlots();

        if (useScenePlots)
        {
            BindSceneSlots();
            WarnIfScenePlotsAreMissing();
        }
        else
        {
            ClearSceneSlots();
            SpawnMissingSlots();
        }
    }

    public FarmPlotData GetPlot(int plotId)
    {
        if (plotId < 0 || plotId >= _plots.Count)
        {
            return null;
        }

        return _plots[plotId];
    }

    public FarmPlotData GetPlot(int ringIndex, int sectorIndex)
    {
        if (ringIndex < 0 || ringIndex >= RingCount || sectorIndex < 0 || sectorIndex >= PlotsPerRing)
        {
            return null;
        }

        int plotId = GetPlotId(ringIndex, sectorIndex);
        return GetPlot(plotId);
    }

    public bool UnlockPlot(int plotId)
    {
        FarmPlotData plot = GetPlot(plotId);
        if (plot != null && plot.UnlockPlot())
        {
            PlayableFarmState.UpdatePlotData(plotId, PlotState.Empty, -1, 1);
            RefreshBoundPlot(plotId);
            CheckAndUnlockAvailablePlots();
            if (!UsesPlantPicker())
            {
                TryPlantConfiguredPlot(plotId);
            }
            OnPlotUnlocked?.Invoke(plot);
            GameEventBus.OnPlotUnlockedEvent?.Invoke(plotId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Kiá»ƒm tra xem má»™t Ã´ Ä‘áº¥t cÃ³ Ä‘á»§ Ä‘iá»u kiá»‡n chuyá»ƒn tá»« RingLocked sang Locked khÃ´ng:
    /// (1) Ã” Ä‘áº¥t cÃ¹ng sector á»Ÿ ring bÃªn trong (ringIndex - 1, sectorIndex) Ä‘Ã£ Ä‘Æ°á»£c má»Ÿ khÃ³a (IsUnlocked).
    /// (2) Ã” Ä‘áº¥t cÃ¹ng ring cÃ³ sector index trÆ°á»›c Ä‘Ã³ (ringIndex, sectorIndex - 1) Ä‘Ã£ Ä‘Æ°á»£c má»Ÿ khÃ³a (IsUnlocked).
    /// </summary>
    public bool CanUnlockPlotToLockedState(int ringIndex, int sectorIndex)
    {
        // (1) Äiá»u kiá»‡n 1: Ã” Ä‘áº¥t cÃ¹ng sector á»Ÿ ring bÃªn trong Ä‘Ã£ má»Ÿ khÃ³a (Ring 0 máº·c Ä‘á»‹nh thá»a mÃ£n)
        bool innerRingCondition = false;
        if (ringIndex == 0)
        {
            innerRingCondition = true;
        }
        else
        {
            FarmPlotData innerPlot = GetPlot(ringIndex - 1, sectorIndex);
            innerRingCondition = innerPlot != null && innerPlot.IsUnlocked;
        }

        // (2) Äiá»u kiá»‡n 2: Ã” Ä‘áº¥t cÃ¹ng ring cÃ³ index trÆ°á»›c Ä‘Ã³ Ä‘Ã£ má»Ÿ khÃ³a (Sector 0 máº·c Ä‘á»‹nh thá»a mÃ£n)
        bool prevSectorCondition = false;
        if (sectorIndex == 0)
        {
            prevSectorCondition = true;
        }
        else
        {
            FarmPlotData prevPlot = GetPlot(ringIndex, sectorIndex - 1);
            prevSectorCondition = prevPlot != null && prevPlot.IsUnlocked;
        }

        return innerRingCondition && prevSectorCondition;
    }

    /// <summary>
    /// Kiá»ƒm tra vÃ  má»Ÿ khÃ³a táº¥t cáº£ cÃ¡c Ã´ Ä‘áº¥t Ä‘ang á»Ÿ tráº¡ng thÃ¡i RingLocked sang Locked khi thá»a mÃ£n Ä‘iá»u kiá»‡n
    /// </summary>
    private void CheckAndUnlockAvailablePlots()
    {
        bool changed;
        do
        {
            changed = false;
            for (int ringIndex = 0; ringIndex < RingCount; ringIndex++)
            {
                for (int sectorIndex = 0; sectorIndex < PlotsPerRing; sectorIndex++)
                {
                    FarmPlotData plot = GetPlot(ringIndex, sectorIndex);
                    if (plot != null && plot.State == FarmPlotState.RingLocked)
                    {
                        if (CanUnlockPlotToLockedState(ringIndex, sectorIndex))
                        {
                            plot.UnlockRing();
                            PlayableFarmState.UpdatePlotData(plot.Id, PlotState.Locked, -1, 1);
                            RefreshBoundPlot(plot.Id);
                            changed = true;
                        }
                    }
                }
            }
        } while (changed);
    }

    public bool TryOccupyPlot(int plotId, int plantId)
    {
        return TryOccupyPlot(plotId, plantId, -1);
    }

    public bool TryOccupyPlot(int plotId, int plantId, int visualPlantId)
    {
        FarmPlotData plot = GetPlot(plotId);
        if (!CanOccupy(plot, plantId))
        {
            return false;
        }

        bool success = plot.Occupy(plantId, 1, visualPlantId);
        if (success)
        {
            PlayableFarmState.UpdatePlotData(plotId, PlotState.Planted, plantId, 1, visualPlantId);
            RefreshBoundPlot(plotId);
            SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_plant_grow);
        }
        return success;
    }

    public bool TryPlantConfiguredPlot(int plotId)
    {
        FarmPlotData plot = GetPlot(plotId);
        FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        PlantConfig plantConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.PlantConfig : null;
        int plantId = farmConfig != null
            ? farmConfig.GetInitialPlantId(plotId, plantConfig)
            : (plot != null && plot.Config != null ? plot.Config.defaultPlantId : -1);
        if (plantId < 0 || plantConfig == null || plantConfig.GetPlant(plantId) == null)
        {
            Debug.LogWarning($"[FarmPlotSystem] Plot {plotId} has no valid default plant.", this);
            return false;
        }

        return TryOccupyPlot(plotId, plantId);
    }

    private static bool UsesPlantPicker()
    {
        ScenePlayableSO scenario = PlayableScenarioRuntime.CurrentScenario;
        return scenario != null && scenario.UsesPlantPicker;
    }

    private bool CanOccupy(FarmPlotData plot, int plantId)
    {
        return plot != null && plantId >= 0 && plot.State == FarmPlotState.Empty;
    }

    public void ReleasePlot(int plotId)
    {
        FarmPlotData plot = GetPlot(plotId);
        if (plot != null && plot.Release())
        {
            PlayableFarmState.UpdatePlotData(plotId, PlotState.Empty, -1, 1);
        }
    }

    private void CreatePlots()
    {
        int plotId = 0;
        for (int ringIndex = 0; ringIndex < RingCount; ringIndex++)
        {
            if (plotId >= GetPlayablePlotCount())
            {
                break;
            }
            CreateRingPlots(ringIndex, ref plotId);
        }

        CheckAndUnlockAvailablePlots();
    }

    private void CreateRingPlots(int ringIndex, ref int plotId)
    {
        FarmConfig farmConfig = ConfigHolder.Instance?.FarmConfig;
        for (int sectorIndex = 0; sectorIndex < PlotsPerRing; sectorIndex++)
        {
            if (plotId >= GetPlayablePlotCount())
            {
                break;
            }
            PlotConfig config = farmConfig != null
                ? (farmConfig.GetPlotConfig(ringIndex, sectorIndex) ?? farmConfig.GetPlotConfig(plotId))
                : null;

            PlotSaveData save = null;
            if (PlayableScenarioRuntime.TryGetInitialPlotData(plotId, out save))
            {
                PlayableFarmState.UpdatePlotData(plotId, save.state, save.plantedPlantId, save.currentLevel, save.visualPlantId);
            }
            else if (PlayableFarmState.PlayerData != null)
            {
                save = PlayableFarmState.GetPlotSaveData(plotId);
                if (save == null)
                {
                    //FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
                    if (config != null && config.isUnlockedByDefault)
                    {
                        int defaultPlantId = farmConfig != null
                            ? farmConfig.GetInitialPlantId(plotId, ConfigHolder.Instance?.PlantConfig)
                            : config.defaultPlantId;
                        PlotState initialSaveState = defaultPlantId >= 0 ? PlotState.Planted : PlotState.Empty;
                        save = PlayableFarmState.GetOrCreatePlotSaveData(plotId, initialSaveState, defaultPlantId, 1);
                    }
                    else if (ringIndex == 0 && sectorIndex == 0)
                    {
                        save = PlayableFarmState.GetOrCreatePlotSaveData(plotId, PlotState.Locked, -1, 1);
                    }
                }
            }

            FarmPlotData plotData = new FarmPlotData(plotId, ringIndex, sectorIndex);
            plotData.LoadData(config, save);
            _plots.Add(plotData);

            plotId++;
        }
    }

    private void SpawnMissingSlots()
    {
        if (layoutHelper == null)
        {
            return;
        }

        for (int ringIndex = 0; ringIndex < RingCount; ringIndex++)
        {
            SpawnMissingRingSlots(ringIndex);
        }
    }

    private void SpawnMissingRingSlots(int ringIndex)
    {
        Plot[] plotPrefabs = PlotPrefabs;
        if (plotPrefabs == null || ringIndex >= plotPrefabs.Length)
        {
            return;
        }

        for (int sectorIndex = 0; sectorIndex < PlotsPerRing; sectorIndex++)
        {
            int plotId = GetPlotId(ringIndex, sectorIndex);
            if (plotId >= _plots.Count)
            {
                break;
            }

            if (_plotComponents.ContainsKey(plotId))
            {
                continue;
            }

            Plot prefab = plotPrefabs[ringIndex];
            if (prefab == null)
            {
                continue;
            }

            Vector3 position = GetPlotPosition(ringIndex, sectorIndex);
            Quaternion rotation = GetPlotRotation(sectorIndex);

            Plot slot = Instantiate(prefab, position, rotation, GetSlotRoot());
            if (slot != null)
            {
                slot.Bind(this, GetPlot(plotId));
                RegisterPlotComponent(slot);
                _runtimeSpawnedSlots.Add(slot);
            }
        }
    }

    private void BindSceneSlots()
    {
        List<Plot> availableSlots = GetAuthoredSceneSlots();

        for (int plotId = 0; plotId < _plots.Count; plotId++)
        {
            FarmPlotData plotData = _plots[plotId];
            Plot sceneSlot = GetNextSceneSlot(availableSlots);
            if (sceneSlot == null)
            {
                continue;
            }

            availableSlots.Remove(sceneSlot);
            sceneSlot.gameObject.SetActive(true);
            sceneSlot.Bind(this, plotData);
            RegisterPlotComponent(sceneSlot);
        }

        for (int i = 0; i < availableSlots.Count; i++)
        {
            if (availableSlots[i] != null)
            {
                availableSlots[i].gameObject.SetActive(false);
            }
        }
    }

    private void WarnIfScenePlotsAreMissing()
    {
        if (_plotComponents.Count >= _plots.Count)
        {
            return;
        }

        Debug.LogWarning(
            $"[FarmPlotSystem] Use Scene Plots is enabled, but only {_plotComponents.Count}/{_plots.Count} Plot objects were found under '{GetSlotRoot().name}'. " +
            "Disable Use Scene Plots for one Play Mode run to regenerate prefab-linked scene plots.",
            this);
    }

    private static Plot GetNextSceneSlot(List<Plot> availableSlots)
    {
        if (availableSlots == null || availableSlots.Count == 0)
        {
            return null;
        }

        // The Slot Root sibling order is the explicit authoring order in the
        // scene. Do not re-sort by the obsolete circular-layout coordinates.
        return availableSlots[0];
    }

    private void ClearSceneSlots()
    {
        List<Plot> sceneSlots = GetAuthoredSceneSlots();
        for (int i = 0; i < sceneSlots.Count; i++)
        {
            if (sceneSlots[i] != null)
            {
                DestroyPlotObject(sceneSlots[i].gameObject);
            }
        }
    }

    private List<Plot> GetAuthoredSceneSlots()
    {
        Transform root = GetSlotRoot();
        var slots = new List<Plot>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Plot slot = child.GetComponent<Plot>();
            if (slot == null)
            {
                continue;
            }

            // Slot Root is the explicit scene-authoring boundary. Names are
            // not reliable here: the authored slots can legitimately retain
            // Unity's "(Clone)" suffix after being saved from an earlier setup.
            // Runtime slots are tracked separately in _runtimeSpawnedSlots.
            slots.Add(slot);
        }
        return slots;
    }

    // Temporary playable layout cap. Raise this Inspector value when a future
    // scenario needs more visible plots or progressive plot unlocking.
    private int GetPlayablePlotCount()
    {
        int layoutCapacity = RingCount * PlotsPerRing;
        return Mathf.Clamp(maxSpawnedPlots, 1, layoutCapacity);
    }

    private Transform GetSlotRoot()
    {
        return slotRoot != null ? slotRoot : transform;
    }

    private int GetPlotId(int ringIndex, int sectorIndex)
    {
        return ringIndex * PlotsPerRing + sectorIndex;
    }

    private void ClearRuntimeSlots()
    {
        for (int i = 0; i < _runtimeSpawnedSlots.Count; i++)
        {
            Plot slot = _runtimeSpawnedSlots[i];
            if (slot != null)
            {
                DestroyPlotObject(slot.gameObject);
            }
        }
        _runtimeSpawnedSlots.Clear();
    }

    private static void DestroyPlotObject(GameObject slotObject)
    {
        if (slotObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(slotObject);
        }
        else
        {
            DestroyImmediate(slotObject);
        }
    }
}
