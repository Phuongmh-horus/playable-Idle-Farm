using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlotVisualConfigSO", menuName = "Farm/PlotVisualConfigSO")]
public class PlotVisualConfigSO : ScriptableObject
{
    [Serializable]
    public struct PlotStateSpriteEntry
    {
        public FarmPlotState state;
        public Sprite sprite;
    }

    [SerializeField] private List<PlotStateSpriteEntry> stateSprites = new List<PlotStateSpriteEntry>();

    // Cache dictionary để lookup O(1) lúc runtime
    private Dictionary<FarmPlotState, Sprite> _spriteLookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void OnValidate()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _spriteLookup = new Dictionary<FarmPlotState, Sprite>();
        if (stateSprites == null) return;

        foreach (var entry in stateSprites)
        {
            _spriteLookup[entry.state] = entry.sprite;
        }
    }

    /// <summary>
    /// Lấy Sprite tương ứng với FarmPlotState
    /// </summary>
    public Sprite GetSprite(FarmPlotState state)
    {
        if (_spriteLookup == null)
        {
            BuildLookup();
        }

        if (_spriteLookup != null && _spriteLookup.TryGetValue(state, out Sprite sprite))
        {
            return sprite;
        }

        return null;
    }
}
