using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using Alchemy;
using Alchemy.Inspector;
#endif
// Luna playable UI manager.
// [FIX] Removed ConvertLegacyTextToTmp() which called FontEngine.LoadFontFace(Font, int)

public class LunaUIManager : MonoBehaviour
{
    public static LunaUIManager Instance { get; private set; }
    public bool IsTutorialVisible => tutorialLayer != null && tutorialLayer.activeSelf;


    #region Inspector Fields
    [SerializeField] private Camera mainCamera;

    [Header("Scenario")]
    [SerializeField] private int scenario = 1;


    // [SerializeField] private int scenario = 107;
    // [SerializeField] private int numberScene = 1;

    #region  Game Level

    [Header(" === Game Level === ")]
    // [SerializeField, Min(1)] private int numberLevel = 1;
    [SerializeField] private List<ScenePlayableSO> scenePlayableSOs = new List<ScenePlayableSO>();

    public ScenePlayableSO CurrentScenePlayableSO => scenePlayableSOs[_currentScene];



    private int _currentScene = 0;

    // public bool IsPlayMoreGame => scenePlayableSOs.Count > 1 && numberLevel > 1 ? true : false;

    [SerializeField] private GameObject transitionPanel;
    private CanvasGroup transitionCanvasGroup;
    private Coroutine _transitionRoutine;

    // public LevelDataConfigSO FirstLevel => scenePlayableSOs.Count > 0 ? scenePlayableSOs[0].LevelDataConfig : null;
    #endregion 
    #region  General Config

    [Header("============ General Config ============")]
    [SerializeField] private CTAConfigPlayableSO ctaConfig;


    #region Tutorial Layer
    [Header("Tutorial Layer")]
    [SerializeField] private GameObject tutorialLayer;
    [SerializeField] private RectTransform tutorialHand;
    [SerializeField] private List<TMP_Text> tutorialTMPText = new List<TMP_Text>();
    #endregion

    #region  EndCard
    [Header("Endcard")]
    [SerializeField] private GameObject endcardRoot;

    [SerializeField] private GameObject losePanel;

    [SerializeField] private GameObject winPanel;

    [SerializeField] private GameObject endcardSingle;
    private CanvasGroup endcardCanvasGroup;

    [SerializeField] private List<CanvasGroup> specificCanvasGroupsToHideOnEndcard = new List<CanvasGroup>();

    private Coroutine hiddenCanvasGroupsRoutine;
    #endregion


    #region  CTA Buttons
    [Header("CTA Buttons")]
    [SerializeField] private List<Button> ctaButtons = new List<Button>();

    #endregion

    #region Behavior Settings
    [Header("Behavior")]
    [SerializeField] private bool pauseOnTutorial = true; // pause the game when tutorial is shown
    [SerializeField] private bool pauseOnEndcard = true;
    [SerializeField] private bool pauseTimeScaleOnEndcard = false;
    [SerializeField] private bool autoCTAOnEndcard = false;
    [SerializeField] public bool AutoCTAFirstTouch = false;

    #endregion

    #region  Behavior - Mid Game
    private int _currentClickCount = 0;
    private bool _clickThresholdTriggered = false;

    private int _timeOpenStore = 0 ;
    // [Header("Marketing Video")]
    // public bool IsMarketingVideoPlaying = false;
    #endregion


    


    #region  Auto CTA
    [Header("Behavior - Auto CTA")]
    private bool _triggeredAutoCTA = false;

    #endregion
    #region  AFK Tutorial
    [Header ("AFK Tutorial")]
    [SerializeField] private RectTransform afkHand;
    private int _currentHandPositionIndex = 0;
    private float _afkElapsed = 0f;

    private bool isTriggeredAFKTutorial = false;

    #endregion


    #region Praise SFX
    [Header("SFX Praise user")]

    [SerializeField] private Image praiseUserImage;

    public bool PlayPraiseSfx
    {
        get => CurrentScenePlayableSO.playPraiseSfx;
    }

    #endregion

    #region  Timer
    [Header("Timer")]


    [SerializeField] private AudioSource timerAudioSource;
    [SerializeField] private GameObject timerLayer;
    [SerializeField] private TMP_Text remainingTimeText;
    [SerializeField] private Animator remainingTimeTextAnimator;
    [SerializeField] private Animator warningFrameAnimator;

    private int remainTime = 9999;
    private bool isWarningTime = false;
    private float _elapsedTime = 0f;

    private bool isFirstWarningTriggered = false;
    #endregion
    #endregion


    [SerializeField] private UIGamePlayLayer uIGamePlayLayer;

    #region  Optional Gameplay 
    [Header("============ Optional Gameplay ============")]
    
    [Header("Auto PopupUI")]
    public PopupStore PopupStoreUI;

    public PlantStore plantStore;

    private List<int> usedPlantIds = new List<int>();

    public PlantConfig plantConfig;
    private bool _triggeredAutoPopupStoreUI = false;
    private int timePopupUI = 0;

    #endregion

    #endregion

/// <summary>
/// ///////////////////////////////////////////////////////////////////////////////
/// </summary>//
/// 
/// 
/// 

