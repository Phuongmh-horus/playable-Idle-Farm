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

        if (slotSystem == null) 
            slotSystem = GetComponentInChildren<FarmPlotSystem>();
        if (slotSystem != null)
        {
            slotSystem.Initialize();
        }

        if (irrigationSystem == null)
            irrigationSystem = GetComponentInChildren<FarmIrrigationSystem>();
        irrigationSystem?.Initialize();

        if (harvestSystem == null)
            harvestSystem = GetComponentInChildren<FarmHarvestSystem>();
        harvestSystem?.Initialize();

        ScenePlayableSO scenario = PlayableScenarioRuntime.CurrentScenario;
        FruitTransportStation station = TransportStation;
        if (station != null && scenario != null)
        {
            station.gameObject.SetActive(scenario.showConveyorAndSell);
        }
    }
}
