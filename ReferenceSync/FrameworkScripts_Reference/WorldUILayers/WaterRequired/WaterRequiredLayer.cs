using System;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

public class WaterRequiredLayer : WorldLayerBase
{
    [SerializeField] private Image fillImage;

    private MotionHandle _fillMotionHandle;
    private MotionHandle _scaleMotionHandle;
    private MotionHandle _delayMotionHandle;

    protected override void Reset()
    {
        base.Reset();
        layerDepth = WorldLayerDepth.StatusIcon;
        ApplySorting();
    }

    protected override void Awake()
    {
        if (layerDepth == WorldLayerDepth.Popup)
        {
            layerDepth = WorldLayerDepth.StatusIcon;
        }
        base.Awake();
        ResolveFillImage();
    }

    private void ResolveFillImage()
    {
        if (fillImage != null) return;
        if (transform.childCount > 1)
        {
            fillImage = transform.GetChild(1).GetComponent<Image>();
        }
        if (fillImage == null)
        {
            fillImage = GetComponentInChildren<Image>();
        }
    }

    public void AnimateWater(float startFill, float targetFill, float duration, float delayBeforeRelease, Action onComplete = null)
    {
        CancelMotions();
        ResolveFillImage();

        if (fillImage != null)
        {
            fillImage.fillAmount = startFill;
        }

        transform.localScale = Vector3.one;

        _fillMotionHandle = LMotion.Create(startFill, targetFill, duration)
            .WithEase(Ease.OutQuad)
            .WithOnComplete(() =>
            {
                if (delayBeforeRelease > 0f)
                {
                    _delayMotionHandle = LMotion.Create(0f, 1f, delayBeforeRelease)
                        .WithOnComplete(() => RunExitAnimation(onComplete))
                        .RunWithoutBinding();
                }
                else
                {
                    RunExitAnimation(onComplete);
                }
            })
            .Bind(val =>
            {
                if (fillImage != null)
                {
                    fillImage.fillAmount = val;
                }
            });
    }

    private void RunExitAnimation(Action onComplete)
    {
        _scaleMotionHandle = LMotion.Create(Vector3.one, Vector3.zero, 0.2f)
            .WithEase(Ease.InBack)
            .WithOnComplete(() =>
            {
                onComplete?.Invoke();
                if (WorldLayerManager.Instance != null)
                {
                    WorldLayerManager.Instance.Hide(this);
                }
            })
            .BindToLocalScale(transform);
    }

    public void CancelMotions()
    {
        if (_fillMotionHandle.IsActive()) _fillMotionHandle.TryCancel();
        if (_scaleMotionHandle.IsActive()) _scaleMotionHandle.TryCancel();
        if (_delayMotionHandle.IsActive()) _delayMotionHandle.TryCancel();
    }

    public override void CleanupBeforeHide()
    {
        CancelMotions();
        transform.localScale = Vector3.one;
        base.CleanupBeforeHide();
    }

    public override void Hide()
    {
        CleanupBeforeHide();
        base.Hide();
    }
}