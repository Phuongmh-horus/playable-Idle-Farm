using System;
using UnityEngine;
using UnityEngine.UI;

public class GuestOrderOfferLayer : WorldLayerBase
{
    [SerializeField] private Button btnOffer;

    private Action _onClickOfferCallback;

    protected override void Awake()
    {
        base.Awake();
        blockSelectionClick = false;
        if (btnOffer == null) btnOffer = GetComponentInChildren<Button>(true);

        if (btnOffer != null)
        {
            btnOffer.onClick.AddListener(OnOfferClicked);
        }
    }

    public void InitData(Action onClickOffer = null)
    {
        _onClickOfferCallback = onClickOffer;
    }

    private void OnOfferClicked()
    {
        _onClickOfferCallback?.Invoke();
    }
}
