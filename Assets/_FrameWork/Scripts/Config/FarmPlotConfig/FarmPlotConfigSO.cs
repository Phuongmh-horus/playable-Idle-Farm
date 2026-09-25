using UnityEngine;

[CreateAssetMenu(fileName = "FarmPlotConfigSO", menuName = "ScriptableObjects/FarmPlotConfig")]
public sealed class FarmPlotConfigSO : ScriptableObject
{
    public Plot[] plotPrefabs;
    public Plot[] PlotPrefabs => plotPrefabs;
}
