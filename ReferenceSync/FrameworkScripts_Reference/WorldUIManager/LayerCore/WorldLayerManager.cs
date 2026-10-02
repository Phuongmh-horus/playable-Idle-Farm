using System;
using System.Collections.Generic;
using UnityEngine;

public partial class WorldLayerManager : MonoSingleton<WorldLayerManager>
{
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private RectTransform worldLayerParent;
    [SerializeField] private WorldLayerReferenceSO worldLayerReferenceSO;
    [SerializeField] private List<WorldLayerReferenceData> layerPreload = new();

    public static event Action<WorldLayerBase> OnLayerShown;

    private readonly Dictionary<WorldLayerType, Queue<WorldLayerBase>> _poolDic = new();
    private readonly List<WorldLayerBase> _activeLayers = new();

    public IReadOnlyList<WorldLayerBase> ActiveLayers => _activeLayers;

    private void Reset()
    {
        if (!worldCanvas) worldCanvas = GetComponentInChildren<Canvas>();
        if (!worldLayerParent) worldLayerParent = transform as RectTransform;
    }

    protected override void Awake()
    {
        base.Awake();
        PreloadLayers();

        if (!TryGetComponent<WorldSinglePopupAutoCloseHandler>(out _))
        {
            gameObject.AddComponent<WorldSinglePopupAutoCloseHandler>();
        }
    }

    private void OnDestroy()
    {
        OnLayerShown = null;
    }

    private void PreloadLayers()
    {
        foreach (var data in layerPreload)
        {
            if (data?.layerBase != null)
            {
                var instance = Instantiate(data.layerBase, worldLayerParent);
                instance.LayerType = data.layerType;
                instance.CleanupBeforeHide();
                GetOrCreatePool(data.layerType).Enqueue(instance);
            }
        }
    }

    private Queue<WorldLayerBase> GetOrCreatePool(WorldLayerType layerType)
    {
        if (!_poolDic.TryGetValue(layerType, out var pool))
        {
            pool = new Queue<WorldLayerBase>();
            _poolDic[layerType] = pool;
        }
        return pool;
    }

    public T ShowWorldLayer<T>(WorldLayerType layerType, Transform target, Vector3 offset = default) where T : WorldLayerBase
    {
        var layer = Spawn(layerType);
        if (layer is T typedLayer)
        {
            typedLayer.SetupFollow(target, offset);
            OnLayerShown?.Invoke(typedLayer);
            return typedLayer;
        }
        return null;
    }

    public T ShowWorldLayer<T>(WorldLayerType layerType, Vector3 position) where T : WorldLayerBase
    {
        var layer = Spawn(layerType);
        if (layer is T typedLayer)
        {
            typedLayer.SetupPosition(position);
            OnLayerShown?.Invoke(typedLayer);
            return typedLayer;
        }
        return null;
    }

    private WorldLayerBase Spawn(WorldLayerType layerType)
    {
        WorldLayerSpawnMode spawnMode = worldLayerReferenceSO != null 
            ? worldLayerReferenceSO.GetSpawnMode(layerType) 
            : WorldLayerSpawnMode.Multiple;

        // Nếu là chế độ Single: Chỉ cho phép 1 cái duy nhất hiển thị
        // Tắt cái cũ đang hiển thị và tái sử dụng luôn
        if (spawnMode == WorldLayerSpawnMode.Single)
        {
            for (int i = _activeLayers.Count - 1; i >= 0; i--)
            {
                var activeLayer = _activeLayers[i];
                if (activeLayer != null && activeLayer.LayerType == layerType)
                {
                    ReleaseToPool(activeLayer); // Tắt ngay lập tức và đẩy về Pool
                    break;
                }
            }
        }

        var pool = GetOrCreatePool(layerType);
        WorldLayerBase layer = null;

        while (pool.Count > 0)
        {
            layer = pool.Dequeue();
            if (layer != null) break;
        }

        if (layer == null)
        {
            var prefab = worldLayerReferenceSO != null ? worldLayerReferenceSO.GetLayerBase(layerType) : null;
            if (prefab == null)
            {
                Debug.LogError($"[WorldLayerManager] Prefab not found for WorldLayerType: {layerType}");
                return null;
            }

            layer = Instantiate(prefab, worldLayerParent);
            layer.LayerType = layerType;
        }

        if (!_activeLayers.Contains(layer))
        {
            _activeLayers.Add(layer);
        }

        return layer;
    }

    public void Hide(WorldLayerBase layer, bool animate = true)
    {
        if (layer == null) return;

        if (animate)
        {
            layer.Hide(true);
        }
        else
        {
            ReleaseToPool(layer);
        }
    }

    public void ReleaseToPool(WorldLayerBase layer)
    {
        if (layer == null) return;

        _activeLayers.Remove(layer);
        layer.CleanupBeforeHide();
        GetOrCreatePool(layer.LayerType).Enqueue(layer);
    }

    public void HideWorldLayer(WorldLayerType layerType, Transform target, bool animate = true)
    {
        if (target == null) return;

        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            var layer = _activeLayers[i];
            if (layer.LayerType == layerType && layer.FollowTarget == target)
            {
                Hide(layer, animate);
            }
        }
    }

    public void HideAllByTarget(Transform target, bool animate = true)
    {
        if (target == null) return;

        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            var layer = _activeLayers[i];
            if (layer.FollowTarget == target)
            {
                Hide(layer, animate);
            }
        }
    }

    public void HideAllByType(WorldLayerType layerType, bool animate = true)
    {
        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            var layer = _activeLayers[i];
            if (layer.LayerType == layerType)
            {
                Hide(layer, animate);
            }
        }
    }

    public void HideAllSingleLayers(bool animate = true)
    {
        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            if (i >= _activeLayers.Count) continue;
            var layer = _activeLayers[i];
            if (layer == null || layer.IsHiding) continue;

            WorldLayerSpawnMode spawnMode = worldLayerReferenceSO != null 
                ? worldLayerReferenceSO.GetSpawnMode(layer.LayerType) 
                : WorldLayerSpawnMode.Multiple;

            if (spawnMode == WorldLayerSpawnMode.Single)
            {
                Hide(layer, animate);
            }
        }
    }

    public void HideLayersByTypes(ICollection<WorldLayerType> types, WorldLayerBase exceptLayer = null, bool animate = true)
    {
        if (types == null || types.Count == 0) return;

        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            if (i >= _activeLayers.Count) continue;
            var layer = _activeLayers[i];
            if (layer == null || layer == exceptLayer || layer.IsHiding) continue;

            if (types.Contains(layer.LayerType))
            {
                Hide(layer, animate);
            }
        }
    }

    public void HideAll(bool animate = false)
    {
        for (int i = _activeLayers.Count - 1; i >= 0; i--)
        {
            Hide(_activeLayers[i], animate);
        }
    }
}
