using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the ordered tutorial UI phases in TutorialLayer.
/// Gameplay deliberately reports milestones through the public methods instead
/// of this class polling game objects. When real plant, blade, sell, and upgrade
/// systems are added, call the corresponding Report... method at that system's
/// authoritative completion point.
/// </summary>
public sealed class PlayableTutorialFlow : MonoBehaviour
{
    public enum Phase
    {
        Sprinklers = 0,
        Harvest = 1,
        Sell = 2,
        Upgrade = 3
    }

    [Serializable]
    private sealed class PhaseView
    {
        [SerializeField] private Phase phase;
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform hand;
        [SerializeField] private RectTransform ring;
        //[SerializeField] private SpotlightCutoutOverlay spotlight;
        private bool _initialVisualsCaptured;
        private Vector3 _initialHandPosition;
        private Vector3 _initialHandScale;
        private Vector3 _initialRingScale;

        public Phase Phase => phase;
        public GameObject Root => root;
        public RectTransform Hand => hand;
        public RectTransform Ring => ring;
        //public SpotlightCutoutOverlay Spotlight => spotlight;

        public PhaseView(Phase phase, GameObject root)
        {
            this.phase = phase;
            this.root = root;
        }

        public void ResolveVisuals()
        {
            if (_initialVisualsCaptured) return;
            if (hand != null)
            {
                _initialHandPosition = hand.localPosition;
                _initialHandScale = hand.localScale;
            }
            if (ring != null) _initialRingScale = ring.localScale;
            _initialVisualsCaptured = true;
        }

        public void ResetVisuals()
        {
            if (!_initialVisualsCaptured) return;
            if (hand != null)
            {
                hand.localPosition = _initialHandPosition;
                hand.localScale = _initialHandScale;
            }
            if (ring != null) ring.localScale = _initialRingScale;
        }
    }

    [Header("Sequence")]
    [Tooltip("The first enabled tutorial step. Change this for scenarios that begin mid-flow.")]
    [SerializeField] private Phase startPhase;
    [Tooltip("Disabled phases are skipped, allowing a scenario to omit steps.")]
    [SerializeField] private List<Phase> skippedPhases = new List<Phase>();
    [SerializeField] private bool playOnStart = true;

    [Header("Phase Roots")]
    [SerializeField] private List<PhaseView> phaseViews = new List<PhaseView>();

    [Header("Motion")]
    [SerializeField, Min(0.05f)] private float ringPulseDuration = 0.8f;
    [SerializeField] private float ringPulseScale = 1.16f;
    [SerializeField, Min(0.05f)] private float handTravelDuration = 0.75f;
    [SerializeField, Min(0.05f)] private float handTapDuration = 0.25f;
    [SerializeField] private Vector2 handTapDistance = new Vector2(18f, 18f);
    [SerializeField] private float handTapScale = 0.9f;

    public static PlayableTutorialFlow Instance { get; private set; }
    public static event Action<PlayableTutorialFlow> InstanceAvailable;
    public Phase? CurrentPhase { get; private set; }
    public bool IsRunning => CurrentPhase.HasValue;
    public bool HasCompleted { get; private set; }
    public event Action<Phase?> PhaseChanged;

    private Coroutine _motion;
    private readonly Dictionary<Phase, PhaseView> _views = new Dictionary<Phase, PhaseView>();
    private Button _temporaryNextButton;
    private Phase? _expectedPhase;

    private void Awake()
    {
        Instance = this;
        InstanceAvailable?.Invoke(this);
        EnsurePhaseViews();
        CacheViews();
        SetAllInactive();
    }

    private void Start()
    {
        if (playOnStart) StartAt(startPhase);
    }

