using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingCurrencyLayer : WorldLayerBase
{
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private TextMeshProUGUI currencyOutlineText;
    [SerializeField] private float floatDistance = 0.8f;
    [SerializeField] private float floatDuration = 0.75f;
    [SerializeField] private float delayBeforeFade = 0.35f;
    [SerializeField] private float fadeDuration = 0.3f;
    private Coroutine _animationRoutine;
    private Vector3 _animationStartPosition;
    private bool _hasAnimationStartPosition;

    protected override void Awake()
    {
        layerDepth = WorldLayerDepth.TopOverlay;
        blockSelectionClick = false;
        base.Awake();
        if (currencyText == null) currencyText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (currencyOutlineText == null && currencyText != null && currencyText.transform.parent != null)
        {
            currencyOutlineText = currencyText.transform.parent.GetComponent<TextMeshProUGUI>();
        }
        EnsureCurrencyOutline();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowAmount(double amount, Action onComplete = null)
    {
        CancelMotions();
        string value = "+ " + T_Utility.FormatGold(amount);
        if (currencyText != null) currencyText.text = value;
        if (currencyOutlineText != null) currencyOutlineText.text = value;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        Vector3 start = transform.position;
        _animationStartPosition = start;
        _hasAnimationStartPosition = true;
        transform.localScale = Vector3.one * 0.4f;
        _animationRoutine = StartCoroutine(ShowAmountRoutine(start, onComplete));
    }

    private IEnumerator ShowAmountRoutine(Vector3 start, Action onComplete)
    {
        float moveTime = Mathf.Max(0f, floatDuration);
        float fadeStart = Mathf.Max(0f, delayBeforeFade);
        float fadeTime = Mathf.Max(0f, fadeDuration);
        float totalDuration = Mathf.Max(0.2f, Mathf.Max(moveTime, fadeStart + fadeTime));
        Vector3 end = start + Vector3.up * floatDistance;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float scaleProgress = RuntimeEaseUtility.Evaluate(RuntimeEase.OutBack, elapsed / 0.2f);
            float moveProgress = moveTime <= 0f ? 1f : RuntimeEaseUtility.Evaluate(RuntimeEase.OutQuad, elapsed / moveTime);
            transform.localScale = Vector3.LerpUnclamped(Vector3.one * 0.4f, Vector3.one, scaleProgress);
            transform.position = Vector3.LerpUnclamped(start, end, moveProgress);

            if (canvasGroup != null && elapsed >= fadeStart)
            {
                float fadeProgress = fadeTime <= 0f ? 1f : RuntimeEaseUtility.Evaluate(RuntimeEase.InQuad, (elapsed - fadeStart) / fadeTime);
                canvasGroup.alpha = Mathf.LerpUnclamped(1f, 0f, fadeProgress);
            }
            yield return null;
        }

        transform.localScale = Vector3.one;
        transform.position = end;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        _animationRoutine = null;
        onComplete?.Invoke();
        if (WorldLayerManager.Instance != null) WorldLayerManager.Instance.Hide(this);
    }

    protected override void OnDisable()
    {
        CancelMotions();
        ResetVisuals();
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        CancelMotions();
        ResetVisuals();
        base.OnDestroy();
    }

    public void CancelMotions()
    {
        if (_animationRoutine == null) return;
        StopCoroutine(_animationRoutine);
        _animationRoutine = null;
    }

    private void ResetVisuals()
    {
        transform.localScale = Vector3.one;
        if (_hasAnimationStartPosition) transform.position = _animationStartPosition;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    public override void CleanupBeforeHide() { CancelMotions(); ResetVisuals(); base.CleanupBeforeHide(); }
    public override void Hide() { CleanupBeforeHide(); base.Hide(); }

    private void EnsureCurrencyOutline()
    {
        if (currencyText == null || currencyOutlineText != null) return;

        Transform originalParent = currencyText.transform.parent;
        if (originalParent == null) return;

        GameObject outlineObject = new GameObject("Outline_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform outlineRect = outlineObject.GetComponent<RectTransform>();
        outlineRect.SetParent(originalParent, false);
        outlineRect.anchorMin = currencyText.rectTransform.anchorMin;
        outlineRect.anchorMax = currencyText.rectTransform.anchorMax;
        outlineRect.pivot = currencyText.rectTransform.pivot;
        outlineRect.anchoredPosition = currencyText.rectTransform.anchoredPosition;
        outlineRect.sizeDelta = currencyText.rectTransform.sizeDelta;
        outlineRect.localScale = Vector3.one * 1.15f;

        currencyText.transform.SetParent(outlineRect, false);
        currencyText.rectTransform.localScale = Vector3.one * 0.95f;
        outlineRect.SetAsFirstSibling();

        currencyOutlineText = outlineObject.GetComponent<TextMeshProUGUI>();
        currencyOutlineText.font = currencyText.font;
        currencyOutlineText.fontSize = currencyText.fontSize;
        currencyOutlineText.alignment = currencyText.alignment;
        currencyOutlineText.enableAutoSizing = currencyText.enableAutoSizing;
        currencyOutlineText.fontSizeMin = currencyText.fontSizeMin;
        currencyOutlineText.fontSizeMax = currencyText.fontSizeMax;
        currencyOutlineText.color = new Color(0.15994173f, 0.35547677f, 0f, 1f);
    }
}
