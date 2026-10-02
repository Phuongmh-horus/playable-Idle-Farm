using UnityEngine;

[CreateAssetMenu(fileName = "FarmPlotConfigSO", menuName = "ScriptableObjects/FarmPlotConfig")]
public sealed class FarmPlotConfigSO : ScriptableObject
{
    [Header("Plot Prefabs")]
    public Plot[] plotPrefabs;
    public Plot[] PlotPrefabs => plotPrefabs;

    [Header("Unlock Animation")]
    public AnimatedHoe animatedHoePrefab;

    [Header("State Transition")]
    [Min(0f)] public float fadeDuration = 0.25f;
}
