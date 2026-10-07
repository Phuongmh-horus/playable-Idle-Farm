using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prewarmed UI coin burst used when a truck sale pays into CurrencyLayer.
/// All token Images are authored under CurrencyLayer so Luna never creates UI
/// objects at runtime.
/// </summary>
public sealed class CurrencyFlyEffect : MonoBehaviour
{
    private sealed class Token
    {
        public RectTransform Rect;
        public Image Image;
        public bool InUse;
    }

    public static CurrencyFlyEffect Instance { get; private set; }

    [SerializeField, Min(1)] private int prewarmCount = 5;
    [SerializeField, Min(0.1f)] private float flightDuration = 0.55f;
    [SerializeField, Min(0f)] private float stagger = 0.045f;
    [SerializeField, Min(1)] private int coinsPerPayout = 5;
    [Header("Prewarmed inspector references")]
    [SerializeField] private RectTransform flyRoot;
    [SerializeField] private Image[] prewarmedTokens;
    [SerializeField] private RectTransform payoutSource;

    private readonly List<Token> _tokens = new List<Token>(6);
    private RectTransform _flyRoot;
    private RectTransform _target;
    private Canvas _canvas;
    private Canvas _sourceCanvas;
    private PlayableCurrencyDisplay _display;
    private bool _isPlaying;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // LunaUIManager can resolve this layer before sibling Awake calls.
        // Retry here so the prewarm always occurs before the first payout.
        if (_flyRoot == null)
        {
            Initialize(GetComponent<PlayableCurrencyDisplay>());
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Initialize(PlayableCurrencyDisplay display)
    {
        if (display == null || _flyRoot != null)
        {
            return;
        }

        _target = display.ReceiveTarget;
        _display = display;
        _canvas = display.GetComponentInParent<Canvas>();
        if (_target == null || _canvas == null || flyRoot == null || prewarmedTokens == null)
        {
            Debug.LogError("CurrencyFlyEffect needs an FX pool root and prewarmed token Images on CurrencyLayer.", this);
            return;
        }

        _flyRoot = flyRoot;
        _tokens.Clear();
        for (int i = 0; i < prewarmedTokens.Length; i++)
        {
            Image image = prewarmedTokens[i];
            if (image == null)
            {
                continue;
            }

            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            _tokens.Add(new Token { Rect = image.rectTransform, Image = image });
        }

        int requiredCount = Mathf.Max(prewarmCount, coinsPerPayout);
        if (_tokens.Count < requiredCount)
        {
            Debug.LogError("CurrencyFlyEffect needs at least " + requiredCount + " prewarmed token Images.", this);
            _tokens.Clear();
        }
    }

    public void SetPayoutSource(Transform source)
    {
        payoutSource = source as RectTransform;
        _sourceCanvas = payoutSource != null ? payoutSource.GetComponentInParent<Canvas>() : null;
    }

    public static bool PlayFromWorld(Vector3 worldPosition, Action onComplete)
    {
        return Instance != null && Instance.Play(worldPosition, onComplete);
    }

    public static bool PlayFromSellout(Action onComplete)
    {
        return Instance != null && Instance.PlayFromPayoutSource(onComplete);
    }

    private bool Play(Vector3 worldPosition, Action onComplete)
    {
        if (_isPlaying || _flyRoot == null || _target == null || _tokens.Count == 0)
        {
            return false;
        }

        Camera sceneCamera = Camera.main;
        if (sceneCamera == null)
        {
            return false;
        }

        Vector2 start = Vector2.zero;
        if (!TryGetCanvasPosition(sceneCamera.WorldToScreenPoint(worldPosition), out start)) return false;
        return PlayFromScreenPosition(start, onComplete);
    }

    private bool PlayFromPayoutSource(Action onComplete)
    {
        if (payoutSource == null || _flyRoot == null) return false;

        if (payoutSource.root == _flyRoot.root)
        {
            Vector3 sourceWorldPosition = payoutSource.TransformPoint(payoutSource.rect.center);
            Vector2 localStart = _flyRoot.InverseTransformPoint(sourceWorldPosition);
            return PlayFromScreenPosition(localStart, onComplete);
        }

        Camera sourceCamera = _sourceCanvas != null && _sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? (_sourceCanvas.worldCamera != null ? _sourceCanvas.worldCamera : Camera.main)
            : null;
        Vector2 start;
        if (!TryGetCanvasPosition(RectTransformUtility.WorldToScreenPoint(sourceCamera, payoutSource.position), out start))
        {
            return false;
        }

        return PlayFromScreenPosition(start, onComplete);
    }

    private bool PlayFromScreenPosition(Vector2 start, Action onComplete)
    {
        if (_isPlaying || _flyRoot == null || _target == null || _tokens.Count == 0) return false;

        Vector2 end;
        Camera canvasCamera = GetCanvasCamera();
        if (!TryGetCanvasPosition(RectTransformUtility.WorldToScreenPoint(canvasCamera, _target.position), out end))
        {
            return false;
        }

        start = ToTokenAnchoredPosition(start);
        end = ToTokenAnchoredPosition(end);

        _isPlaying = true;
        SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_cash_fly);
        StartCoroutine(PlayBatch(start, end, onComplete));
        return true;
    }

    private IEnumerator PlayBatch(Vector2 start, Vector2 end, Action onComplete)
    {
        int count = Mathf.Min(coinsPerPayout, _tokens.Count);
        int completed = 0;
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(FlyToken(_tokens[i], start, end, i * stagger, () => completed++));
        }

        while (completed < count)
        {
            yield return null;
        }

        _isPlaying = false;
        SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_cash);
        _display?.PlayReceiveFeedback();
        onComplete?.Invoke();
    }

    private IEnumerator FlyToken(Token token, Vector2 start, Vector2 end, float delay, Action onComplete)
    {
        token.InUse = true;
        token.Rect.gameObject.SetActive(true);
        token.Rect.anchoredPosition = start;
        token.Rect.localScale = Vector3.one;
        token.Image.color = Color.white;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        float horizontal = UnityEngine.Random.Range(-90f, 90f);
        Vector2 control = Vector2.Lerp(start, end, 0.45f) + new Vector2(horizontal, 130f);
        float duration = Mathf.Max(0.1f, flightDuration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            float inverse = 1f - eased;
            token.Rect.anchoredPosition = inverse * inverse * start + 2f * inverse * eased * control + eased * eased * end;
            token.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.72f, eased);
            if (t >= 0.8f)
            {
                Color color = token.Image.color;
                color.a = Mathf.InverseLerp(1f, 0.8f, t);
                token.Image.color = color;
            }
            yield return null;
        }

        token.Rect.anchoredPosition = end;
        token.Rect.gameObject.SetActive(false);
        token.InUse = false;
        onComplete?.Invoke();
    }

    private Camera GetCanvasCamera()
    {
        return _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? (_canvas.worldCamera != null ? _canvas.worldCamera : Camera.main)
            : null;
    }

    private bool TryGetCanvasPosition(Vector2 screenPosition, out Vector2 canvasPosition)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _flyRoot, screenPosition, GetCanvasCamera(), out canvasPosition);
    }

    private Vector2 ToTokenAnchoredPosition(Vector2 localPosition)
    {
        // CurrencyFlyPool uses bottom-left anchored token RectTransforms,
        // while local positions returned by RectTransformUtility are pivot-relative.
        return localPosition - _flyRoot.rect.min;
    }
}
