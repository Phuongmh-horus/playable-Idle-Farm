using UnityEngine;

public class PlotVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer plotRenderer;

    [Header("Visual Config SO")]
    [SerializeField] private PlotVisualConfigSO visualConfig;

    public PlotVisualConfigSO VisualConfig
    {
        get => visualConfig;
        set => visualConfig = value;
    }

    private void Awake()
    {
        EnsureRenderer();
    }

    /// <summary>
    /// Cập nhật hiển thị theo FarmPlotState thông qua ScriptableObject
    /// </summary>
    public void SetState(FarmPlotState state)
    {
        EnsureRenderer();
        if (plotRenderer == null || visualConfig == null)
            return;

        Sprite spriteFromConfig = visualConfig.GetSprite(state);
        if (spriteFromConfig != null)
        {
            plotRenderer.sprite = spriteFromConfig;
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
