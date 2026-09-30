using System;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour
{
    [SerializeField] private SpriteRenderer plantSprite;
    [SerializeField] private PlantAnimator plantAnimator;
    private FruitTransportStation _cachedTransportStation;
    private int _plantLevel = 1;
    private int _plantId;
    private int _ringIndex;
    private float _currentWater;
    private int _filledSlotCount;

    public int PlantId => _plantId;
    public int PlantLevel => _plantLevel;
    public int FilledSlotCount => _filledSlotCount;
    public bool IsFull => _filledSlotCount >= _plantLevel;
    public bool IsPlantMaxLevel => _plantLevel >= MaxLevel;

    #region Config Shortcuts

    public PlantConfigData Config => ConfigHolder.Instance?.PlantConfig?.GetPlant(_plantId);
    public int MaxLevel => Config?.GetMaxLevel() ?? 1;
    public float WaterRequirement => Config?.GetWaterRequirement(_ringIndex) ?? 0;
    public double UpgradeCost => Config?.GetUpgradeCost(_plantLevel, _ringIndex) ?? 0;
    public float LastWaterStartProgress { get; private set; }
    public float LastWaterTargetProgress { get; private set; }

    #endregion

    private void OnValidate()
    {
        if (plantSprite == null)
            plantSprite = GetComponentInChildren<SpriteRenderer>();
        if (plantAnimator == null)
            plantAnimator = GetComponent<PlantAnimator>();
        if (!plantAnimator) plantAnimator = gameObject.AddComponent<PlantAnimator>();
    }

    public void Init(int id, int initialLevel = 1, int ring = 0)
    {
        _plantId = id;
        _ringIndex = ring;
        _plantLevel = Math.Max(1, initialLevel);
        _currentWater = 0f;
        _filledSlotCount = 0;
        LastWaterStartProgress = 0f;
        LastWaterTargetProgress = 0f;
        UpdatePlantSprite();
    }

    private void UpdatePlantSprite()
    {
        if (plantSprite != null && Config != null)
        {
            Sprite sprite = Config.GetPlantSprite(_filledSlotCount);
            if (sprite != null)
            {
                plantSprite.sprite = sprite;
            }
        }
    }

    public bool AddWater(float amount, bool delayVisual = false)
    {
        if (amount <= 0f || IsFull || Config == null)
            return false;

        float potentialWater = _currentWater + amount;
        float waterRequirement = Config.GetWaterRequirement(_ringIndex);
        if (waterRequirement <= 0f)
        {
            return false;
        }
        LastWaterStartProgress = _currentWater / waterRequirement;
        bool slotCompleted = potentialWater >= waterRequirement && waterRequirement > 0f;

        LastWaterTargetProgress = slotCompleted ? 1f : (waterRequirement > 0f ? Mathf.Clamp01(potentialWater / waterRequirement) : 0f);

        if (slotCompleted)
        {
            _currentWater = 0f;
            _filledSlotCount++;

            if (!delayVisual)
            {
                ApplyFruitGrowthVisual();
            }
            PlayableTutorialFlow.Instance?.ReportFruitGrown();
        }
        else
        {
            _currentWater = potentialWater;
        }

        return slotCompleted;
    }

    public void ApplyFruitGrowthVisual()
    {
        UpdatePlantSprite();
        plantAnimator?.PlayGrowScaleAnim();
    }

    public bool CanHarvest()
    {
        if (_filledSlotCount <= 0)
            return false;

        FruitTransportStation transport = GetTransportStation();
        if (transport != null && (!transport.IsIdleAtStation || transport.IsFull))
            return false;

        return true;
    }

    public double Harvest()
    {
        if (!CanHarvest() || _filledSlotCount <= 0)
            return 0;
        
        FruitTransportStation transport = GetTransportStation();
        if (transport == null)
        {
            return 0d;
        }

        int acceptedCount = Mathf.Min(_filledSlotCount, transport.GetCapacityLeft());
        double price = Config?.GetFruitPrice(_plantLevel, _ringIndex) ?? 0;
        double totalGained = price * acceptedCount;
        if (transport != null)
        {
            transport.CollectFruitsWithAnimation(PlantId, _filledSlotCount, transform.position, _plantLevel, _ringIndex);
        }
        GameEventBus.OnCropHarvestedEvent?.Invoke(PlantId, _filledSlotCount);
        PlayableTutorialFlow.Instance?.ReportFruitHarvested();
        _filledSlotCount -= acceptedCount;
        if (_filledSlotCount == 0) _currentWater = 0f;
        UpdatePlantSprite();
        return totalGained;
    }

    private FruitTransportStation GetTransportStation()
    {
        if (_cachedTransportStation == null)
        {
            _cachedTransportStation = FarmSystem.Instance?.TransportStation;
        }
        return _cachedTransportStation;
    }

    public bool TryUpgrade()
    {
        if (Config == null || _plantLevel >= Config.GetMaxLevel())
            return false;
        _plantLevel++;
        _currentWater = 0f;
        UpdatePlantSprite();
        PlayableTutorialFlow.Instance?.ReportFirstPlantUpgraded(this);
        return true;
    }

    public void SetLevel(int level)
    {
        int maxLevel = Config != null ? Config.GetMaxLevel() : 1;
        _plantLevel = Mathf.Clamp(level, 1, Math.Max(1, maxLevel));
        _currentWater = 0f;
        UpdatePlantSprite();
    }
}
