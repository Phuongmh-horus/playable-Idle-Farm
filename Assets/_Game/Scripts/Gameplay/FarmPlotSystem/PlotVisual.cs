using UnityEngine;

public class PlotVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer plotRenderer;
    [SerializeField] private SpriteRenderer overlayRenderer;

    [Header("Visual Config SO")]
    [SerializeField] private PlotVisualConfigSO visualConfig;

    public PlotVisualConfigSO VisualConfig
    {
        get { return visualConfig; }
        set { visualConfig = value; }
    }

    private void Awake()
    {
        EnsureRenderer();
        ResetRenderers();
    }

    private void OnEnable()
    {
        ResetRenderers();
    }

    private void OnDisable()
    {
        ResetRenderers();
    }

    // Luna excludes the old coroutine transition methods. Keep plot state changes
    // immediate so an obsolete overlay can never hide the newly assigned sprite.
    public void SetState(FarmPlotState state, bool animate = true)
    {
        EnsureRenderer();
        if (plotRenderer == null || visualConfig == null)
        {
            return;
        }

        Sprite newSprite = visualConfig.GetSprite(state);
        if (newSprite == null)
        {
            return;
        }

        plotRenderer.sprite = newSprite;
        ResetRenderers();
    }

    private void ResetRenderers()
    {
        if (plotRenderer != null)
        {
            Color color = plotRenderer.color;
            color.a = 1f;
            plotRenderer.color = color;
        }

        if (overlayRenderer != null)
        {
            overlayRenderer.sprite = null;
            overlayRenderer.gameObject.SetActive(false);
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
