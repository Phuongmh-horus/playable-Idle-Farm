using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LuckyWheelConfig", menuName = "ScriptableObjects/LuckyWheelConfig", order = 3)]
public class LuckyWheelConfigSO : ScriptableObject
{
    public const int RequiredSlotCount = 6;

    [Header("Rewards")]
    [SerializeField] private List<LuckyWheelSlotData> slots = new()
    {
        new LuckyWheelSlotData { SlotId = "slot_0" },
        new LuckyWheelSlotData { SlotId = "slot_1" },
        new LuckyWheelSlotData { SlotId = "slot_2" },
        new LuckyWheelSlotData { SlotId = "slot_3" },
        new LuckyWheelSlotData { SlotId = "slot_4" },
        new LuckyWheelSlotData { SlotId = "slot_5" }
    };

    [Header("Spin")]
    [Min(0.1f)] [SerializeField] private float spinDuration = 5f;
    [Min(1)] [SerializeField] private int fullRotationCount = 6;
    [Tooltip("Normalized spin progress. Use a slow start and a slow finish for anticipation.")]
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Center of slot 0, in clockwise degrees measured from the top of the wheel.")]
    [SerializeField] private float firstSlotCenterAngle;
    [Tooltip("Pointer angle, in clockwise degrees measured from the top of the wheel.")]
    [SerializeField] private float pointerAngle;

    [Header("Daily limits (UTC)")]
    [Min(1)] [SerializeField] private int freeSpinsPerDay = 1;
    [Min(0)] [SerializeField] private int adsSpinsPerDay = 10;

    public IReadOnlyList<LuckyWheelSlotData> Slots => slots;
    public float SpinDuration => spinDuration;
    public int FullRotationCount => fullRotationCount;
    public AnimationCurve SpinCurve => spinCurve;
    public float FirstSlotCenterAngle => firstSlotCenterAngle;
    public float PointerAngle => pointerAngle;
    public int FreeSpinsPerDay => freeSpinsPerDay;
    public int AdsSpinsPerDay => adsSpinsPerDay;
    public float DegreesPerSlot => 360f / RequiredSlotCount;

    public bool IsValid(out string error)
    {
        if (slots == null || slots.Count != RequiredSlotCount)
        {
            error = $"Lucky Wheel requires exactly {RequiredSlotCount} slots.";
            return false;
        }

        var slotIds = new HashSet<string>();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                error = $"Lucky Wheel slot {i} is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(slots[i].SlotId) || !slotIds.Add(slots[i].SlotId))
            {
                error = $"Lucky Wheel slot {i} must have a non-empty, unique Slot Id.";
                return false;
            }

            if (slots[i].Reward == null)
            {
                error = $"Lucky Wheel slot {i} has no reward.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public float GetSlotCenterAngle(int slotIndex)
    {
        return firstSlotCenterAngle + slotIndex * DegreesPerSlot;
    }
}

[Serializable]
public class LuckyWheelSlotData
{
    [Tooltip("Stable identifier for this slot. Keep it unique when result logic is replaced later.")]
    public string SlotId;
    public RewardItem Reward = new();
}
