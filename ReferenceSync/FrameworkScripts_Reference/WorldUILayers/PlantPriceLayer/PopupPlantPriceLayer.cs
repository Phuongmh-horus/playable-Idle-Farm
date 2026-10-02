using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupPlantPriceLayer : WorldLayerBase
{
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;

    private PlantPriceData _data;

    protected override void Awake()
    {
        base.Awake();
        EnsureUIElements();
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(BuyButtonOnClick);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEventBus.OnCashChange += OnCashChange;
        Reload();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEventBus.OnCashChange -= OnCashChange;
    }

    protected override void OnDestroy()
    {
        GameEventBus.OnCashChange -= OnCashChange;
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuyButtonOnClick);
        }
        base.OnDestroy();
    }

    private void EnsureUIElements()
    {
        if (buyButton == null)
        {
            buyButton = GetComponentInChildren<Button>(true);
        }
        if (priceText == null)
        {
            priceText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    public void InitData(PlantPriceData data)
    {
        _data = data;
        Reload();
    }

    /// <summary>
    /// Reload lại UI: cập nhật giá và kiểm tra trạng thái khả dụng của button mua
    /// </summary>
    public void Reload()
    {
        if (_data == null) return;

        if (priceText != null)
        {
            priceText.text = T_Utility.FormatCash(_data.Price);
        }

        if (buyButton != null)
        {
            bool canAfford = _data.Price <= 0 || DataManager.HasEnoughCash(_data.Price);
            buyButton.interactable = canAfford;
        }
    }

    private void OnCashChange()
    {
        Reload();
    }

    private void BuyButtonOnClick()
    {
        if (_data == null) return;

        if (_data.Price > 0 && !DataManager.HasEnoughCash(_data.Price))
        {
            Reload();
            return;
        }
        _data.OnBuyClicked.Invoke(_data);
        WorldLayerManager.Instance.Hide(this);
    }
}

public class PlantPriceData
{
    public int PlantId;
    public double Price;
    public Plot TargetPlot;
    public Action<PlantPriceData> OnBuyClicked;
}