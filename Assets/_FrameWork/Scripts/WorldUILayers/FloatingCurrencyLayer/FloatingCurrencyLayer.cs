using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingCurrencyLayer : WorldLayerBase
{
    [SerializeField] private TextMeshProUGUI currencyText;
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
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowAmount(double amount, Action onComplete = null)
    {
        CancelMotions();
        if (currencyText != null) currencyText.text = "+" + T_Utility.FormatGold(amount);
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
}
