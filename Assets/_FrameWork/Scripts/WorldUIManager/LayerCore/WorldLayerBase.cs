using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(RectTransform))]
[RequireComponent(typeof(BillboardJobFollower))]
public class WorldLayerBase : MonoBehaviour
{
    private static readonly List<WorldLayerBase> ActiveInteractableLayers = new List<WorldLayerBase>();

    [Header("─── Components ───")]
    [SerializeField] protected Canvas canvas;
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected RectTransform rectTransform;
    [SerializeField] protected GraphicRaycaster graphicRaycaster;
    [SerializeField] protected BillboardJobFollower billboardFollower;
    [SerializeField] protected AnimShowPopupWorld animHandler;

    [Header("─── Sorting / Depth ───")]
    [SerializeField] protected WorldLayerDepth layerDepth = WorldLayerDepth.Popup;

    [Header("─── Interaction ───")]
    [SerializeField] protected bool blockSelectionClick = true;

    [Header("─── Camera Focus ───")]
    [Tooltip("Tự động di chuyển và zoom camera vào popup khi popup này hiện lên.")]
    [SerializeField] protected bool focusCameraOnShow = false;

    [Tooltip("Sử dụng offset riêng cho popup này thay vì dùng offset chung trong CameraSettings.")]
    [SerializeField] protected bool useCustomFocusOffset = false;

    [SerializeField] protected Vector3 customFocusOffset = Vector3.zero;

    protected Transform followTarget;
    protected Vector3 worldOffset;
    protected Camera mainCamera;

    public WorldLayerType LayerType { get; set; }
    public WorldLayerDepth LayerDepth
    {
        get => layerDepth;
        set
        {
            layerDepth = value;
            ApplySorting();
        }
    }
    public Transform FollowTarget => followTarget;
    public bool BlockSelectionClick => blockSelectionClick;
    public RectTransform RectTransform => rectTransform;
    public BillboardJobFollower BillboardFollower => billboardFollower;

    public UnityEvent OnShowLayer = new UnityEvent();
    public UnityEvent OnHideLayer = new UnityEvent();

    protected bool isHiding;
    public bool IsHiding => isHiding;

    public virtual void ApplySorting()
    {
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            Canvas parentCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>() : null;
            if (parentCanvas != null)
            {
                canvas.sortingLayerID = parentCanvas.sortingLayerID;
            }
            canvas.sortingOrder = (int)layerDepth;
        }
    }

    private void OnValidate()
    {
        if (animHandler == null) animHandler = GetComponent<AnimShowPopupWorld>();
        if (billboardFollower == null) billboardFollower = GetComponent<BillboardJobFollower>();
    }

    protected virtual void Reset()
    {
        if (canvas == null) canvas = GetComponent<Canvas>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (graphicRaycaster == null) graphicRaycaster = GetComponent<GraphicRaycaster>();
        if (animHandler == null) animHandler = GetComponent<AnimShowPopupWorld>();
        if (billboardFollower == null) billboardFollower = GetComponent<BillboardJobFollower>();

        if (canvas)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            ApplySorting();
        }
    }

    protected virtual void Awake()
    {
        if (!canvas) canvas = GetComponent<Canvas>();
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!rectTransform) rectTransform = GetComponent<RectTransform>();
        if (!graphicRaycaster) graphicRaycaster = GetComponent<GraphicRaycaster>();
        if (!billboardFollower) billboardFollower = GetComponent<BillboardJobFollower>();

        mainCamera = CameraManager.Instance != null ? CameraManager.Instance.MainCamera : Camera.main;

        SetupCameraForWorldCanvas();
        ApplySorting();
    }

    protected virtual void OnEnable()
    {
        if (blockSelectionClick && !ActiveInteractableLayers.Contains(this))
        {
            ActiveInteractableLayers.Add(this);
        }
    }

    protected virtual void OnDisable()
    {
        ActiveInteractableLayers.Remove(this);
        WorldLayerManager manager = WorldLayerManager.Instance;
        if (manager != null)
        {
            manager.NotifyLayerDisabled(this);
        }
    }

    protected virtual void OnDestroy()
    {
        ActiveInteractableLayers.Remove(this);
    }

    private void SetupCameraForWorldCanvas()
    {
        if (canvas != null)
        {
            // All WorldLayerBase instances follow a scene target, so they must
            // render in world space regardless of the imported prefab setting.
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera != null ? mainCamera : Camera.main;
        }
    }

    public static bool IsPointerOverAnyWorldLayer(Vector2 screenPoint, Camera cam)
    {
        if (cam == null) cam = Camera.main;

        for (int i = ActiveInteractableLayers.Count - 1; i >= 0; i--)
        {
            var item = ActiveInteractableLayers[i];
            if (item == null)
            {
                ActiveInteractableLayers.RemoveAt(i);
                continue;
            }

            if (!item.gameObject.activeInHierarchy || !item.enabled || !item.blockSelectionClick || item.rectTransform == null)
                continue;

            if (item.canvas != null && !item.canvas.enabled)
                continue;

            if (item.canvasGroup != null && (!item.canvasGroup.blocksRaycasts || item.canvasGroup.alpha <= 0f))
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(item.rectTransform, screenPoint, cam))
                return true;
        }

        return false;
    }

    public virtual void SetupFollow(Transform target, Vector3 offset = default)
    {
        followTarget = target;
        worldOffset = offset;

        if (billboardFollower != null)
        {
            billboardFollower.SetTarget(target, offset);
            billboardFollower.ApplyRotationOnce();
        }

        Show();
    }

    public virtual void SetupPosition(Vector3 position)
    {
        followTarget = null;
        transform.position = position;

        if (billboardFollower != null)
        {
            billboardFollower.SetTarget(null);
            billboardFollower.ApplyRotationOnce();
        }

        Show();
    }

    public virtual void Show()
    {
        isHiding = false;
        SetupCameraForWorldCanvas();

        if (canvas) canvas.enabled = true;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        else
        {
            if (blockSelectionClick && !ActiveInteractableLayers.Contains(this))
            {
                ActiveInteractableLayers.Add(this);
            }
        }

        OnShowLayer?.Invoke();
    }

    public virtual void CleanupBeforeHide()
    {
        ActiveInteractableLayers.Remove(this);

        if (canvas) canvas.enabled = false;

        if (billboardFollower != null)
        {
            billboardFollower.SetTarget(null);
        }

        followTarget = null;
        OnHideLayer?.Invoke();
    }

    public virtual void Hide()
    {
        Hide(true);
    }

    public virtual void Hide(bool animate)
    {
        if (isHiding) return;

        if (animate && animHandler != null && gameObject.activeInHierarchy && canvas != null && canvas.enabled)
        {
            isHiding = true;
            ActiveInteractableLayers.Remove(this);

            if (canvasGroup)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            animHandler.PlayHideAnim(() =>
            {
                isHiding = false;
                FinishHide();
            });
        }
        else
        {
            isHiding = false;
            FinishHide();
        }
    }

    protected virtual void FinishHide()
    {
        if (WorldLayerManager.Instance != null)
        {
            WorldLayerManager.Instance.ReleaseToPool(this);
        }
        else
        {
            CleanupBeforeHide();
            gameObject.SetActive(false);
        }
    }
}
