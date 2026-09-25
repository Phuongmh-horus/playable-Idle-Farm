using DG.Tweening;
using UnityEngine;

/// <summary>Small plant-growth feedback for the playable.</summary>
public class PlantAnimator : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float growScaleMultiplier = 1.25f;
    [SerializeField] private float growDuration = 0.25f;
    [SerializeField] private Ease growEase = Ease.OutQuad;

    private Vector3 _baseScale = Vector3.one;
    private Tween _growTween;

    private void Awake()
    {
        if (targetTransform == null) targetTransform = transform;
        _baseScale = targetTransform.localScale;
    }

    private void OnDisable() { StopAnimation(); }
    private void OnDestroy() { StopAnimation(); }

    public void PlayGrowScaleAnim(float delay = 0f)
    {
        if (targetTransform == null) return;
        StopAnimation();
        _baseScale = targetTransform.localScale;
        Vector3 peak = new Vector3(_baseScale.x * growScaleMultiplier, _baseScale.y * growScaleMultiplier, _baseScale.z);
        _growTween = targetTransform.DOScale(peak, growDuration * 0.5f)
            .SetDelay(delay)
            .SetEase(growEase)
            .SetLoops(2, LoopType.Yoyo)
            .OnComplete(ResetScale);
    }

    private void StopAnimation()
    {
        if (_growTween != null) _growTween.Kill();
        ResetScale();
    }

    private void ResetScale()
    {
        if (targetTransform != null) targetTransform.localScale = _baseScale;
    }
}
