using LitMotion;
using UnityEngine;

[CreateAssetMenu(fileName = "FarmPlotConfigSO", menuName = "ScriptableObjects/FarmPlotConfig")]
public sealed class FarmPlotConfigSO : ScriptableObject
{
    public Plot[] plotPrefabs;
    public MegaPlot megaPlotPrefab;

    [Header("Transition Visual Settings")]
    public float fadeDuration = 0.25f;
    public Ease fadeEase = Ease.InOutQuad;
}
