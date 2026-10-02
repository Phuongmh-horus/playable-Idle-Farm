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

    [Header("Scenario")]
    [SerializeField] private PlayableScenarioId scenario = PlayableScenarioId.Scenario123;
    [SerializeField] private List<ScenePlayableSO> scenePlayableSOs = new List<ScenePlayableSO>();
    [Header("Persistent store CTAs")]
    [SerializeField] private List<Button> ctaButtons = new List<Button>();
    [Header("Repeating endcard")]
    [SerializeField] private GameObject endcardRoot;
    [SerializeField] private Button continueButton;
    [SerializeField] private List<Button> endcardStoreButtons = new List<Button>();
    [SerializeField, Min(1f)] private float fallbackEndcardDelaySeconds = 60f;
    [Header("Optional references (auto-resolved when empty)")]
    [SerializeField] private PlayableTutorialFlow tutorialFlow;
    [SerializeField] private PlantStore plantStore;
    [SerializeField] private PlantConfig plantConfig;
    [SerializeField] private Button selloutButton;
    [SerializeField] private CanvasGroup selloutCanvasGroup;
    [SerializeField] private TMPro.TMP_Text selloutValueText;
    [Header("Pre-attached playable UI")]
    [SerializeField] private PlayableUpgradePopupView upgradePopup;
    [SerializeField] private Sprite sprinklerUpgradeIcon;
    [SerializeField] private Sprite harvesterUpgradeIcon;
    [SerializeField] private Sprite transportUpgradeIcon;
    [Tooltip("Enable only when this scene should take tutorial start/skip values from ScenePlayableSO. By default, TutorialLayer's inspector settings win.")]
    [SerializeField] private bool useSceneSoTutorialSettings;

    public ScenePlayableSO CurrentScenePlayableSO { get; private set; }
    public bool IsTutorialVisible => tutorialFlow != null && tutorialFlow.IsRunning;
    private bool _waitingForFruitSelection;
    private bool _endcardVisible;
    private float _nextEndcardAt;
    private FruitTransportStation _transportStation;
    [SerializeField] private PlayableUpgradePopupPresenter upgradePopupPresenter;
    private PlayableCurrencyDisplay _currencyDisplay;

    public bool IsGameplayInputBlocked => upgradePopupPresenter != null && upgradePopupPresenter.IsVisible;

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
        WireStoreButtons();
        WireEndcardButtons();
        WireSelloutButton();
        SubscribeTransportStation();
        HideEndcard();
        ConfigureScenario();
        RestartEndcardCountdown();
        GameEventBus.OnStartGame?.Invoke();
    }

    private void Update()
    {
        if (_waitingForFruitSelection || _endcardVisible) return;
        if (IsGameplayInputBlocked || IsTutorialVisible)
        {
            _nextEndcardAt += Time.unscaledDeltaTime;
            return;
        }
        if (Time.unscaledTime < _nextEndcardAt) return;
        ShowEndcard();
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
        if (Instance == this) Instance = null;
    }

    public void ConfigureScenario()
    {
        CurrentScenePlayableSO = FindScenarioConfig();
        if (CurrentScenePlayableSO == null) { Debug.LogWarning("LunaUIManager: no ScenePlayableSO is assigned."); return; }
        bool useScenarioTutorial = useSceneSoTutorialSettings || CurrentScenePlayableSO.scenario != PlayableScenarioId.Scenario123;
        if (tutorialFlow != null && useScenarioTutorial)
        {
            tutorialFlow.Configure(CurrentScenePlayableSO.tutorialStartPhase, CurrentScenePlayableSO.skippedTutorialPhases, false);
            tutorialFlow.gameObject.SetActive(CurrentScenePlayableSO.showTutorial);
        }
        _waitingForFruitSelection = CurrentScenePlayableSO.showFruitPicker;
        if (_waitingForFruitSelection) ShowConfiguredFruitPicker();
        else { HideFruitPicker(); StartConfiguredTutorial(); }
    }

    private ScenePlayableSO FindScenarioConfig()
    {
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

    private void ShowConfiguredFruitPicker()
    {
        if (plantStore == null) { Debug.LogWarning("LunaUIManager: Scenario 125 needs SelectPlantStore."); return; }
        List<PlantConfigData> choices = GetConfiguredFruitChoices();
        if (choices.Count == 0) { _waitingForFruitSelection = false; StartConfiguredTutorial(); return; }
        plantStore.SetupStore(choices);
    }

    private List<PlantConfigData> GetConfiguredFruitChoices()
    {
        List<PlantConfigData> choices = new List<PlantConfigData>();
        if (plantConfig == null || CurrentScenePlayableSO == null) return choices;
        foreach (int id in CurrentScenePlayableSO.selectablePlantIds)
        {
            PlantConfigData plant = plantConfig.GetPlant(id);
            if (plant != null) choices.Add(plant);
        }
        return choices;
    }

    private void HandlePlantSelected(int plantId)
    {
        if (!_waitingForFruitSelection) return;
        _waitingForFruitSelection = false;
        HideFruitPicker();
        FarmPlotSystem plotSystem = FarmSystem.Instance != null ? FarmSystem.Instance.PlotSystem : null;
        if (plotSystem != null)
        {
            // Scenario 125: player choice becomes the starter plant in the first open plot.
            plotSystem.TryOccupyPlot(0, plantId);
        }
        StartConfiguredTutorial();
        RestartEndcardCountdown();
    }

    private void ResolveReferences()
    {
        Transform playableRoot = transform.root;
        if (tutorialFlow == null) tutorialFlow = playableRoot.GetComponentInChildren<PlayableTutorialFlow>(true);
        if (plantStore == null) plantStore = playableRoot.GetComponentInChildren<PlantStore>(true);
        if (endcardRoot == null) endcardRoot = FindChildByName(playableRoot, "EndGamePanel");
        ResolvePreloadedUpgradePopups(playableRoot);

        if (selloutButton == null)
        {
            Button[] buttons = playableRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == "Sellout_Button")
                {
                    selloutButton = buttons[i];
                    break;
                }
            }
        }

        if (selloutCanvasGroup == null && selloutButton != null)
        {
            selloutCanvasGroup = selloutButton.GetComponent<CanvasGroup>();
        }

        if (selloutValueText == null && selloutButton != null)
        {
            selloutValueText = selloutButton.GetComponentInChildren<TMPro.TMP_Text>(true);
        }

        GameObject currencyRoot = FindChildByName(playableRoot, "CurrencyLayer");
        if (currencyRoot != null)
        {
            _currencyDisplay = currencyRoot.GetComponent<PlayableCurrencyDisplay>();
            if (_currencyDisplay == null) _currencyDisplay = currencyRoot.AddComponent<PlayableCurrencyDisplay>();
        }

        if (endcardRoot == null) return;

        foreach (Button button in endcardRoot.GetComponentsInChildren<Button>(true))
        {
            if (button.name == "Continue_Button") continueButton = button;
            else if (!endcardStoreButtons.Contains(button)) endcardStoreButtons.Add(button);
        }

    }

    private static GameObject FindChildByName(Transform root, string objectName)
    {
        Transform[] items = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].name == objectName) return items[i].gameObject;
        }
        return null;
    }

    private void WireStoreButtons()
    {
        Transform[] items = transform.root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < items.Length; i++)
        {
            Transform item = items[i];
            if (item.name != "Logo_Button" && item.name != "PlayNow_Button") continue;
            Button button = item.GetComponent<Button>();
            if (button != null && !ctaButtons.Contains(button)) ctaButtons.Add(button);
        }
        foreach (Button button in ctaButtons) WireStoreButton(button);
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
        if (selloutValueText != null)
        {
            selloutValueText.text = station != null ? T_Utility.FormatGold(station.LoadedRevenue) : "0";
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
        float delay = CurrentScenePlayableSO != null
            ? CurrentScenePlayableSO.endcardDelaySeconds
            : fallbackEndcardDelaySeconds;
        _nextEndcardAt = Time.unscaledTime + Mathf.Max(1f, delay);
    }

    [ContextMenu("Show Endcard Now")]
    public void ShowEndcard()
    {
        if (endcardRoot == null) return;
        _endcardVisible = true;
        endcardRoot.SetActive(true);
    }

    public void ContinuePlaying()
    {
        HideEndcard();
        RestartEndcardCountdown();
    }

    private void HideEndcard()
    {
        _endcardVisible = false;
        if (endcardRoot != null) endcardRoot.SetActive(false);
    }

    private void HideFruitPicker() { if (plantStore != null) plantStore.HideStore(); }

    public void ShowIrrigationUpgrade()
    {
        FarmIrrigationSystem system = FarmSystem.Instance != null ? FarmSystem.Instance.IrrigationSystem : null;
        if (system != null)
        {
            ShowUpgrade("SPRINKLER", () => GetSprinklerUpgradeDescription(system.Level, system.MaxLevel),
                () => sprinklerUpgradeIcon != null ? sprinklerUpgradeIcon : GetLocalSprite(system.transform),
                () => system.Level, () => system.MaxLevel, () => system.NextUpgradeCost, () => system.CanUpgrade, system.TryUpgrade);
        }
    }

    public void ShowHarvestUpgrade()
    {
        FarmHarvestSystem system = FarmSystem.Instance != null ? FarmSystem.Instance.HarvestSystem : null;
        if (system != null && system.AutomaticArmEnabled)
        {
            ShowUpgrade("HARVEST ARM", () => "BLADES  " + system.TotalBladeCount + "  ->  " + GetHarvesterBladeCount(system.Level + 1),
                () => harvesterUpgradeIcon != null ? harvesterUpgradeIcon : GetLocalSprite(system.transform),
                () => system.Level, () => system.MaxLevel, () => system.NextUpgradeCost, () => system.CanUpgrade, system.TryUpgrade);
        }
    }

    public void ShowTransportUpgrade()
    {
        FruitTransportStation system = FarmSystem.Instance != null ? FarmSystem.Instance.TransportStation : null;
        if (system != null)
        {
            ShowUpgrade("TRUCK", () => "CAPACITY  " + system.Capacity + "  ->  " + GetTransportCapacity(system.Level + 1),
                () => transportUpgradeIcon != null ? transportUpgradeIcon : GetLocalSprite(system.transform),
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
        Func<string> stat = () => config != null
            ? GetPlantUpgradeDescription(config, plant.PlantLevel, plant.RingIndex)
            : "INCREASE FRUIT VALUE";
        Func<Sprite> icon = () => config != null ? config.GetIcon(plant.PlantLevel) : GetLocalSprite(plant.transform);
        string title = config != null ? config.PlantName.ToUpperInvariant() : "PLANT";
        ShowUpgrade(title, stat, icon, () => plant.PlantLevel, () => plant.MaxLevel, () => plant.UpgradeCost, plot.CanUpgradePlant, () => UpgradePlant(plot, plant));
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
        PlayableFarmState.UpdatePlotData(plot.PlotId, PlotState.Planted, plant.PlantId, plant.PlantLevel);
        GameEventBus.OnCropUpgradedEvent?.Invoke(plant.PlantId, plant.PlantLevel);
        return true;
    }

    private void ShowUpgrade(string title, Func<string> getStatDescription, Func<Sprite> getIllustration, Func<int> getLevel,
        Func<int> getMaxLevel, Func<double> getCost, Func<bool> canUpgrade, Func<bool> performUpgrade)
    {
        if (upgradePopupPresenter == null) upgradePopupPresenter = GetComponent<PlayableUpgradePopupPresenter>();
        if (upgradePopupPresenter == null)
        {
            Debug.LogError("LunaUIManager needs a pre-attached PlayableUpgradePopupPresenter.");
            return;
        }

        if (upgradePopup == null) ResolvePreloadedUpgradePopups(transform.root);
        upgradePopupPresenter.Show(upgradePopup, title, getStatDescription, getIllustration, getLevel, getMaxLevel, getCost, canUpgrade, performUpgrade);
    }

    private void ResolvePreloadedUpgradePopups(Transform playableRoot)
    {
        upgradePopup = ResolvePopup(playableRoot, upgradePopup, "PlayableUpgradePopupLayer");
        HidePopupAtStartup(upgradePopup);
    }

    private static PlayableUpgradePopupView ResolvePopup(Transform root, PlayableUpgradePopupView assigned, string objectName)
    {
        if (assigned != null && assigned.gameObject.scene == root.gameObject.scene) return assigned;
        GameObject sceneObject = FindChildByName(root, objectName);
        if (sceneObject == null && WorldLayerManager.Instance != null)
        {
            sceneObject = FindChildByName(WorldLayerManager.Instance.transform, objectName);
        }
        if (sceneObject == null)
        {
            PlayableUpgradePopupView[] scenePopups = Resources.FindObjectsOfTypeAll<PlayableUpgradePopupView>();
            for (int i = 0; i < scenePopups.Length; i++)
            {
                PlayableUpgradePopupView candidate = scenePopups[i];
                if (candidate != null && candidate.gameObject.scene == root.gameObject.scene && candidate.name == objectName)
                {
                    sceneObject = candidate.gameObject;
                    break;
                }
            }
        }
        if (sceneObject == null)
        {
            return null;
        }

        PlayableUpgradePopupView popup = sceneObject.GetComponent<PlayableUpgradePopupView>();
        return popup;
    }

    private static void HidePopupAtStartup(PlayableUpgradePopupView popup)
    {
        if (popup != null) popup.gameObject.SetActive(false);
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

    private static string GetSprinklerUpgradeDescription(int level, int maxLevel)
    {
        FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        SprinklerConfig config = farmConfig != null ? farmConfig.sprinkler : null;
        if (config == null) return "INCREASE WATER OUTPUT";

        int currentLevel = Mathf.Clamp(level, 1, maxLevel);
        int nextLevel = Mathf.Min(currentLevel + 1, maxLevel);
        return "WATER  " + config.GetWaterAmount(currentLevel) + " -> " + config.GetWaterAmount(nextLevel)
            + "\nARMS  " + config.GetNozzleCount(currentLevel) + " -> " + config.GetNozzleCount(nextLevel);
    }

    private static string GetPlantUpgradeDescription(PlantConfigData config, int level, int ringIndex)
    {
        int currentLevel = Mathf.Clamp(level, 1, config.GetMaxLevel());
        int nextLevel = Mathf.Min(currentLevel + 1, config.GetMaxLevel());
        if (currentLevel >= config.GetMaxLevel())
        {
            return "FRUIT  " + config.GetFruitCount(currentLevel) + "  MAX"
                + "\nPRICE  " + T_Utility.FormatGold(config.GetFruitPrice(currentLevel, ringIndex)) + "  MAX";
        }

        return "FRUIT  " + config.GetFruitCount(currentLevel) + " -> " + config.GetFruitCount(nextLevel)
            + "\nPRICE  " + T_Utility.FormatGold(config.GetFruitPrice(currentLevel, ringIndex)) + " -> "
            + T_Utility.FormatGold(config.GetFruitPrice(nextLevel, ringIndex));
    }

    private static int GetHarvesterBladeCount(int level)
    {
        FarmConfig config = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        return config != null && config.harvester != null ? config.harvester.GetTotalBlades(level) : level;
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
