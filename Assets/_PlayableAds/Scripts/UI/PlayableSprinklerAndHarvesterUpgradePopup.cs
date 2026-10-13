using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PlayableUpgradePopupTab
{
    Sprinkler = 0,
    Harvester = 1
}

/// <summary>
/// Luna-safe controller for the inspector-authored hybrid upgrade popup.
/// </summary>
public sealed class PlayableSprinklerAndHarvesterUpgradePopup : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private Toggle sprinklerTab;
    [SerializeField] private Toggle harvesterTab;
    [SerializeField] private Image sprinklerTabBackground;
    [SerializeField] private Image harvesterTabBackground;
    [SerializeField] private Sprite selectedTabSprite;
    [SerializeField] private Sprite unselectedTabSprite;

    [Header("Panels")]
    [SerializeField] private CanvasGroup sprinklerPanel;
    [SerializeField] private CanvasGroup harvesterPanel;

    [Header("Sprinkler panel")]
    [SerializeField] private Image sprinklerIcon;
    [SerializeField] private TMP_Text sprinklerLevelText;
    [SerializeField] private TMP_Text sprinklerStatOneName;
    [SerializeField] private TMP_Text sprinklerStatOneCurrent;
    [SerializeField] private TMP_Text sprinklerStatOneNext;
    [SerializeField] private Image sprinklerStatOneArrow;
    [SerializeField] private TMP_Text sprinklerStatTwoName;
    [SerializeField] private TMP_Text sprinklerStatTwoCurrent;
    [SerializeField] private TMP_Text sprinklerStatTwoNext;
    [SerializeField] private Image sprinklerStatTwoArrow;
    [SerializeField] private Button sprinklerUpgradeButton;
    [SerializeField] private TMP_Text sprinklerCostText;
    [SerializeField] private Image sprinklerCurrencyIcon;

    [Header("Harvester panel")]
    [SerializeField] private Image harvesterIcon;
    [SerializeField] private TMP_Text harvesterLevelText;
    [SerializeField] private TMP_Text harvesterStatOneName;
    [SerializeField] private TMP_Text harvesterStatOneCurrent;
    [SerializeField] private TMP_Text harvesterStatOneNext;
    [SerializeField] private Image harvesterStatOneArrow;
    [SerializeField] private TMP_Text harvesterStatTwoName;
    [SerializeField] private TMP_Text harvesterStatTwoCurrent;
    [SerializeField] private TMP_Text harvesterStatTwoNext;
    [SerializeField] private Image harvesterStatTwoArrow;
    [SerializeField] private Button harvesterUpgradeButton;
    [SerializeField] private TMP_Text harvesterCostText;
    [SerializeField] private Image harvesterCurrencyIcon;

    [Header("Common")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button dimButton;

    private CanvasGroup _rootCanvasGroup;
    private FarmIrrigationSystem _irrigation;
    private FarmHarvestSystem _harvest;
    private Sprite _configuredSprinklerIcon;
    private Sprite _configuredHarvesterIcon;
    private PlayableUpgradePopupTab _selectedTab = PlayableUpgradePopupTab.Sprinkler;
    private bool _listenersWired;

    public bool IsVisible { get { return gameObject.activeSelf; } }
    public PlayableUpgradePopupTab SelectedTab { get { return _selectedTab; } }

    private void Awake()
    {
        _rootCanvasGroup = GetComponent<CanvasGroup>();
        if (sprinklerIcon != null)
        {
            _configuredSprinklerIcon = sprinklerIcon.sprite;
            sprinklerIcon.preserveAspect = true;
        }
        if (harvesterIcon != null)
        {
            _configuredHarvesterIcon = harvesterIcon.sprite;
            harvesterIcon.preserveAspect = true;
        }
        SetRootVisible(false);
        SetPanelVisible(sprinklerPanel, false);
        SetPanelVisible(harvesterPanel, false);
    }

    private void OnEnable()
    {
        WireListeners();
        SubscribeSystems();
        Refresh();
    }

    private void OnDisable()
    {
        UnwireListeners();
        UnsubscribeSystems();
    }

    public void Show(PlayableUpgradePopupTab tab)
    {
        _irrigation = GetIrrigation();
        _harvest = GetHarvest();
        if (tab == PlayableUpgradePopupTab.Harvester && !IsHarvesterAvailable()) tab = PlayableUpgradePopupTab.Sprinkler;
        if (tab == PlayableUpgradePopupTab.Sprinkler && _irrigation == null && IsHarvesterAvailable()) tab = PlayableUpgradePopupTab.Harvester;

        _selectedTab = tab;
        gameObject.SetActive(true);
        SetRootVisible(true);
        SelectTab(tab);
    }

    public void Hide()
    {
        SetRootVisible(false);
        gameObject.SetActive(false);
    }

    private void WireListeners()
    {
        if (_listenersWired) return;
        sprinklerTab.onValueChanged.AddListener(OnSprinklerTabChanged);
        harvesterTab.onValueChanged.AddListener(OnHarvesterTabChanged);
        sprinklerUpgradeButton.onClick.AddListener(UpgradeSprinkler);
        harvesterUpgradeButton.onClick.AddListener(UpgradeHarvester);
        closeButton.onClick.AddListener(Hide);
        dimButton.onClick.AddListener(Hide);
        _listenersWired = true;
    }

    private void UnwireListeners()
    {
        if (!_listenersWired) return;
        sprinklerTab.onValueChanged.RemoveListener(OnSprinklerTabChanged);
        harvesterTab.onValueChanged.RemoveListener(OnHarvesterTabChanged);
        sprinklerUpgradeButton.onClick.RemoveListener(UpgradeSprinkler);
        harvesterUpgradeButton.onClick.RemoveListener(UpgradeHarvester);
        closeButton.onClick.RemoveListener(Hide);
        dimButton.onClick.RemoveListener(Hide);
        _listenersWired = false;
    }

    private void SubscribeSystems()
    {
        _irrigation = GetIrrigation();
        _harvest = GetHarvest();
        GameEventBus.OnGoldChange += RefreshAffordability;
        if (_irrigation != null) _irrigation.OnSprinklerLevelChanged += OnSystemLevelChanged;
        if (_harvest != null) _harvest.OnHarvesterLevelChanged += OnSystemLevelChanged;
    }

    private void UnsubscribeSystems()
    {
        GameEventBus.OnGoldChange -= RefreshAffordability;
        if (_irrigation != null) _irrigation.OnSprinklerLevelChanged -= OnSystemLevelChanged;
        if (_harvest != null) _harvest.OnHarvesterLevelChanged -= OnSystemLevelChanged;
    }

    private void OnSystemLevelChanged(int level) { Refresh(); }
    private void OnSprinklerTabChanged(bool selected) { if (selected) SelectTab(PlayableUpgradePopupTab.Sprinkler); }
    private void OnHarvesterTabChanged(bool selected) { if (selected) SelectTab(PlayableUpgradePopupTab.Harvester); }

    private void SelectTab(PlayableUpgradePopupTab tab)
    {
        _selectedTab = tab;
        sprinklerTab.SetIsOnWithoutNotify(tab == PlayableUpgradePopupTab.Sprinkler);
        harvesterTab.SetIsOnWithoutNotify(tab == PlayableUpgradePopupTab.Harvester);
        Refresh();
    }

    public void Refresh()
    {
        _irrigation = GetIrrigation();
        _harvest = GetHarvest();
        bool sprinklerAvailable = _irrigation != null;
        bool harvesterAvailable = IsHarvesterAvailable();
        sprinklerTab.gameObject.SetActive(sprinklerAvailable);
        harvesterTab.gameObject.SetActive(harvesterAvailable);

        if (!sprinklerAvailable && harvesterAvailable) _selectedTab = PlayableUpgradePopupTab.Harvester;
        else if (_selectedTab == PlayableUpgradePopupTab.Harvester && !harvesterAvailable) _selectedTab = PlayableUpgradePopupTab.Sprinkler;

        bool sprinklerSelected = _selectedTab == PlayableUpgradePopupTab.Sprinkler;
        sprinklerTabBackground.sprite = sprinklerSelected ? selectedTabSprite : unselectedTabSprite;
        harvesterTabBackground.sprite = sprinklerSelected ? unselectedTabSprite : selectedTabSprite;
        SetPanelVisible(sprinklerPanel, sprinklerSelected && sprinklerAvailable);
        SetPanelVisible(harvesterPanel, !sprinklerSelected && harvesterAvailable);
        if (sprinklerAvailable) RefreshSprinkler();
        if (harvesterAvailable) RefreshHarvester();
        RefreshAffordability();
    }

    private void RefreshSprinkler()
    {
        FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        SprinklerConfig config = farmConfig != null ? farmConfig.sprinkler : null;
        int level = _irrigation.Level;
        int maxLevel = _irrigation.MaxLevel;
        int nextLevel = Mathf.Min(level + 1, maxLevel);
        bool isMax = level >= maxLevel;
        SetText(sprinklerLevelText, "LV. " + level + "/" + maxLevel);
        SetText(sprinklerStatOneName, "WATER");
        SetText(sprinklerStatOneCurrent, config != null ? config.GetWaterAmount(level).ToString("0.##") : "-");
        SetText(sprinklerStatOneNext, isMax ? "MAX" : config != null ? config.GetWaterAmount(nextLevel).ToString("0.##") : "-");
        SetText(sprinklerStatTwoName, "Arms");
        SetText(sprinklerStatTwoCurrent, config != null ? config.GetNozzleCount(level).ToString() : "-");
        SetText(sprinklerStatTwoNext, isMax ? "MAX" : config != null ? config.GetNozzleCount(nextLevel).ToString() : "-");
        SetStatArrow(sprinklerStatOneArrow, !isMax);
        SetStatArrow(sprinklerStatTwoArrow, !isMax);
        SetText(sprinklerCostText, isMax ? "MAX" : T_Utility.FormatGold(_irrigation.NextUpgradeCost));
    }

    private void RefreshHarvester()
    {
        FarmConfig farmConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmConfig : null;
        HarvesterConfig config = farmConfig != null ? farmConfig.harvester : null;
        int level = _harvest.Level;
        int maxLevel = _harvest.MaxLevel;
        int nextLevel = Mathf.Min(level + 1, maxLevel);
        bool isMax = level >= maxLevel;
        SetText(harvesterLevelText, "LV. " + level + "/" + maxLevel);
        SetText(harvesterStatOneName, "DURATION");
        SetText(harvesterStatOneCurrent, config != null ? config.GetRotationDuration(level).ToString("0.##") + "s" : "-");
        SetText(harvesterStatOneNext, isMax ? "MAX" : config != null ? config.GetRotationDuration(nextLevel).ToString("0.##") + "s" : "-");
        SetText(harvesterStatTwoName, "Arms");
        SetText(harvesterStatTwoCurrent, config != null ? config.GetHarvesterCount(level).ToString() : "-");
        SetText(harvesterStatTwoNext, isMax ? "MAX" : config != null ? config.GetHarvesterCount(nextLevel).ToString() : "-");
        SetStatArrow(harvesterStatOneArrow, !isMax);
        SetStatArrow(harvesterStatTwoArrow, !isMax);
        SetText(harvesterCostText, isMax ? "MAX" : T_Utility.FormatGold(_harvest.NextUpgradeCost));
    }

    /// <summary>
    /// Chỉ cập nhật tương tác của nút Upgrade và làm mờ cash icon khi không đủ tiền.
    /// </summary>
    private void RefreshAffordability()
    {
        if (_irrigation != null && sprinklerUpgradeButton != null)
        {
            bool canUpgrade = _irrigation.CanUpgrade;
            sprinklerUpgradeButton.interactable = canUpgrade;
            if (sprinklerCurrencyIcon != null)
            {
                Color c = sprinklerCurrencyIcon.color;
                float targetAlpha = canUpgrade ? 1f : 0.35f;
                if (Mathf.Abs(c.a - targetAlpha) > 0.01f)
                {
                    c.a = targetAlpha;
                    sprinklerCurrencyIcon.color = c;
                }
            }
        }
        if (_harvest != null && harvesterUpgradeButton != null)
        {
            bool canUpgrade = _harvest.CanUpgrade;
            harvesterUpgradeButton.interactable = canUpgrade;
            if (harvesterCurrencyIcon != null)
            {
                Color c = harvesterCurrencyIcon.color;
                float targetAlpha = canUpgrade ? 1f : 0.35f;
                if (Mathf.Abs(c.a - targetAlpha) > 0.01f)
                {
                    c.a = targetAlpha;
                    harvesterCurrencyIcon.color = c;
                }
            }
        }
    }

    private void UpgradeSprinkler() { if (_irrigation != null) { _irrigation.TryUpgrade(); Refresh(); } }
    private void UpgradeHarvester() { if (_harvest != null) { _harvest.TryUpgrade(); Refresh(); } }
    private bool IsHarvesterAvailable() { return _harvest != null && _harvest.AutomaticArmEnabled; }
    private static FarmIrrigationSystem GetIrrigation() { return FarmSystem.Instance != null ? FarmSystem.Instance.IrrigationSystem : null; }
    private static FarmHarvestSystem GetHarvest() { return FarmSystem.Instance != null ? FarmSystem.Instance.HarvestSystem : null; }

    private void SetRootVisible(bool visible)
    {
        if (_rootCanvasGroup == null) _rootCanvasGroup = GetComponent<CanvasGroup>();
        if (_rootCanvasGroup != null)
        {
            _rootCanvasGroup.alpha = visible ? 1f : 0f;
            _rootCanvasGroup.interactable = visible;
            _rootCanvasGroup.blocksRaycasts = visible;
        }
    }

    private static void SetPanelVisible(CanvasGroup panel, bool visible)
    {
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null && text.text != value)
        {
            text.text = value;
        }
    }

    private static void SetStatArrow(Image arrow, bool visible)
    {
        if (arrow != null && arrow.gameObject.activeSelf != visible)
        {
            arrow.gameObject.SetActive(visible);
        }
    }
}

