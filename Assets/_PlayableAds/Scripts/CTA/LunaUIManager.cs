using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal playable UI flow: persistent store CTAs plus a repeating endcard.
/// Gameplay never pauses or resets when the store is opened or the endcard closes.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class LunaUIManager : MonoBehaviour
{
    public static LunaUIManager Instance { get; private set; }

    [Header("Repeating endcard timing")]
    [Tooltip("Thời gian xuất hiện endcard lần đầu (giây).")]
    [SerializeField] private float initialEndcardDelay = 45f;
    [Tooltip("Thời gian xuất hiện lại endcard sau khi bấm tiếp tục (giây).")]
    [SerializeField] private float recurringEndcardDelay = 45f;
    [Tooltip("Thời gian tối đa trước khi hiện endcard (giây). Bất kể đang ở trạng thái nào.")]
    [SerializeField] private float maxGameTimeout = 120f;
    [Tooltip("Cho phép hiện endcard kể cả khi tutorial chưa hoàn thành nếu người chơi idle/hết giờ.")]
    [SerializeField] private bool allowEndcardDuringTutorial = true;

    [Header("Scenario")]
    [SerializeField] private PlayableScenarioId scenario = PlayableScenarioId.Scenario123;
    [SerializeField] private List<ScenePlayableSO> scenePlayableSOs = new List<ScenePlayableSO>();
    [Tooltip("Optional exact index in scenePlayableSOs. Use -1 to select the first config matching scenario.")]
    [SerializeField] private int scenePlayableConfigIndex = -1;
    [Header("Persistent store CTAs")]
    [SerializeField] private List<Button> ctaButtons = new List<Button>();
    [Header("Repeating endcard")]
    [SerializeField] private GameObject endcardRoot;
    [SerializeField] private Button continueButton;
    [SerializeField] private List<Button> endcardStoreButtons = new List<Button>();
    [Header("Optional references (auto-resolved when empty)")]
    [SerializeField] private PlayableTutorialFlow tutorialFlow;
    [SerializeField] private PlantStore plantStore;
    [SerializeField] private List<PlantStore> plantStores = new List<PlantStore>();
    [SerializeField] private PlantConfig plantConfig;
    [SerializeField] private Button selloutButton;
    [SerializeField] private CanvasGroup selloutCanvasGroup;
    [SerializeField] private TMPro.TMP_Text selloutValueText;
    [Header("Pre-attached playable UI")]
    [SerializeField] private PlayableUpgradePopupView upgradePopup;
    [SerializeField] private PlayableSprinklerAndHarvesterUpgradePopup combinedUpgradePopup;
    [SerializeField] private Sprite sprinklerUpgradeIcon;
    [SerializeField] private Sprite harvesterUpgradeIcon;
    [SerializeField] private Sprite transportUpgradeIcon;
    [Tooltip("Enable only when this scene should take tutorial start/skip values from ScenePlayableSO. By default, TutorialLayer's inspector settings win.")]
    [SerializeField] private bool useSceneSoTutorialSettings;

    [Header("Responsive bottom bar layout")]
    [SerializeField] private RectTransform canvasHiddenEndGameRect;
    [SerializeField] private float landscapeBottomBarWidth = 1920f;
    [SerializeField] private float bottomBarHeight = 200f;
    [SerializeField] private float bottomBarOffsetY = 30f;

    public ScenePlayableSO CurrentScenePlayableSO { get; private set; }
    public bool IsTutorialVisible => tutorialFlow != null && tutorialFlow.IsRunning;
    private bool _waitingForFruitSelection;
    private bool _startTutorialAfterPlantSelection;
    private int _pendingPlantPlotId = -1;
    private bool _endcardVisible;
    private bool _hasShownEndcard;
    private float _nextEndcardAt;
    private float _totalPlayTime;
    private float _lastInteractionTime;
    private FruitTransportStation _transportStation;
    private FarmPlotSystem _plotSystem;
    private readonly List<int> _columnChoiceIds = new List<int>(2);
    private int _columnNextPlantIndex;
    private ScenePlayableSO _columnChoiceScenario;
    [SerializeField] private PlayableUpgradePopupPresenter upgradePopupPresenter;
    private PlayableCurrencyDisplay _currencyDisplay;

    public bool IsGameplayInputBlocked =>
        (combinedUpgradePopup != null && combinedUpgradePopup.IsVisible) ||
        (upgradePopupPresenter != null && upgradePopupPresenter.IsVisible);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentScenePlayableSO = FindScenarioConfig();
        PlayableScenarioRuntime.Prepare(CurrentScenePlayableSO);
        ResolveReferences();
    }

    private void OnEnable()
    {
        GameEventBus.ActionOnPlantSelected += HandlePlantSelected;
    }

    private void OnDisable()
    {
        GameEventBus.ActionOnPlantSelected -= HandlePlantSelected;
    }

    private void Start()
    {
        AdjustBottomBarLayout();
        WireStoreButtons();
        WireEndcardButtons();
        WireSelloutButton();
        SubscribeTransportStation();
        SubscribePlotSystem();
        HideEndcard();
        ConfigureScenario();
        RestartEndcardCountdown();
        GameEventBus.OnStartGame?.Invoke();
    }

