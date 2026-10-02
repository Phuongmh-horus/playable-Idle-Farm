using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AnimShowPopup : MonoBehaviour
{
    [SerializeField] protected LayerBase layerBase;
    [SerializeField] protected CanvasGroup dimCanvasGroup;
    [SerializeField] protected RectTransform panelRect;
    [SerializeField] protected float duration = 0.4f;
    [SerializeField] protected RuntimeEase easeType = RuntimeEase.OutBack;
    [SerializeField] protected float interval = 0f;
    public UnityEvent OnStartAnim;
    public UnityEvent OnCompleteAnim;

    private void Reset()
    {
        if (layerBase == null) layerBase = GetComponent<LayerBase>();
        if (panelRect == null) panelRect = transform.Find("Panel")?.GetComponent<RectTransform>();
        if (dimCanvasGroup == null) dimCanvasGroup = transform.GetChild(0).GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (layerBase) layerBase.OnShowLayer.AddListener(PlayAnim);
        if (panelRect) panelRect.localScale = Vector3.zero;
        if (dimCanvasGroup) dimCanvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (layerBase) layerBase.OnShowLayer.RemoveListener(PlayAnim);
        CancelAnimation();
        ResetVisuals();
    }

    private void OnDisable()
    {
        CancelAnimation();
        ResetVisuals();
    }

    private Coroutine _animationRoutine;

    private void PlayAnim()
    {
        CancelAnimation();

        if (panelRect) panelRect.localScale = Vector3.zero;
        if (dimCanvasGroup) dimCanvasGroup.alpha = 0;
        OnStartAnim?.Invoke();

        _animationRoutine = StartCoroutine(PlayAnimationRoutine());
    }

    private IEnumerator PlayAnimationRoutine()
    {
        float animationDuration = Mathf.Max(0f, duration);
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = RuntimeEaseUtility.Evaluate(easeType, elapsed / animationDuration);
            if (panelRect) panelRect.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, progress);
            if (dimCanvasGroup) dimCanvasGroup.alpha = Mathf.LerpUnclamped(0f, 1f, progress);
            yield return null;
        }

        if (panelRect) panelRect.localScale = Vector3.one;
        if (dimCanvasGroup) dimCanvasGroup.alpha = 1f;

        float delay = Mathf.Max(0f, interval);
        while (delay > 0f)
        {
            delay -= Time.deltaTime;
            yield return null;
        }

        _animationRoutine = null;
        OnCompleteAnim?.Invoke();
    }

    private void CancelAnimation()
    {
        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
            _animationRoutine = null;
        }
    }

    private void ResetVisuals()
    {
        if (panelRect) panelRect.localScale = Vector3.zero;
        if (dimCanvasGroup) dimCanvasGroup.alpha = 0f;
    }
}
