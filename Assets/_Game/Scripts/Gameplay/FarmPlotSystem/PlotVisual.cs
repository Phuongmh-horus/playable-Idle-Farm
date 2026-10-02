using System.Collections;
using UnityEngine;

public class PlotVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer plotRenderer;
    [SerializeField] private SpriteRenderer overlayRenderer;

    [Header("Visual Config SO")]
    [SerializeField] private PlotVisualConfigSO visualConfig;

    private Coroutine _fadeCoroutine;

    public PlotVisualConfigSO VisualConfig
    {
        get => visualConfig;
        set => visualConfig = value;
    }

    private void Awake()
    {
        EnsureRenderer();
    }

    private void OnEnable()
    {
        CancelFade();
        ResetRenderers();
    }

    private void OnDisable()
    {
        CancelFade();
        ResetRenderers();
    }

    private void OnDestroy()
    {
        CancelFade();
    }

    /// <summary>
    /// Cập nhật hiển thị theo FarmPlotState thông qua ScriptableObject
    /// </summary>
    public void SetState(FarmPlotState state, bool animate = true)
    {
        EnsureRenderer();
        if (plotRenderer == null || visualConfig == null)
            return;

        Sprite newSprite = visualConfig.GetSprite(state);
        if (newSprite == null || plotRenderer.sprite == newSprite)
            return;

        Sprite oldSprite = plotRenderer.sprite;
        CancelFade();

        FarmPlotConfigSO plotConfig = ConfigHolder.Instance != null ? ConfigHolder.Instance.FarmPlotConfigSO : null;
        float duration = plotConfig != null ? plotConfig.fadeDuration : 0.25f;
        if (!animate || oldSprite == null || duration <= 0f)
        {
            plotRenderer.sprite = newSprite;
            ResetRenderers();
            return;
        }

        EnsureOverlayRenderer();
        if (overlayRenderer != null)
        {
            CopyRendererState(plotRenderer, overlayRenderer);
            overlayRenderer.sprite = oldSprite;
            SetAlpha(overlayRenderer, 1f);
            overlayRenderer.gameObject.SetActive(true);
        }

        plotRenderer.sprite = newSprite;
        SetAlpha(plotRenderer, 0f);
        _fadeCoroutine = StartCoroutine(FadeRoutine(duration));
    }

    private IEnumerator FadeRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            ApplyFade(progress * progress * (3f - 2f * progress));
            yield return null;
        }

        _fadeCoroutine = null;
        ResetRenderers();
    }

    private void ApplyFade(float progress)
    {
        SetAlpha(plotRenderer, progress);
        SetAlpha(overlayRenderer, 1f - progress);
    }

    private void EnsureOverlayRenderer()
    {
        if (overlayRenderer != null || plotRenderer == null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("PlotTransitionOverlay");
        Transform sourceTransform = plotRenderer.transform;
        Transform overlayTransform = overlayObject.transform;
        overlayTransform.SetParent(sourceTransform.parent, false);
        overlayTransform.localPosition = sourceTransform.localPosition;
        overlayTransform.localRotation = sourceTransform.localRotation;
        overlayTransform.localScale = sourceTransform.localScale;
        overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
    }

    private static void CopyRendererState(SpriteRenderer source, SpriteRenderer target)
    {
        target.sharedMaterial = source.sharedMaterial;
        target.color = source.color;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder;
        target.flipX = source.flipX;
        target.flipY = source.flipY;
        target.drawMode = source.drawMode;
        target.size = source.size;
        target.maskInteraction = source.maskInteraction;
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private void ResetRenderers()
    {
        SetAlpha(plotRenderer, 1f);
        if (overlayRenderer != null)
        {
            overlayRenderer.sprite = null;
            overlayRenderer.gameObject.SetActive(false);
        }
    }

    private void CancelFade()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
    }

    private void EnsureRenderer()
    {
        if (plotRenderer == null)
        {
            plotRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureRenderer();
    }

    public void Setup(SpriteRenderer renderer, PlotVisualConfigSO config)
    {
        plotRenderer = renderer;
        visualConfig = config;
    }
#endif
}
