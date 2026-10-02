using UnityEngine;

public class ShakingZAnimation : MonoBehaviour
{
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeStrength = 10f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private float shakeRandomness = 90f;

    [SerializeField] private float delayBetweenShakes = 0.3f;
    [SerializeField] private float pauseBetweenGroups = 3f;

    private enum ShakePhase { FirstShake, ShortDelay, SecondShake, LongDelay }

    private ShakePhase _phase;
    private Quaternion _baseRotation;
    private float _phaseElapsed;
    private float _fromOffset;
    private float _toOffset;
    private int _segmentIndex;

    private void OnEnable()
    {
        _baseRotation = transform.localRotation;
        BeginShake(ShakePhase.FirstShake);
    }

    private void Update()
    {
        _phaseElapsed += Time.deltaTime;
        if (_phase == ShakePhase.FirstShake || _phase == ShakePhase.SecondShake)
        {
            UpdateShake();
            return;
        }

        float delay = _phase == ShakePhase.ShortDelay ? Mathf.Max(0f, delayBetweenShakes) : Mathf.Max(0f, pauseBetweenGroups);
        if (_phaseElapsed < delay) return;
        BeginShake(_phase == ShakePhase.ShortDelay ? ShakePhase.SecondShake : ShakePhase.FirstShake);
    }

    private void UpdateShake()
    {
        float duration = Mathf.Max(0f, shakeDuration);
        if (duration <= 0f || _phaseElapsed >= duration)
        {
            transform.localRotation = _baseRotation;
            _phase = _phase == ShakePhase.FirstShake ? ShakePhase.ShortDelay : ShakePhase.LongDelay;
            _phaseElapsed = 0f;
            return;
        }

        int vibrato = Mathf.Max(1, shakeVibrato);
        float segmentProgress = _phaseElapsed / duration * vibrato;
        int segment = Mathf.FloorToInt(segmentProgress);
        if (segment != _segmentIndex)
        {
            _segmentIndex = segment;
            _fromOffset = _toOffset;
            _toOffset = NextOffset(segment);
        }

        float localProgress = segmentProgress - segment;
        float fade = 1f - _phaseElapsed / duration;
        float offset = Mathf.Lerp(_fromOffset, _toOffset, Mathf.SmoothStep(0f, 1f, localProgress)) * fade;
        transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, offset);
    }

    private void BeginShake(ShakePhase phase)
    {
        _phase = phase;
        _phaseElapsed = 0f;
        _segmentIndex = 0;
        _fromOffset = 0f;
        _toOffset = NextOffset(0);
    }

    private float NextOffset(int segment)
    {
        float randomness = Mathf.Clamp01(shakeRandomness / 180f);
        float variedStrength = Mathf.Lerp(1f, Random.Range(0.35f, 1f), randomness) * shakeStrength;
        return (segment & 1) == 0 ? variedStrength : -variedStrength;
    }

    private void OnDisable()
    {
        transform.localRotation = _baseRotation;
    }
}
