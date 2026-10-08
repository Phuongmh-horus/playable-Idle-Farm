using UnityEngine;

public static class PlantSpawner
{
    public static void Prewarm(int countPerPrefab)
    {
        PoolHolder pool = PoolHolder.Instance;
        PlantConfig config = ConfigHolder.Instance != null ? ConfigHolder.Instance.PlantConfig : null;
        if (pool == null || config == null || config.plants == null) return;

        for (int i = 0; i < config.plants.Count; i++)
        {
            PlantConfigData plantConfig = config.plants[i];
            Plant prefab = plantConfig != null ? plantConfig.GetPrefab() : null;
            if (prefab == null) continue;

            int requiredCount = countPerPrefab;
            GardenSaveData garden = PlayableFarmState.PlayerData != null ? PlayableFarmState.PlayerData.GardenData : null;
            if (garden != null && garden.plots != null)
            {
                int savedCount = 0;
                for (int plotIndex = 0; plotIndex < garden.plots.Count; plotIndex++)
                {
                    PlotSaveData plot = garden.plots[plotIndex];
                    if (plot != null && plot.plantedPlantId == plantConfig.ID) savedCount++;
                }
                requiredCount = Mathf.Max(requiredCount, savedCount);
            }

            ScenePlayableSO scenario = PlayableScenarioRuntime.CurrentScenario;
            if (scenario != null && scenario.selectablePlantIds != null)
            {
                for (int selectableIndex = 0; selectableIndex < scenario.selectablePlantIds.Count; selectableIndex++)
                {
                    if (scenario.selectablePlantIds[selectableIndex] == plantConfig.ID)
                    {
                        requiredCount = Mathf.Max(1, requiredCount);
                        break;
                    }
                }
            }

            if (requiredCount > 0) pool.Prewarm(prefab, requiredCount);
        }
    }

    public static Plant SpawnPlant(int plantId, int level, Transform parent, Vector3 localPosition = default, int ringIndex = 0, int visualPlantId = -1)
    {
        Plant plantPrefab = ConfigHolder.Instance?.PlantConfig?.GetPlant(plantId)?.GetPrefab();
        if (plantPrefab == null) return null;

        Plant plant = PoolHolder.Instance != null
            ? PoolHolder.Instance.Get(plantPrefab, parent) as Plant
            : Object.Instantiate(plantPrefab, parent);
        if (plant != null)
        {
            plant.transform.localPosition = localPosition;
            plant.transform.rotation = Quaternion.Euler(90, 0, 0);
            plant.transform.localScale = plantPrefab.transform.localScale;
            plant.Init(plantId, level, ringIndex, visualPlantId);
        }
        return plant;
    }

    public static void DespawnPlant(Plant plant)
    {
        if (plant == null) return;

        plant.ResetForPool();
        if (PoolHolder.Instance != null)
            PoolHolder.Instance.Release(plant);
        else
            Object.Destroy(plant.gameObject);
    }

    public static Plant SpawnMegaPlant(int plantId, Transform parent, Vector3 localPosition, Plot owner)
    {
        Plant plantPrefab = ConfigHolder.Instance?.PlantConfig?.GetPlant(plantId)?.GetPrefab();
        if (plantPrefab == null) return null;

        Plant plant = PoolHolder.Instance != null
            ? PoolHolder.Instance.Get(plantPrefab, parent) as Plant
            : Object.Instantiate(plantPrefab, parent);
        if (plant != null)
        {
            plant.transform.localPosition = localPosition;
            plant.transform.rotation = Quaternion.Euler(90, 0, 0);
            plant.transform.localScale = plantPrefab.transform.localScale;
            plant.Init(plantId, owner.PlotData?.PlantLevel ?? 1, owner.RingIndex, owner.PlotData?.VisualPlantId ?? -1);
        }
        return plant;
    }
}
