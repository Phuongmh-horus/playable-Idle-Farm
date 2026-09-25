using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupBuyPlotLayer : WorldLayerBase
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;

    private Plot _currentPlot;
    private BuyPlotData _data;

    public event Action<Plot> OnUnlockSuccess;

    protected override void Awake()
    {
        base.Awake();
        AutoFindUIElements();

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(BuyButtonOnClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    protected override void OnDestroy()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuyButtonOnClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }

        base.OnDestroy();
    }

    private void AutoFindUIElements()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string bName = btn.gameObject.name.ToLower();
            var tmp = btn.GetComponentInChildren<TMP_Text>(true);
            string tText = tmp != null ? tmp.text.ToLower() : "";

            if (closeButton == null && (bName.Contains("close") || tText.Contains("close") || bName.Contains("exit")))
            {
                closeButton = btn;
            }
            else if (buyButton == null && (bName.Contains("buy") || bName.Contains("unlock") || bName.Contains("button") || tText.Contains("buy") || tText.Contains("$")))
            {
                buyButton = btn;
                if (priceText == null && tmp != null)
                {
                    priceText = tmp;
                }
            }
        }

        if (priceText == null)
        {
            priceText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void InitData(Plot plot)
    {
        _currentPlot = plot;
        _data = null;
        RefreshUI();
    }

    public void InitData(BuyPlotData data)
    {
        _data = data;
        _currentPlot = data != null ? data.TargetPlot : null;
        RefreshUI();
    }

    public void RefreshUI()
    {
        double cost = GetCost();
        if (priceText != null)
        {
            priceText.text = cost > 0 ? $"${T_Utility.FormatGold(cost)}" : "FREE";
        }

        if (buyButton != null)
        {
            buyButton.interactable = cost <= 0 || PlayableFarmState.HasEnoughGold(cost);
        }
    }

    private double GetCost()
    {
        if (_data != null && _data.Cost >= 0)
        {
            return _data.Cost;
        }

        if (_currentPlot != null && _currentPlot.PlotData != null && _currentPlot.PlotData.Config != null)
        {
            return _currentPlot.PlotData.Config.unlockCost;
        }

        return 10;
    }

    public void BuyButtonOnClick()
    {
        if (_data != null && _data.OnBuyClicked != null)
        {
            _data.OnBuyClicked.Invoke(_data);
            Close();
            return;
        }

        if (_currentPlot == null || _currentPlot.State != FarmPlotState.Locked)
        {
            Close();
            return;
        }

        double cost = GetCost();
        if (cost > 0)
        {
            if (!PlayableFarmState.ConsumeGold(cost))
            {
                Debug.LogWarning($"[PopupBuyPlotLayer] Not enough gold to unlock plot {_currentPlot.PlotId}!");
                return;
            }
        }

        bool success = _currentPlot.UnlockPlot();
        if (success)
        {
            PlayableFarmState.UpdatePlotData(_currentPlot.PlotId, PlotState.Empty, -1, 0);
            Plot unlockedPlot = _currentPlot;
            OnUnlockSuccess?.Invoke(unlockedPlot);
        }

        Close();
    }

    public void Close()
    {
        WorldLayerManager.Instance.Hide(this);
    }
}

public class BuyPlotData
{
    public Plot TargetPlot;
    public double Cost;
    public Action<BuyPlotData> OnBuyClicked;
}
