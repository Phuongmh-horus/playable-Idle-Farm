using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Lightweight world-layer scale/fade feedback. It deliberately does not use
/// DOTween modules, which are unnecessary in the exported Luna playable.
/// </summary>
public class AnimShowPopupWorld : MonoBehaviour
{
    [SerializeField] protected WorldLayerBase worldLayerBase;
    [SerializeField] protected RectTransform panelRect;
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected bool useScale = true;
    [SerializeField] protected bool useFade = true;
    [SerializeField] protected float duration = 0.3f;
    [SerializeField] protected float hideDuration = 0.2f;
    public UnityEvent OnStartAnim;
    public UnityEvent OnCompleteAnim;
    public UnityEvent OnStartHideAnim;
    public UnityEvent OnCompleteHideAnim;

    private Vector3 _targetScale = Vector3.one;
    private Vector3 _fromScale;
    private Vector3 _toScale;
    private float _fromAlpha;
    private float _toAlpha;
    private float _elapsed;
    private float _animationDuration;
    private bool _isAnimating;
    private bool _isShowing;
    private Action _onComplete;

    public Transform TargetTransform { get { return panelRect != null ? panelRect : transform; } }

    private void Awake()
    {
        _targetScale = TargetTransform.localScale == Vector3.zero ? Vector3.one : TargetTransform.localScale;
        if (worldLayerBase != null) worldLayerBase.OnShowLayer.AddListener(PlayAnim);
        enabled = false;
    }

    private void OnDestroy()
    {
        if (worldLayerBase != null) worldLayerBase.OnShowLayer.RemoveListener(PlayAnim);
        CancelAnim();
    }

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsed += Time.unscaledDeltaTime;
        float progress = _animationDuration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _animationDuration);
        float eased = progress * progress * (3f - 2f * progress);
        Transform target = TargetTransform;
        if (useScale) target.localScale = Vector3.LerpUnclamped(_fromScale, _toScale, eased);
        if (useFade && canvasGroup != null) canvasGroup.alpha = Mathf.LerpUnclamped(_fromAlpha, _toAlpha, eased);
        if (progress < 1f) return;

        _isAnimating = false;
        if (_isShowing)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            OnCompleteAnim?.Invoke();
        }
        else
        {
            if (useScale) target.localScale = _targetScale;
            OnCompleteHideAnim?.Invoke();
        }

        Action completed = _onComplete;
        _onComplete = null;
        if (completed != null) completed();
        enabled = false;
    }

    public void CancelAnim()
    {
        _isAnimating = false;
        _onComplete = null;
        enabled = false;
    }

    public void PlayShowAnim(Action onComplete) { PlayAnim(onComplete); }
    public void PlayAnim() { PlayAnim(null); }
    public void PlayAnim(Action onComplete) { StartAnimation(true, duration, onComplete); }

    public void Close()
    {
        if (worldLayerBase != null) worldLayerBase.Hide();
        else PlayHideAnim(null);
    }

    public void PlayHideAnim() { PlayHideAnim(null); }
    public void PlayHideAnim(Action onComplete) { StartAnimation(false, hideDuration, onComplete); }

    private void StartAnimation(bool showing, float animationDuration, Action onComplete)
    {
        CancelAnim();
        enabled = true;
        Transform target = TargetTransform;
        _isShowing = showing;
        _animationDuration = Mathf.Max(0f, animationDuration);
        _elapsed = 0f;
        _onComplete = onComplete;
        _toScale = showing ? _targetScale : Vector3.zero;
        _fromScale = target.localScale;
        _toAlpha = showing ? 1f : 0f;
        _fromAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        if (showing)
        {
            if (useScale) { target.localScale = Vector3.zero; _fromScale = Vector3.zero; }
            if (useFade && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                _fromAlpha = 0f;
            }
            OnStartAnim?.Invoke();
        }
        else
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
            OnStartHideAnim?.Invoke();
        }

        _isAnimating = true;
        if (_animationDuration <= 0f) Update();
    }
}
