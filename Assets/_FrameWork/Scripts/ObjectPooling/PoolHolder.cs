using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-900)]
public sealed class PoolHolder : MonoSingleton<PoolHolder>
{
    private sealed class Pool
    {
        public readonly Queue<MonoBehaviour> Available = new Queue<MonoBehaviour>(8);
        public readonly HashSet<int> AvailableIds = new HashSet<int>();
        public MonoBehaviour Prefab;
        public int TotalCount;
        public int MaxSize;
    }

    private readonly Dictionary<string, Pool> _pools = new Dictionary<string, Pool>(16);
    private readonly Dictionary<int, string> _instanceKeys = new Dictionary<int, string>(64);
    private Transform _poolRoot;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        DontDestroyOnLoad(gameObject);
        EnsurePoolRoot();
    }

    public void Prewarm(MonoBehaviour prefab, int count, int maxSize = 0, string customKey = "")
    {
        if (prefab == null || count <= 0) return;

        Pool pool = GetOrCreatePool(prefab, customKey);
        if (maxSize > 0) pool.MaxSize = Mathf.Max(pool.MaxSize, maxSize);

        int targetCount = pool.MaxSize > 0 ? Mathf.Min(count, pool.MaxSize) : count;
        while (pool.TotalCount < targetCount)
        {
            MonoBehaviour instance = CreateInstance(pool, GetPoolKey(prefab, customKey));
            if (instance == null) break;
            ReturnToPool(instance, pool);
        }
    }

    public MonoBehaviour Get(MonoBehaviour prefab, Transform parent = null, Vector3 position = default(UnityEngine.Vector3),
         Quaternion rotation = default(UnityEngine.Quaternion), string customKey = "")
    {
        if (prefab == null) return null;

        string key = GetPoolKey(prefab, customKey);
        Pool pool = GetOrCreatePool(prefab, customKey);
        MonoBehaviour result = DequeueValid(pool);

        if (result == null)
        {
            if (pool.MaxSize > 0 && pool.TotalCount >= pool.MaxSize)
            {
                Debug.LogWarning("Pool exhausted: " + key);
                return null;
            }

            result = CreateInstance(pool, key);
            if (result == null) return null;
        }

        Transform resultTransform = result.transform;
        resultTransform.SetParent(parent, false);
        resultTransform.position = position;
        resultTransform.rotation = rotation;
        result.gameObject.SetActive(true);
        return result;
    }

    public void Release(MonoBehaviour instance, string customKey = "")
    {
        if (instance == null) return;

        int instanceId = instance.GetInstanceID();
        string key;
        bool isRegistered = _instanceKeys.TryGetValue(instanceId, out key);
        if (!isRegistered)
        {
            key = string.IsNullOrEmpty(customKey) ? instance.name : customKey;
            _instanceKeys[instanceId] = key;
        }

        Pool pool;
        if (!_pools.TryGetValue(key, out pool))
        {
            pool = new Pool { Prefab = instance };
            _pools.Add(key, pool);
        }
        if (!isRegistered) pool.TotalCount++;

        ReturnToPool(instance, pool);
    }

    public void SetMaxSize(MonoBehaviour prefab, int size, string customKey = "")
    {
        if (prefab == null) return;
        Pool pool = GetOrCreatePool(prefab, customKey);
        pool.MaxSize = Mathf.Max(0, size);
    }

    public static string GetKey(MonoBehaviour prefab)
    {
        return prefab != null ? prefab.GetType().FullName + ":" + prefab.GetInstanceID() : string.Empty;
    }

    private Pool GetOrCreatePool(MonoBehaviour prefab, string customKey)
    {
        string key = GetPoolKey(prefab, customKey);
        Pool pool;
        if (!_pools.TryGetValue(key, out pool))
        {
            pool = new Pool { Prefab = prefab };
            _pools.Add(key, pool);
        }
        return pool;
    }

    private static string GetPoolKey(MonoBehaviour prefab, string customKey)
    {
        return string.IsNullOrEmpty(customKey) ? GetKey(prefab) : customKey;
    }

    private MonoBehaviour CreateInstance(Pool pool, string key)
    {
        if (pool.Prefab == null) return null;

        MonoBehaviour instance = Instantiate(pool.Prefab, EnsurePoolRoot());
        instance.name = pool.Prefab.name;
        instance.gameObject.SetActive(false);
        pool.TotalCount++;
        _instanceKeys[instance.GetInstanceID()] = key;
        return instance;
    }

    private static MonoBehaviour DequeueValid(Pool pool)
    {
        while (pool.Available.Count > 0)
        {
            MonoBehaviour instance = pool.Available.Dequeue();
            if (instance == null)
            {
                pool.TotalCount = Mathf.Max(0, pool.TotalCount - 1);
                continue;
            }

            pool.AvailableIds.Remove(instance.GetInstanceID());
            return instance;
        }
        return null;
    }

    private void ReturnToPool(MonoBehaviour instance, Pool pool)
    {
        int instanceId = instance.GetInstanceID();
        if (!pool.AvailableIds.Add(instanceId)) return;

        instance.gameObject.SetActive(false);
        Transform instanceTransform = instance.transform;
        instanceTransform.SetParent(EnsurePoolRoot(), false);
        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = Quaternion.identity;
        instanceTransform.localScale = Vector3.one;
        pool.Available.Enqueue(instance);
    }

    private Transform EnsurePoolRoot()
    {
        if (_poolRoot != null) return _poolRoot;

        Transform existing = transform.Find("RuntimePool");
        if (existing != null)
        {
            _poolRoot = existing;
            return _poolRoot;
        }

        GameObject root = new GameObject("RuntimePool");
        _poolRoot = root.transform;
        _poolRoot.SetParent(transform, false);
        return _poolRoot;
    }

    protected override void OnDestroy()
    {
        _pools.Clear();
        _instanceKeys.Clear();
        base.OnDestroy();
    }
}
