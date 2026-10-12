using UnityEngine;

public class PlotVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer plotRenderer;

    [Header("Luna state renderers")]
    [SerializeField] private SpriteRenderer ringLockedRenderer;
    [SerializeField] private SpriteRenderer lockedRenderer;
    [SerializeField] private SpriteRenderer emptyRenderer;
    [SerializeField] private SpriteRenderer occupiedRenderer;

    private FarmPlotState _currentState = (FarmPlotState)(-1);
    private bool _hasState;

    private void Awake()
    {
        EnsureRendererReferences();
    }

    private void OnEnable()
    {
        EnsureRendererReferences();
        if (_hasState)
        {
            ApplyState(_currentState);
        }
    }

    public void SetState(FarmPlotState state, bool animate = true)
    {
        _currentState = state;
        _hasState = true;
        ApplyState(state);
    }

    private void ApplyState(FarmPlotState state)
    {
        SpriteRenderer activeRenderer = GetStateRenderer(state);
        if (activeRenderer != null)
        {
            SetRendererActive(ringLockedRenderer, ringLockedRenderer == activeRenderer);
            SetRendererActive(lockedRenderer, lockedRenderer == activeRenderer);
            SetRendererActive(emptyRenderer, emptyRenderer == activeRenderer);
            SetRendererActive(occupiedRenderer, occupiedRenderer == activeRenderer);
            return;
        }

        // Fallback tối thiểu nếu chạy trên GameObject chưa gắn các child renderer phụ
        if (plotRenderer != null)
        {
            SetRendererActive(plotRenderer, true);
        }
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

    private static void SetRendererActive(SpriteRenderer renderer, bool isActive)
    {
        if (renderer == null) return;
        renderer.enabled = isActive;
        GameObject go = renderer.gameObject;
        if (go.activeSelf != isActive)
        {
            go.SetActive(isActive);
        }
    }

    private void EnsureRendererReferences()
    {
        if (plotRenderer == null)
        {
            plotRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }
        if (ringLockedRenderer == null && plotRenderer != null)
        {
            ringLockedRenderer = plotRenderer;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureRendererReferences();
    }
#endif
}
