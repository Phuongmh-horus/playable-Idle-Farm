using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls one pre-attached upgrade popup. Popup artwork is placed in the
/// scene during authoring and is only activated/deactivated at runtime.
/// </summary>
public sealed class PlayableUpgradePopupPresenter : MonoBehaviour
{
    private PlayableUpgradePopupView _popupView;
    private CanvasGroup _canvasGroup;
    private Func<int> _getLevel;
    private Func<int> _getMaxLevel;
    private Func<double> _getCost;
    private Func<bool> _canUpgrade;
    private Func<bool> _performUpgrade;
    private string _title;
    private Func<string> _getStatDescription;
    private Func<Sprite> _getIllustration;

    public bool IsVisible { get { return _popupView != null && _popupView.gameObject.activeSelf; } }

    private void OnEnable() { GameEventBus.OnGoldChange += Refresh; }
    private void OnDisable() { GameEventBus.OnGoldChange -= Refresh; }

    public void Show(PlayableUpgradePopupView popupView, string title, Func<string> getStatDescription, Func<Sprite> getIllustration,
        Func<int> getLevel, Func<int> getMaxLevel, Func<double> getCost, Func<bool> canUpgrade, Func<bool> performUpgrade)
    {
        Hide();
        if (popupView == null)
        {
            Debug.LogError("Playable upgrade popup is not pre-attached to the gameplay canvas.");
            return;
        }

        _popupView = popupView;
        _getLevel = getLevel;
        _getMaxLevel = getMaxLevel;
        _getCost = getCost;
        _canUpgrade = canUpgrade;
        _performUpgrade = performUpgrade;
        _title = title;
        _getStatDescription = getStatDescription;
        _getIllustration = getIllustration;
        _popupView.ResolveMissingReferences();
        _canvasGroup = _popupView.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            Debug.LogError("Pre-attached upgrade popup needs a CanvasGroup: " + _popupView.name);
            ClearData();
            return;
        }

        _popupView.gameObject.SetActive(true);
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        WireButtons();
        Refresh();
    }

    public void Hide()
    {
        if (_popupView != null)
        {
            Button closeButton = _popupView.CloseButton;
            Button dimButton = _popupView.DimButton;
            Button upgradeButton = _popupView.UpgradeButton;
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
            if (dimButton != null) dimButton.onClick.RemoveListener(Hide);
            if (upgradeButton != null) upgradeButton.onClick.RemoveListener(Upgrade);
            _popupView.gameObject.SetActive(false);
        }
        ClearData();
    }

    private void ClearData()
    {
        _popupView = null;
        _canvasGroup = null;
        _getLevel = null;
        _getMaxLevel = null;
        _getCost = null;
        _canUpgrade = null;
        _performUpgrade = null;
        _title = null;
        _getStatDescription = null;
        _getIllustration = null;
    }

    private void WireButtons()
    {
        Button closeButton = _popupView.CloseButton;
        Button dimButton = _popupView.DimButton;
        Button upgradeButton = _popupView.UpgradeButton;
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (dimButton != null) dimButton.onClick.AddListener(Hide);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(Upgrade);
    }

    private void Upgrade()
    {
        if (_canUpgrade == null || _performUpgrade == null || !_canUpgrade())
        {
            Refresh();
            return;
        }
        _performUpgrade();
        Refresh();
    }

    private void Refresh()
    {
        if (_popupView == null) return;
        int level = _getLevel != null ? _getLevel() : 0;
        int maxLevel = _getMaxLevel != null ? Mathf.Max(1, _getMaxLevel()) : Mathf.Max(1, level);
        level = Mathf.Clamp(level, 1, maxLevel);
        double cost = _getCost != null ? _getCost() : -1d;
        TMP_Text titleText = _popupView.TitleText;
        TMP_Text levelText = _popupView.LevelText;
        TMP_Text statText = _popupView.StatText;
        TMP_Text costText = _popupView.CostText;
        Image illustration = _popupView.Illustration;
        Button upgradeButton = _popupView.UpgradeButton;

        if (titleText != null) titleText.text = _title;
        if (levelText != null) levelText.text = maxLevel > 0 ? "LV. " + level + "/" + maxLevel : "LV. " + level;
        if (statText != null) statText.text = _getStatDescription != null ? _getStatDescription() : string.Empty;
        if (costText != null) costText.text = cost > 0d ? T_Utility.FormatGold(cost) : "MAX";
        Sprite illustrationSprite = _getIllustration != null ? _getIllustration() : null;
        if (illustration != null && illustrationSprite != null)
        {
            illustration.sprite = illustrationSprite;
            illustration.preserveAspect = true;
        }
        if (upgradeButton != null) upgradeButton.interactable = _canUpgrade != null && _canUpgrade();
    }
}