#if UNITY_EDITOR
    private float _lastAspect;
#endif

    private void Update()
    {
#if UNITY_EDITOR
        float currentAspect = (float)Screen.width / Screen.height;
        if (Mathf.Abs(currentAspect - _lastAspect) > 0.01f)
        {
            _lastAspect = currentAspect;
            AdjustBottomBarLayout();
        }
#endif
        if (_endcardVisible) return;

        float dt = Time.unscaledDeltaTime;
        _totalPlayTime += dt;

        // 1. Hard Timeout toàn cục (120s): Bắt buộc bật Endcard lần đầu nếu người chơi chưa từng thấy Endcard
        if (!_hasShownEndcard && _totalPlayTime >= maxGameTimeout)
        {
            ShowEndcard();
            return;
        }

        // 2. Nếu người chơi đang chạm màn hình (vuốt / kéo), hoãn nhẹ 1.5s để không ngắt thao tác
        if (Input.GetMouseButton(0))
        {
            _lastInteractionTime = Time.unscaledTime;
            if (Time.unscaledTime >= _nextEndcardAt)
            {
                _nextEndcardAt = Time.unscaledTime + 1.5f;
            }
            return;
        }

        // 3. Nếu đang mở popup (chọn quả hoặc upgrade), chỉ hoãn nếu chưa chạm mốc thời gian đếm ngược
        if (_waitingForFruitSelection || IsGameplayInputBlocked)
        {
            if (Time.unscaledTime < _nextEndcardAt)
            {
                return;
            }
        }
        else if (IsTutorialVisible && !allowEndcardDuringTutorial)
        {
            _nextEndcardAt += dt;
            return;
        }

        // 4. Hết thời gian đếm ngược -> Bật Endcard
        if (Time.unscaledTime >= _nextEndcardAt)
        {
            ShowEndcard();
        }
    }

    private void OnDestroy()
    {
        if (tutorialFlow != null)
        {
            tutorialFlow.PhaseChanged -= HandleTutorialPhaseChanged;
        }
        if (_transportStation != null)
        {
            _transportStation.OnFruitLoaded -= HandleFruitLoaded;
            _transportStation.OnVehicleStateChanged -= HandleVehicleStateChanged;
        }
        if (_plotSystem != null)
        {
            _plotSystem.OnPlotUnlocked -= HandlePlotUnlocked;
        }
        if (Instance == this) Instance = null;
    }

    public void ConfigureScenario()
    {
        CurrentScenePlayableSO = FindScenarioConfig();
        SubscribePlotSystem();
        HideFruitPicker();
        if (CurrentScenePlayableSO == null) { Debug.LogWarning("LunaUIManager: no ScenePlayableSO is assigned."); return; }
        bool useScenarioTutorial = useSceneSoTutorialSettings || CurrentScenePlayableSO.scenario != PlayableScenarioId.Scenario123;
        if (tutorialFlow != null && useScenarioTutorial)
        {
            tutorialFlow.Configure(CurrentScenePlayableSO.tutorialStartPhase, CurrentScenePlayableSO.skippedTutorialPhases, false);
            tutorialFlow.gameObject.SetActive(CurrentScenePlayableSO.showTutorial);
        }
        _waitingForFruitSelection = false;
        _pendingPlantPlotId = -1;
        ResetColumnPickerStateIfScenarioChanged();
        if (CurrentScenePlayableSO.UsesPlantPicker && TryShowPlantPicker(GetFirstEmptyPlot(), true))
        {
            return;
        }

        HideFruitPicker();
        StartConfiguredTutorial();
    }

    private ScenePlayableSO FindScenarioConfig()
    {
        if (scenePlayableConfigIndex >= 0 && scenePlayableConfigIndex < scenePlayableSOs.Count)
        {
            ScenePlayableSO indexedConfig = scenePlayableSOs[scenePlayableConfigIndex];
            if (indexedConfig != null) return indexedConfig;
        }

        foreach (ScenePlayableSO config in scenePlayableSOs) if (config != null && config.scenario == scenario) return config;
        foreach (ScenePlayableSO config in scenePlayableSOs) if (config != null) return config;
        return null;
    }

    private void StartConfiguredTutorial()
    {
        if (tutorialFlow == null || CurrentScenePlayableSO == null || !CurrentScenePlayableSO.showTutorial) return;
        tutorialFlow.gameObject.SetActive(true);
        tutorialFlow.StartConfiguredPhase();
    }

    private bool TryShowPlantPicker(Plot targetPlot, bool startTutorialAfterSelection)
    {
        PlantStore activeStore = GetActivePlantStore();
        if (targetPlot == null || targetPlot.State != FarmPlotState.Empty || activeStore == null)
        {
            return false;
        }

        List<PlantConfigData> choices = GetConfiguredFruitChoices();
        if (choices.Count == 0)
        {
            return false;
        }

        activeStore.Prewarm(choices.Count);
        _pendingPlantPlotId = targetPlot.PlotId;
        _startTutorialAfterPlantSelection = startTutorialAfterSelection;
        _waitingForFruitSelection = true;
        if (_nextEndcardAt - Time.unscaledTime < 6f)
        {
            _nextEndcardAt = Time.unscaledTime + 6f;
        }
        activeStore.SetupStore(choices);
        return true;
    }

    private List<PlantConfigData> GetConfiguredFruitChoices()
    {
        List<PlantConfigData> choices = new List<PlantConfigData>();
        if (plantConfig == null || CurrentScenePlayableSO == null) return choices;
        if (CurrentScenePlayableSO.plantPickerVariant == PlayablePlantPickerVariant.AllPlants)
        {
            for (int i = 0; i < plantConfig.plants.Count; i++)
            {
                AddPlantChoice(choices, plantConfig.plants[i]);
            }
        }
        else
        {
            EnsureColumnChoiceIds();
            for (int i = 0; i < _columnChoiceIds.Count; i++)
            {
                PlantConfigData plant = plantConfig.GetPlant(_columnChoiceIds[i]);
                if (plant != null)
                {
                    choices.Add(plant);
                }
            }
        }
        return choices;
    }

    private void ResetColumnPickerStateIfScenarioChanged()
    {
        if (_columnChoiceScenario == CurrentScenePlayableSO)
        {
            return;
        }

        _columnChoiceScenario = CurrentScenePlayableSO;
        _columnChoiceIds.Clear();
        _columnNextPlantIndex = 0;
    }

    /// <summary>
    /// Column mode owns a two-item rolling window. After a choice, only the
    /// chosen plant leaves the window; the other option survives until picked.
    /// </summary>
    private void EnsureColumnChoiceIds()
    {
        ResetColumnPickerStateIfScenarioChanged();
        FillColumnChoices(2);
    }

    private void AdvanceColumnChoices(int selectedPlantId)
    {
        if (CurrentScenePlayableSO == null ||
            CurrentScenePlayableSO.plantPickerVariant == PlayablePlantPickerVariant.AllPlants)
        {
            return;
        }

        _columnChoiceIds.Remove(selectedPlantId);
        FillColumnChoices(2);
    }

    private void FillColumnChoices(int targetCount)
    {
        if (plantConfig == null || CurrentScenePlayableSO == null)
        {
            return;
        }

        List<int> orderedIds = GetColumnPlantOrder();
        while (_columnChoiceIds.Count < targetCount && _columnNextPlantIndex < orderedIds.Count)
        {
            int candidateId = orderedIds[_columnNextPlantIndex++];
            if (_columnChoiceIds.Contains(candidateId) || IsPlantAlreadyPlanted(candidateId))
            {
                continue;
            }

            _columnChoiceIds.Add(candidateId);
        }
    }

    private List<int> GetColumnPlantOrder()
    {
        List<int> orderedIds = new List<int>();
        if (plantConfig == null || CurrentScenePlayableSO == null)
        {
            return orderedIds;
        }

        List<int> configuredIds = CurrentScenePlayableSO.selectablePlantIds;
        if (configuredIds != null && configuredIds.Count > 0)
        {
            for (int i = 0; i < configuredIds.Count; i++)
            {
                AddColumnPlantId(orderedIds, configuredIds[i]);
            }
        }
        else
        {
            for (int i = 0; i < plantConfig.plants.Count; i++)
            {
                PlantConfigData plant = plantConfig.plants[i];
                if (plant != null)
                {
                    AddColumnPlantId(orderedIds, plant.ID);
                }
            }
        }

        return orderedIds;
    }

    private void AddColumnPlantId(List<int> orderedIds, int plantId)
    {
        if (plantConfig.GetPlant(plantId) != null && !orderedIds.Contains(plantId))
        {
            orderedIds.Add(plantId);
        }
    }

    private void AddPlantChoice(List<PlantConfigData> choices, PlantConfigData plant)
    {
        if (plant == null || IsPlantAlreadyPlanted(plant.ID) || choices.Exists(item => item.ID == plant.ID))
        {
            return;
        }

        choices.Add(plant);
    }

    private bool IsPlantAlreadyPlanted(int plantId)
    {
        List<PlotSaveData> plots = PlayableFarmState.PlayerData.GardenData.plots;
        for (int i = 0; i < plots.Count; i++)
        {
            if (plots[i] != null && plots[i].state == PlotState.Planted && GetVisualPlantId(plots[i]) == plantId)
            {
                return true;
            }
        }

        return false;
    }

    private int GetVisualPlantId(PlotSaveData plot)
    {
        return plot.visualPlantId >= 0 ? plot.visualPlantId : plot.plantedPlantId;
    }

    private void HandlePlantSelected(int plantId)
    {
        if (!_waitingForFruitSelection) return;
        if (!GetConfiguredFruitChoices().Exists(plant => plant.ID == plantId))
        {
            Debug.LogWarning($"LunaUIManager: plant {plantId} is not a configured picker choice.");
            return;
        }

        Plot targetPlot = _plotSystem != null ? _plotSystem.GetPlotComponent(_pendingPlantPlotId) : null;
        int gameplayPlantId = ResolveGameplayPlantId(targetPlot, plantId);
        if (gameplayPlantId < 0 || targetPlot == null || targetPlot.State != FarmPlotState.Empty ||
            !PlayableScenarioRuntime.CanSwapConfiguredVisualOrder(_pendingPlantPlotId, plantId) ||
            !_plotSystem.TryOccupyPlot(_pendingPlantPlotId, gameplayPlantId, plantId))
        {
            Debug.LogWarning("LunaUIManager: selected plant could not be placed on the pending plot.");
            return;
        }

        if (!PlayableScenarioRuntime.SwapConfiguredVisualOrder(_pendingPlantPlotId, plantId))
        {
            Debug.LogError("LunaUIManager: selected plant was placed, but its visual order could not be swapped.");
        }

        PlayableFarmState.UnlockCollectionPlant(plantId, 1);
        AdvanceColumnChoices(plantId);
        bool startTutorial = _startTutorialAfterPlantSelection;
        _waitingForFruitSelection = false;
        _startTutorialAfterPlantSelection = false;
        _pendingPlantPlotId = -1;
        HideFruitPicker();
        if (startTutorial) StartConfiguredTutorial();
        RestartEndcardCountdown();
    }

    private int ResolveGameplayPlantId(Plot targetPlot, int visualPlantId)
    {
        FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        PlantConfig config = plantConfig != null ? plantConfig : ConfigHolder.Instance?.PlantConfig;
        int configuredId = farmConfig != null && targetPlot != null
            ? farmConfig.GetInitialPlantId(targetPlot.PlotId, config)
            : -1;
        if (configuredId >= 0 && config != null && config.GetPlant(configuredId) != null)
        {
            return configuredId;
        }

        if (targetPlot != null && targetPlot.PlotData != null && targetPlot.PlotData.Config != null &&
            targetPlot.PlotData.Config.defaultPlantId >= 0)
        {
            return targetPlot.PlotData.Config.defaultPlantId;
        }

        return visualPlantId;
    }

    private bool IsConfiguredPlantChoice(int plantId)
    {
        if (CurrentScenePlayableSO == null)
        {
            return false;
        }

        return GetConfiguredFruitChoices().Exists(plant => plant.ID == plantId);
    }

    public void RequestPlantChoice(Plot plot)
    {
        if (plot == null || plot.State != FarmPlotState.Empty)
        {
            return;
        }

        if (_waitingForFruitSelection && IsFruitPickerVisible())
        {
            return;
        }

        if (CurrentScenePlayableSO == null || !CurrentScenePlayableSO.UsesPlantPicker)
        {
            _plotSystem?.TryPlantConfiguredPlot(plot.PlotId);
        }
        else
        {
            TryShowPlantPicker(plot, false);
        }
    }

    private void SubscribePlotSystem()
    {
        FarmPlotSystem system = FarmSystem.Instance != null ? FarmSystem.Instance.PlotSystem : null;
        if (_plotSystem == system)
        {
            return;
        }

        if (_plotSystem != null)
        {
            _plotSystem.OnPlotUnlocked -= HandlePlotUnlocked;
        }

        _plotSystem = system;
        if (_plotSystem != null)
        {
            // The scenario is prepared in LunaUIManager.Awake. If FarmSystem's
            // Awake order was earlier, rebuild its empty runtime cache now
            // against that prepared state before asking it for an empty plot.
            _plotSystem.EnsureInitialized();
            _plotSystem.OnPlotUnlocked += HandlePlotUnlocked;
        }
    }

    private void HandlePlotUnlocked(FarmPlotData plotData)
    {
        if (plotData == null || plotData.State != FarmPlotState.Empty || CurrentScenePlayableSO == null || !CurrentScenePlayableSO.UsesPlantPicker)
        {
            return;
        }

        if (CurrentScenePlayableSO.showPlantPickerOnlyAtStart)
        {
            // The initial picker selection is the only player choice in this
            // mode. Every later plot follows the authored FarmConfig order.
            if (!_plotSystem.TryPlantConfiguredPlot(plotData.Id))
            {
                Debug.LogWarning($"LunaUIManager: plot {plotData.Id} has no valid configured plant for first-time picker mode.");
            }
            return;
        }

        Plot plot = _plotSystem != null ? _plotSystem.GetPlotComponent(plotData.Id) : null;
        if (TryShowPlantPicker(plot, false))
        {
            return;
        }

        // A picker plot must remain empty when every valid choice has already
        // been used; never silently plant a duplicate fallback crop.
    }

    private Plot GetFirstEmptyPlot()
    {
        if (_plotSystem == null)
        {
            return null;
        }

        IReadOnlyList<FarmPlotData> plots = _plotSystem.Plots;
        for (int i = 0; i < plots.Count; i++)
        {
            if (plots[i] != null && plots[i].State == FarmPlotState.Empty)
            {
                return _plotSystem.GetPlotComponent(plots[i].Id);
            }
        }

        return null;
    }

    private void ResolveReferences()
    {
        if (plantStore != null && !plantStores.Contains(plantStore))
        {
            plantStores.Add(plantStore);
        }
        if (plantConfig == null && ConfigHolder.Instance != null)
        {
            plantConfig = ConfigHolder.Instance.PlantConfig;
        }

        if (upgradePopup != null)
        {
            upgradePopup.gameObject.SetActive(false);
        }
        if (combinedUpgradePopup != null)
        {
            combinedUpgradePopup.Hide();
        }

        if (canvasHiddenEndGameRect == null && selloutButton != null)
        {
            canvasHiddenEndGameRect = selloutButton.transform.parent as RectTransform;
        }
    }

    [ContextMenu("Adjust Bottom Bar Layout")]
    public void AdjustBottomBarLayout()
    {
        if (canvasHiddenEndGameRect == null && selloutButton != null)
        {
            canvasHiddenEndGameRect = selloutButton.transform.parent as RectTransform;
        }

        if (canvasHiddenEndGameRect == null) return;

        float aspect = (float)Screen.width / Screen.height;
        if (aspect > 1.0f)
        {
            // Màn hình ngang (Landscape: x : y > 1, ví dụ Full HD / 4K): Center-Bottom, width = 1920
            canvasHiddenEndGameRect.anchorMin = new Vector2(0.5f, 0f);
            canvasHiddenEndGameRect.anchorMax = new Vector2(0.5f, 0f);
            canvasHiddenEndGameRect.pivot = new Vector2(0.5f, 0f);
            canvasHiddenEndGameRect.sizeDelta = new Vector2(landscapeBottomBarWidth, bottomBarHeight);
            canvasHiddenEndGameRect.anchoredPosition = new Vector2(0f, bottomBarOffsetY);
        }
        else
        {
            // Màn hình dọc hoặc vuông (Portrait / Square: x : y <= 1): Stretch ngang ở đáy
            canvasHiddenEndGameRect.anchorMin = new Vector2(0f, 0f);
            canvasHiddenEndGameRect.anchorMax = new Vector2(1f, 0f);
            canvasHiddenEndGameRect.pivot = new Vector2(0.5f, 0f);
            canvasHiddenEndGameRect.sizeDelta = new Vector2(0f, bottomBarHeight);
            canvasHiddenEndGameRect.anchoredPosition = new Vector2(0f, bottomBarOffsetY);
        }
    }

    private void WireStoreButtons()
    {
        for (int i = 0; i < ctaButtons.Count; i++)
        {
            WireStoreButton(ctaButtons[i]);
        }
    }

    private void WireEndcardButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ContinuePlaying);
            continueButton.onClick.AddListener(ContinuePlaying);
        }
        foreach (Button button in endcardStoreButtons) WireStoreButton(button);
    }

    private void WireSelloutButton()
    {
        if (selloutButton == null)
        {
            return;
        }

        selloutButton.onClick.RemoveListener(SellLoadedFruit);
        selloutButton.onClick.AddListener(SellLoadedFruit);
        if (tutorialFlow != null)
        {
            tutorialFlow.PhaseChanged -= HandleTutorialPhaseChanged;
            tutorialFlow.PhaseChanged += HandleTutorialPhaseChanged;
        }
        RefreshSelloutButton();
    }

    private void HandleTutorialPhaseChanged(PlayableTutorialFlow.Phase? phase)
    {
        RefreshSelloutButton();
    }

    private void RefreshSelloutButton()
    {
        if (selloutButton == null)
        {
            return;
        }

        FruitTransportStation station = FarmSystem.Instance != null ? FarmSystem.Instance.TransportStation : null;
        bool isVisible = station != null && station.LoadedFruitCount > 0 && station.IsIdleAtStation;
        if (selloutCanvasGroup != null)
        {
            selloutCanvasGroup.alpha = isVisible ? 1f : 0f;
            selloutCanvasGroup.interactable = isVisible;
            selloutCanvasGroup.blocksRaycasts = isVisible;
        }
        selloutButton.interactable = isVisible;
        string selloutValue = station != null ? T_Utility.FormatGold(station.LoadedRevenue) : "0";
        if (selloutValueText != null)
        {
            selloutValueText.text = selloutValue;
        }
    }

    private void SellLoadedFruit()
    {
        FruitTransportStation station = FarmSystem.Instance != null ? FarmSystem.Instance.TransportStation : null;
        if (station != null)
        {
            station.Sell();
        }
        RefreshSelloutButton();
    }

    private void SubscribeTransportStation()
    {
        FruitTransportStation station = FarmSystem.Instance != null ? FarmSystem.Instance.TransportStation : null;
        if (_transportStation == station)
        {
            RefreshSelloutButton();
            return;
        }

        if (_transportStation != null)
        {
            _transportStation.OnFruitLoaded -= HandleFruitLoaded;
            _transportStation.OnVehicleStateChanged -= HandleVehicleStateChanged;
        }

        _transportStation = station;
        if (_transportStation != null)
        {
            _transportStation.OnFruitLoaded += HandleFruitLoaded;
            _transportStation.OnVehicleStateChanged += HandleVehicleStateChanged;
        }
        RefreshSelloutButton();
    }

    private void HandleFruitLoaded(int plantId, int count)
    {
        RefreshSelloutButton();
    }

    private void HandleVehicleStateChanged(TransportVehicleState state)
    {
        RefreshSelloutButton();
    }

    private void WireStoreButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveListener(OnCTAClicked);
        button.onClick.AddListener(OnCTAClicked);
    }

    private void RestartEndcardCountdown()
    {
        float delay = _hasShownEndcard
            ? recurringEndcardDelay
            : initialEndcardDelay;
        _nextEndcardAt = Time.unscaledTime + delay;
    }

    [ContextMenu("Show Endcard Now")]
    public void ShowEndcard()
    {
        if (endcardRoot == null)
        {
            Debug.LogWarning("LunaUIManager: cannot ShowEndcard because endcardRoot is null.");
            return;
        }

        _hasShownEndcard = true;
        _endcardVisible = true;

        HideFruitPicker();
        if (combinedUpgradePopup != null && combinedUpgradePopup.IsVisible)
        {
            combinedUpgradePopup.Hide();
        }
        if (upgradePopupPresenter != null && upgradePopupPresenter.IsVisible)
        {
            upgradePopupPresenter.Hide();
        }

        endcardRoot.SetActive(true);
        WireEndcardButtons();

        Luna.Unity.LifeCycle.GameEnded();

    }

    public void ContinuePlaying()
    {
        HideEndcard();
        RestartEndcardCountdown();
        RestorePendingPlantPicker();
    }

    private void RestorePendingPlantPicker()
    {
        if (!_waitingForFruitSelection && _pendingPlantPlotId < 0)
        {
            Plot emptyPlot = GetFirstEmptyPlot();
            if (emptyPlot != null && CurrentScenePlayableSO != null && CurrentScenePlayableSO.UsesPlantPicker)
            {
                TryShowPlantPicker(emptyPlot, false);
            }
            return;
        }

        Plot pendingPlot = _plotSystem != null && _pendingPlantPlotId >= 0
            ? _plotSystem.GetPlotComponent(_pendingPlantPlotId)
            : null;

        if (pendingPlot == null || pendingPlot.State != FarmPlotState.Empty)
        {
            pendingPlot = GetFirstEmptyPlot();
        }

        if (pendingPlot != null && pendingPlot.State == FarmPlotState.Empty)
        {
            TryShowPlantPicker(pendingPlot, _startTutorialAfterPlantSelection);
        }
        else
        {
            _waitingForFruitSelection = false;
            _pendingPlantPlotId = -1;
        }
    }

    public bool IsFruitPickerVisible()
    {
        PlantStore store = GetActivePlantStore();
        return store != null && store.gameObject.activeSelf;
    }

    private void HideEndcard()
    {
        _endcardVisible = false;
        if (endcardRoot != null) endcardRoot.SetActive(false);
    }

    private void HideFruitPicker()
    {
        for (int i = 0; i < plantStores.Count; i++)
        {
            if (plantStores[i] != null) plantStores[i].HideStore();
        }
        if (plantStore != null && !plantStores.Contains(plantStore)) plantStore.HideStore();
    }

    private PlantStore GetActivePlantStore()
    {
        bool wantsGrid = CurrentScenePlayableSO != null && CurrentScenePlayableSO.plantPickerVariant == PlayablePlantPickerVariant.AllPlants;
        for (int i = 0; i < plantStores.Count; i++)
        {
            PlantStore candidate = plantStores[i];
            if (candidate != null && candidate.IsGridStore == wantsGrid) return candidate;
        }

        return plantStore != null && plantStore.IsGridStore == wantsGrid ? plantStore : null;
    }

    public void ShowIrrigationUpgrade()
    {
        if (UsesHybridHarvestPopup())
        {
            ShowCombinedUpgradePopup(PlayableUpgradePopupTab.Sprinkler);
            return;
        }

        FarmIrrigationSystem system = FarmSystem.Instance != null ? FarmSystem.Instance.IrrigationSystem : null;
        if (system != null)
        {
            Sprite icon = sprinklerUpgradeIcon != null ? sprinklerUpgradeIcon : GetLocalSprite(system.transform);
            ShowUpgrade("SPRINKLER", () => GetSprinklerUpgradeRows(system.Level, system.MaxLevel),
                icon,
                () => system.Level, () => system.MaxLevel, () => system.NextUpgradeCost, () => system.CanUpgrade, system.TryUpgrade);
        }
    }

    public void ShowHarvestUpgrade()
    {
        if (UsesHybridHarvestPopup())
        {
            ShowCombinedUpgradePopup(PlayableUpgradePopupTab.Harvester);
            return;
        }

        FarmHarvestSystem system = FarmSystem.Instance != null ? FarmSystem.Instance.HarvestSystem : null;
        if (system != null && system.AutomaticArmEnabled)
        {
            Sprite icon = harvesterUpgradeIcon != null ? harvesterUpgradeIcon : GetLocalSprite(system.transform);
            ShowUpgrade("HARVEST ARM", () => GetHarvesterUpgradeRows(system),
                icon,
                () => system.Level, () => system.MaxLevel, () => system.NextUpgradeCost, () => system.CanUpgrade, system.TryUpgrade);
        }
    }

    private bool UsesHybridHarvestPopup()
    {
        ScenePlayableSO activeScenario = CurrentScenePlayableSO != null
            ? CurrentScenePlayableSO
            : PlayableScenarioRuntime.CurrentScenario;
        return activeScenario != null && activeScenario.harvestMode == PlayableHarvestMode.Hybrid;
    }

    private void ShowCombinedUpgradePopup(PlayableUpgradePopupTab tab)
    {
        if (combinedUpgradePopup == null)
        {
            Debug.LogError("LunaUIManager requires combinedUpgradePopup serialized on inspector.");
            return;
        }

        combinedUpgradePopup.Show(tab);
    }

    public void ShowTransportUpgrade()
    {
        FruitTransportStation system = FarmSystem.Instance != null ? FarmSystem.Instance.TransportStation : null;
        if (system != null)
        {
            Sprite truckIcon = transportUpgradeIcon != null ? transportUpgradeIcon : GetLocalSprite(system.transform);
            ShowUpgrade("TRUCK", () => OneStatRow("CAPACITY", system.Capacity.ToString(), GetTransportCapacity(system.Level + 1).ToString()),
                truckIcon,
                () => system.Level, () => system.MaxLevel, () => system.NextUpgradeCost, () => system.CanUpgrade, system.TryUpgrade);
        }
    }

    public void ShowPlantUpgrade(Plot plot)
    {
        Plant plant = plot != null ? plot.CurrentPlant : null;
        if (plant == null)
        {
            return;
        }

        PlantConfigData config = plant.Config;
        PlantConfigData visualConfig = plant.VisualConfig;
        Func<UpgradeStatRow[]> stat = () => config != null
            ? GetPlantUpgradeRows(config, plant.PlantLevel, plant.RingIndex)
            : OneStatRow("VALUE", string.Empty, "INCREASE");
        Sprite giantSkin = visualConfig != null ? visualConfig.GetGiantSkin() : (config != null ? config.GetGiantSkin() : null);
        if (giantSkin == null) giantSkin = GetLocalSprite(plant.transform);
        string title = visualConfig != null ? visualConfig.PlantName.ToUpperInvariant() : "PLANT";
        ShowUpgrade(title, stat, giantSkin, () => plant.PlantLevel, () => plant.MaxLevel, () => plant.UpgradeCost, plot.CanUpgradePlant, () => UpgradePlant(plot, plant));
    }

    private bool UpgradePlant(Plot plot, Plant plant)
    {
        if (plot == null || plant == null || !plot.CanUpgradePlant())
        {
            return false;
        }

        double cost = plant.UpgradeCost;
        if (cost > 0 && !PlayableFarmState.ConsumeGold(cost))
        {
            return false;
        }

        if (!plant.TryUpgrade())
        {
            return false;
        }

        plot.SetPlantLevel(plant.PlantLevel);
        // Picker selection is an art-only override. Preserve it while the
        // plot's gameplay plant continues to own price, water, and upgrade
        // stats from its configured ring data.
        PlayableFarmState.UpdatePlotData(plot.PlotId, PlotState.Planted, plant.PlantId, plant.PlantLevel, plant.VisualPlantId);
        GameEventBus.OnCropUpgradedEvent?.Invoke(plant.PlantId, plant.PlantLevel);
        return true;
    }

    private void ShowUpgrade(string title, Func<UpgradeStatRow[]> getStatRows, Sprite illustrationSprite, Func<int> getLevel,
        Func<int> getMaxLevel, Func<double> getCost, Func<bool> canUpgrade, Func<bool> performUpgrade)
    {
        if (upgradePopupPresenter == null) upgradePopupPresenter = GetComponent<PlayableUpgradePopupPresenter>();
        if (upgradePopupPresenter == null)
        {
            Debug.LogError("LunaUIManager needs a pre-attached PlayableUpgradePopupPresenter.");
            return;
        }

        if (upgradePopup == null)
        {
            Debug.LogError("LunaUIManager requires upgradePopup serialized on inspector.");
            return;
        }

        upgradePopupPresenter.Show(upgradePopup, title, getStatRows, illustrationSprite, getLevel, getMaxLevel, getCost, canUpgrade, performUpgrade);
    }

    private static Sprite GetLocalSprite(Transform root)
    {
        if (root == null) return null;
        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].sprite != null) return renderers[i].sprite;
        }
        return null;
    }

    private static readonly UpgradeStatRow[] s_cachedTwoStatRows = new UpgradeStatRow[2];
    private static readonly UpgradeStatRow[] s_cachedOneStatRow = new UpgradeStatRow[1];

    private static UpgradeStatRow[] GetPlantUpgradeRows(PlantConfigData config, int level, int ringIndex)
    {
        int currentLevel = Mathf.Clamp(level, 1, config.GetMaxLevel());
        int nextLevel = Mathf.Min(currentLevel + 1, config.GetMaxLevel());
        bool isMax = currentLevel >= config.GetMaxLevel();
        s_cachedTwoStatRows[0] = new UpgradeStatRow("FRUIT", config.GetFruitCount(currentLevel).ToString(), isMax ? "MAX" : config.GetFruitCount(nextLevel).ToString());
        s_cachedTwoStatRows[1] = new UpgradeStatRow("PRICE", T_Utility.FormatGold(config.GetFruitPrice(currentLevel, ringIndex)), isMax ? "MAX" : T_Utility.FormatGold(config.GetFruitPrice(nextLevel, ringIndex)));
        return s_cachedTwoStatRows;
    }

    private static UpgradeStatRow[] OneStatRow(string label, string current, string next)
    {
        s_cachedOneStatRow[0] = new UpgradeStatRow(label, current, next);
        return s_cachedOneStatRow;
    }

    private static UpgradeStatRow[] GetSprinklerUpgradeRows(int level, int maxLevel)
    {
        FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        SprinklerConfig config = farmConfig != null ? farmConfig.sprinkler : null;
        if (config == null) return OneStatRow("WATER", string.Empty, "INCREASE");

        int currentLevel = Mathf.Clamp(level, 1, maxLevel);
        int nextLevel = Mathf.Min(currentLevel + 1, maxLevel);
        s_cachedTwoStatRows[0] = new UpgradeStatRow("WATER", config.GetWaterAmount(currentLevel).ToString(), config.GetWaterAmount(nextLevel).ToString());
        s_cachedTwoStatRows[1] = new UpgradeStatRow("ARMS", config.GetNozzleCount(currentLevel).ToString(), config.GetNozzleCount(nextLevel).ToString());
        return s_cachedTwoStatRows;
    }

    private static UpgradeStatRow[] GetHarvesterUpgradeRows(FarmHarvestSystem system)
    {
        FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        HarvesterConfig config = farmConfig != null ? farmConfig.harvester : null;
        if (config == null)
        {
            return OneStatRow("ARMS", system.HarvesterCount.ToString(), system.HarvesterCount.ToString());
        }

        int currentLevel = Mathf.Clamp(system.Level, 1, system.MaxLevel);
        int nextLevel = Mathf.Min(currentLevel + 1, system.MaxLevel);
        s_cachedTwoStatRows[0] = new UpgradeStatRow("TIME", config.GetRotationDuration(currentLevel).ToString("0.##") + "s", config.GetRotationDuration(nextLevel).ToString("0.##") + "s");
        s_cachedTwoStatRows[1] = new UpgradeStatRow("ARMS", config.GetHarvesterCount(currentLevel).ToString(), config.GetHarvesterCount(nextLevel).ToString());
        return s_cachedTwoStatRows;
    }

    private static int GetTransportCapacity(int level)
    {
        FarmConfig config = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        return config != null && config.transport != null ? config.transport.GetCapacity(level) : 0;
    }

    public void OnCTAClicked() { GameEventBus.OnCTAClicked?.Invoke(); Luna.Unity.Playable.InstallFullGame(); }
    public void OpenStoreNow() => OnCTAClicked();
    public void StartGame() => GameEventBus.OnStartGame?.Invoke();
    public void PauseGameplay() { }
    public void ResumeGameplay() { }
    public void NotifySelectableClick(bool unloadBeforePlay = false) { }
}
