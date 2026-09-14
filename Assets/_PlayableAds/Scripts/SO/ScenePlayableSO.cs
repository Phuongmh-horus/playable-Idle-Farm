using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScenePlayable", menuName = "PLA/ScenePlayable")]
public class ScenePlayableSO : ScriptableObject
{
    // public LevelDataConfigSO LevelDataConfig;

    [Header ("============ Optional Gameplay ============")]
    
    [Header("GameMode")]
    public GameMode gameMode = GameMode.Normal;

    public bool enableFarmer = true;
    [Header("Auto PopupUI")]
    public bool AutoPopupStoreUI = false;

    [Range(0f, 180f)] public float AutoPopupStoreUITime = 90f;
    public bool enableContinuousPopup = true;

    [Header("Select Plant Store")]
    public bool SelectPlantStore = false;

    [Min(2)] public int numberOfPlantsToSelect = 2; 

    [Header("PrePlant ")]
    public PrePlant prePlant = new PrePlant();

    [Header("Support Can")]

    [Min(0)]public int numberOfBottle = 0;


    [Header("============= General =============")]
    [Header("Hand Tutorial")]
    public Vector2 handPosition ;

    [Header("Behavior - Mid Game")]
    public bool enableClickCta = false;
    public bool autoOpenStore = true;
    public bool EndCardClickCta = false;
    public bool enableContinuousClickCta = true;
    [SerializeField, Min(1)] public List<int> clickThreshold = new List<int> ();

    [Min(0)]public int repeatClickThreshold = 1;



    [Header("Behavior - Auto CTA")]
    public bool autoCTAInGame = false;

    [SerializeField, Range(0, GameAds.ConstGame.TIME_LIMIT)]
    public int timeInGameSecondsBeforeAutoCTA = 30;

    [Header ("AFK Tutorial")]
    public bool isTutorialAutoWhenAFK = false;
    public float timeAFKToDisplayTutorialHand = 5f;
    public List<Vector2> handMovePositions = new List<Vector2>();

    [Header("SFX Praise user")]
    public bool playPraiseSfx = true;

    [Header("Timer")]
    public bool showTimer = false;
    public int InitRemainingTime = 60; 
    public int WarningTime = 10;
    public int SecondWarningTime = 5;




}



public enum GameMode
{
    Normal,
    Auto,
}


[System.Serializable]
public class PrePlant
{
    public PrePlantMode PrePlantMode = PrePlantMode.None;
    [Min(1)]public int numberOfPrePlant = 1;
    public List<int> prePlantIndex = new List<int>();

}
public enum PrePlantMode
{
    None,
    Progress,
    Index,
}
