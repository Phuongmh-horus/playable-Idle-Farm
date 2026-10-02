using System.Collections;
using UnityEngine;

public class HandEndCard : MonoBehaviour
{
    [SerializeField] private bool isWinningEndCard = true;

    [Header("Win End Card")]
    [SerializeField] private RectTransform transform1ToMove;

    [SerializeField] private RectTransform transform2ToMove;

    [SerializeField] private float moveDuration = 1f;

    [SerializeField] private Vector2 offset = new Vector2(0f, 0f);

    // [SerializeField] private float waitTime = 0.5f;


    [Header("Lose End Card")]
    [SerializeField] private RectTransform transformToMove;
    // [SerializeField] private float PulseScale = 1.1f;


    private Coroutine _moveRoutine;
    private Coroutine _pulseRoutine;
    private Transform _pulsedButton;

    private Vector3 GetWorldOffset()
    {
        // Multiply the offset by lossyScale to make it resolution-independent
        return new Vector3(offset.x * transform.lossyScale.x, offset.y * transform.lossyScale.y, 0f);
    }

    void OnEnable()
    {
        CancelAnimations();
        if (isWinningEndCard)
        {
            if (transform1ToMove == null || transform2ToMove == null) return;
            Vector3 worldOffset = GetWorldOffset();
            Vector3 transform1ToMovePosition = transform1ToMove.position + worldOffset;
            Vector3 transform2ToMovePosition = transform2ToMove.position + worldOffset;
            transform.position = transform1ToMovePosition;
            _moveRoutine = StartCoroutine(WinningLoop(transform1ToMovePosition, transform2ToMovePosition));
        }
        else if (transformToMove != null)
        {
            transform.position = transformToMove.position + GetWorldOffset();
            _moveRoutine = StartCoroutine(LosingLoop());
        }
    }
    
    void OnDisable()
    {
        CancelAnimations();
        ResetPulseVisuals();
    }



    [Header("Click Button Animation")]
    [SerializeField] private float rotateHand = 15f;
    [SerializeField] private float pulseHandScale = 1.2f;
    [SerializeField] private float startHandScale = 1f;

    [SerializeField] private float pulseHandDuration = 1f;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float StartScale = 0.9f;


    
    public void AnimationClickButton(Transform buttonTransform)
    {
        if (buttonTransform == null) return;
        CancelPulse();
        ResetPulseVisuals();
        transform.position = buttonTransform.position + GetWorldOffset();
        buttonTransform.localScale = Vector3.one * normalScale;
        _pulsedButton = buttonTransform;
        _pulseRoutine = StartCoroutine(PulseRoutine(buttonTransform));
    }

    private IEnumerator WinningLoop(Vector3 firstPosition, Vector3 secondPosition)
    {
        while (true)
        {
            yield return MoveTo(secondPosition);
            AnimationClickButton(transform2ToMove);
            yield return WaitForDuration(pulseHandDuration);

            yield return MoveTo(firstPosition);
            AnimationClickButton(transform1ToMove);
            yield return WaitForDuration(pulseHandDuration);
        }
    }

    private IEnumerator LosingLoop()
    {
        while (true)
        {
            AnimationClickButton(transformToMove);
            yield return WaitForDuration(pulseHandDuration);
        }
    }

    private IEnumerator MoveTo(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float duration = Mathf.Max(0f, moveDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = RuntimeEaseUtility.Evaluate(RuntimeEase.InOutSine, elapsed / duration);
            transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, progress);
            yield return null;
        }
        transform.position = targetPosition;
    }

    private IEnumerator PulseRoutine(Transform buttonTransform)
    {
        float halfDuration = Mathf.Max(0f, pulseHandDuration * 0.5f);
        yield return AnimatePulseHalf(buttonTransform, 0f, rotateHand, startHandScale, pulseHandScale, normalScale, StartScale, halfDuration);
        yield return AnimatePulseHalf(buttonTransform, rotateHand, 0f, pulseHandScale, startHandScale, StartScale, normalScale, halfDuration);
        ResetPulseVisuals();
        _pulseRoutine = null;
    }

    private IEnumerator AnimatePulseHalf(
        Transform buttonTransform,
        float fromRotation,
        float toRotation,
        float fromHandScale,
        float toHandScale,
        float fromButtonScale,
        float toButtonScale,
        float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && buttonTransform != null)
        {
            elapsed += Time.deltaTime;
            float progress = RuntimeEaseUtility.Evaluate(RuntimeEase.InOutSine, elapsed / duration);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(fromRotation, toRotation, progress));
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(fromHandScale, toHandScale, progress);
            buttonTransform.localScale = Vector3.one * Mathf.LerpUnclamped(fromButtonScale, toButtonScale, progress);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, toRotation);
        transform.localScale = Vector3.one * toHandScale;
        if (buttonTransform != null) buttonTransform.localScale = Vector3.one * toButtonScale;
    }

    private static IEnumerator WaitForDuration(float duration)
    {
        float remaining = Mathf.Max(0f, duration);
        do
        {
            remaining -= Time.deltaTime;
            yield return null;
        }
        while (remaining > 0f);
    }

    private void CancelAnimations()
    {
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = null;
        CancelPulse();
    }

    private void CancelPulse()
    {
        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        _pulseRoutine = null;
    }

    private void ResetPulseVisuals()
    {
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * startHandScale;
        if (_pulsedButton != null) _pulsedButton.localScale = Vector3.one * normalScale;
        _pulsedButton = null;
    }


}
