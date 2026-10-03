using System;
using System.Collections;
using UnityEngine;

public class AnimatedHoe : MonoBehaviour
{
    [Header("Target & References")]
    [SerializeField] private Transform hoeTransform;
    [SerializeField] private ParticleSystem smokeParticle;

    [Header("Animation Settings")]
    [SerializeField] private int digCount = 3;
    [SerializeField] private float swingDuration = 0.22f;
    [SerializeField] private float liftAngle = 45f;
    [SerializeField] private float hitAngle = -35f;
    [SerializeField] private float liftHeight = 0.4f;
    [SerializeField] private float pullDistance = 0.15f;
    [SerializeField] private float popDuration = 0.2f;
    [SerializeField] private Vector3 baseEulerRotation = new Vector3(90f, 0f, 0f);

    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;
    private Vector3 _initialLocalScale = Vector3.one;
    private Coroutine _animationRoutine;

    private Transform TargetTransform => hoeTransform != null ? hoeTransform : transform;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (hoeTransform == null) hoeTransform = transform;
        if (smokeParticle == null) smokeParticle = GetComponentInChildren<ParticleSystem>(true);
    }
#endif

    private void Awake()
    {
        if (hoeTransform == null) hoeTransform = transform;
        if (smokeParticle == null) smokeParticle = GetComponentInChildren<ParticleSystem>(true);
        SaveInitialState();
    }

    private void OnDisable() { CancelAnimation(); }
    private void OnDestroy() { CancelAnimation(); }

    private void SaveInitialState()
    {
        Transform target = TargetTransform;
        _initialLocalPosition = target.localPosition;
        _initialLocalRotation = target.localRotation;
        _initialLocalScale = target.localScale != Vector3.zero ? target.localScale : Vector3.one;
    }

    private void ResetToInitialState()
    {
        Transform target = TargetTransform;
        if (target == null) return;
        target.localPosition = _initialLocalPosition;
        target.localRotation = _initialLocalRotation;
        target.localScale = _initialLocalScale;
    }

    private void CancelAnimation()
    {
        if (_animationRoutine != null) StopCoroutine(_animationRoutine);
        _animationRoutine = null;
        StopSmokeParticle();
        ResetToInitialState();
    }

    private void PlaySmokeParticle()
    {
        if (smokeParticle == null) return;
        smokeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        smokeParticle.Play();
    }

    private void StopSmokeParticle()
    {
        if (smokeParticle != null)
            smokeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void SetHoeRotationZ(float relativeZ)
    {
        Transform target = TargetTransform;
        if (target != null)
            target.localRotation = Quaternion.Euler(baseEulerRotation.x, baseEulerRotation.y, baseEulerRotation.z + relativeZ);
    }

    public void PlayDigAnimation(Vector3 targetPosition, Action onComplete = null)
    {
        CancelAnimation();
        transform.position = targetPosition;
        Transform target = TargetTransform;
        target.localPosition = _initialLocalPosition;
        SetHoeRotationZ(0f);
        target.localScale = Vector3.zero;
        _animationRoutine = StartCoroutine(PlayDigRoutine(onComplete));
    }

    private IEnumerator PlayDigRoutine(Action onComplete)
    {
        Transform target = TargetTransform;
        yield return AnimateScale(target, Vector3.zero, _initialLocalScale, popDuration, true);

        float currentAngle = 0f;
        for (int i = 0; i < Mathf.Max(1, digCount); i++)
        {
            Vector3 liftedPosition = _initialLocalPosition + new Vector3(0f, liftHeight, 0f);
            yield return AnimatePose(target, target.localPosition, liftedPosition, currentAngle, liftAngle,
                swingDuration * 0.35f, false);
            currentAngle = liftAngle;

            Vector3 strikePosition = _initialLocalPosition + new Vector3(0.05f, 0.05f, 0f);
            yield return AnimatePose(target, target.localPosition, strikePosition, currentAngle, hitAngle,
                swingDuration * 0.25f, true);
            currentAngle = hitAngle;
            PlaySmokeParticle();
            SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_pickaxe);

            Vector3 pullPosition = strikePosition + new Vector3(pullDistance, 0f, 0f);
            float pullAngle = hitAngle - 10f;
            yield return AnimatePose(target, target.localPosition, pullPosition, currentAngle, pullAngle,
                swingDuration * 0.4f, false);
            currentAngle = pullAngle;
        }

        yield return AnimateScale(target, target.localScale, Vector3.zero, popDuration, false);
        _animationRoutine = null;
        ResetToInitialState();
        onComplete?.Invoke();
        Despawn();
    }

    private IEnumerator AnimatePose(Transform target, Vector3 fromPosition, Vector3 toPosition,
        float fromAngle, float toAngle, float duration, bool easeIn)
    {
        duration = Mathf.Max(0.01f, duration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = easeIn ? progress * progress : 1f - (1f - progress) * (1f - progress);
            target.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, eased);
            SetHoeRotationZ(Mathf.LerpUnclamped(fromAngle, toAngle, eased));
            yield return null;
        }
        target.localPosition = toPosition;
        SetHoeRotationZ(toAngle);
    }

    private static IEnumerator AnimateScale(Transform target, Vector3 fromScale, Vector3 toScale,
        float duration, bool overshoot)
    {
        duration = Mathf.Max(0.01f, duration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = progress * progress * (3f - 2f * progress);
            if (overshoot)
            {
                const float amount = 1.15f;
                float delta = progress - 1f;
                eased = 1f + (amount + 1f) * delta * delta * delta + amount * delta * delta;
            }
            target.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            yield return null;
        }
        target.localScale = toScale;
    }

    private void Despawn()
    {
        transform.SetParent(null);
        if (PoolHolder.Instance != null) PoolHolder.Instance.Release(this);
        else Destroy(gameObject);
    }

    public static void SpawnAndPlay(AnimatedHoe prefab, Vector3 worldPosition, Transform parent = null, Action onComplete = null)
    {
        if (prefab == null || PoolHolder.Instance == null)
        {
            onComplete?.Invoke();
            return;
        }

        AnimatedHoe instance = PoolHolder.Instance.Get(prefab, parent, worldPosition, Quaternion.identity) as AnimatedHoe;
        if (instance != null) instance.PlayDigAnimation(worldPosition, onComplete);
        else onComplete?.Invoke();
    }
}
