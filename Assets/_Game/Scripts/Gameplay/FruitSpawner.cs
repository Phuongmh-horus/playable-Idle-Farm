using UnityEngine;

public static class FruitSpawner
{
    public static void Prewarm(int fruitCount, int animatedFruitCount, int maxFruitCount = 0, int maxAnimatedFruitCount = 0)
    {
        PoolHolder pool = PoolHolder.Instance;
        FruitConfig config = ConfigHolder.Instance != null ? ConfigHolder.Instance.FruitConfig : null;
        if (pool == null || config == null) return;

        if (config.fruitPrefab != null)
            pool.Prewarm(config.fruitPrefab, fruitCount, maxFruitCount);
        if (config.animatedFruitPrefab != null)
            pool.Prewarm(config.animatedFruitPrefab, animatedFruitCount, maxAnimatedFruitCount);
    }

    #region Fruit Spawning

    public static Fruit SpawnFruit(int plantId, Transform parent, Vector3 localPosition = default, Quaternion localRotation = default)
    {
        var fruitPrefab = ConfigHolder.Instance?.FruitConfig?.fruitPrefab;
        if (!fruitPrefab)
            return null;

        Fruit fruit = SpawnFruit(fruitPrefab, parent, localPosition, localRotation);
        if (fruit != null)
        {
            fruit.Init(plantId);
        }
        return fruit;
    }

    private static Fruit SpawnFruit(Fruit prefab, Transform parent, Vector3 localPosition = default, Quaternion localRotation = default)
    {
        if (prefab == null) return null;
        PoolHolder pool = PoolHolder.Instance;
        Fruit fruit = pool != null
            ? pool.Get((MonoBehaviour)prefab, parent) as Fruit
            : Object.Instantiate(prefab, parent);
        if (fruit != null)
        {
            fruit.transform.localPosition = localPosition;
            fruit.transform.localRotation = localRotation;
        }

        return fruit;
    }

    public static void DespawnFruit(Fruit fruit)
    {
        if (fruit == null) return;

        fruit.ResetForPool();
        if (PoolHolder.Instance != null)
            PoolHolder.Instance.Release(fruit);
        else
            Object.Destroy(fruit.gameObject);
    }

    #endregion

    #region AnimatedFruit Spawning

    public static AnimatedFruit SpawnAnimatedFruit(Transform parent, Vector3 position = default, Quaternion rotation = default)
    {
        var fruitPrefab = ConfigHolder.Instance?.FruitConfig?.animatedFruitPrefab;
        if (fruitPrefab == null)
            return null;

        return PoolHolder.Instance != null
            ? PoolHolder.Instance.Get((MonoBehaviour)fruitPrefab, parent, position, rotation) as AnimatedFruit
            : Object.Instantiate(fruitPrefab, position, rotation, parent);
    }

    public static void DespawnAnimatedFruit(AnimatedFruit animFruit)
    {
        if (animFruit == null) return;
        animFruit.CancelMotion();
        if (PoolHolder.Instance != null)
            PoolHolder.Instance.Release(animFruit);
        else
            Object.Destroy(animFruit.gameObject);
    }

    #endregion
}
