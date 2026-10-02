using System.Collections;
using UnityEngine;

/// <summary>Small plant-growth feedback for the playable.</summary>
public class PlantAnimator : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float growScaleMultiplier = 1.25f;
    [SerializeField] private float growDuration = 0.25f;

    private Vector3 _baseScale = Vector3.one;
    private Coroutine _growRoutine;

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
        _growRoutine = StartCoroutine(GrowRoutine(peak, delay));
    }

    private IEnumerator GrowRoutine(Vector3 peakScale, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        float duration = Mathf.Max(0.01f, growDuration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(progress * Mathf.PI);
            targetTransform.localScale = Vector3.Lerp(_baseScale, peakScale, pulse);
            yield return null;
        }
        _growRoutine = null;
        ResetScale();
    }

    private void StopAnimation()
    {
        if (_growRoutine != null) StopCoroutine(_growRoutine);
        _growRoutine = null;
        ResetScale();
    }

    private void ResetScale()
    {
        if (targetTransform != null) targetTransform.localScale = _baseScale;
    }
}
