using UnityEngine;

public static class PlantSpawner
{
    public static Plant SpawnPlant(int plantId, int level, Transform parent, Vector3 localPosition = default, int ringIndex = 0)
    {
        Plant plantPrefab = ConfigHolder.Instance?.PlantConfig?.GetPlant(plantId)?.GetPrefab();
        if (plantPrefab == null) return null;

        Plant plant = Object.Instantiate(plantPrefab, parent);
        if (plant != null)
        {
            plant.transform.localPosition = localPosition;
            plant.transform.rotation = Quaternion.Euler(90, 0, 0);
            plant.Init(plantId, level, ringIndex);
        }
        return plant;
    }

    public static void DespawnPlant(Plant plant)
    {
        if (plant == null) return;

        Object.Destroy(plant.gameObject);
    }

    public static Plant SpawnMegaPlant(int plantId, Transform parent, Vector3 localPosition, Plot owner)
    {
        Plant plantPrefab = ConfigHolder.Instance?.PlantConfig?.GetPlant(plantId)?.GetPrefab();
        if (plantPrefab == null) return null;

        Plant plant = Object.Instantiate(plantPrefab, parent);
        if (plant != null)
        {
            plant.transform.localPosition = localPosition;
            plant.transform.rotation = Quaternion.Euler(90, 0, 0);
            plant.Init(plantId, owner.PlotData?.PlantLevel ?? 1, owner.RingIndex);
        }
        return plant;
    }
}
