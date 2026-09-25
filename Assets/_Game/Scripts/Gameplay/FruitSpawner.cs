using UnityEngine;

public static class FruitSpawner
{
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
        Fruit fruit = Object.Instantiate(prefab, parent);
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
        Object.Destroy(fruit.gameObject);
    }

    #endregion

    #region AnimatedFruit Spawning

    public static AnimatedFruit SpawnAnimatedFruit(Transform parent, Vector3 position = default, Quaternion rotation = default)
    {
        var fruitPrefab = ConfigHolder.Instance?.FruitConfig?.animatedFruitPrefab;
        if (fruitPrefab == null)
            return null;

        return Object.Instantiate(fruitPrefab, position, rotation, parent);
    }

    public static void DespawnAnimatedFruit(AnimatedFruit animFruit)
    {
        if (animFruit == null) return;
        animFruit.CancelMotion();
        Object.Destroy(animFruit.gameObject);
    }

    #endregion
}
