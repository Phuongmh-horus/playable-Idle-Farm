using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class WaterRequiredLayer : WorldLayerBase
{
    [SerializeField] private Image fillImage;
    private Sequence _sequence;

    protected override void Awake()
    {
        if (layerDepth == WorldLayerDepth.Popup) layerDepth = WorldLayerDepth.StatusIcon;
        base.Awake();
        if (fillImage == null) fillImage = GetComponentInChildren<Image>();
    }

    public void AnimateWater(float startFill, float targetFill, float duration, float delayBeforeRelease, Action onComplete = null)
    {
        CancelMotions();
        if (fillImage != null) fillImage.fillAmount = startFill;
        transform.localScale = Vector3.one;
        _sequence = DOTween.Sequence();
        if (fillImage != null) _sequence.Append(fillImage.DOFillAmount(targetFill, duration).SetEase(Ease.OutQuad));
        else _sequence.AppendInterval(duration);
        _sequence.AppendInterval(delayBeforeRelease);
        _sequence.Append(transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        _sequence.OnComplete(() => { onComplete?.Invoke(); if (WorldLayerManager.Instance != null) WorldLayerManager.Instance.Hide(this); });
    }

    public void CancelMotions() { if (_sequence != null) _sequence.Kill(); _sequence = null; }
    public override void CleanupBeforeHide() { CancelMotions(); transform.localScale = Vector3.one; base.CleanupBeforeHide(); }
    public override void Hide() { CleanupBeforeHide(); base.Hide(); }
}
