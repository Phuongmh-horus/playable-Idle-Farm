using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component tạo hiệu ứng nảy (bounce / punch scale) cho Button hoặc RectTransform khi bấm.
/// Có thể gắn trực tiếp vào GameObject chứa Button (tự động lắng nghe onClick) hoặc gọi hàm PlayBounce() thủ công.
/// </summary>
[DisallowMultipleComponent]
public class ButtonBounceEffect : MonoBehaviour
{
    [Header("Target & Binding")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Button button;
    [SerializeField] private bool autoListenButtonClick = true;

    [Header("Animation Settings")]
    [SerializeField] private float bounceScale = 1.2f;
    [SerializeField] private float downDuration = 0.08f;
    [SerializeField] private float upDuration = 0.16f;
    [SerializeField] private RuntimeEase downEase = RuntimeEase.OutQuad;
    [SerializeField] private RuntimeEase upEase = RuntimeEase.OutBack;

    private Coroutine _bounceRoutine;
    private Vector3 _originalScale = Vector3.one;
    private bool _hasOriginalScale;

    private void Reset()
    {
        if (targetTransform == null) targetTransform = transform;
        if (button == null) button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (targetTransform == null) targetTransform = transform;
        if (button == null) button = GetComponent<Button>();

        _originalScale = targetTransform.localScale;
        _hasOriginalScale = true;

        if (autoListenButtonClick && button != null)
        {
            button.onClick.AddListener(PlayBounce);
        }
    }

    private void OnEnable()
    {
        if (!_hasOriginalScale && targetTransform != null)
        {
            _originalScale = targetTransform.localScale;
            _hasOriginalScale = true;
        }
    }

    private void OnDisable()
    {
        CancelMotion();
    }

    private void OnDestroy()
    {
        if (autoListenButtonClick && button != null)
        {
            button.onClick.RemoveListener(PlayBounce);
        }
        CancelMotion();
    }

    public void PlayBounce()
    {
        if (targetTransform == null) return;
        if (button != null && !button.interactable) return;

        CancelMotion();

        Vector3 startScale = _hasOriginalScale ? _originalScale : targetTransform.localScale;
        Vector3 pressedScale = startScale * bounceScale;
        targetTransform.localScale = startScale;

        _bounceRoutine = StartCoroutine(BounceRoutine(startScale, pressedScale));
    }

    private IEnumerator BounceRoutine(Vector3 startScale, Vector3 pressedScale)
    {
        yield return AnimateScale(startScale, pressedScale, downDuration, downEase);
        yield return AnimateScale(pressedScale, startScale, upDuration, upEase);

        if (targetTransform != null) targetTransform.localScale = startScale;
        _bounceRoutine = null;
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float animationDuration, RuntimeEase animationEase)
    {
        if (animationDuration <= 0f)
        {
            if (targetTransform != null) targetTransform.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < animationDuration && targetTransform != null)
        {
            elapsed += Time.deltaTime;
            float progress = RuntimeEaseUtility.Evaluate(animationEase, elapsed / animationDuration);
            targetTransform.localScale = Vector3.LerpUnclamped(from, to, progress);
            yield return null;
        }

        if (targetTransform != null) targetTransform.localScale = to;
    }

    public void CancelMotion()
    {
        if (_bounceRoutine != null)
        {
            StopCoroutine(_bounceRoutine);
            _bounceRoutine = null;
        }

        if (targetTransform != null && _hasOriginalScale)
        {
            targetTransform.localScale = _originalScale;
        }
    }
}
