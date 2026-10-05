using System;
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

    [SerializeField] private PlotStateSpriteEntry[] stateSprites;

    public Sprite GetSprite(FarmPlotState state)
    {
        if (stateSprites == null)
        {
            return null;
        }

        for (int i = 0; i < stateSprites.Length; i++)
        {
            PlotStateSpriteEntry entry = stateSprites[i];
            if (entry.state == state)
            {
                return entry.sprite;
            }
        }

        return null;
    }
}
