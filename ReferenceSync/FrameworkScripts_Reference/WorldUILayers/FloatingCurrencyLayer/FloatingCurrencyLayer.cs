using System;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

public class FloatingCurrencyLayer : WorldLayerBase
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Animation Config")]
    [SerializeField] private float floatDistance = 0.8f;
    [SerializeField] private float floatDuration = 0.75f;
    [SerializeField] private float delayBeforeFade = 0.35f;
    [SerializeField] private float fadeDuration = 0.3f;

    private MotionHandle _moveMotionHandle;
    private MotionHandle _fadeMotionHandle;
    private MotionHandle _scaleMotionHandle;

    protected override void Reset()
    {
        base.Reset();
        layerDepth = WorldLayerDepth.TopOverlay;
        blockSelectionClick = false;
        ApplySorting();
    }

    protected override void Awake()
    {
        layerDepth = WorldLayerDepth.TopOverlay;
        blockSelectionClick = false;
        base.Awake();
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (currencyText == null)
        {
            currencyText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// Hiển thị giá trị tiền và chạy hiệu ứng bay lên + fade out
    /// </summary>
    public void ShowAmount(double amount, Action onComplete = null)
    {
        CancelMotions();
        ResolveReferences();

        if (currencyText != null)
        {
            currencyText.text =  $"+{T_Utility.FormatCash(amount)}";
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * floatDistance;

        // 1. Pop-in scale nhẹ
        transform.localScale = Vector3.one * 0.4f;
        _scaleMotionHandle = LMotion.Create(Vector3.one * 0.4f, Vector3.one, 0.2f)
            .WithEase(Ease.OutBack)
            .BindToLocalScale(transform);

        // 2. Bay lên trên
        _moveMotionHandle = LMotion.Create(startPos, targetPos, floatDuration)
            .WithEase(Ease.OutQuad)
            .BindToPosition(transform);

        // 3. Mờ dần sau một khoảng chờ và thu hồi về Pool
        _fadeMotionHandle = LMotion.Create(1f, 0f, fadeDuration)
            .WithDelay(delayBeforeFade)
            .WithEase(Ease.InQuad)
            .WithOnComplete(() =>
            {
                onComplete?.Invoke();
                if (WorldLayerManager.Instance != null)
                {
                    WorldLayerManager.Instance.Hide(this);
                }
            })
            .Bind(alpha =>
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = alpha;
                }
            });
    }

    public void CancelMotions()
    {
        if (_moveMotionHandle.IsActive()) _moveMotionHandle.TryCancel();
        if (_fadeMotionHandle.IsActive()) _fadeMotionHandle.TryCancel();
        if (_scaleMotionHandle.IsActive()) _scaleMotionHandle.TryCancel();
    }

    public override void CleanupBeforeHide()
    {
        CancelMotions();
        transform.localScale = Vector3.one;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        base.CleanupBeforeHide();
    }

    public override void Hide()
    {
        CleanupBeforeHide();
        base.Hide();
    }
}
