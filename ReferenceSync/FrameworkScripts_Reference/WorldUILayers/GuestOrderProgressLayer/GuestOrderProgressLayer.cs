using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuestOrderProgressLayer : WorldLayerBase
{
    [SerializeField] private Image imgFruitIcon;
    [SerializeField] private TextMeshProUGUI txtProgress;
    [SerializeField] private Image imgFill;

    private int _currentCount;
    private int _requiredCount;

    protected override void Awake()
    {
        base.Awake();
        blockSelectionClick = false;

        if (txtProgress == null) txtProgress = GetComponentInChildren<TextMeshProUGUI>(true);
        if (imgFruitIcon == null)
        {
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img != imgFill)
                {
                    imgFruitIcon = img;
                    break;
                }
            }
        }
    }

    public void InitData(Sprite fruitIcon = null, int current = 0, int required = 1)
    {
        _currentCount = current;
        _requiredCount = Mathf.Max(1, required);

        if (imgFruitIcon != null)
        {
            if (fruitIcon != null)
            {
                imgFruitIcon.sprite = fruitIcon;
                imgFruitIcon.gameObject.SetActive(true);
            }
            else
            {
                imgFruitIcon.gameObject.SetActive(false);
            }
        }

        UpdateDisplay();
    }

    public void UpdateProgress(int current, int required = -1)
    {
        _currentCount = current;
        if (required > 0)
        {
            _requiredCount = required;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (txtProgress != null)
        {
            txtProgress.text = $"{_currentCount}/{_requiredCount}";
        }

        if (imgFill != null)
        {
            float fillRatio = Mathf.Clamp01((float)_currentCount / _requiredCount);
            imgFill.fillAmount = fillRatio;
        }
    }
}
