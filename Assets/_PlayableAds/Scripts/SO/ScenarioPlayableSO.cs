// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// [CreateAssetMenu(fileName = "ScenarioPlayable", menuName = "PLA/ScenarioPlayable")]
// public class ScenarioPlayableSO : ScriptableObject
// {
//     public int ScenarioId;

//     public LevelDataConfigSO LevelDataConfig;

//     [Header("Game Level")]
//     public bool playMoreGame = false;
//     public int numberLevel = 1;


//     [Header ("AFK Tutorial")]
//     public bool isTutorialAutoWhenAFK = false;
//     public List<Vector2> handMovePositions = new List<Vector2>();


//     [Header("Behavior - Click EndCard - Suýt thắng")]
//     public bool enableClickCta = false;

//     [SerializeField, Min(1)] private int clickThreshold = 10;
//     public bool enableContinuousClickCta = false;


//     [Header("Behavior - Auto CTA")]
//     public bool autoCTAInGame = false;

//     [SerializeField, Range(0, GameAds.ConstGame.TIME_LIMIT)]
//     public int timeInGameSecondsBeforeAutoCTA = 30;

//     [Header("SFX Praise user")]
//     public bool playPraiseSfx = true;

//     [Header("Timer")]
//     public bool showTimer = false;
//     public int InitRemainingTime = 60; 
//     public int WarningTime = 10;
//     public int SecondWarningTime = 5;


//     [Header ("============ Optional Gameplay ============")]
//     [Header("Remain Cat CTA")]
//     public bool autoCtaRemainCat = false;

//     [Min(0)]
//     public int remainCatCount = 2;


//     [Header("Conveyor")]
//     [SerializeField] private bool showConveyorWin = false;
//     public bool IsShowConveyorWin => showConveyorWin;

//     [SerializeField] private bool showConveyorCapacity = false;
//     public bool IsShowConveyorCapacity => showConveyorCapacity;


//     [Header("Unload Carrier trước khi chơi")]
//     [SerializeField] private bool unloadBeforePlay = false;
//     [SerializeField] private float waitTime = 1.5f;
//     [SerializeField] private List<int> orderCarries = new List<int>();

// }
