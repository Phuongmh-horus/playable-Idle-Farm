using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct CtaTextProfile
{
    public string title;
    [TextArea] public string message;
    public string buttonLabel;
}

public sealed class CTAPresenter : MonoBehaviour, ICTAPresenter
{
    [Header("References")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Button ctaButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text buttonText;

    [Header("Text Profiles")]
    [SerializeField] private CtaTextProfile winProfile;
    [SerializeField] private CtaTextProfile loseProfile;
    [SerializeField] private CtaTextProfile genericProfile;

    [Header("Behavior")]
    [SerializeField] private bool hideOnButtonClick;

    public event Action CtaButtonClicked;

    public bool IsVisible { get; private set; }

    private void Awake()
    {
        if (rootObject == null) rootObject = gameObject;
        if (rootCanvasGroup == null) rootCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (ctaButton != null) ctaButton.onClick.AddListener(HandleButtonClicked);
    }

    private void OnDisable()
    {
        if (ctaButton != null) ctaButton.onClick.RemoveListener(HandleButtonClicked);
    }

    public void Show(CtaReason reason)
    {
        if (rootObject != null) rootObject.SetActive(true);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
        }

        ApplyProfile(reason);
        IsVisible = true;
    }

    public void Hide()
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (rootObject != null) rootObject.SetActive(false);
        IsVisible = false;
    }

    private void HandleButtonClicked()
    {
        CtaButtonClicked?.Invoke();
        if (hideOnButtonClick) Hide();
    }

    private void ApplyProfile(CtaReason reason)
    {
        var profile = ResolveProfile(reason);

        if (titleText != null) titleText.text = profile.title;
        if (messageText != null) messageText.text = profile.message;
        if (buttonText != null) buttonText.text = profile.buttonLabel;
    }

    private CtaTextProfile ResolveProfile(CtaReason reason)
    {
        switch (reason)
        {
            case CtaReason.Win:
                return winProfile;
            case CtaReason.Lose:
            case CtaReason.PreLose:
                return loseProfile;
            default:
                return genericProfile;
        }
    }
}
