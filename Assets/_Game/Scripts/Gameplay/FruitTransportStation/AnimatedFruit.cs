using System;
using System.Collections;
using UnityEngine;

public class AnimatedFruit : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AnimatedFruitConfig config;

    private Coroutine _flightRoutine;
    private Action _onArrived;

    private AnimatedFruitConfig Config
    {
        get
        {
            return config != null
                ? config
                : ConfigHolder.Instance != null && ConfigHolder.Instance.FruitConfig != null
                    ? ConfigHolder.Instance.FruitConfig.animatedFruitConfig
                    : null;
        }
    }

    private void OnDisable() { CancelMotion(); }

    public void Fly(Vector3 startPosition, Transform targetTransform, Sprite fruitSprite, Action onArrived = null)
    {
        CancelMotion();
        _onArrived = onArrived;
        if (spriteRenderer != null) spriteRenderer.sprite = fruitSprite;

        AnimatedFruitConfig cfg = Config;
        float popRadius = cfg != null ? cfg.popRadius : 0.6f;
        Vector2 random = UnityEngine.Random.insideUnitCircle;
        if (random.sqrMagnitude < 0.01f) random = Vector2.right;
        Vector3 popPosition = startPosition + new Vector3(random.x, 0f, random.y) * popRadius;
        float initialScale = cfg != null ? cfg.popInitialScale : 0f;
        float preFlightScaleMultiplier = cfg != null && cfg.preFlightScaleMultiplier > 0f
            ? cfg.preFlightScaleMultiplier : 1.25f;
        float flightScaleMultiplier = cfg != null && cfg.flightScaleMultiplier > 0f
            ? cfg.flightScaleMultiplier : 1.15f;
        float peakScale = (cfg != null ? cfg.popPeakScale : 0.42f) * preFlightScaleMultiplier;
        float flightStartScale = (cfg != null ? cfg.startScale : 0.35f) * flightScaleMultiplier;
        float endScale = cfg != null ? cfg.endScale : 0.25f;

        transform.position = startPosition;
        transform.localScale = Vector3.one * initialScale;
        float popDuration = cfg != null ? cfg.popDuration : 0.25f;
        float flightDuration = cfg != null ? cfg.flightDuration : 0.55f;
        _flightRoutine = StartCoroutine(FlightRoutine(popPosition, targetTransform, peakScale, flightStartScale, endScale, popDuration, flightDuration));
    }

    private IEnumerator FlightRoutine(Vector3 popPosition, Transform targetTransform, float peakScale, float flightStartScale,
        float endScale, float popDuration, float flightDuration)
    {
        Vector3 startPosition = transform.position;
        Vector3 initialScale = transform.localScale;
        popDuration = Mathf.Max(0.01f, popDuration);
        for (float elapsed = 0f; elapsed < popDuration; elapsed += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / popDuration);
            float positionProgress = 1f - (1f - progress) * (1f - progress);
            float scale;
            if (progress < 0.65f)
            {
                float growProgress = progress / 0.65f;
                growProgress = 1f - (1f - growProgress) * (1f - growProgress);
                scale = Mathf.Lerp(initialScale.x, peakScale, growProgress);
            }
            else
            {
                float settleProgress = (progress - 0.65f) / 0.35f;
                scale = Mathf.Lerp(peakScale, flightStartScale, settleProgress);
            }
            transform.position = Vector3.Lerp(startPosition, popPosition, positionProgress);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        transform.position = popPosition;
        transform.localScale = Vector3.one * flightStartScale;
        flightDuration = Mathf.Max(0.01f, flightDuration);
        for (float elapsed = 0f; elapsed < flightDuration; elapsed += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / flightDuration);
            float eased = progress * progress;
            Vector3 targetPosition = targetTransform != null ? targetTransform.position : popPosition;
            transform.position = Vector3.Lerp(popPosition, targetPosition, eased);
            transform.localScale = Vector3.one * Mathf.Lerp(flightStartScale, endScale, eased);
            yield return null;
        }

        _flightRoutine = null;
        CompleteFlight();
    }

    private void CompleteFlight()
    {
        Action callback = _onArrived;
        _onArrived = null;
        callback?.Invoke();
    }

    public void CancelMotion()
    {
        if (_flightRoutine != null) StopCoroutine(_flightRoutine);
        _flightRoutine = null;
        _onArrived = null;
    }
}
