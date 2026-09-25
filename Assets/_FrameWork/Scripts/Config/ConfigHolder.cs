using UnityEngine;

/// <summary>
/// Explicit references used by the playable farm only. Assign these on the
/// ConfigHolder prefab; no runtime asset scans or live-service configuration.
/// </summary>
// Scenario state is prepared at -1000; farm systems may safely read these
// inspector references from Awake after this lightweight bootstrap at -900.
[DefaultExecutionOrder(-900)]
public class ConfigHolder : MonoSingleton<ConfigHolder>
{
    [Header("Farm")]
    public FarmPlotConfigSO FarmPlotConfigSO;
    public CircularFarmPlotLayoutConfigSO CircularFarmPlotLayoutConfigSO;
    public FarmConfig FarmConfig;
    public PlantConfig PlantConfig;
    public FruitConfig FruitConfig;
    public ValueUpgradePanelConfig ValueUpgradePanelConfig;
}
