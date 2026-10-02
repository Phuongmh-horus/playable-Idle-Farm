using System;
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
        GameEventBus.OnGoldChange += OnGoldChange;
        Reload();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEventBus.OnGoldChange -= OnGoldChange;
    }

    protected override void OnDestroy()
    {
        GameEventBus.OnGoldChange -= OnGoldChange;
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
    /// Reload láº¡i UI: cáº­p nháº­t giÃ¡ vÃ  kiá»ƒm tra tráº¡ng thÃ¡i kháº£ dá»¥ng cá»§a button mua
    /// </summary>
    public void Reload()
    {
        if (_data == null) return;

        if (priceText != null)
        {
            priceText.text = T_Utility.FormatGold(_data.Price);
        }

        if (buyButton != null)
        {
            bool canAfford = _data.Price <= 0 || PlayableFarmState.HasEnoughGold(_data.Price);
            buyButton.interactable = canAfford;
        }
    }

    private void OnGoldChange()
    {
        Reload();
    }

    private void BuyButtonOnClick()
    {
        if (_data == null) return;

        if (_data.Price > 0 && !PlayableFarmState.HasEnoughGold(_data.Price))
        {
            Reload();
            return;
        }

        if (_data.OnBuyClicked != null)
        {
            _data.OnBuyClicked.Invoke(_data);
        }
        else if (_data.TargetPlot != null)
        {
            _data.TargetPlot.PurchasePlot();
        }
    }

    public override void CleanupBeforeHide()
    {
        _data = null;
        base.CleanupBeforeHide();
    }
}

public class PlantPriceData
{
    public int PlantId;
    public double Price;
    public Plot TargetPlot;
    public Action<PlantPriceData> OnBuyClicked;
}