    private void OnDestroy()
    {
        if (_temporaryNextButton != null)
        {
            _temporaryNextButton.onClick.RemoveListener(CompleteCurrentPhase);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    [ContextMenu("Start Configured Phase")]
    public void StartConfiguredPhase()
    {
        StartAt(startPhase);
    }

    public void Configure(Phase configuredStartPhase, List<Phase> configuredSkippedPhases, bool shouldPlayOnStart)
    {
        startPhase = configuredStartPhase;
        skippedPhases = configuredSkippedPhases != null ? new List<Phase>(configuredSkippedPhases) : new List<Phase>();
        playOnStart = shouldPlayOnStart;
    }

    public void StartAt(Phase phase)
    {
        if (HasCompleted || CurrentPhase.HasValue || _expectedPhase.HasValue)
        {
            return;
        }

        CacheViews();
        StopMotion();
        SetAllInactive();

        _expectedPhase = FindEnabledPhaseAtOrAfter(phase);
        if (!_expectedPhase.HasValue)
        {
            CompleteFlow();
            return;
        }

        ShowExpectedPhase();
    }

    /// <summary>Use for diagnostics or a future non-linear scenario transition.</summary>
    public void Show(Phase phase)
    {
        if (!_views.TryGetValue(phase, out PhaseView view) || view.Root == null) return;

        StopMotion();
        SetAllInactive();
        view.Root.SetActive(true);
        CurrentPhase = phase;
        PhaseChanged?.Invoke(CurrentPhase);

        // if (view.Spotlight != null)
        // {

        //     if (phase == Phase.Sprinklers)
        //     {
        //         if (view.Ring != null)
        //         {
        //             view.Spotlight.SetTarget(view.Ring);
        //         }
        //         else
        //         {
        //             FarmPlotSystem plotSystem = FarmSystem.Instance != null ? FarmSystem.Instance.PlotSystem : null;
        //             Plot starterPlot = plotSystem != null ? plotSystem.GetPlotComponent(0) : null;
        //             if (starterPlot != null)
        //             {
        //                 view.Spotlight.SetTarget(starterPlot.transform);
        //             }
        //         }
        //     }
        //     else if (phase == Phase.Upgrade)
        //     {
        //         if (view.Hand != null)
        //         {
        //             view.Spotlight.SetTarget(view.Hand);
        //         }
        //         else
        //         {
        //             FarmPlotSystem plotSystem = FarmSystem.Instance != null ? FarmSystem.Instance.PlotSystem : null;
        //             Plot starterPlot = plotSystem != null ? plotSystem.GetPlotComponent(0) : null;
        //             if (starterPlot != null)
        //             {
        //                 view.Spotlight.SetTarget(starterPlot.transform);
        //             }
        //         }
        //     }

        //     view.Spotlight.Refresh();
        // }

        _motion = StartCoroutine(PlayMotion(view));
    }

    /// <summary>
    /// Completes the visible step and reveals the next enabled one. Gameplay
    /// should normally use a named Report... method below rather than this API.
    /// </summary>
    [ContextMenu("Complete Current Phase")]
    public void CompleteCurrentPhase()
    {
        if (!CurrentPhase.HasValue || CurrentPhase != _expectedPhase) return;

        _expectedPhase = FindEnabledPhaseAtOrAfter((Phase)((int)CurrentPhase.Value + 1));
        if (!_expectedPhase.HasValue)
        {
            CompleteFlow();
            return;
        }

        // Upgrade must wait until the starter plant is affordable. The check
        // below also covers an icon that became available before Sell ended.
        if (_expectedPhase == Phase.Upgrade)
        {
            HideCurrentPhase();
            ShowExpectedPhase();
            return;
        }

        ShowExpectedPhase();
    }

    // TODO(gameplay): call when the tutorial plant first produces fruit.
    [ContextMenu("Simulate: Fruit Grown")]
    public void ReportFruitGrown() => CompleteIfCurrent(Phase.Sprinklers);

    // TODO(gameplay): call when the harvest machine blade actually cuts a fruit.
    // Phase 3 must wait for the juice-transfer milestone, so do not advance here.
    [ContextMenu("Simulate: Fruit Harvested")]
    public void ReportFruitHarvested() => CompleteAndWaitIfCurrent(Phase.Harvest);

    // TODO(gameplay): call after harvested fruit becomes juice and reaches the bottle/cart.
    [ContextMenu("Simulate: Juice Delivered To Bottles")]
    public void ReportJuiceDeliveredToBottles()
    {
        if (!HasCompleted && _expectedPhase == Phase.Sell && !CurrentPhase.HasValue)
        {
            ShowExpectedPhase();
        }
    }

    // TODO(gameplay): bind the real Sell Out button's successful click to this method.
    [ContextMenu("Simulate: Sell Out Clicked")]
    public void ReportSellOutClicked() => CompleteIfCurrent(Phase.Sell);

    // Called by the starter plot when it becomes affordable. Keeping the plot
    // argument prevents a later/optional plot from advancing this tutorial.
    public void ReportFirstPlantUpgradeAffordable(Plot plot)
    {
        if (!IsStarterPlot(plot)) return;
        ReportFirstPlantUpgradeAffordable();
    }

    // TODO(gameplay): call when the first plant can be upgraded for the first time.
    [ContextMenu("Simulate: First Plant Upgrade Affordable")]
    public void ReportFirstPlantUpgradeAffordable()
    {
        if (!HasCompleted && _expectedPhase == Phase.Upgrade && !CurrentPhase.HasValue)
        {
            ShowExpectedPhase();
        }
    }

    // Called only after Plant.TryUpgrade succeeds. The plant argument ensures
    // that upgrades on optional plots never complete the starter tutorial.
    public void ReportFirstPlantUpgraded(Plant plant)
    {
        if (!IsStarterPlant(plant)) return;
        ReportFirstPlantUpgraded();
    }

    // TODO(gameplay): bind the real Upgrade button's successful click to this method.
    [ContextMenu("Simulate: First Plant Upgraded")]
    public void ReportFirstPlantUpgraded() => CompleteIfCurrent(Phase.Upgrade);

    // The upgrade icon is the tutorial's intended interaction target. Do not
    // hold phase 4 open until the later confirmation button is pressed.
    public void ReportFirstPlantUpgradeIconClicked(Plot plot)
    {
        if (!IsStarterPlot(plot)) return;
        CompleteIfCurrent(Phase.Upgrade);
    }

    /// <summary>
    /// Any upgrade icon is a valid acknowledgement of phase 4. The icon
    /// layer is shared by plots, sprinkler/harvester, and transport, so this
    /// keeps tutorial dismissal consistent for every upgrade target.
    /// </summary>
    public void ReportAnyUpgradeIconClicked()
    {
        CompleteIfCurrent(Phase.Upgrade);
    }

    private void CompleteIfCurrent(Phase expected)
    {
        if (CurrentPhase == expected) CompleteCurrentPhase();
    }

    private void CompleteAndWaitIfCurrent(Phase expected)
    {
        if (CurrentPhase != expected || _expectedPhase != expected) return;

        _expectedPhase = FindEnabledPhaseAtOrAfter((Phase)((int)expected + 1));
        if (!_expectedPhase.HasValue)
        {
            CompleteFlow();
            return;
        }

        HideCurrentPhase();
        // Harvest normally waits for cargo arrival before showing Sell. If a
        // scenario skips Sell, the next gate is Upgrade and may already be
        // ready, so it is safe to evaluate that one immediately.
        if (_expectedPhase == Phase.Upgrade)
        {
            ShowExpectedPhase();
        }
    }

    private void ShowExpectedPhase()
    {
        if (!_expectedPhase.HasValue || HasCompleted)
        {
            return;
        }

        if (_expectedPhase == Phase.Upgrade && !IsStarterPlantUpgradeAffordable())
        {
            return;
        }

        Show(_expectedPhase.Value);
    }

    private static bool IsStarterPlot(Plot plot)
    {
        if (plot == null || plot.PlotId != 0) return false;

        FarmPlotSystem plotSystem = FarmSystem.Instance != null ? FarmSystem.Instance.PlotSystem : null;
        return plotSystem == null || plotSystem.GetPlotComponent(0) == plot;
    }

    private static bool IsStarterPlant(Plant plant)
    {
        if (plant == null) return false;

        FarmPlotSystem plotSystem = FarmSystem.Instance != null ? FarmSystem.Instance.PlotSystem : null;
        if (plotSystem == null) return false;

        Plot starterPlot = plotSystem.GetPlotComponent(0);
        return starterPlot != null && starterPlot.CurrentPlant == plant;
    }

    private static bool IsStarterPlantUpgradeAffordable()
    {
        FarmPlotSystem plotSystem = FarmSystem.Instance != null ? FarmSystem.Instance.PlotSystem : null;
        Plot starterPlot = plotSystem != null ? plotSystem.GetPlotComponent(0) : null;
        return starterPlot != null && starterPlot.CanUpgradePlant();
    }

    private void HideCurrentPhase()
    {
        StopMotion();
        SetAllInactive();
        CurrentPhase = null;
        PhaseChanged?.Invoke(null);
    }

    private void CompleteFlow()
    {
        HideCurrentPhase();
        _expectedPhase = null;
        HasCompleted = true;
    }

    /// <summary>Starts a deliberately new playable run. Normal repeat harvest/sell cycles must not call this.</summary>
    public void ResetForNewRun()
    {
        HasCompleted = false;
        _expectedPhase = null;
        StartAt(startPhase);
    }

    private IEnumerator PlayMotion(PhaseView view)
    {
        if (view.Phase == Phase.Sprinklers && view.Ring != null)
        {
            yield return PulseRing(view.Ring);
            yield break;
        }

        if (view.Phase == Phase.Harvest && view.Hand != null)
        {
            // TODO(gameplay): replace Phase 1's UI ring with the tutorial plant's world-to-canvas position.
            PhaseView plantPhase;
            _views.TryGetValue(Phase.Sprinklers, out plantPhase);
            RectTransform target = plantPhase != null ? plantPhase.Ring : null;
            if (target != null) yield return TravelHand(view.Hand, target.position);
            yield break;
        }

        if ((view.Phase == Phase.Sell || view.Phase == Phase.Upgrade) && view.Hand != null)
        {
            yield return TapHand(view.Hand);
        }
    }

    private IEnumerator PulseRing(RectTransform ring)
    {
        Vector3 baseScale = ring.localScale;
        while (true)
        {
            float t = Mathf.PingPong(Time.unscaledTime / ringPulseDuration, 1f);
            ring.localScale = Vector3.Lerp(baseScale, baseScale * ringPulseScale, t);
            yield return null;
        }
    }

    private IEnumerator TravelHand(RectTransform hand, Vector3 destination)
    {
        Vector3 origin = hand.position;
        while (true)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / handTravelDuration)
            {
                hand.position = Vector3.Lerp(origin, destination, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            hand.position = destination;
            yield return new WaitForSecondsRealtime(0.2f);
            hand.position = origin;
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    private IEnumerator TapHand(RectTransform hand)
    {
        Vector3 origin = hand.localPosition;
        Vector3 pressed = origin + new Vector3(handTapDistance.x, -handTapDistance.y, 0f);
        Vector3 baseScale = hand.localScale;
        while (true)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / handTapDuration)
            {
                hand.localPosition = Vector3.Lerp(origin, pressed, t);
                hand.localScale = Vector3.Lerp(baseScale, baseScale * handTapScale, t);
                yield return null;
            }
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / handTapDuration)
            {
                hand.localPosition = Vector3.Lerp(pressed, origin, t);
                hand.localScale = Vector3.Lerp(baseScale * handTapScale, baseScale, t);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.15f);
        }
    }

    private void StopMotion()
    {
        if (_motion != null) StopCoroutine(_motion);
        _motion = null;
        foreach (PhaseView view in phaseViews) view?.ResetVisuals();
    }

    private void SetAllInactive()
    {
        foreach (PhaseView view in phaseViews)
        {
            if (view != null && view.Root != null) view.Root.SetActive(false);
        }
    }

    private Phase? FindEnabledPhaseAtOrAfter(Phase phase)
    {
        for (int index = (int)phase; index <= (int)Phase.Upgrade; index++)
        {
            Phase candidate = (Phase)index;
            if (!skippedPhases.Contains(candidate) && _views.ContainsKey(candidate)) return candidate;
        }
        return null;
    }

    private void CacheViews()
    {
        _views.Clear();
        foreach (PhaseView view in phaseViews)
        {
            if (view == null || view.Root == null) continue;
            view.ResolveVisuals();
            _views[view.Phase] = view;
        }
    }

    private void EnsurePhaseViews()
    {
        if (phaseViews.Count > 0) return;

        foreach (Phase phase in Enum.GetValues(typeof(Phase)))
        {
            Transform phaseRoot = transform.Find("Phase" + ((int)phase + 1));
            if (phaseRoot != null) phaseViews.Add(new PhaseView(phase, phaseRoot.gameObject));
        }
    }
}
