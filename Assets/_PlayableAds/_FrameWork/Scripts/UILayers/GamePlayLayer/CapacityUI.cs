using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CapacityUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI capacityUI;
    [SerializeField] private SliderValidator progressSlider;
    [SerializeField] private Canvas highlightCanvas;
    [SerializeField] private Image fillImage;
    [SerializeField] private float duration = 0.2f;

    [SerializeField] private CapacityConfig capacityConfig;

    private List<CapacitySliderStateData> SliderStates => capacityConfig?.sliderStates;

    private int _currentStateIndex = -1;
    private float _displayedPercent = 0f;
    private Tween _textTween;

    private void Start()
    {
        // SetCapacity(0, 100);
        fillImage.enabled = false;

    }
    public void SetCapacity(int capacity, int maxCapacity)
    {
        var percent = maxCapacity > 0 ? Mathf.Clamp01((float)capacity / maxCapacity) : 0f;

        SetProgressValue(percent);
        SetTextValueSmooth(percent);
        ApplyVisualForPercent(percent, false);
    }

    private void SetTextValueSmooth(float targetPercent)
    {
        if (capacityUI == null) return;
        if (_textTween != null && _textTween.IsActive())
        {
            _textTween.Kill();
        }

        if (targetPercent == 0f)
        {
            _displayedPercent = 0f;
            capacityUI.text = "0%";
        }
        else
        {
            _textTween = DOVirtual.Float(_displayedPercent, targetPercent, duration, val =>
            {
                _displayedPercent = val;
                capacityUI.text = Mathf.RoundToInt(val * 100f) + "%";
            }).SetEase(Ease.OutQuad);
        }
    }

    private void OnDestroy()
    {

        if (_textTween != null && _textTween.IsActive())
        {
            _textTween.Kill();
        }
    }

    public void SetTutorialHighlightActive(bool isActive)
    {
        if (highlightCanvas == null) return;
        Debug.Log($"Capacity canvas active: {isActive}, sorting: {highlightCanvas.sortingOrder}");
        highlightCanvas.overrideSorting = isActive;
    }

    private void ApplyVisualForPercent(float percent, bool forceRefresh)
    {
        fillImage.enabled = true;
        if (fillImage == null || SliderStates == null || SliderStates.Count == 0)
        {
            return;
        }

        var matchedIndex = GetMatchedStateIndex(percent);
        if (matchedIndex < 0)
        {
            return;
        }

        if (!forceRefresh && matchedIndex == _currentStateIndex)
        {
            return;
        }

        _currentStateIndex = matchedIndex;
        var state = SliderStates[matchedIndex];
        fillImage.sprite = state.sprite;
        fillImage.type = state.imageType;
        fillImage.SetAllDirty();

        
    }

    private int GetMatchedStateIndex(float percent)
    {
        if (SliderStates == null || SliderStates.Count == 0)
        {
            return -1;
        }

        for (var i = 0; i < SliderStates.Count; i++)
        {
            if (SliderStates[i] != null && SliderStates[i].Matches(percent))
            {
                return i;
            }
        }

        // Fallback: If percent is out of bounds, return the first state for negative percent, or the last state otherwise
        if (percent < 0f)
        {
            return 0;
        }

        return SliderStates.Count - 1;
    }

    public void SetProgressValue(float value)
    {
        if (value == 0)
        {
            // progressSlider.Value = value;
            progressSlider.SetValueSmooth(0f, duration);
        }
        else
        {
            progressSlider.SetValueSmooth(value, duration);
        }
    }
}
