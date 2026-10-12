using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderValidator : MonoBehaviour
{
    [SerializeField] private Slider slider;
    public Vector2 Range = Vector2.up;
    [Range(0f, 1f)] public float Progress;
    private bool _isAnimating;
    private float _animationElapsed;
    private float _animationDuration;
    private float _animationStart;
    private float _animationTarget;

    [SerializeField] private Image fillImage;
    void Start()
    {
        Value = 0f;
        enabled = false;
    }
    private void OnValidate()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        UpdateProgress();
    }

    private void Update()
    {
        if (!_isAnimating)
        {
            enabled = false;
            return;
        }

        _animationElapsed += Time.unscaledDeltaTime;
        float progress = _animationDuration <= 0f ? 1f : Mathf.Clamp01(_animationElapsed / _animationDuration);
        float eased = progress * progress * (3f - 2f * progress);
        Progress = Mathf.Lerp(_animationStart, _animationTarget, eased);
        UpdateProgress();
        if (progress >= 1f)
        {
            _isAnimating = false;
            enabled = false;
        }
    }

    public float Value
    {
        get => Mathf.Approximately(Range.x, Range.y) ? 0f : (Progress - Range.x) / (Range.y - Range.x);
        set
        {
            Progress = Mathf.Clamp(value * (Range.y - Range.x) + Range.x, Range.x, Range.y);
            UpdateProgress();
        }
    }

    public void SetValueSmooth(float value, float duration)
    {
        _animationStart = Progress;
        _animationTarget = Mathf.Clamp(value * (Range.y - Range.x) + Range.x, Range.x, Range.y);
        _animationDuration = Mathf.Max(0f, duration);
        _animationElapsed = 0f;
        _isAnimating = true;
        enabled = true;
    }

    private void UpdateProgress()
    {
        if (slider)
        {
            slider.value = Mathf.Clamp(Progress, Range.x, Range.y);
            if (fillImage == null) return;
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
