using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class FloatingCurrencyLayer : WorldLayerBase
{
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private float floatDistance = 0.8f;
    [SerializeField] private float floatDuration = 0.75f;
    [SerializeField] private float delayBeforeFade = 0.35f;
    [SerializeField] private float fadeDuration = 0.3f;
    private Sequence _sequence;

    protected override void Awake()
    {
        layerDepth = WorldLayerDepth.TopOverlay;
        blockSelectionClick = false;
        base.Awake();
        if (currencyText == null) currencyText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowAmount(double amount, Action onComplete = null)
    {
        CancelMotions();
        if (currencyText != null) currencyText.text = "+" + T_Utility.FormatGold(amount);
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        Vector3 start = transform.position;
        transform.localScale = Vector3.one * 0.4f;
        _sequence = DOTween.Sequence();
        _sequence.Join(transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));
        _sequence.Join(transform.DOMove(start + Vector3.up * floatDistance, floatDuration).SetEase(Ease.OutQuad));
        if (canvasGroup != null) _sequence.Insert(delayBeforeFade, canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        _sequence.OnComplete(() => { onComplete?.Invoke(); if (WorldLayerManager.Instance != null) WorldLayerManager.Instance.Hide(this); });
    }

    public void CancelMotions() { if (_sequence != null) _sequence.Kill(); _sequence = null; }
    public override void CleanupBeforeHide() { CancelMotions(); transform.localScale = Vector3.one; if (canvasGroup != null) canvasGroup.alpha = 1f; base.CleanupBeforeHide(); }
    public override void Hide() { CleanupBeforeHide(); base.Hide(); }
}
