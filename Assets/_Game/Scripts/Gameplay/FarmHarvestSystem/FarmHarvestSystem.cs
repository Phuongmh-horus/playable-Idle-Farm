using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FarmHarvestSystem : MonoBehaviour
{
    [SerializeField] private HarvesterArmConfig armConfig;
    [SerializeField] private int harvesterLevel = 1;

    [SerializeField] private HarvesterArm harvesterArmPrefab;
    [SerializeField] private Transform harvesterArmParent;

    [Header("Playable Modes")]
    [SerializeField] private HarvestMachine harvestMachine;

    [SerializeField] private bool logPlotHarvested = true;

    private FarmPlotSystem _plotSystem;
    public FarmPlotSystem PlotSystem => _plotSystem != null ? _plotSystem : (_plotSystem = FarmSystem.Instance?.PlotSystem);

    private readonly HashSet<int> _reachedPlotIds = new HashSet<int>();
    private readonly List<HarvesterArm> _spawnedHarvesterArms = new List<HarvesterArm>();
    private PlayableHarvestMode _harvestMode = PlayableHarvestMode.ManualBlade;

    public IReadOnlyList<HarvesterArm> Arms => _spawnedHarvesterArms;
    public HarvesterArm PrimaryArm => _spawnedHarvesterArms.Count > 0 ? _spawnedHarvesterArms[0] : null;
    public Transform ArmRoot => harvesterArmParent != null ? harvesterArmParent : transform;

    public HarvesterConfig Config => ConfigHolder.Instance?.FarmConfig?.harvester;

    public HarvesterArmConfig ArmConfig => armConfig != null ? armConfig : (harvesterArmPrefab != null ? harvesterArmPrefab.Config : null);
    public float AlignmentTolerance => ArmConfig != null ? ArmConfig.alignmentTolerance : 3f;

    public int MaxLevel => (Config != null && Config.maxLevel > 0) ? Config.maxLevel : 10;
    public float RotationDuration => Config != null ? Config.GetRotationDuration(harvesterLevel) : 15f;
    public float RotationSpeed
    {
        get
        {
            if (PlayableScenarioRuntime.CurrentScenario != null && PlayableScenarioRuntime.CurrentScenario.scenario == PlayableScenarioId.Scenario124)
            {
                if (harvesterLevel >= 6)
                {
                    float minDuration = Config != null ? Config.minimumRotationDuration : 6f;
                    return 360f / Mathf.Max(0.1f, minDuration);
                }
                return 32f + (harvesterLevel - 1) * 8f;
            }
            return Config != null ? Config.GetRotationSpeed(harvesterLevel) : 24f;
        }
    }
    public int HarvesterCount
    {
        get
        {
            if (PlayableScenarioRuntime.CurrentScenario != null && PlayableScenarioRuntime.CurrentScenario.scenario == PlayableScenarioId.Scenario124)
            {
                if (harvesterLevel >= 6) return 4;
                if (harvesterLevel >= 5) return 3;
                if (harvesterLevel >= 3) return 2;
                return 1;
            }
            return Config != null ? Config.GetHarvesterCount(harvesterLevel) : (1 + (harvesterLevel - 1) / 5);
        }
    }
    public int TotalBladeCount => Config != null ? Config.GetTotalBlades(harvesterLevel) : harvesterLevel;
    public int Level => harvesterLevel;
    public PlayableHarvestMode HarvestMode => _harvestMode;
    public bool AutomaticArmEnabled => UsesAutomaticArm;
    public double NextUpgradeCost => (Config != null && harvesterLevel < MaxLevel) ? Config.GetUpgradeCost(harvesterLevel) : -1;
    public bool IsMaxLevel => harvesterLevel >= MaxLevel;
    public bool CanUpgrade => !IsMaxLevel && (NextUpgradeCost <= 0 || PlayableFarmState.HasEnoughGold(NextUpgradeCost));

    public event Action<Plot, double> PlotHarvested;
    public event Action<int> OnHarvesterLevelChanged;

    private void Update()
    {
        if (!UsesAutomaticArm)
            return;

        RotateHarvester();
        CheckPlotAlignment();
    }

    public void Initialize()
    {
        _reachedPlotIds.Clear();
        ResolveSceneReferences();
        ConfigureMode(PlayableScenarioRuntime.CurrentScenario != null
            ? PlayableScenarioRuntime.CurrentScenario.harvestMode
            : PlayableHarvestMode.ManualBlade);
        LoadSavedLevel();
    }

    public void ConfigureMode(PlayableHarvestMode mode)
    {
        _harvestMode = mode;
        ResolveSceneReferences();

        if (harvestMachine != null)
        {
            harvestMachine.gameObject.SetActive(UsesManualBlade);
        }

        CollectExistingArms();
        for (int i = 0; i < _spawnedHarvesterArms.Count; i++)
        {
            HarvesterArm arm = _spawnedHarvesterArms[i];
            if (arm != null)
            {
                arm.gameObject.SetActive(UsesAutomaticArm);
            }
        }
        if (UsesAutomaticArm)
        {
            EnsureHarvesterArms();
        }

        _reachedPlotIds.Clear();
    }

    private bool UsesManualBlade => _harvestMode == PlayableHarvestMode.ManualBlade || _harvestMode == PlayableHarvestMode.Hybrid;
    private bool UsesAutomaticArm => _harvestMode == PlayableHarvestMode.AutomaticArm || _harvestMode == PlayableHarvestMode.Hybrid;

    private void LoadSavedLevel()
    {
        int targetLevel = harvesterLevel;
        if (PlayableFarmState.PlayerData != null)
        {
            var save = PlayableFarmState.GetHarvesterSaveData();
            if (save != null && save.currentLevel > 0)
            {
                targetLevel = save.currentLevel;
            }
        }

        SetLevel(targetLevel, save: false);
    }

    private void SetLevel(int level, bool save = true)
    {
        harvesterLevel = Mathf.Clamp(level, 1, MaxLevel);
        if (save)
        {
            PlayableFarmState.UpdateHarvesterLevel(harvesterLevel);
        }
        EnsureHarvesterArms();
        UpdateBladeCountVisual();
        OnHarvesterLevelChanged?.Invoke(harvesterLevel);
        GameEventBus.OnHarvesterUpgradedEvent?.Invoke(harvesterLevel);
    }

    public bool TryUpgrade()
    {
        if (IsMaxLevel || Config == null)
            return false;

        double cost = Config.GetUpgradeCost(harvesterLevel);
        if (cost > 0)
        {
            if (!PlayableFarmState.HasEnoughGold(cost))
                return false;

            PlayableFarmState.ConsumeGold(cost);
        }

        SetLevel(harvesterLevel + 1, save: true);
        SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_upgrade);
        return true;
    }

    public void SetHarvesterLevel(int level)
    {
        SetLevel(level, save: true);
    }

    private int ResolveBladeCount(int armIndex)
    {
        if (PlayableScenarioRuntime.CurrentScenario != null && PlayableScenarioRuntime.CurrentScenario.scenario == PlayableScenarioId.Scenario124)
        {
            if (harvesterLevel >= 4) return 3;
            if (harvesterLevel >= 2) return 2;
            return 1;
        }
        return Config != null ? Config.GetBladeCount(harvesterLevel, armIndex) : Mathf.Clamp(harvesterLevel - armIndex * 5, 1, 3);
    }

    private void UpdateBladeCountVisual()
    {
        int totalArms = _spawnedHarvesterArms.Count;
        for (int i = 0; i < totalArms; i++)
        {
            if (_spawnedHarvesterArms[i] != null)
            {
                int bladeCount = ResolveBladeCount(i);
                _spawnedHarvesterArms[i].SetBladeCount(bladeCount);
            }
        }
    }

    private void RotateHarvester()
    {
        float speed = RotationSpeed;
        float dt = Time.deltaTime;
        for (int i = 0; i < _spawnedHarvesterArms.Count; i++)
        {
            HarvesterArm arm = _spawnedHarvesterArms[i];
            if (arm != null)
            {
                arm.RotateArm(speed, dt);
                arm.SpinBlades(dt);
                arm.AnimateBladePulse(dt);
            }
        }
    }

    private void CheckPlotAlignment()
    {
        FarmPlotSystem plots = PlotSystem;
        if (plots == null || _spawnedHarvesterArms.Count == 0)
        {
            return;
        }

        float minimumAlignmentDot = Mathf.Cos(AlignmentTolerance * Mathf.Deg2Rad);
        IReadOnlyList<FarmPlotData> plotList = plots.Plots;
        int plotCount = plotList.Count;
        for (int plotIdx = 0; plotIdx < plotCount; plotIdx++)
        {
            FarmPlotData plot = plotList[plotIdx];
            if (plot == null) continue;
            HarvesterArm alignedArm = null;

            for (int i = 0; i < _spawnedHarvesterArms.Count; i++)
            {
                HarvesterArm arm = _spawnedHarvesterArms[i];
                if (arm == null || !arm.gameObject.activeInHierarchy) continue;

                if (plot.RingIndex >= arm.CurrentBladeCount)
                    continue;

                Vector3 harvesterForward = arm.GetForwardDirection();
                Vector3 plotDirection = plots.GetPlotDirection(plot.Id, arm.Pivot.position);
                plotDirection = Vector3.ProjectOnPlane(plotDirection, arm.Pivot.forward);
                if (plotDirection.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                if (Vector3.Dot(harvesterForward, plotDirection.normalized) >= minimumAlignmentDot)
                {
                    alignedArm = arm;
                    break;
                }
            }

            HandlePlotAlignment(plot, alignedArm != null, alignedArm);
        }
    }

    private void HandlePlotAlignment(FarmPlotData plot, bool aligned, HarvesterArm sourceArm)
    {
        if (!aligned)
        {
            _reachedPlotIds.Remove(plot.Id);
            return;
        }

        if (_reachedPlotIds.Add(plot.Id))
        {
            HarvestPlot(plot, sourceArm);
        }
    }

    private void HarvestPlot(FarmPlotData plotData, HarvesterArm sourceArm)
    {
        FarmPlotSystem plots = PlotSystem;
        if (plots == null || plotData == null)
            return;

        Plot targetPlot = plots.GetPlotComponent(plotData.Id);
        if (targetPlot == null || targetPlot.State != FarmPlotState.Occupied || targetPlot.CurrentPlant == null)
            return;

        Plant plant = targetPlot.CurrentPlant;
        if (!plant.CanHarvest())
            return;

        double revenue = plant.Harvest();
        if (revenue > 0 || plant.FilledSlotCount == 0)
        {
            //SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_reaper_collect);
            LogHarvest(targetPlot, revenue);
            PlotHarvested?.Invoke(targetPlot, revenue);
        }
    }

    private void LogHarvest(Plot plot, double revenue)
    {
        if (!logPlotHarvested)
            return;

        Debug.Log($"Harvester harvested plot {plot.PlotId} yielding {revenue:F1} coins.", this);
    }

    private void EnsureHarvesterArms()
    {
        Transform parent = harvesterArmParent != null ? harvesterArmParent : transform;
        int targetCount = Mathf.Max(1, HarvesterCount);

        CollectExistingArms();

        if (!UsesAutomaticArm)
        {
            return;
        }

        if (PoolHolder.Instance != null && harvesterArmPrefab != null)
        {
            int maxArmCount = targetCount;
            if (PlayableScenarioRuntime.CurrentScenario != null && PlayableScenarioRuntime.CurrentScenario.scenario == PlayableScenarioId.Scenario124)
            {
                maxArmCount = Mathf.Max(maxArmCount, 4);
            }
            else if (Config != null)
            {
                maxArmCount = Mathf.Max(maxArmCount, Config.GetHarvesterCount(MaxLevel));
            }

            PoolHolder.Instance.Prewarm(
                harvesterArmPrefab,
                Mathf.Max(0, maxArmCount - _spawnedHarvesterArms.Count),
                maxArmCount);
        }

        float baseZ = (_spawnedHarvesterArms.Count > 0 && _spawnedHarvesterArms[0] != null && _spawnedHarvesterArms[0].Pivot != null)
            ? _spawnedHarvesterArms[0].Pivot.localEulerAngles.z
            : (ArmConfig != null ? ArmConfig.rotationOffsetZ : 0f);

        while (_spawnedHarvesterArms.Count < targetCount)
        {
            if (harvesterArmPrefab != null)
            {
                HarvesterArm newArm = null;
                if (PoolHolder.Instance != null)
                {
                    newArm = PoolHolder.Instance.Get(harvesterArmPrefab, parent) as HarvesterArm;
                }
                if (newArm == null)
                {
                    newArm = Instantiate(harvesterArmPrefab, parent);
                }
                if (newArm == null) break;
                newArm.transform.localPosition = Vector3.zero;
                newArm.transform.localRotation = Quaternion.identity;
                newArm.transform.localScale = Vector3.one;
                newArm.gameObject.SetActive(true);
                _spawnedHarvesterArms.Add(newArm);
            }
            else
            {
                break;
            }
        }

        while (_spawnedHarvesterArms.Count > targetCount)
        {
            int lastIndex = _spawnedHarvesterArms.Count - 1;
            HarvesterArm excessArm = _spawnedHarvesterArms[lastIndex];
            _spawnedHarvesterArms.RemoveAt(lastIndex);
            if (excessArm != null)
            {
                if (Application.isPlaying)
                {
                    if (PoolHolder.Instance != null && harvesterArmPrefab != null)
                        PoolHolder.Instance.Release(excessArm, PoolHolder.GetKey(harvesterArmPrefab));
                    else
                        Destroy(excessArm.gameObject);
                }
                else
                {
                    DestroyImmediate(excessArm.gameObject);
                }
            }
        }

        float angleStep = 360f / targetCount;
        for (int i = 0; i < _spawnedHarvesterArms.Count; i++)
        {
            HarvesterArm arm = _spawnedHarvesterArms[i];
            if (arm != null && arm.Pivot != null)
            {
                arm.Pivot.localRotation = Quaternion.Euler(0f, 0f, baseZ + i * angleStep);
                int bladeCount = ResolveBladeCount(i);
                arm.SetBladeCount(bladeCount);
                arm.gameObject.SetActive(true);
            }
        }
    }

    private void ResolveSceneReferences()
    {
        if (harvestMachine == null)
        {
            harvestMachine = GetComponentInChildren<HarvestMachine>(true);
        }

        if (harvesterArmParent == null)
        {
            Transform farmRoot = transform.parent;
            harvesterArmParent = farmRoot != null ? farmRoot.Find("AutoHarvesterRoot") : null;
        }
    }

    private void CollectExistingArms()
    {
        _spawnedHarvesterArms.RemoveAll(arm => arm == null);
        Transform parent = harvesterArmParent != null ? harvesterArmParent : transform;
        HarvesterArm[] existingArms = parent.GetComponentsInChildren<HarvesterArm>(true);
        for (int i = 0; i < existingArms.Length; i++)
        {
            HarvesterArm arm = existingArms[i];
            if (arm != null && !_spawnedHarvesterArms.Contains(arm))
            {
                _spawnedHarvesterArms.Add(arm);
            }
        }
    }
}
