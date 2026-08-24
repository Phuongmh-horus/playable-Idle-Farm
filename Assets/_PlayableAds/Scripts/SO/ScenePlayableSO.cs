using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScenePlayable", menuName = "PLA/ScenePlayable")]
public class ScenePlayableSO : ScriptableObject
{
    public LevelDataConfigSO LevelDataConfig;

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


    [Header ("============ Optional Gameplay ============")]
    [Header("Remain Cat CTA")]
    public bool autoCtaRemainCat = false;

    [Min(0)]
    public int remainCatCount = 2;


    [Header("Conveyor")]
    [SerializeField] public bool showConveyorWin = false;
    public bool IsShowConveyorWin => showConveyorWin;

    [SerializeField] public bool showConveyorCapacity = false;
    public bool IsShowConveyorCapacity => showConveyorCapacity;

    public bool showConveyorCapacityText = false;
    public bool IsShowConveyorCapacityText => showConveyorCapacityText;

    [Header("Unload Carrier trước khi chơi")]
    [SerializeField] public bool unloadBeforePlay = false;
    [SerializeField] public List<int> orderCarries = new List<int>();

}
