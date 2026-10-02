using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "UIManager/WorldLayerReferenceSO", fileName = "WorldLayerReferenceSO", order = 2)]
public class WorldLayerReferenceSO : ScriptableObject
{
    public List<WorldLayerReferenceData> layerReferenceList = new();
    private Dictionary<WorldLayerType, WorldLayerBase> _layerReferenceDic;

    public Dictionary<WorldLayerType, WorldLayerBase> LayerBaseDictionary
    {
        get
        {
            if (_layerReferenceDic == null || _layerReferenceDic.Count == 0)
            {
                _layerReferenceDic = new Dictionary<WorldLayerType, WorldLayerBase>();
                foreach (var layerReferenceData in layerReferenceList)
                {
                    if (layerReferenceData != null && layerReferenceData.layerBase != null)
                    {
                        _layerReferenceDic[layerReferenceData.layerType] = layerReferenceData.layerBase;
                    }
                }
            }
            return _layerReferenceDic;
        }
    }

    public WorldLayerBase GetLayerBase(WorldLayerType layerType)
    {
        return LayerBaseDictionary.TryGetValue(layerType, out var layerBase) ? layerBase : null;
    }

    public WorldLayerSpawnMode GetSpawnMode(WorldLayerType layerType)
    {
        foreach (var data in layerReferenceList)
        {
            if (data != null && data.layerType == layerType)
            {
                return data.spawnMode;
            }
        }
        return WorldLayerSpawnMode.Multiple;
    }

    public void RefreshDictionary()
    {
        _layerReferenceDic = null;
    }
}

[Serializable]
public class WorldLayerReferenceData
{
    public WorldLayerType layerType;
    public WorldLayerBase layerBase;
    public WorldLayerSpawnMode spawnMode = WorldLayerSpawnMode.Multiple;
}
