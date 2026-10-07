using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stable view binding for the imported upgrade artwork. Assign fields in the
/// inspector when an artwork changes; ResolveMissingReferences keeps the
/// current imported prefabs working until then.
/// </summary>
public sealed class PlayableUpgradePopupView : MonoBehaviour
{
    [SerializeField] private Image illustration;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text statOneNameText;
    [SerializeField] private TMP_Text statOneCurrentText;
    [SerializeField] private Image statOneArrowImage;
    [SerializeField] private TMP_Text statOneNextText;
    [SerializeField] private GameObject statTwoRoot;
    [SerializeField] private TMP_Text statTwoNameText;
    [SerializeField] private TMP_Text statTwoCurrentText;
    [SerializeField] private Image statTwoArrowImage;
    [SerializeField] private TMP_Text statTwoNextText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text costForegroundText;
    [SerializeField] private Image upgradeCurrencyIcon;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button dimButton;
    private bool _referencesResolved;

    public Image Illustration { get { ResolveMissingReferences(); return illustration; } }
    public TMP_Text TitleText { get { ResolveMissingReferences(); return titleText; } }
    public TMP_Text LevelText { get { ResolveMissingReferences(); return levelText; } }
    public TMP_Text StatOneNameText { get { ResolveMissingReferences(); return statOneNameText; } }
    public TMP_Text StatOneCurrentText { get { ResolveMissingReferences(); return statOneCurrentText; } }
    public Image StatOneArrowImage { get { ResolveMissingReferences(); return statOneArrowImage; } }
    public TMP_Text StatOneNextText { get { ResolveMissingReferences(); return statOneNextText; } }
    public GameObject StatTwoRoot { get { ResolveMissingReferences(); return statTwoRoot; } }
    public TMP_Text StatTwoNameText { get { ResolveMissingReferences(); return statTwoNameText; } }
    public TMP_Text StatTwoCurrentText { get { ResolveMissingReferences(); return statTwoCurrentText; } }
    public Image StatTwoArrowImage { get { ResolveMissingReferences(); return statTwoArrowImage; } }
    public TMP_Text StatTwoNextText { get { ResolveMissingReferences(); return statTwoNextText; } }
    public TMP_Text CostText { get { ResolveMissingReferences(); return costText; } }
    public TMP_Text CostForegroundText { get { ResolveMissingReferences(); return costForegroundText; } }
    public Image UpgradeCurrencyIcon { get { ResolveMissingReferences(); return upgradeCurrencyIcon; } }
    public Button UpgradeButton { get { ResolveMissingReferences(); return upgradeButton; } }
    public Button CloseButton { get { ResolveMissingReferences(); return closeButton; } }
    public Button DimButton { get { ResolveMissingReferences(); return dimButton; } }

    public void ResolveMissingReferences()
    {
        if (_referencesResolved) return;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            string itemName = texts[i].gameObject.name.ToLowerInvariant();
            if (titleText == null && itemName.Contains("titletext")) titleText = texts[i];
            else if (levelText == null && itemName.Contains("leveltext")) levelText = texts[i];
            else if (statOneNameText == null && itemName.Contains("stat1name")) statOneNameText = texts[i];
            else if (statOneCurrentText == null && itemName.Contains("stat1current")) statOneCurrentText = texts[i];
            else if (statOneNextText == null && itemName.Contains("stat1next")) statOneNextText = texts[i];
            else if (statTwoNameText == null && itemName.Contains("stat2name")) statTwoNameText = texts[i];
            else if (statTwoCurrentText == null && itemName.Contains("stat2current")) statTwoCurrentText = texts[i];
            else if (statTwoNextText == null && itemName.Contains("stat2next")) statTwoNextText = texts[i];
            else if (costText == null && itemName.Contains("costtext")) costText = texts[i];
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string itemName = buttons[i].gameObject.name.ToLowerInvariant();
            if (closeButton == null && itemName.Contains("close")) closeButton = buttons[i];
            else if (dimButton == null && itemName.Contains("dim")) dimButton = buttons[i];
            else if (upgradeButton == null && itemName.Contains("upgrade")) upgradeButton = buttons[i];
        }

        if (upgradeButton == null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != closeButton && buttons[i] != dimButton)
                {
                    upgradeButton = buttons[i];
                    break;
                }
            }
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            string itemName = images[i].gameObject.name.ToLowerInvariant();
            if (statOneArrowImage == null && itemName.Contains("stat1arrow")) statOneArrowImage = images[i];
            else if (statTwoArrowImage == null && itemName.Contains("stat2arrow")) statTwoArrowImage = images[i];
            else if (upgradeCurrencyIcon == null && upgradeButton != null &&
                images[i].transform.IsChildOf(upgradeButton.transform) &&
                (itemName.Contains("cash") || itemName.Contains("currency")))
            {
                upgradeCurrencyIcon = images[i];
            }
        }

        if (statTwoRoot == null)
        {
            Transform statTwoTransform = transform.Find("Card/Inner/StatsRoot/Stat2Root");
            if (statTwoTransform != null) statTwoRoot = statTwoTransform.gameObject;
        }

        if (costForegroundText == null && costText != null)
        {
            TMP_Text[] costTexts = costText.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < costTexts.Length; i++)
            {
                if (costTexts[i] != costText)
                {
                    costForegroundText = costTexts[i];
                    break;
                }
            }
        }

        if (illustration == null)
        {
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject.name == "Image")
                {
                    illustration = images[i];
                    break;
                }
            }
        }

        _referencesResolved = true;
    }
}
