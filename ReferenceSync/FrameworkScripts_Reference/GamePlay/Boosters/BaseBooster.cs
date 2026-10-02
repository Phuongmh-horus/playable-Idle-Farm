using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseBooster : MonoBehaviour
{
    public BoosterDataSO BoosterData;
    public Button        button;

    public                          GameObject        boosterLockGO;
    public                          GameObject        boosterGO;
    public                          TextMeshProUGUI   boosterLockText;
    protected virtual               void              Awake() { button?.onClick.AddListener(OnButtonClick); }

    public abstract void ChangeLockStateBooster();
    public abstract void OnButtonClick();
    public abstract void ExecuteBooster();
    public abstract void BuyBooster();

    protected abstract void OnBuyBoosterByCash();
    protected abstract void OnBuyBoosterByAds();
}
