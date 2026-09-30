using System;
using UnityEngine;
using DG.Tweening;

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

    private Vector3 _initialLocalPosition = Vector3.zero;
    private Quaternion _initialLocalRotation = Quaternion.identity;
    private Vector3 _initialLocalScale = Vector3.one;

    private Sequence _animationSequence;
    private bool _isPlaying;

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
        if (hoeTransform == null)
        {
            hoeTransform = transform;
        }

        if (smokeParticle == null)
        {
            smokeParticle = GetComponentInChildren<ParticleSystem>(true);
        }

        SaveInitialState();
    }

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
        if (target != null)
        {
            target.localPosition = _initialLocalPosition;
            target.localRotation = _initialLocalRotation;
            target.localScale = _initialLocalScale;
        }
    }

    private void OnDisable()
    {
        CancelAnimation();
    }

    private void OnDestroy()
    {
        CancelAnimation();
    }

    private void CancelAnimation()
    {
        if (_animationSequence != null && _animationSequence.IsActive())
        {
            _animationSequence.Kill();
        }
        _animationSequence = null;
        _isPlaying = false;
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
        {
            smokeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void SetHoeRotationZ(float relativeZ)
    {
        Transform target = TargetTransform;
        if (target != null)
        {
            target.localRotation = Quaternion.Euler(
                baseEulerRotation.x,
                baseEulerRotation.y,
                baseEulerRotation.z + relativeZ
            );
        }
    }

    public void PlayDigAnimation(Vector3 targetPosition, Action onComplete = null)
    {
        CancelAnimation();
        _isPlaying = true;

        transform.position = targetPosition;
        Transform target = TargetTransform;

        target.localPosition = _initialLocalPosition;
        SetHoeRotationZ(0f);
        target.localScale = Vector3.zero;

        _animationSequence = DOTween.Sequence();

        // 1. Pop in xuất hiện
        _animationSequence.Append(target.DOScale(_initialLocalScale, popDuration).SetEase(Ease.OutBack));

        float currentRelativeZ = 0f;

        // 2. Thực hiện các nhịp cuốc đất
        for (int i = 0; i < digCount; i++)
        {
            // --- Giai đoạn 1: Vung lên (Wind-up) ---
            Vector3 liftedPos = _initialLocalPosition + new Vector3(0f, liftHeight, 0f);
            float windUpTime = swingDuration * 0.35f;
            float targetLiftZ = liftAngle;

            Sequence windUpSeq = DOTween.Sequence();
            windUpSeq.Join(target.DOLocalMove(liftedPos, windUpTime).SetEase(Ease.OutQuad));
            
            float zStart1 = currentRelativeZ;
            float zEnd1 = targetLiftZ;
            windUpSeq.Join(DOVirtual.Float(zStart1, zEnd1, windUpTime, SetHoeRotationZ).SetEase(Ease.OutQuad));
            
            currentRelativeZ = targetLiftZ;
            _animationSequence.Append(windUpSeq);

            // --- Giai đoạn 2: Bổ xuống đất (Strike down) ---
            Vector3 strikePos = _initialLocalPosition + new Vector3(0.05f, 0.05f, 0f);
            float strikeTime = swingDuration * 0.25f;
            float targetStrikeZ = hitAngle;

            Sequence strikeSeq = DOTween.Sequence();
            strikeSeq.Join(target.DOLocalMove(strikePos, strikeTime).SetEase(Ease.InQuad));
            
            float zStart2 = currentRelativeZ;
            float zEnd2 = targetStrikeZ;
            strikeSeq.Join(DOVirtual.Float(zStart2, zEnd2, strikeTime, SetHoeRotationZ).SetEase(Ease.InQuad));
            
            strikeSeq.OnComplete(PlaySmokeParticle);

            currentRelativeZ = targetStrikeZ;
            _animationSequence.Append(strikeSeq);

            // --- Giai đoạn 3: Kéo cuốc về sau (Pull back) ---
            Vector3 pullPos = strikePos + new Vector3(pullDistance, 0f, 0f);
            float pullTime = swingDuration * 0.4f;
            float targetPullZ = hitAngle - 10f;

            Sequence pullSeq = DOTween.Sequence();
            pullSeq.Join(target.DOLocalMove(pullPos, pullTime).SetEase(Ease.OutQuad));
            
            float zStart3 = currentRelativeZ;
            float zEnd3 = targetPullZ;
            pullSeq.Join(DOVirtual.Float(zStart3, zEnd3, pullTime, SetHoeRotationZ).SetEase(Ease.Linear));

            currentRelativeZ = targetPullZ;
            _animationSequence.Append(pullSeq);
        }

        // 3. Pop out biến mất
        _animationSequence.Append(target.DOScale(Vector3.zero, popDuration).SetEase(Ease.InBack));

        _animationSequence.OnComplete(() =>
        {
            _isPlaying = false;
            ResetToInitialState();
            onComplete?.Invoke();
            Despawn();
        });
        
        _animationSequence.OnKill(() => 
        {
            _isPlaying = false;
        });
    }

    private void Despawn()
    {
        if (gameObject == null) return;
        transform.SetParent(null);
        PoolHolder.Instance.Release(this);
    }

    public static void SpawnAndPlay(AnimatedHoe prefab, Vector3 worldPosition, Transform parent = null, Action onComplete = null)
    {
        if (prefab == null)
        {
            onComplete?.Invoke();
            return;
        }

        AnimatedHoe hoeInstance = PoolHolder.Instance.Get(prefab, parent, worldPosition, Quaternion.identity);
        if (hoeInstance != null)
        {
            hoeInstance.PlayDigAnimation(worldPosition, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }
}
