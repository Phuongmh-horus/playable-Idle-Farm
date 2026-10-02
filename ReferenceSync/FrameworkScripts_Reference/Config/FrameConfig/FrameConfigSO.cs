using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FrameIconSO", menuName = "ScriptableObjects/FrameIconSO", order = 1)]
public class FrameConfigSO : ScriptableObject
{
    [SerializeField]
    private List<FrameConfigSOData> frameConfigList = new();

    [NonSerialized]
    private Dictionary<int, FrameConfigSOData> frameConfigs;

    public Dictionary<int, FrameConfigSOData> FrameConfigs
    {
        get
        {
            if (frameConfigs == null || frameConfigs.Count == 0)
                RebuildDictionary();
            return frameConfigs;
        }
    }

    private void OnValidate()
    {
        if (frameConfigList == null || frameConfigList.Count == 0) return;
        for (var i = 0; i < frameConfigList.Count; i++)
        {
            frameConfigList[i].ID = i;
        }

        RebuildDictionary();
    }

    private void RebuildDictionary()
    {
        frameConfigs = new Dictionary<int, FrameConfigSOData>();
        foreach (var data in frameConfigList)
        {
            if (!frameConfigs.ContainsKey(data.ID))
                frameConfigs.Add(data.ID, data);
            else
                Debug.LogWarning($"Duplicate frame ID: {data.ID}");
        }
    }

    public FrameConfigSOData GetFrameConfigByID(int id)
    {
        return FrameConfigs.TryGetValue(id, out var frame) ? frame : null;
    }

    public Sprite GetFrameIconSpriteByID(int id)
    {
        var frame = GetFrameConfigByID(id);
        if (frame != null) return frame.FrameIcon;
        Debug.LogError($"FrameIcon with ID {id} not found.");
        return null;
    }
}

[Serializable]
public class FrameConfigSOData
{
    public int ID;
    public Sprite FrameIcon;
    public Price Price;
}
