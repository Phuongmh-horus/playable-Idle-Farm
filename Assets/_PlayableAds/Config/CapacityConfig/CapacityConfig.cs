using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CapacityConfig", menuName = "ScriptableObjects/CapacityConfig", order = 5)]

public class CapacityConfig : ScriptableObject
{
    public List<CapacitySliderStateData> sliderStates = new List<CapacitySliderStateData>();
}

[System.Serializable]
public class CapacitySliderStateData
{
    [Range(0f, 1f)] public float minThreshold = 0f;
    [FormerlySerializedAs("threshold")]
    [Range(0f, 1f)] public float maxThreshold = 1f;
    public Sprite sprite;
    public Image.Type imageType = Image.Type.Tiled;

    public bool Matches(float percent)
    {
        return percent >= minThreshold && percent <= maxThreshold;
    }
}

