using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ScenePlayable", menuName = "PLA/Scene Playable")]
public class ScenePlayableSO : ScriptableObject
{
    [Header("Identity")]
    public PlayableScenarioId scenario = PlayableScenarioId.Scenario123;
    [Header("Tutorial")]
    public bool showTutorial = true;
    public PlayableTutorialFlow.Phase tutorialStartPhase = PlayableTutorialFlow.Phase.Sprinklers;
    public List<PlayableTutorialFlow.Phase> skippedTutorialPhases = new List<PlayableTutorialFlow.Phase>();
    [Header("Plant selection flow")]
    [Tooltip("Show PlantStore at startup and after every plot unlock. When disabled, each unlocked plot immediately grows its configured default plant.")]
    [FormerlySerializedAs("showFruitPicker")]
    public bool enablePlantPicker;
    public PlayablePlantPickerVariant plantPickerVariant = PlayablePlantPickerVariant.ConfiguredPlants;
    [Tooltip("Plant IDs shown by PlantStore, kept in this exact display order.")]
    public List<int> selectablePlantIds = new List<int>();

    public bool UsesPlantPicker => enablePlantPicker;
    [Header("Farm preset")]
    [Min(1)] public int initialPlotCount = 1;
    [Tooltip("Starter plant IDs applied left-to-right to the first plots for this scenario.")]
    public List<int> initialPlantIds = new List<int>();
    [Min(0)] public int startingCurrency;
    public bool showConveyorAndSell = true;
    public bool showUpgrade = true;
    [Header("Farm mechanics")]
    [Tooltip("Manual uses the draggable blade, Automatic uses rotating harvester arms, Hybrid enables both.")]
    public PlayableHarvestMode harvestMode = PlayableHarvestMode.ManualBlade;
    public bool enableIrrigation = true;
    [Header("Repeating endcard")]
    [Tooltip("Seconds of uninterrupted play before this scenario shows the endcard again.")]
    [Min(1f)] public float endcardDelaySeconds = 60f;
}

public enum PlayableScenarioId { Scenario123 = 123, Scenario124 = 124, Scenario125 = 125, Scenario126 = 126 }

public enum PlayablePlantPickerVariant
{
    ConfiguredPlants = 0,
    AllPlants = 1
}

public enum PlayableHarvestMode
{
    ManualBlade = 0,
    AutomaticArm = 1,
    Hybrid = 2
}
