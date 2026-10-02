using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuestOrderSellLayer : WorldLayerBase
{
    [SerializeField] private Button btnSell;
    [SerializeField] private TextMeshProUGUI txtGemReward;

    private Action _onClickSellCallback;

    protected override void Awake()
    {
        base.Awake();
        blockSelectionClick = false;

        if (btnSell != null)
        {
            btnSell.onClick.AddListener(OnSellClicked);
        }
    }

    public void InitData(int gemReward = 0, Action onClickSell = null)
    {
        _onClickSellCallback = onClickSell;
        if (txtGemReward != null)
        {
            txtGemReward.text = gemReward > 0 ? $"{gemReward} Gem" : string.Empty;
        }
    }

    private void OnSellClicked()
    {
        _onClickSellCallback?.Invoke();
    }
}