    #region Private Fields

    private bool isStartGame = false;
    private bool isResumeGame = false;
    private bool isEndGame = false;


    private Vector2 _handBaseAnchoredPos;
    private Coroutine _handRoutine;
    private Coroutine _buttonPulseRoutine;
    private Coroutine _textRoutine;
    private Vector3 _textBaseScale = Vector3.one;
    // private bool _endGameSent = false;
    private Coroutine _endcardRoutine;
    private bool _useCtaOnlyPulse;
    private bool _tutorialPauseApplied;

    #endregion

    #region  Unity Lifecycle Methods
    private void OnEnable()
    {
        GameEventBus.OnStartGame += StartGame;
        GameEventBus.OnEndGame += HandleGameEnd;
        GameEventBus.OnShowCTA += HandleShowCTA;
        GameEventBus.OnTimeUpdate += UpdateRemainingTime;
        GameEventBus.ActionOnPlantSelected += ActionOnPlantSelected;
        Luna.Unity.LifeCycle.OnPause += PauseGameplay;
        Luna.Unity.LifeCycle.OnResume += ResumeGameplay;
    }

    private void OnDisable()
    {
        GameEventBus.OnStartGame -= StartGame;
        GameEventBus.OnEndGame -= HandleGameEnd;
        GameEventBus.OnShowCTA -= HandleShowCTA;
        GameEventBus.OnTimeUpdate -= UpdateRemainingTime;
        GameEventBus.ActionOnPlantSelected -= ActionOnPlantSelected;
        Luna.Unity.LifeCycle.OnPause -= PauseGameplay;
        Luna.Unity.LifeCycle.OnResume -= ResumeGameplay;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tutorialTMPText.Count == 0 && tutorialLayer != null)
        {
            var texts = tutorialLayer.GetComponentsInChildren<TMP_Text>(true);
            tutorialTMPText.AddRange(texts);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        EnsureEventSystem();
        if (tutorialLayer != null)
        {
            ShowTutorial(true);
        }

        if (endcardRoot != null)
        {
            endcardRoot.SetActive(false);
        }

        SetCTAButtonsVisible(false);

        EnsureEndcardCanvasGroup();
        WireCTAButtons();

        if (tutorialHand != null)
        {
            tutorialHand.anchoredPosition = CurrentScenePlayableSO.handPosition;
            _handBaseAnchoredPos = tutorialHand.anchoredPosition;
        }

        if (tutorialTMPText.Count > 0)
        {
            foreach (var text in tutorialTMPText)
            {
                if (text != null)
                {
                    _textBaseScale = text.rectTransform.localScale;
                    break; // Use the first valid text's scale
                }
            }
        }

        EnsureTransitionPanel();
        LoadLevel();

    }

    private void Update()
    {
        // if (tutorialLayer != null && tutorialLayer.activeSelf)
        // {
        //     if (pauseOnTutorial && !_tutorialPauseApplied)
        //     {
        //         ForcePauseGame();
        //         _tutorialPauseApplied = true;
        //     }

        //     if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        //     {
        //         Debug.Log("Tutorial touched/clicked, starting game...");
        //         StartGameplayFromTutorial();
        //     }
        // }
        if(!isResumeGame) return ;
        _elapsedTime += Time.deltaTime; 
        if(CurrentScenePlayableSO.showTimer)
        {
            GameEventBus.OnTimeUpdate?.Invoke((float)CurrentScenePlayableSO.InitRemainingTime - _elapsedTime);

            if (_elapsedTime >= CurrentScenePlayableSO.InitRemainingTime)
            {
                // GameEventBus.OnLoseTrigger?.Invoke(ELoseReason.TimeOut);
                GameEventBus.OnEndGame?.Invoke(EndGameState.Lose);
            }
        }
        if(CurrentScenePlayableSO.isTutorialAutoWhenAFK)
        {
            _afkElapsed += Time.deltaTime; 
            if(_afkElapsed < CurrentScenePlayableSO.timeAFKToDisplayTutorialHand || isTriggeredAFKTutorial == true)
                return;
            isTriggeredAFKTutorial = true;
            List<int> indexs = GetValidIndexList();

            if(indexs.Count == 0) return;
            int randomIndex = UnityEngine.Random.Range(0, indexs.Count);

            int carrierIndex = indexs[randomIndex];
            afkHand.gameObject.SetActive(true);
            afkHand.anchoredPosition = CurrentScenePlayableSO.handMovePositions[carrierIndex];
            _currentHandPositionIndex = carrierIndex;

        }

        UpdateAutoCTA(_elapsedTime);
        UpdateAutoPopupStoreUI(_elapsedTime);


    }


    #endregion
    
    #region  Public API
    public void NotifySelectableClick(bool unloadBeforePlay = false)
    {   
        if(unloadBeforePlay) return;
        ResetAFKTimer();
        if (tutorialLayer != null && tutorialLayer.activeSelf)
        {
            if (pauseOnTutorial && !_tutorialPauseApplied)
            {
                ForcePauseGame();
                _tutorialPauseApplied = true;
            }

            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                Debug.Log("Tutorial touched/clicked, starting game...");
                StartGameplayFromTutorial();
            }
        }

