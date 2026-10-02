using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaterRequiredLayer : WorldLayerBase
{
    [SerializeField] private Image fillImage;
    private Coroutine _animationRoutine;

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
        _animationRoutine = StartCoroutine(AnimateWaterRoutine(startFill, targetFill, duration, delayBeforeRelease, onComplete));
    }

    private IEnumerator AnimateWaterRoutine(float startFill, float targetFill, float fillDuration, float releaseDelay, Action onComplete)
    {
        float elapsed = 0f;
        fillDuration = Mathf.Max(0f, fillDuration);
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            if (fillImage != null)
            {
                float progress = RuntimeEaseUtility.Evaluate(RuntimeEase.OutQuad, elapsed / fillDuration);
                fillImage.fillAmount = Mathf.LerpUnclamped(startFill, targetFill, progress);
            }
            yield return null;
        }
        if (fillImage != null) fillImage.fillAmount = targetFill;

        float delay = Mathf.Max(0f, releaseDelay);
        while (delay > 0f)
        {
            delay -= Time.deltaTime;
            yield return null;
        }

        const float hideDuration = 0.2f;
        elapsed = 0f;
        while (elapsed < hideDuration)
        {
            elapsed += Time.deltaTime;
            float progress = RuntimeEaseUtility.Evaluate(RuntimeEase.InBack, elapsed / hideDuration);
            transform.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.zero, progress);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        _animationRoutine = null;
        onComplete?.Invoke();
        if (WorldLayerManager.Instance != null) WorldLayerManager.Instance.Hide(this);
    }

    protected override void OnDisable()
    {
        CancelMotions();
        transform.localScale = Vector3.one;
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        CancelMotions();
        transform.localScale = Vector3.one;
        base.OnDestroy();
    }

    public void CancelMotions()
    {
        if (_animationRoutine == null) return;
        StopCoroutine(_animationRoutine);
        _animationRoutine = null;
    }
    public override void CleanupBeforeHide() { CancelMotions(); transform.localScale = Vector3.one; base.CleanupBeforeHide(); }
    public override void Hide() { CleanupBeforeHide(); base.Hide(); }
}
