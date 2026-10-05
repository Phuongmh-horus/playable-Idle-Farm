using UnityEngine;

public class PlotVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer plotRenderer;

    [Header("Luna state renderers")]
    [SerializeField] private SpriteRenderer ringLockedRenderer;
    [SerializeField] private SpriteRenderer lockedRenderer;
    [SerializeField] private SpriteRenderer emptyRenderer;
    [SerializeField] private SpriteRenderer occupiedRenderer;

    [Header("Visual Config SO")]
    [SerializeField] private PlotVisualConfigSO visualConfig;

    [Header("Luna-safe direct sprites")]
    [SerializeField] private Sprite ringLockedSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite occupiedSprite;

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
        EnsureRenderer();
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
        if (plotRenderer == null)
        {
            return;
        }

        // Keep the complete state-to-sprite mapping inside this exported method.
        // Luna can strip private helpers that are not present in its generated manifest.
        Sprite newSprite;
        switch (state)
        {
            case FarmPlotState.RingLocked:
                newSprite = ringLockedSprite;
                break;
            case FarmPlotState.Locked:
                newSprite = lockedSprite;
                break;
            case FarmPlotState.Empty:
                newSprite = emptySprite;
                break;
            case FarmPlotState.Occupied:
                newSprite = occupiedSprite;
                break;
            default:
                newSprite = null;
                break;
        }

        SpriteRenderer stateRenderer = GetStateRenderer(state);
        if (stateRenderer != null)
        {
            SetStateRenderersActive(stateRenderer);
            ResetRenderers(stateRenderer);

            return;
        }

        plotRenderer.gameObject.SetActive(false);
        plotRenderer.enabled = false;

        plotRenderer.sprite = null;
        plotRenderer.sprite = newSprite;
        plotRenderer.enabled = true;
        plotRenderer.gameObject.SetActive(true);
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
    }

    private void ResetRenderers(SpriteRenderer activeRenderer)
    {
        if (activeRenderer == null) return;
        Color color = activeRenderer.color;
        color.a = 1f;
        activeRenderer.color = color;
    }

    private SpriteRenderer GetStateRenderer(FarmPlotState state)
    {
        switch (state)
        {
            case FarmPlotState.RingLocked: return ringLockedRenderer;
            case FarmPlotState.Locked: return lockedRenderer;
            case FarmPlotState.Empty: return emptyRenderer;
            case FarmPlotState.Occupied: return occupiedRenderer;
            default: return null;
        }
    }

    private void SetStateRenderersActive(SpriteRenderer activeRenderer)
    {
        SpriteRenderer[] stateRenderers = { ringLockedRenderer, lockedRenderer, emptyRenderer, occupiedRenderer };
        for (int i = 0; i < stateRenderers.Length; i++)
        {
            SpriteRenderer renderer = stateRenderers[i];
            if (renderer == null) continue;
            bool isActive = renderer == activeRenderer;
            renderer.enabled = isActive;
            renderer.gameObject.SetActive(isActive);
        }
    }

    private void EnsureRenderer()
    {
        if (plotRenderer == null)
        {
            plotRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        // Scene instances created before the Luna renderer fields were added can
        // override those serialized references with null. Resolve the authored
        // state children by name so old scene data still uses the cache-safe path.
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null) continue;

            switch (renderer.gameObject.name)
            {
                case "SpriteLocked":
                    if (lockedRenderer == null) lockedRenderer = renderer;
                    break;
                case "SpriteEmpty":
                    if (emptyRenderer == null) emptyRenderer = renderer;
                    break;
                case "SpriteOccupied":
                    if (occupiedRenderer == null) occupiedRenderer = renderer;
                    break;
            }
        }

        if (ringLockedRenderer == null && plotRenderer != null)
        {
            ringLockedRenderer = plotRenderer;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureRenderer();
        // Keep direct sprite bindings explicit in the prefab. Runtime lookup
        // through a ScriptableObject is intentionally not used by PLA.
    }

    public void Setup(SpriteRenderer renderer, PlotVisualConfigSO config)
    {
        plotRenderer = renderer;
        visualConfig = config;
        OnValidate();
    }
#endif
}
