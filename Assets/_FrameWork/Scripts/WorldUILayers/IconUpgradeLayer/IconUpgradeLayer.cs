using System;
using UnityEngine;
using UnityEngine.UI;

public class IconUpgradeLayer : WorldLayerBase
{
    [SerializeField] private Button upgradeButton;

    private Action _onClickAction;

    protected override void Reset()
    {
        base.Reset();
        layerDepth = WorldLayerDepth.StatusIcon;
        EnsureUIElements();
        ApplySorting();
    }

    protected override void Awake()
    {
        if (layerDepth == WorldLayerDepth.Popup)
        {
            layerDepth = WorldLayerDepth.StatusIcon;
        }
        base.Awake();
        EnsureUIElements();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(HandleButtonClicked);
        }
    }

    protected override void OnDestroy()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(HandleButtonClicked);
        }
        base.OnDestroy();
    }

    private void EnsureUIElements()
    {
        if (upgradeButton == null)
        {
            upgradeButton = GetComponentInChildren<Button>(true);
        }
    }

    public override void SetupFollow(Transform target, Vector3 offset = default(UnityEngine.Vector3))
    {
        base.SetupFollow(target, offset);
    }

    public void InitData(Action onClick = null)
    {
        _onClickAction = onClick;
    }

    private void HandleButtonClicked()
    {
        PlayableTutorialFlow.Instance?.ReportAnyUpgradeIconClicked();
        _onClickAction?.Invoke();
    }

    public override void CleanupBeforeHide()
    {
        _onClickAction = null;
        base.CleanupBeforeHide();
    }
}
