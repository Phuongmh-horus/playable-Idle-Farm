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
    private PlayableUpgradePopupView _wiredView;
    private Func<int> _getLevel;
    private Func<int> _getMaxLevel;
    private Func<double> _getCost;
    private Func<bool> _canUpgrade;
    private Func<bool> _performUpgrade;
    private string _title;
    private Func<UpgradeStatRow[]> _getStatRows;
    private Sprite _illustrationSprite;

    public bool IsVisible { get { return _popupView != null && _popupView.gameObject.activeSelf; } }

    private void OnEnable() { GameEventBus.OnGoldChange += RefreshAffordability; }
    private void OnDisable() { GameEventBus.OnGoldChange -= RefreshAffordability; }

    public void Show(PlayableUpgradePopupView popupView, string title, Func<UpgradeStatRow[]> getStatRows, Sprite illustrationSprite,
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
        _getStatRows = getStatRows;
        _illustrationSprite = illustrationSprite;

        CanvasGroup canvasGroup = _popupView.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Pre-attached upgrade popup needs a CanvasGroup: " + _popupView.name);
            ClearData();
            return;
        }

        _popupView.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Image illustration = _popupView.Illustration;
        if (illustration != null && _illustrationSprite != null)
        {
            illustration.sprite = _illustrationSprite;
            illustration.preserveAspect = true;
        }

        WireButtonsOnce(_popupView);
        RefreshContent();
    }

    public void Hide()
    {
        if (_popupView != null)
        {
            _popupView.gameObject.SetActive(false);
        }
        ClearData();
    }

    private void ClearData()
    {
        _popupView = null;
        _getLevel = null;
        _getMaxLevel = null;
        _getCost = null;
        _canUpgrade = null;
        _performUpgrade = null;
        _title = null;
        _getStatRows = null;
        _illustrationSprite = null;
    }

    private void WireButtonsOnce(PlayableUpgradePopupView view)
    {
        if (_wiredView == view) return;
        _wiredView = view;

        Button closeButton = view.CloseButton;
        Button dimButton = view.DimButton;
        Button upgradeButton = view.UpgradeButton;
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (dimButton != null) dimButton.onClick.AddListener(Hide);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(Upgrade);
    }

    private void Upgrade()
    {
        if (_canUpgrade == null || _performUpgrade == null || !_canUpgrade())
        {
            RefreshAffordability();
            return;
        }
        _performUpgrade();
        RefreshContent();
    }

    public void Refresh()
    {
        RefreshContent();
    }

    /// <summary>
    /// Cập nhật đầy đủ nội dung (Text, Stats, Cost) khi mở popup hoặc khi nâng cấp thành công.
    /// </summary>
    private void RefreshContent()
    {
        if (_popupView == null) return;
        int level = _getLevel != null ? _getLevel() : 0;
        int maxLevel = _getMaxLevel != null ? Mathf.Max(1, _getMaxLevel()) : Mathf.Max(1, level);
        level = Mathf.Clamp(level, 1, maxLevel);
        double cost = _getCost != null ? _getCost() : -1d;
        TMP_Text titleText = _popupView.TitleText;
        TMP_Text levelText = _popupView.LevelText;
        TMP_Text costText = _popupView.CostText;
        TMP_Text costForegroundText = _popupView.CostForegroundText;
        bool isMaxLevel = level >= maxLevel;

        if (titleText != null) titleText.text = _title;
        if (levelText != null) levelText.text = maxLevel > 0 ? "LV. " + level + "/" + maxLevel : "LV. " + level;
        UpgradeStatRow[] statRows = _getStatRows != null ? _getStatRows() : null;
        ApplyStatRow(0, statRows != null && statRows.Length > 0 ? statRows[0] : default(UpgradeStatRow), isMaxLevel);
        ApplyStatRow(1, statRows != null && statRows.Length > 1 ? statRows[1] : default(UpgradeStatRow), isMaxLevel);
        string costValue = isMaxLevel || cost <= 0d ? "MAX" : T_Utility.FormatGold(cost);
        if (costText != null) costText.text = costValue;
        if (costForegroundText != null) costForegroundText.text = costValue;

        RefreshAffordability();
    }

    /// <summary>
    /// Chỉ cập nhật trạng thái đủ tiền của nút bấm khi vàng thay đổi, không cấp phát chuỗi hay tính toán lại stat.
    /// </summary>
    private void RefreshAffordability()
    {
        if (_popupView == null) return;
        bool canUpgrade = _canUpgrade != null && _canUpgrade();
        Button upgradeButton = _popupView.UpgradeButton;
        if (upgradeButton != null) upgradeButton.interactable = canUpgrade;
        Image currencyIcon = _popupView.UpgradeCurrencyIcon;
        if (currencyIcon != null)
        {
            Color iconColor = currencyIcon.color;
            iconColor.a = canUpgrade ? 1f : 0.35f;
            currencyIcon.color = iconColor;
        }
    }

    private void ApplyStatRow(int rowIndex, UpgradeStatRow row, bool isMaxLevel)
    {
        bool visible = !string.IsNullOrEmpty(row.Label) || !string.IsNullOrEmpty(row.Current) || !string.IsNullOrEmpty(row.Next);
        GameObject root = rowIndex == 0 ? null : _popupView.StatTwoRoot;
        if (root != null) root.SetActive(visible);

        TMP_Text nameText = rowIndex == 0 ? _popupView.StatOneNameText : _popupView.StatTwoNameText;
        TMP_Text current = rowIndex == 0 ? _popupView.StatOneCurrentText : _popupView.StatTwoCurrentText;
        TMP_Text next = rowIndex == 0 ? _popupView.StatOneNextText : _popupView.StatTwoNextText;
        Image arrow = rowIndex == 0 ? _popupView.StatOneArrowImage : _popupView.StatTwoArrowImage;
        if (nameText != null) nameText.text = row.Label ?? string.Empty;
        if (current != null) current.text = isMaxLevel && visible ? "" : row.Current ?? string.Empty;
        if (next != null) next.text = isMaxLevel && visible ? "MAX" : row.Next ?? string.Empty;
        if (arrow != null) arrow.gameObject.SetActive(!isMaxLevel && visible && !string.IsNullOrEmpty(row.Next));
    }
}

public struct UpgradeStatRow
{
    public string Label;
    public string Current;
    public string Next;

    public UpgradeStatRow(string label, string current, string next)
    {
        Label = label;
        Current = current;
        Next = next;
    }
}
