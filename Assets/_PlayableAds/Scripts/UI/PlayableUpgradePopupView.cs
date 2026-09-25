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
    [SerializeField] private TMP_Text statText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button dimButton;
    private bool _referencesResolved;

    public Image Illustration { get { ResolveMissingReferences(); return illustration; } }
    public TMP_Text TitleText { get { ResolveMissingReferences(); return titleText; } }
    public TMP_Text LevelText { get { ResolveMissingReferences(); return levelText; } }
    public TMP_Text StatText { get { ResolveMissingReferences(); return statText; } }
    public TMP_Text CostText { get { ResolveMissingReferences(); return costText; } }
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
            else if (statText == null && itemName.Contains("statinfo")) statText = texts[i];
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

        if (illustration == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
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
