using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight adapter between the playable's in-memory gold balance and the
/// existing CurrencyLayer artwork. It intentionally has no persistence or
/// animation dependency: every sale and upgrade uses PlayableFarmState as the source.
/// </summary>
public sealed class PlayableCurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Image coinIcon;
    [SerializeField] private RectTransform receiveTarget;
    private Coroutine _pulseRoutine;

    public RectTransform ReceiveTarget => receiveTarget != null ? receiveTarget : transform as RectTransform;

    private void Awake()
    {
        if (goldText == null) goldText = GetComponentInChildren<TMP_Text>(true);
        if (coinIcon == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].name.ToLowerInvariant().Contains("icon"))
                {
                    coinIcon = images[i];
                    break;
                }
            }
        }
        if (receiveTarget == null && coinIcon != null) receiveTarget = coinIcon.rectTransform;
    }

    private void OnEnable()
    {
        GameEventBus.OnGoldChange += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameEventBus.OnGoldChange -= Refresh;
    }

    public void Refresh()
    {
        if (goldText != null) goldText.text = T_Utility.FormatGold(PlayableFarmState.PlayerData.Gold);
    }

    public void PlayReceiveFeedback()
    {
        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private System.Collections.IEnumerator PulseRoutine()
    {
        Vector3 baseScale = transform.localScale;
        const float duration = 0.18f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.1f;
            transform.localScale = baseScale * scale;
            yield return null;
        }
        transform.localScale = baseScale;
        _pulseRoutine = null;
    }
}
