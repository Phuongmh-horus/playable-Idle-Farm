using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderValidator : MonoBehaviour
{
    [SerializeField] private Slider slider;
    public Vector2 Range = Vector2.up;
    [Range(0f, 1f)] public float Progress;
    private Tween motionTween;

    [SerializeField] private Image fillImage;
    void Start()
    {
        Value = 0f;
    }
    private void OnValidate()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        UpdateProgress();
    }

    public float Value
    {
        get => (Progress - Range.x) / (Range.y - Range.x);
        set
        {
            Progress = Mathf.Clamp(value * (Range.y - Range.x) + Range.x, Range.x, Range.y);
            UpdateProgress();
        }
    }

    public void SetValueSmooth(float value, float duration)
    {
        if (motionTween != null && motionTween.IsActive())
        {
            motionTween.Kill();
        }

        var targetValue = Mathf.Clamp(value * (Range.y - Range.x) + Range.x, Range.x, Range.y);
        motionTween = DOVirtual.Float(Progress, targetValue, duration, x =>
        {
            Progress = x;
            UpdateProgress();
        }).SetEase(Ease.OutQuad);
    }

    private void UpdateProgress()
    {
        if (slider)
        {
            slider.value = Mathf.Clamp(Progress, Range.x, Range.y);
            if (slider.value <= 0.01f)
            {
                slider.value = 0f;
                fillImage.enabled = false;
            }
            else
            {
                fillImage.enabled = true;
            }
        }
    }
}
