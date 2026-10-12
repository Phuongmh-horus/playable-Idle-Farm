using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FarmIrrigationSystem : MonoBehaviour
{
    [Header("Sprinkler Settings")]
    [SerializeField] private SprinklerArmConfig armConfig;
    [Tooltip("Playable pacing: every sprinkler pass completes one fruit slot, regardless of the product game's long-form water requirement.")]
    [SerializeField] private bool completeOneFruitPerPass = true;

    [Header("Prefab & Spawning")]
    [SerializeField] private SprinklerArm sprinklerArmPrefab;
    [SerializeField] private Transform sprinklerArmParent;

    private FarmPlotSystem _plotSystem;
    private FarmPlotSystem PlotSystem => _plotSystem != null ? _plotSystem : (_plotSystem = FarmSystem.Instance?.PlotSystem);

    private readonly HashSet<int> _reachedPlotIds = new HashSet<int>();
    private readonly List<SprinklerArm> _spawnedSprinklerArms = new List<SprinklerArm>();

    private SprinklerConfig Config => ConfigHolder.Instance?.FarmConfig?.sprinkler;

    public SprinklerArmConfig ArmConfig => armConfig != null ? armConfig : (sprinklerArmPrefab != null ? sprinklerArmPrefab.Config : null);
    public float AlignmentTolerance => ArmConfig != null ? ArmConfig.alignmentTolerance : 3f;
    public float WaterAnimationDuration => ArmConfig != null ? ArmConfig.waterAnimationDuration : 0.5f;

    public int MaxLevel => (Config != null && Config.maxLevel > 0) ? Config.maxLevel : 30;

    private int _sprinklerLevel = 1;
    public int Level => _sprinklerLevel;
    public int CurrentArmLength => PlotSystem != null ? PlotSystem.GetOwnedRingCount() : 1;
    public double NextUpgradeCost => (Config != null && _sprinklerLevel < MaxLevel) ? Config.GetUpgradeCost(_sprinklerLevel) : -1;
    public bool IsMaxLevel => _sprinklerLevel >= MaxLevel;
    public bool CanUpgrade => !IsMaxLevel && (NextUpgradeCost <= 0 || PlayableFarmState.HasEnoughGold(NextUpgradeCost));

    #region Events

    public event Action<FarmPlotData> PlotReached;
    public event Action<Plot, float, float> PlotWaterPumped; // (plot, waterAmount, duration)
    public event Action<int> OnSprinklerLevelChanged;

    #endregion

    private void Update()
    {
        RotateSprinkler();
        CheckPlotAlignment();
    }

    public void Initialize()
    {
        _reachedPlotIds.Clear();
        if (PlotSystem != null)
        {
            PlotSystem.OnPlotUnlocked -= HandlePlotUnlocked;
            PlotSystem.OnPlotUnlocked += HandlePlotUnlocked;
        }
        LoadSavedLevel();
    }

    private void OnDestroy()
    {
        if (_plotSystem != null)
        {
            _plotSystem.OnPlotUnlocked -= HandlePlotUnlocked;
        }
    }

    private void HandlePlotUnlocked(FarmPlotData plot)
    {
        UpdateSprinklerVisual();
    }

    private void LoadSavedLevel()
    {
        int targetLevel = _sprinklerLevel;
        if (PlayableFarmState.PlayerData != null)
        {
            var save = PlayableFarmState.GetSprinklerSaveData();
            if (save != null && save.currentLevel > 0)
            {
                targetLevel = save.currentLevel;
            }
        }

        SetLevel(targetLevel, save: false);
    }

    private void SetLevel(int level, bool save = true)
    {
        _sprinklerLevel = Mathf.Clamp(level, 1, MaxLevel);
        if (save)
        {
            PlayableFarmState.UpdateSprinklerLevel(_sprinklerLevel);
        }
        EnsureSprinklerArms();
        UpdateSprinklerVisual();
        OnSprinklerLevelChanged?.Invoke(_sprinklerLevel);
        GameEventBus.OnSprinklerUpgradedEvent?.Invoke(_sprinklerLevel);
    }

    public bool TryUpgrade()
    {
        if (IsMaxLevel || Config == null)
            return false;

        double cost = Config.GetUpgradeCost(_sprinklerLevel);
        if (cost > 0)
        {
            if (!PlayableFarmState.HasEnoughGold(cost))
                return false;

            PlayableFarmState.ConsumeGold(cost);
        }

        SetLevel(_sprinklerLevel + 1, save: true);
        SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_upgrade);
        return true;
    }

    private void UpdateSprinklerVisual()
    {
        int length = CurrentArmLength;
        for (int i = 0; i < _spawnedSprinklerArms.Count; i++)
        {
            if (_spawnedSprinklerArms[i] != null)
            {
                _spawnedSprinklerArms[i].SetArmLength(length);
            }
        }
    }

    private void RotateSprinkler()
    {
        float speed = Config != null ? Config.GetRotationSpeed(_sprinklerLevel) : 0f;
        float dt = Time.deltaTime;
        for (int i = 0; i < _spawnedSprinklerArms.Count; i++)
        {
            if (_spawnedSprinklerArms[i] != null)
            {
                _spawnedSprinklerArms[i].RotateArm(speed, dt);
            }
        }
    }

    private void CheckPlotAlignment()
    {
        FarmPlotSystem plots = PlotSystem;
        if (plots == null || _spawnedSprinklerArms.Count == 0)
        {
            return;
        }

        float minimumAlignmentDot = Mathf.Cos(AlignmentTolerance * Mathf.Deg2Rad);
        IReadOnlyList<FarmPlotData> plotList = plots.Plots;
        int plotCount = plotList != null ? plotList.Count : 0;
        for (int p = 0; p < plotCount; p++)
        {
            FarmPlotData plot = plotList[p];
            if (plot == null) continue;
            SprinklerArm alignedArm = null;

            for (int i = 0; i < _spawnedSprinklerArms.Count; i++)
            {
                SprinklerArm arm = _spawnedSprinklerArms[i];
                if (arm == null || !arm.gameObject.activeInHierarchy) continue;

                // Chá»‰ tÆ°á»›i Ä‘Æ°á»£c nếu Ä‘á»™ dÃ i cánh tay vÆ°Æ¡n tá»›i Ring cá»§a Ã´ Ä‘áº¥t
                if (plot.RingIndex >= arm.CurrentArmLength)
                    continue;

                Vector3 sprinklerForward = arm.GetForwardDirection();
                Vector3 plotDirection = plots.GetPlotDirection(plot.Id, arm.Pivot.position);
                // The arm rotates around its local Z axis. Ignore height/depth outside that
                // rotation plane so a visually aligned sprinkler can reach the plant.
                plotDirection = Vector3.ProjectOnPlane(plotDirection, arm.Pivot.forward);
                if (plotDirection.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                if (Vector3.Dot(sprinklerForward, plotDirection.normalized) >= minimumAlignmentDot)
                {
                    alignedArm = arm;
                    break;
                }
            }

            HandlePlotAlignment(plot, alignedArm != null, alignedArm);
        }
    }

    private void HandlePlotAlignment(FarmPlotData plot, bool aligned, SprinklerArm sourceArm)
    {
        if (!aligned)
        {
            _reachedPlotIds.Remove(plot.Id);
            return;
        }

        // BÆ¡m nÆ°á»›c 1 láº§n duy nháº¥t khi vá»«a quÃ©t trÃºng Ã´ Ä‘áº¥t
        if (_reachedPlotIds.Add(plot.Id))
        {
            PlotReached?.Invoke(plot);

            PumpWaterToPlot(plot, sourceArm);
        }
    }

    private void PumpWaterToPlot(FarmPlotData plotData, SprinklerArm sourceArm)
    {
        FarmPlotSystem plots = PlotSystem;
        if (plots == null || plotData == null)
            return;

        Plot targetPlot = plots.GetPlotComponent(plotData.Id);
        if (targetPlot == null)
            return;

        float amount = Config != null ? Config.GetWaterAmount(_sprinklerLevel) : 5f;

        if (targetPlot.State == FarmPlotState.Occupied && targetPlot.CurrentPlant != null && !targetPlot.CurrentPlant.IsFull)
        {
            Plant plant = targetPlot.CurrentPlant;
            if (completeOneFruitPerPass)
            {
                amount = Mathf.Max(amount, plant.WaterRequirement);
            }
            // Progress-bar feedback is intentionally omitted from the playable.
            // Plant owns the immediate growth visual when this water completes a slot.
            plant.AddWater(amount);
            SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_watering_machine);
        }

        PlotWaterPumped?.Invoke(targetPlot, amount, WaterAnimationDuration);
    }

    private void EnsureSprinklerArms()
    {
        Transform parent = sprinklerArmParent != null ? sprinklerArmParent : transform;
        int nozzleCount = Config != null ? Config.GetNozzleCount(_sprinklerLevel) : 1;
        int targetCount = Mathf.Max(1, nozzleCount);

        // Thu tháº­p các arm hiện có nếu danh sách rỗng
        if (_spawnedSprinklerArms.Count == 0)
        {
            SprinklerArm[] existingArms = parent.GetComponentsInChildren<SprinklerArm>(true);
            foreach (SprinklerArm arm in existingArms)
            {
                if (arm != null && !_spawnedSprinklerArms.Contains(arm))
                {
                    _spawnedSprinklerArms.Add(arm);
                }
            }
        }

        // Dá»n dáº¹p null items
        _spawnedSprinklerArms.RemoveAll(arm => arm == null);

        if (PoolHolder.Instance != null && sprinklerArmPrefab != null)
        {
            int maxArmCount = Config != null ? Mathf.Max(1, Config.GetNozzleCount(MaxLevel)) : targetCount;
            PoolHolder.Instance.Prewarm(
                sprinklerArmPrefab,
                Mathf.Max(0, maxArmCount - _spawnedSprinklerArms.Count),
                maxArmCount);
        }

        // LÆ°u góc cÆ¡ sá»Ÿ hiá»‡n táº¡i nếu Ä‘Ã£ có arm quay Ä‘á»ƒ tránh giáº­t hÃ¬nh khi nÃ¢ng cáº¥p
        float baseZ = (_spawnedSprinklerArms.Count > 0 && _spawnedSprinklerArms[0] != null && _spawnedSprinklerArms[0].Pivot != null)
            ? _spawnedSprinklerArms[0].Pivot.localEulerAngles.z
            : (ArmConfig != null ? ArmConfig.rotationOffsetZ : 0f);

        // Nếu thiếu arm thÃ¬ spawn thÃªm
        while (_spawnedSprinklerArms.Count < targetCount)
        {
            if (sprinklerArmPrefab != null)
            {
                SprinklerArm newArm = PoolHolder.Instance != null
                    ? PoolHolder.Instance.Get(sprinklerArmPrefab, parent) as SprinklerArm
                    : Instantiate(sprinklerArmPrefab, parent);
                if (newArm == null) break;
                newArm.transform.localPosition = Vector3.zero;
                newArm.transform.localRotation = Quaternion.identity;
                newArm.transform.localScale = Vector3.one;
                _spawnedSprinklerArms.Add(newArm);
            }
            else
            {
                break;
            }
        }

        // Nếu thá»«a arm thÃ¬ áº©n hoáº·c xoá bá»›t
        while (_spawnedSprinklerArms.Count > targetCount)
        {
            int lastIndex = _spawnedSprinklerArms.Count - 1;
            SprinklerArm excessArm = _spawnedSprinklerArms[lastIndex];
            _spawnedSprinklerArms.RemoveAt(lastIndex);
            if (excessArm != null)
            {
                if (Application.isPlaying)
                {
                    if (PoolHolder.Instance != null && sprinklerArmPrefab != null)
                        PoolHolder.Instance.Release(excessArm, PoolHolder.GetKey(sprinklerArmPrefab));
                    else
                        Destroy(excessArm.gameObject);
                }
                else
                {
                    DestroyImmediate(excessArm.gameObject);
                }
            }
        }

        // Chia Ä‘á»u góc cho các arm theo nozzleCount (360 Ä‘á»™ / targetCount)
        float angleStep = 360f / targetCount;
        int length = CurrentArmLength;
        for (int i = 0; i < _spawnedSprinklerArms.Count; i++)
        {
            SprinklerArm arm = _spawnedSprinklerArms[i];
            if (arm != null && arm.Pivot != null)
            {
                arm.Pivot.localRotation = Quaternion.Euler(0f, 0f, baseZ + i * angleStep);
                arm.SetArmLength(length);
            }
        }
    }
}
