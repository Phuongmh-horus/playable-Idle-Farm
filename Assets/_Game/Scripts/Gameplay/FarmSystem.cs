using UnityEngine;

public sealed class FarmSystem : MonoSingleton<FarmSystem>
{
    [SerializeField] private FarmPlotSystem slotSystem;
    [SerializeField] private FarmIrrigationSystem irrigationSystem;
    [SerializeField] private FarmHarvestSystem harvestSystem;
    [SerializeField] private FruitTransportStation transportStation;

    public FarmPlotSystem PlotSystem => slotSystem;
    public FarmIrrigationSystem IrrigationSystem => irrigationSystem != null ? irrigationSystem : (irrigationSystem = GetComponentInChildren<FarmIrrigationSystem>());
    public FarmHarvestSystem HarvestSystem => harvestSystem != null ? harvestSystem : (harvestSystem = GetComponentInChildren<FarmHarvestSystem>());
    public FruitTransportStation TransportStation => transportStation != null ? transportStation : (transportStation = GetComponentInChildren<FruitTransportStation>());

    protected override void Awake()
    {
        base.Awake();

        PrewarmRuntimePools();

        if (slotSystem == null) 
            slotSystem = GetComponentInChildren<FarmPlotSystem>();
        if (slotSystem != null)
        {
            slotSystem.Initialize();
        }

        if (irrigationSystem == null)
            irrigationSystem = GetComponentInChildren<FarmIrrigationSystem>();

        ScenePlayableSO scenario = PlayableScenarioRuntime.CurrentScenario;
        bool irrigationEnabled = scenario == null || scenario.enableIrrigation;
        if (irrigationSystem != null)
        {
            irrigationSystem.gameObject.SetActive(irrigationEnabled);
            if (irrigationEnabled)
            {
                irrigationSystem.Initialize();
            }
        }

        if (harvestSystem == null)
            harvestSystem = GetComponentInChildren<FarmHarvestSystem>();
        harvestSystem?.Initialize();

        FruitTransportStation station = TransportStation;
        if (station != null && scenario != null)
        {
            station.gameObject.SetActive(scenario.showConveyorAndSell);
        }
    }

    private static void PrewarmRuntimePools()
    {
        const int plantCountPerPrefab = 0;
        const int fallbackFruitCapacity = 10;

        int fruitCapacity = fallbackFruitCapacity;
        int maxFruitCapacity = fallbackFruitCapacity;
        FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        if (farmConfig != null && farmConfig.transport != null)
        {
            fruitCapacity = Mathf.Max(1, farmConfig.transport.GetCapacity(1));
            maxFruitCapacity = Mathf.Max(fruitCapacity, farmConfig.transport.GetCapacity(farmConfig.transport.MaxLevel));
        }

        PlantSpawner.Prewarm(plantCountPerPrefab);
        FruitSpawner.Prewarm(fruitCapacity, fruitCapacity, maxFruitCapacity, maxFruitCapacity);
    }
}
