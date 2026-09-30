using UnityEngine;

public class MegaPlot : Plot
{
    private int _sectorIndex;
    private int _plantId = -1;
    public int SectorIndex => _sectorIndex;

    public void BindSector(FarmPlotSystem owner, int sectorIndex, int plantId)
    {
        _slotSystem = owner;
        _sectorIndex = sectorIndex;
        _plantId = plantId;

        UpdateVisualByState(FarmPlotState.Occupied);
        SpawnMegaPlantInstance();
    }

    private void SpawnMegaPlantInstance()
    {
        ClearSpawnedPlant();
        if (_plantId < 0) return;
        Transform parent = plantRoot != null ? plantRoot.transform : transform;
        _spawnedPlant = PlantSpawner.SpawnMegaPlant(_plantId, parent, Vector3.zero, this);
    }

    protected override void HandlePlantRootTapped(StaticSelectable selectable)
    {
        if (_spawnedPlant == null) return;
        Transform popupTarget = plantRoot ? plantRoot.transform : transform;
        if (WorldLayerManager.Instance)
        {
            WorldLayerManager.Instance.ShowPopupUpgradePlantLayer(this, popupTarget);
        }
    }
}