        if (CurrentScenePlayableSO.enableClickCta == false) return;
        if (_clickThresholdTriggered && !CurrentScenePlayableSO.enableContinuousClickCta) return;
        _currentClickCount++;
        // Debug.Log($"Current click count: {_currentClickCount}");
        
        int threshold = 0;


        if (_timeOpenStore >= CurrentScenePlayableSO.clickThreshold.Count)
        {
            if (CurrentScenePlayableSO.repeatClickThreshold <= 0) return;
            threshold = CurrentScenePlayableSO.repeatClickThreshold;
        }
        else
        {
            threshold = Mathf.Max(1, CurrentScenePlayableSO.clickThreshold[_timeOpenStore]);
        }

        if (_currentClickCount < threshold) return;
        _currentClickCount = 0;
        _clickThresholdTriggered = true;

        if (CurrentScenePlayableSO.EndCardClickCta)
        {
            GameEventBus.OnEndGame?.Invoke(EndGameState.Pause);   
        }
        if (CurrentScenePlayableSO.autoOpenStore)
        {
            OpenStoreNow();
            _timeOpenStore++;
        }
    }
    
    



    #if UNITY_EDITOR
    [Button]
    #endif
    [ContextMenu("SetupStore") ]
    public void SetupStore()
    {
        List<PlantConfigData> plantConfigDatas = GetUnusedPlantConfigDatas();
        int numberOfPlantsToSelect = Mathf.Clamp(CurrentScenePlayableSO.numberOfPlantsToSelect, 1, plantConfigDatas.Count);
        List<PlantConfigData> selectedPlants = GetRandomPlants(plantConfigDatas, numberOfPlantsToSelect);

        // option 1
        // lấy theo thứ tự 
        // for (int i = 0; i < numberOfPlantsToSelect; i++)
        // {
        //     selectedPlants.Add(plantConfigDatas[i]);
        // }
        // // option 2
        // lấy ngẫu nhiên

        SetupStore(selectedPlants);
    }

    private List<PlantConfigData> GetRandomPlants(List<PlantConfigData> plantConfigDatas, int numberOfPlantsToSelect)
    {
        if (plantConfigDatas.Count <= numberOfPlantsToSelect)
        {
            return new List<PlantConfigData>(plantConfigDatas);
        }
        List<PlantConfigData> selectedPlants = new List<PlantConfigData>();
        HashSet<int> selectedIndices = new HashSet<int>();
        System.Random random = new System.Random();

        while (selectedPlants.Count < numberOfPlantsToSelect)
        {
            int randomIndex = random.Next(plantConfigDatas.Count);
            if (!selectedIndices.Contains(randomIndex))
            {
                selectedIndices.Add(randomIndex);
                selectedPlants.Add(plantConfigDatas[randomIndex]);
            }
        }
        return selectedPlants;
    }

    private void SetupStore(List<PlantConfigData> plantConfigs)
    {
        if (plantStore != null)
        {
            plantStore.SetupStore(plantConfigs);
        }
    }

    private List<PlantConfigData> GetUnusedPlantConfigDatas()
    {
        List<PlantConfigData> unusedPlantConfigs = new List<PlantConfigData>();
        if (plantConfig == null || plantConfig.plants == null) return unusedPlantConfigs;

        foreach (var plant in plantConfig.plants)
        {
            if (!usedPlantIds.Contains(plant.id))
            {
                unusedPlantConfigs.Add(plant);
            }
        }

        return unusedPlantConfigs;
        
    }

    #endregion

    #region  Private Methods


    private List<int> GetValidIndexList()
    {
        return new List<int>();
    }
    private void UpdateAutoCTA(float elapsedTime)
    {
        if (CurrentScenePlayableSO.autoCTAInGame && elapsedTime >= CurrentScenePlayableSO.timeInGameSecondsBeforeAutoCTA && !_triggeredAutoCTA)
        {
            OpenStoreNow();
            _triggeredAutoCTA = true;
        }
    }
    
    
    
    private void UpdateAutoPopupStoreUI(float elapsedTime)
    {
        if (!CurrentScenePlayableSO.AutoPopupStoreUI) return ;
        if (_triggeredAutoPopupStoreUI && !CurrentScenePlayableSO.enableContinuousPopup) return;

        if(elapsedTime >= CurrentScenePlayableSO.AutoPopupStoreUITime * (timePopupUI + 1))
        {
            ShowPopupStoreUI();
        }
    }


    private void ShowPopupStoreUI()
    {
        if(PopupStoreUI != null)
        {
            PopupStoreUI.OpenPopupStore();
            _triggeredAutoPopupStoreUI = true;
            timePopupUI++;
            PauseGameplay();
        }
    }



    #if UNITY_EDITOR
    [Button]
    #endif 
    [ContextMenu("StartGame")]
    public void StartGame()
    {
        Debug.Log("Game Started");
        isStartGame = true;
        isResumeGame = true;
    }
    private void EndGame()
    {
        isEndGame = true;
        isResumeGame = false;
        Luna.Unity.LifeCycle.GameEnded();
    }
    

    public void PauseGameplay() 
    {
        // Pause the game
        Debug.Log("Game Paused");
        if(isStartGame == false) return ;
        isResumeGame = false;
    }

    public void ResumeGameplay() 
    {

        Debug.Log("Game Resumed");
        if(isStartGame == false) return ;
        if(isEndGame) return ;
        isResumeGame = true;
        // Resume the game
    }
    private void UpdateRemainingTime(float remainingTime)
    {
        int current = Mathf.FloorToInt(remainingTime);
        if (current == remainTime) return;
        remainTime = current;
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        if (remainingTime <= 0)
        {
            remainingTimeText.SetText("00:00");
            return;
        }
        remainingTimeText.SetText("{0:00}:{1:00}", minutes, seconds);
        if(remainTime <= CurrentScenePlayableSO.WarningTime && isWarningTime == false)
        {
            isWarningTime = true;
            string trigger = "WarningTime";
            timerAudioSource.Play();
            remainingTimeTextAnimator.SetTrigger(trigger);
        }
        if(remainTime <= CurrentScenePlayableSO.SecondWarningTime && isWarningTime == true && isFirstWarningTriggered == false)
        {
            isFirstWarningTriggered = true;
            warningFrameAnimator.SetTrigger("WarningTime");
        }
    }

    private void TurnOffAnimationWarningTime()
    {
        remainingTimeTextAnimator.SetTrigger("TurnOffWarning");  
        warningFrameAnimator.SetTrigger("TurnOffWarning");
        timerAudioSource.Stop();
    }

    private void ActionOnPlantSelected(int plantId)
    {
        usedPlantIds.Add(plantId);
        plantStore.HideStore();
    }


    private Coroutine ShowHandWhenAfk;
    /// <summary>
    /// Shows or hides the tutorial layer. When showing, it starts the hand animation and text pulse. When hiding, it stops them.
    /// </summary>
    /// <param name="show"></param>
    private void ShowTutorial(bool show)
    {
        if (tutorialLayer != null) tutorialLayer.SetActive(show);
        if (show) _tutorialPauseApplied = false;

        if (show)
        {
            StartHandAnimation();
            StartTextPulse();
        }
        else
        {
            StopHandAnimation();
            StopTextPulse();
        }

    }
    // private IEnumerator ShowHandWhenAFK()
    // {
    //     tutorialHand.gameObject.SetActive(false);
    //     float timer = 0f;
    //     while (true)
    //     {
    //         timer += Time.deltaTime;
    //         if (timer >= CurrentScenePlayableSO.timeAFKToDisplayTutorialHand)
    //         {
    //             if (tutorialHand != null && tutorialHand.gameObject.activeSelf == false)
    //             {
    //                 tutorialHand.gameObject.SetActive(true);
    //             }
    //         }
    //         yield return null;
    //     }
    // }

 
    
    /// <summary>
    /// Call when first touch/click is detected on tutorial layer. It hides the tutorial, starts the game, and sends game start event.
    /// </summary>
    private void StartGameplayFromTutorial()
    {
        ShowTutorial(false);

        // if (GameplayManager.Instance != null)
        // {
        //     GameplayManager.IsGameStarted = true;
        //     GameplayManager.Instance.ContinueGame();
        // }

        // Luna.Unity.LifeCycle.GameStarted();
        if(AutoCTAFirstTouch)
        {
            Luna.Unity.Playable.InstallFullGame();
        }

        GameEventBus.OnStartGame?.Invoke();
        
    }
    private void HandleGameEnd(EndGameState endGameState)
    {
        if(isEndGame == true) return;

        // if (IsPlayMoreGame && endGameState == EndGameState.Win && _currentScene < numberLevel - 1)
        // {
        //     _currentScene++;
            
        //     // if(_currentLevelIndex >= levelGame.Count)
        //     // {
        //     //     _currentLevelIndex = 0;
        //     // }
        //     LevelDataConfigSO nextLevelData = CurrentScenePlayableSO.LevelDataConfig;
        //     TransitionAndLoadLevel(nextLevelData);
        //     return;
        // }

        SoundManager.Instance?.StopAllSfx();
        ShowEndcard(endGameState);
        EndGame();
        TurnOffAnimationWarningTime();
        // inputController.RemoveClickableLayerMask(LayerMask.NameToLayer("Carrier"));
        switch (endGameState)
        {
            case EndGameState.Win:
                GameEventBus.OnWinGame?.Invoke(); 
                break;
            case EndGameState.Lose:
                GameEventBus.OnLoseGame?.Invoke();
                break;
            case EndGameState.Pause:
                // CustomTimeScaleGroup.Instance?.ApplyTimeScale(0f);
                // CustomTimeScaleGroup.Instance.ClearTargets();
                GameEventBus.OnPauseGame?.Invoke();
                // Handle pause logic
                break;
        }

    }


    private void HandleShowCTA()
    {
        SetCTAButtonsVisible(true);
    }

    public void ShowCtaOnlyEndgame()
    {
        EnsureEventSystem();
        if (tutorialLayer != null) tutorialLayer.SetActive(false);
        if (endcardRoot != null) endcardRoot.SetActive(false);

        _useCtaOnlyPulse = true;
        SetCTAButtonsVisible(true);
    }

    private void ShowEndcard(EndGameState endGameState)
    {
        ShowTutorial(false);
        HideSpecificGameObject();
        EnsureEventSystem();
        if (_endcardRoutine != null) StopCoroutine(_endcardRoutine);
        _endcardRoutine = StartCoroutine(EndcardRoutine(endGameState));
    }

    private void HideSpecificGameObject()
    {
        hiddenCanvasGroupsRoutine = StartCoroutine(HiddenCanvasGroupsRoutine());
    }

    private IEnumerator HiddenCanvasGroupsRoutine()
    {
        if (specificCanvasGroupsToHideOnEndcard == null || specificCanvasGroupsToHideOnEndcard.Count == 0)
        {
            hiddenCanvasGroupsRoutine = null;
            yield break;
        }

        float delay = Mathf.Max(0f, ctaConfig.endcardDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        // Debug.Log("Delay: " + delay);
        for (int i = 0; i < specificCanvasGroupsToHideOnEndcard.Count; i++)
            StartCoroutine(FadeOutCanvasGroup(specificCanvasGroupsToHideOnEndcard[i], ctaConfig.endcardFadeDuration));
        
        yield return null;
        yield return new WaitForSeconds(ctaConfig.endcardFadeDuration);
        hiddenCanvasGroupsRoutine = null;
    }


    private IEnumerator EndcardRoutine(EndGameState endGameState)
    {
        float delay = Mathf.Max(0f, ctaConfig.endcardDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        EnsureEndcardCanvasGroup();
        if (endcardRoot != null) endcardRoot.SetActive(true);
        if (endcardSingle != null) endcardSingle.SetActive(true);


        if (endcardCanvasGroup != null)
        {
            endcardCanvasGroup.alpha = 0f;
            endcardCanvasGroup.interactable = false;
            endcardCanvasGroup.blocksRaycasts = true;
        }

        SetCTAButtonsVisible(true);
        switch (endGameState)
        {
            case EndGameState.Win:
            case EndGameState.Lose:
                foreach (Transform child in endcardRoot.transform)
                {
                    child.gameObject.SetActive(false);
                }
                break;
            case EndGameState.Pause:
                break;
        }


        if (winPanel != null) winPanel.SetActive(endGameState == EndGameState.Win);
        if (losePanel != null) losePanel.SetActive(endGameState == EndGameState.Lose);
        yield return StartCoroutine(FadeInCanvasGroup(endcardCanvasGroup, ctaConfig.endcardFadeDuration));

        if (endcardCanvasGroup != null)
        {
            endcardCanvasGroup.interactable = true;
            endcardCanvasGroup.blocksRaycasts = true;
        }

        if (autoCTAOnEndcard)
        {
            OpenStoreNow();
        }

        if (pauseOnEndcard)
        {
            ForcePauseGame();
            if (pauseTimeScaleOnEndcard) Time.timeScale = 0f;
        }

        _endcardRoutine = null;
    }

    private void EnsureEndcardCanvasGroup()
    {
        if (endcardCanvasGroup != null) return;
        if (endcardRoot == null) return;
        endcardCanvasGroup = endcardRoot.GetComponent<CanvasGroup>();
        if (endcardCanvasGroup == null)
        {
            endcardCanvasGroup = endcardRoot.AddComponent<CanvasGroup>();
        }
        endcardCanvasGroup.alpha = 0f;

        losePanel.SetActive(false);
        winPanel.SetActive(false);
    }
    private void EnsureTimerLayer()
    {
        if (timerLayer == null) return;
        _elapsedTime = 0f; // Reset elapsed time at the start of the game
        timerLayer.SetActive(CurrentScenePlayableSO.showTimer);
        UpdateRemainingTime((float)CurrentScenePlayableSO.InitRemainingTime);
    }

    private IEnumerator FadeInCanvasGroup(CanvasGroup canvasGroup, float? fadeDuration = null)
    {
        CanvasGroup targetCanvasGroup = canvasGroup ?? endcardCanvasGroup;
        if (targetCanvasGroup == null) yield break;

        float duration = Mathf.Max(0.05f, fadeDuration ?? ctaConfig.endcardFadeDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            targetCanvasGroup.alpha = k;
            yield return null;
        }

        targetCanvasGroup.alpha = 1f;
    }


    private IEnumerator FadeOutCanvasGroup(CanvasGroup canvasGroup, float? fadeDuration = null)
    {
        CanvasGroup targetCanvasGroup = canvasGroup ?? endcardCanvasGroup;
        if (targetCanvasGroup == null) yield break;

        float duration = Mathf.Max(0.05f, fadeDuration ?? ctaConfig.endcardFadeDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            targetCanvasGroup.alpha = 1f - k;
            yield return null;
        }

        targetCanvasGroup.alpha = 0f;
    }

    private void SetCTAButtonsVisible(bool visible)
    {
        if (ctaButtons == null) return;
        for (int i = 0; i < ctaButtons.Count; i++)
        {
            if (ctaButtons[i] != null)
            {
                ctaButtons[i].gameObject.SetActive(visible);
                ctaButtons[i].interactable = visible;

                var graphic = ctaButtons[i].targetGraphic;
                if (graphic != null) graphic.raycastTarget = true;
            }
        }

        if (visible)
        {
            WireCTAButtons();
            if (_useCtaOnlyPulse)
            {
                for (int i = 0; i < ctaButtons.Count; i++)
                {
                    var btn = ctaButtons[i];
                    if (btn == null) continue;
                    btn.transform.localScale = new Vector3(ctaConfig.ctaOnlyStartScale, ctaConfig.ctaOnlyStartScale, ctaConfig.ctaOnlyStartScale);
                }
            }
            StartButtonPulse();
        }
        else
        {
            StopButtonPulse();
        }
    }

    public void OnCTAClicked()
    {
        GameEventBus.OnCTAClicked?.Invoke();
        OpenStoreNow();
    }


    /// <summary>
    /// call when endCard or stopTutorial
    /// </summary>
    private void ForcePauseGame()
    {
        // if (GameplayManager.Instance == null) return;
        // if (GameplayManager.IsGameStarted)
        // {
        //     GameplayManager.IsGameStarted = false;
        // }
        // GameplayManager.Instance.PauseGame();
    }


    public void OpenStoreNow()
    {
        Luna.Unity.Playable.InstallFullGame();
    }

    private void StartHandAnimation()
    {
        if (tutorialHand == null) return;
        if (_handRoutine != null) StopCoroutine(_handRoutine);
        _handRoutine = StartCoroutine(HandMoveRoutine());
    }

    private void StopHandAnimation()
    {
        if (_handRoutine != null)
        {
            StopCoroutine(_handRoutine);
            _handRoutine = null;
        }

        if (tutorialHand != null)
        {
            tutorialHand.anchoredPosition = _handBaseAnchoredPos;
        }
    }

    private void StartTextPulse()
    {
        if (tutorialTMPText == null) return;
        if (_textRoutine != null) StopCoroutine(_textRoutine);
        _textRoutine = StartCoroutine(TextPulseRoutine());
    }

    private void StopTextPulse()
    {
        if (_textRoutine != null)
        {
            StopCoroutine(_textRoutine);
            _textRoutine = null;
        }

        if (tutorialTMPText != null)
        {
            foreach (var text in tutorialTMPText)
            {
                if (text != null)
                {
                    text.rectTransform.localScale = _textBaseScale;
                }
            }
        }
    }

    private void StartButtonPulse()
    {
        if (_buttonPulseRoutine != null) StopCoroutine(_buttonPulseRoutine);
        _buttonPulseRoutine = StartCoroutine(ButtonImpactThenPulseRoutine());
    }

    private void StopButtonPulse()
    {
        if (_buttonPulseRoutine != null)
        {
            StopCoroutine(_buttonPulseRoutine);
            _buttonPulseRoutine = null;
        }

        if (ctaButtons == null) return;
        for (int i = 0; i < ctaButtons.Count; i++)
        {
            if (ctaButtons[i] != null)
            {
                ctaButtons[i].transform.localScale = Vector3.one;
            }
        }
    }

    private IEnumerator ButtonImpactThenPulseRoutine()
    {
        float impactDuration = Mathf.Max(0.05f, ctaConfig.ctaImpactDuration);
        float impactT = 0f;
        while (impactT < impactDuration)
        {
            impactT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(impactT / impactDuration);
            float scale = Mathf.Lerp(ctaConfig.ctaImpactStartScale, 1f, k);
            float angle = Mathf.Lerp(ctaConfig.ctaImpactStartAngle, 0f, k);

            for (int i = 0; i < ctaButtons.Count; i++)
            {
                var btn = ctaButtons[i];
                if (btn == null || !btn.gameObject.activeInHierarchy) continue;
                var rt = btn.transform as RectTransform;
                if (rt != null)
                {
                    rt.localScale = new Vector3(scale, scale, scale);
                    rt.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
                else
                {
                    btn.transform.localScale = new Vector3(scale, scale, scale);
                    btn.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
            }

            yield return null;
        }

        for (int i = 0; i < ctaButtons.Count; i++)
        {
            var btn = ctaButtons[i];
            if (btn == null) continue;
            btn.transform.localRotation = Quaternion.identity;
        }

        float duration = Mathf.Max(0.05f, ctaConfig.buttonPulseDuration);
        while (ctaButtons != null && ctaButtons.Count > 0)
        {
            float t = Mathf.PingPong(Time.unscaledTime / duration, 1f);
            float scale;
            if (_useCtaOnlyPulse)
            {
                scale = Mathf.Lerp(ctaConfig.ctaOnlyStartScale, ctaConfig.ctaOnlyPulseScale, t);
            }
            else
            {
                float targetScale = ctaConfig.buttonPulseScale;
                scale = Mathf.Lerp(1f, targetScale, t);
            }
            for (int i = 0; i < ctaButtons.Count; i++)
            {
                var btn = ctaButtons[i];
                if (btn == null || !btn.gameObject.activeInHierarchy) continue;
                btn.transform.localScale = new Vector3(scale, scale, scale);
            }
            yield return null;
        }

        _buttonPulseRoutine = null;
    }

    private void WireCTAButtons()
    {
        if (ctaButtons == null) return;
        for (int i = 0; i < ctaButtons.Count; i++)
        {
            var btn = ctaButtons[i];
            if (btn == null) continue;
            btn.onClick.RemoveListener(OnCTAClicked);
            btn.onClick.AddListener(OnCTAClicked);
        }
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private IEnumerator TextPulseRoutine()
    {
        if (tutorialTMPText.Count == 0) yield break;

        var from = _textBaseScale;
        var to = _textBaseScale * ctaConfig.textScaleUp;
        float duration = Mathf.Max(0.05f, ctaConfig.textScaleDuration);

        while (tutorialLayer != null && tutorialLayer.activeSelf)
        {
            float t = Mathf.PingPong(Time.unscaledTime / duration, 1f);
            foreach (var text in tutorialTMPText)
            {
                if (text != null)
                {
                    text.rectTransform.localScale = Vector3.Lerp(from, to, t);
                }
            }
            yield return null;
        }

        foreach (var text in tutorialTMPText)
        {
            if (text != null)
            {
                text.rectTransform.localScale = _textBaseScale;
            }
        }
        _textRoutine = null;
    }




    // [SerializeField] private Transform firstTarget;
    // [SerializeField] private Transform secondTarget;
    // private float timeMoving = 0.75f;
    // private float timeWaiting = 2f;
    private IEnumerator HandMoveRoutine()
    {
        // tutorialHand.transform.position = firstTarget.position;
        // while (true)
        // {
        //     tutorialHand.transform.position = firstTarget.position;
        //     float elapsedTime = 0f;
        //     while (elapsedTime < timeMoving)
        //     {
        //         tutorialHand.transform.position = Vector3.Lerp(firstTarget.position, secondTarget.position, elapsedTime / timeMoving);
        //         elapsedTime += Time.deltaTime;
        //         yield return null;
        //     }
        //     tutorialHand.transform.position = secondTarget.position;
        //     yield return new WaitForSeconds(timeWaiting);

        //     elapsedTime = 0f;
        //     while (elapsedTime < timeMoving / 2)
        //     {
        //         tutorialHand.transform.position = Vector3.Lerp(secondTarget.position, firstTarget.position, elapsedTime / (timeMoving / 2));
        //         elapsedTime += Time.deltaTime;
        //         yield return null;
        //     }
        //     tutorialHand.transform.position = firstTarget.position;
        //     yield return null;
        // }

        if (tutorialHand == null) yield break;

        //  Vector2 originalPos = tutorialHand.anchoredPosition;
        Vector3 originalScale = tutorialHand.localScale;
        Vector3 targetScale = originalScale * ctaConfig.handScaleUp;

        while (true)
        {
            float elapsedTime = 0f;
            while (elapsedTime < ctaConfig.handMoveDuration)
            {
                float t = elapsedTime / ctaConfig.handMoveDuration;
                tutorialHand.localScale = Vector3.Lerp(originalScale, targetScale, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            elapsedTime = 0f;
            while (elapsedTime < ctaConfig.handMoveDuration / 2)
            {
                float t = elapsedTime / (ctaConfig.handMoveDuration / 2);
                tutorialHand.localScale = Vector3.Lerp(targetScale, originalScale, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        // yield return null;

    }




    private void EnsurePraiseUser()
    {
        if (praiseUserImage == null) return;
        praiseUserImage.gameObject.SetActive(false);
    }


    private Coroutine _praiseVfxRoutine;

    public void PlayPraiseVfx(string praiseText = null)
    {
        if (praiseUserImage != null)
        {

            praiseUserImage.gameObject.SetActive(true);
            if(_praiseVfxRoutine != null) StopCoroutine(_praiseVfxRoutine);
            _praiseVfxRoutine = StartCoroutine(PlayPraiseVfxRoutine(praiseText));
        }
    }

    private IEnumerator PlayPraiseVfxRoutine(string praiseText)
    {
        string defaultPraiseText = "good";
        praiseText = praiseText?.ToLower();
        praiseText = string.IsNullOrEmpty(praiseText) ? defaultPraiseText : praiseText;
        praiseUserImage.gameObject.SetActive(true);
        praiseUserImage.sprite = UIManager.Instance.GetSpriteByType(praiseText);
        praiseUserImage.SetNativeSize();
        // Impact
        float impactDuration = Mathf.Max(0.05f, ctaConfig.praiseVfxImpactDuration);
        float impactT = 0f;
        while (impactT < impactDuration)
        {
            impactT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(impactT / impactDuration);
            float scale = Mathf.Lerp(ctaConfig.ctaImpactStartScale, 1f, k);
            float angle = Mathf.Lerp(ctaConfig.ctaImpactStartAngle, 0f, k);

            praiseUserImage.transform.localScale = Vector3.one * scale;
            praiseUserImage.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        praiseUserImage.transform.localRotation = Quaternion.identity;

        // Pulse
        float duration = Mathf.Max(0.05f, ctaConfig.praiseVfxDuration);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float t = Mathf.PingPong(Time.unscaledTime / duration, 1f);

            float scale;
            if (_useCtaOnlyPulse)
            {
                scale = Mathf.Lerp(ctaConfig.ctaOnlyStartScale, ctaConfig.ctaOnlyPulseScale, t);
            }
            else
            {
                scale = Mathf.Lerp(1f, ctaConfig.buttonPulseScale, t);
            }

            if (praiseUserImage == null || !praiseUserImage.gameObject.activeInHierarchy)
                yield break;

            praiseUserImage.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        // Fade + Shrink
        Color startColor = praiseUserImage.color;
        Vector3 startScale = praiseUserImage.transform.localScale;
        Vector3 endScale = Vector3.one * ctaConfig.praiseFadeEndScale;

        float fadeTime = 0f;
        while (fadeTime < ctaConfig.praiseFadeDuration)
        {
            fadeTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fadeTime / ctaConfig.praiseFadeDuration);

            // Alpha giảm dần
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            praiseUserImage.color = c;

            // Scale nhỏ dần
            praiseUserImage.transform.localScale =
                Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        // Reset
        Color finalColor = startColor;
        finalColor.a = 1f;
        praiseUserImage.color = finalColor;

        praiseUserImage.transform.localScale = Vector3.one;
        praiseUserImage.gameObject.SetActive(false);
        _praiseVfxRoutine = null;
    
    }

    private void EnsureAFKHand()
    {
        afkHand.gameObject.SetActive(false);
    }

    private void ResetAFKTimer()
    {
        _afkElapsed = 0f;
        afkHand.gameObject.SetActive(false);
        isTriggeredAFKTutorial = false;
    }

    private void CheckInvalidCarriersAFKTutorialHand()
    {
        List<int> indexs = new List<int>();
        if(indexs.Contains(_currentHandPositionIndex) == false)
        {
            ResetAFKTimer();
        }
    }


    private void EnsureScenario()
    {

        switch (scenario)
        {
            case 107:
                break;
            default:
                break;
        }
        EnsureTimerLayer();
        EnsurePraiseUser();
        EnsureAFKHand();
        EnsureConveyorUI();
        EnsurePopupUI();
        EnsureGameplay();
    }


    private void EnsureConveyorUI()
    {
        uIGamePlayLayer.EnsureUI();
    }
    private void EnsurePopupUI()
    {
        if(PopupStoreUI != null)
        {
            PopupStoreUI.ClosePopupStore();
        }
    }

    private void EnsureGameplay()
    {
        if (CurrentScenePlayableSO.gameMode == GameMode.Auto)
        {
            
        }
        if (CurrentScenePlayableSO.enableFarmer)
        {
            
        }

        if (CurrentScenePlayableSO.prePlant.PrePlantMode != PrePlantMode.None)
        {
            
        }

        if (CurrentScenePlayableSO.SelectPlantStore)
        {
            SetupStore();
        }
        else plantStore.HideStore();


    }

    // private void TransitionAndLoadLevel(LevelDataConfigSO levelData)
    // {
    //     if (transitionPanel == null || transitionCanvasGroup == null)
    //     {
    //         // LoadLevel(FirstLevel);
    //         return;
    //     }

    //     transitionPanel.SetActive(true);
    //     _transitionRoutine = StartCoroutine(TransitionRoutine(levelData));
    // }

    private void LoadLevel(LevelDataConfigSO levelDataConfig = null)
    {
        // if (LevelManager.Instance != null)
        // {
        //     LoadMapAndRailAndCamera(levelDataConfig);
        //     LevelManager.Instance.LoadLevel(levelDataConfig.levelData);
        //     SplineSystem.Instance?.LoadLevel(levelDataConfig.levelData);
            EnsureScenario();
        // }



    }



    private void EnsureTransitionPanel()
    {
        if (transitionPanel == null) return;
        transitionCanvasGroup = transitionPanel.GetComponent<CanvasGroup>();
        transitionCanvasGroup.alpha = 0f;
        transitionPanel.SetActive(false);
    }
    // private void FadeTransition()
    // {
    //     if (transitionPanel == null || transitionCanvasGroup == null) return;
    //     transitionPanel.SetActive(true);
    //     StartCoroutine(TransitionRoutine(level));
    // }

    // private IEnumerator TransitionRoutine(LevelDataConfigSO levelData)
    // {
    //     yield return StartCoroutine(FadeInCanvasGroup(transitionCanvasGroup, ctaConfig.transitionFadeDuration));

    //     LoadLevel(levelData);

    //     float t = 0f;
    //     while (t < ctaConfig.transitionDelay)
    //     {
    //         t += Time.unscaledDeltaTime;
    //         yield return null;
    //     }

    //     // Fade out
    //     yield return StartCoroutine(FadeOutCanvasGroup(transitionCanvasGroup, ctaConfig.transitionFadeDuration));

    //     transitionPanel.SetActive(false);
    //     _transitionRoutine = null;
    // }


    #endregion

}