using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inspector-bound view for the imported upgrade artwork.
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
    public Image Illustration { get { return illustration; } }
    public TMP_Text TitleText { get { return titleText; } }
    public TMP_Text LevelText { get { return levelText; } }
    public TMP_Text StatOneNameText { get { return statOneNameText; } }
    public TMP_Text StatOneCurrentText { get { return statOneCurrentText; } }
    public Image StatOneArrowImage { get { return statOneArrowImage; } }
    public TMP_Text StatOneNextText { get { return statOneNextText; } }
    public GameObject StatTwoRoot { get { return statTwoRoot; } }
    public TMP_Text StatTwoNameText { get { return statTwoNameText; } }
    public TMP_Text StatTwoCurrentText { get { return statTwoCurrentText; } }
    public Image StatTwoArrowImage { get { return statTwoArrowImage; } }
    public TMP_Text StatTwoNextText { get { return statTwoNextText; } }
    public TMP_Text CostText { get { return costText; } }
    public TMP_Text CostForegroundText { get { return costForegroundText; } }
    public Image UpgradeCurrencyIcon { get { return upgradeCurrencyIcon; } }
    public Button UpgradeButton { get { return upgradeButton; } }
    public Button CloseButton { get { return closeButton; } }
    public Button DimButton { get { return dimButton; } }
}
