using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScenePlayable", menuName = "PLA/Scene Playable")]
public class ScenePlayableSO : ScriptableObject
{
    [Header("Identity")]
    public PlayableScenarioId scenario = PlayableScenarioId.Scenario123;
    [Header("Tutorial")]
    public bool showTutorial = true;
    public PlayableTutorialFlow.Phase tutorialStartPhase = PlayableTutorialFlow.Phase.Sprinklers;
    public List<PlayableTutorialFlow.Phase> skippedTutorialPhases = new List<PlayableTutorialFlow.Phase>();
    [Header("Fruit picker - scenario 125")]
    [Tooltip("Shown only for Scenario 125. IDs stay in this exact display order.")]
    public bool showFruitPicker;
    public List<int> selectablePlantIds = new List<int>();
    [Header("Farm preset")]
    [Min(1)] public int initialPlotCount = 1;
    [Tooltip("Starter plant IDs applied left-to-right to the first plots for this scenario.")]
    public List<int> initialPlantIds = new List<int>();
    [Min(0)] public int startingCurrency;
    public bool showConveyorAndSell = true;
    public bool showUpgrade = true;
    [Header("Repeating endcard")]
    [Tooltip("Seconds of uninterrupted play before this scenario shows the endcard again.")]
    [Min(1f)] public float endcardDelaySeconds = 60f;
}

public enum PlayableScenarioId { Scenario123 = 123, Scenario124 = 124, Scenario125 = 125, Scenario126 = 126 }
