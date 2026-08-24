using DG.Tweening;
using UnityEngine;

public class ShakingZAnimation : MonoBehaviour
{
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeStrength = 10f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private float shakeRandomness = 90f;

    [SerializeField] private float delayBetweenShakes = 0.3f;
    [SerializeField] private float pauseBetweenGroups = 3f;

    private Sequence _sequence;

    private void OnEnable()
    {
        _sequence?.Kill();

        _sequence = DOTween.Sequence()
            .Append(CreateShake())
            .AppendInterval(delayBetweenShakes)
            .Append(CreateShake())
            .AppendInterval(pauseBetweenGroups)
            .SetLoops(-1);
    }

    private Tween CreateShake()
    {
        return transform.DOShakeRotation(
            shakeDuration,
            new Vector3(0, 0, shakeStrength),
            shakeVibrato,
            shakeRandomness);
    }

    private void OnDisable()
    {
        _sequence?.Kill();
    }
}