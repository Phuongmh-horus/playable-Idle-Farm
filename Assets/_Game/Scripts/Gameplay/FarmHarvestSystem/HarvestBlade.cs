using System;
using System.Collections.Generic;
using UnityEngine;

public class HarvestBlade : MonoBehaviour
{
    [Header("Visual & Animation")]
    [Tooltip("Transform của phần lưỡi dao hiển thị để tự xoay khi kéo")]
    [SerializeField] private Transform bladeVisual;

    [Tooltip("Tốc độ tự xoay của lưỡi dao khi đang kéo (độ/giây)")]
    [SerializeField] private float bladeSpinSpeed = 720f;

    [Tooltip("Tỉ lệ phóng to khi nhặt/kéo lưỡi dao")]
    [SerializeField] private float dragScale = 1.15f;

    [Tooltip("Thời gian bay về vị trí gốc sau khi thả (giây)")]
    [SerializeField] private float returnDuration = 0.35f;

    [Header("Harvest Detection")]
    [Tooltip("Bán kính quét thu hoạch các ô đất xung quanh lưỡi dao (world units)")]
    [SerializeField] private float harvestRadius = 0.8f;

    [Tooltip("Khoảng cách tối đa (bán kính) để chạm nhặt lưỡi dao (world units)")]
    [SerializeField] private float grabRadius = 1.0f;

    [SerializeField] private ParticleSystem fireFlare;

    [Header("References")]
    [SerializeField] private Transform harvesterBladeRoot;

    private Camera _mainCamera;
    private FarmPlotSystem _plotSystem;
    private bool _isDragging;
    private Vector3 _dragOffset;
    private bool _isReturning;
    private float _returnElapsed;
    private Vector3 _returnStartPosition;
    private Quaternion _returnStartRotation;
    private Vector3 _returnStartScale;
    private float _sliceFeedbackElapsed = -1f;
    private const float SliceFeedbackDuration = 0.16f;

    public static HarvestBlade Instance { get; private set; }

    public bool IsDragging => _isDragging;
    public Transform BladeRoot => harvesterBladeRoot;
    public Transform Visual => bladeVisual != null ? bladeVisual : transform;
    public FarmPlotSystem PlotSystem => _plotSystem != null ? _plotSystem : (_plotSystem = FarmSystem.Instance?.PlotSystem);

    public event Action<Plot, double> OnPlantHarvested;
    public event Action OnDragStarted;
    public event Action OnDragEnded;

    private void Awake()
    {
        Instance = this;
        _mainCamera = Camera.main;
        // if (fireFlare == null)
        // {
        //     fireFlare = GetComponentInChildren<ParticleSystem>();
        // }
    }

    private void OnDestroy()
    {
        CancelMotions();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDisable()
    {
        // StopFireFlare();
        CancelMotions();
        _isDragging = false;
        transform.localScale = Vector3.one;
    }

    public void Initialize(Transform root, FarmPlotSystem plots = null)
    {
        Instance = this;
        harvesterBladeRoot = root;
        _plotSystem = plots != null ? plots : FarmSystem.Instance?.PlotSystem;
        SnapToRoot();
    }

    /// <summary>
    /// Đưa HarvestBlade về ngay vị trí gốc
    /// </summary>
    public void SnapToRoot()
    {
        CancelMotions();
        _isDragging = false;
        // StopFireFlare();

        if (harvesterBladeRoot != null)
        {
            transform.position = harvesterBladeRoot.position;
            transform.rotation = harvesterBladeRoot.rotation;
        }

        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        HandleDirectPointerInput();

        if (_isDragging)
        {
            SpinVisual(Time.deltaTime);
            UpdateDragScale(Time.deltaTime);
            CheckAndHarvestNearbyPlants();
        }
        else if (_isReturning)
        {
            UpdateReturnMotion(Time.deltaTime);
        }
    }

    private void HandleDirectPointerInput()
    {
        if (LunaUIManager.Instance != null && LunaUIManager.Instance.IsGameplayInputBlocked)
        {
            if (_isDragging)
            {
                StopDragging();
            }
            return;
        }

        // Hỗ trợ cả chuột và cảm ứng trực tiếp ngoài UI
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        if (!_isDragging && Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = GetPointerWorldPosition(Input.mousePosition);
            if (IsPointerOverBlade(mouseWorldPos))
            {
                StartDragging(mouseWorldPos);
            }
        }
        else if (_isDragging && Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPos = GetPointerWorldPosition(Input.mousePosition);
            transform.position = mouseWorldPos + _dragOffset;
        }
        else if (_isDragging && Input.GetMouseButtonUp(0))
        {
            StopDragging();
        }
    }

    private bool IsPointerOverBlade(Vector3 pointerWorldPos)
    {
        return Vector3.Distance(transform.position, pointerWorldPos) <= grabRadius;
    }

    public Vector3 GetPointerWorldPosition(Vector3 screenPos)
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return transform.position;

        Ray ray = _mainCamera.ScreenPointToRay(screenPos);
        Vector3 planePoint = harvesterBladeRoot != null ? harvesterBladeRoot.position : transform.position;

        bool is2DMode = _mainCamera.orthographic &&
                        Mathf.Abs(Vector3.Dot(_mainCamera.transform.forward, Vector3.forward)) > 0.95f &&
                        Mathf.Abs(_mainCamera.transform.forward.y) < 0.2f;
        Vector3 planeNormal = is2DMode ? Vector3.back : Vector3.up;

        Plane groundPlane = new Plane(planeNormal, planePoint);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        if (_mainCamera.orthographic)
        {
            Vector3 pos = _mainCamera.ScreenToWorldPoint(screenPos);
            pos.z = transform.position.z;
            return pos;
        }

        return transform.position;
    }

    public void StartDragging(Vector3 pointerWorldPos, bool snapToPosition = false)
    {
        CancelMotions();
        if (snapToPosition)
        {
            transform.position = pointerWorldPos;
            _dragOffset = Vector3.zero;
        }
        else
        {
            _dragOffset = transform.position - pointerWorldPos;
        }

        _isDragging = true;

        // PlayFireFlare();

        transform.localScale = Vector3.one * dragScale;

        OnDragStarted?.Invoke();
    }

    public void StopDragging()
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        // StopFireFlare();

        OnDragEnded?.Invoke();
        ReturnToRoot();
    }

    private void ReturnToRoot()
    {
        CancelMotions();

        Vector3 targetPos = harvesterBladeRoot != null ? harvesterBladeRoot.position : transform.position;
        Quaternion targetRot = harvesterBladeRoot != null ? harvesterBladeRoot.rotation : Quaternion.identity;

        if (returnDuration <= 0f)
        {
            transform.SetPositionAndRotation(targetPos, targetRot);
            transform.localScale = Vector3.one;
            return;
        }

        _returnStartPosition = transform.position;
        _returnStartRotation = transform.rotation;
        _returnStartScale = transform.localScale;
        _returnElapsed = 0f;
        _isReturning = true;
    }

    private void UpdateReturnMotion(float deltaTime)
    {
        _returnElapsed += deltaTime;
        float progress = Mathf.Clamp01(_returnElapsed / returnDuration);
        float eased = progress * progress * (3f - 2f * progress);
        Vector3 targetPosition = harvesterBladeRoot != null ? harvesterBladeRoot.position : _returnStartPosition;
        Quaternion targetRotation = harvesterBladeRoot != null ? harvesterBladeRoot.rotation : _returnStartRotation;
        transform.position = Vector3.Lerp(_returnStartPosition, targetPosition, eased);
        transform.rotation = Quaternion.Slerp(_returnStartRotation, targetRotation, eased);
        transform.localScale = Vector3.Lerp(_returnStartScale, Vector3.one, eased);
        if (progress >= 1f)
        {
            _isReturning = false;
        }
    }

    private void SpinVisual(float deltaTime)
    {
        Transform visual = Visual;
        if (visual == null || Mathf.Approximately(bladeSpinSpeed, 0f))
            return;

        visual.Rotate(0f, 0f, -bladeSpinSpeed * deltaTime, Space.Self);
    }

    private void CheckAndHarvestNearbyPlants()
    {
        FarmPlotSystem plots = PlotSystem;
        if (plots == null) return;

        Vector3 currentBladePos = transform.position;
        IReadOnlyList<Plot> slots = plots.SpawnedPlotComponents;

        for (int i = 0; i < slots.Count; i++)
        {
            Plot slot = slots[i];
            if (slot == null || slot.State != FarmPlotState.Occupied)
                continue;

            Plant plant = slot.CurrentPlant;
            if (plant == null || !plant.CanHarvest())
                continue;

            float dist = Vector3.Distance(currentBladePos, slot.PlantPosition);
            if (dist <= harvestRadius)
            {
                double revenue = plant.Harvest();
                PlaySliceFeedback();
                OnPlantHarvested?.Invoke(slot, revenue);
            }
        }
    }

    private void PlaySliceFeedback()
    {
        _sliceFeedbackElapsed = 0f;
    }

    private void UpdateDragScale(float deltaTime)
    {
        float multiplier = 1f;
        if (_sliceFeedbackElapsed >= 0f)
        {
            _sliceFeedbackElapsed += deltaTime;
            float progress = Mathf.Clamp01(_sliceFeedbackElapsed / SliceFeedbackDuration);
            multiplier += 0.1f * Mathf.Sin(progress * Mathf.PI);
            if (progress >= 1f)
            {
                _sliceFeedbackElapsed = -1f;
            }
        }
        transform.localScale = Vector3.one * (dragScale * multiplier);
    }

    // public void PlayFireFlare()
    // {
    //     if (fireFlare != null && !fireFlare.isPlaying)
    //     {
    //         fireFlare.Play();
    //     }
    // }

    // public void StopFireFlare()
    // {
    //     if (fireFlare != null && fireFlare.isPlaying)
    //     {
    //         fireFlare.Stop();
    //     }
    // }

    private void CancelMotions()
    {
        _isReturning = false;
        _returnElapsed = 0f;
        _sliceFeedbackElapsed = -1f;
    }



#if UNITY_EDITOR
    [Header("Gizmos Debug")]
    [SerializeField] private bool showGizmosAlways = false;
    [SerializeField] private bool showHarvestRadiusGizmo = true;
    [SerializeField] private bool showGrabRadiusGizmo = true;
    [SerializeField] private bool showGizmoLabels = true;

    private void OnDrawGizmos()
    {
        if (showGizmosAlways)
        {
            DrawHarvestBladeGizmos();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmosAlways)
        {
            DrawHarvestBladeGizmos();
        }
    }

    private void DrawHarvestBladeGizmos()
    {
        Vector3 center = transform.position;
        Camera cam = _mainCamera != null ? _mainCamera : Camera.main;
        bool is2DMode = cam != null && cam.orthographic &&
                        Mathf.Abs(Vector3.Dot(cam.transform.forward, Vector3.forward)) > 0.95f &&
                        Mathf.Abs(cam.transform.forward.y) < 0.2f;
        Vector3 discNormal = is2DMode ? Vector3.forward : Vector3.up;

        // 1. Harvest Radius (Màu đỏ/cam đặc trưng cho vùng chém thu hoạch)
        if (showHarvestRadiusGizmo && harvestRadius > 0f)
        {
            // Vòng tròn dây
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(center, harvestRadius);

            // Vùng bán kính mờ trong 2D / 3D
            UnityEditor.Handles.color = new Color(1f, 0.3f, 0.2f, 0.08f);
            UnityEditor.Handles.DrawSolidDisc(center, discNormal, harvestRadius);

            if (showGizmoLabels)
            {
                GUIStyle harvestStyle = new GUIStyle
                {
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(1f, 0.4f, 0.3f, 1f) },
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                };
                Vector3 labelOffset = is2DMode ? new Vector3(0f, harvestRadius + 0.05f, 0f) : new Vector3(0f, 0.05f, harvestRadius);
                UnityEditor.Handles.Label(center + labelOffset, $"Harvest Radius: {harvestRadius:F2}", harvestStyle);
            }
        }

        // 2. Grab Radius (Màu xanh lục/xanh lam cho vùng chạm cầm/kéo)
        if (showGrabRadiusGizmo && grabRadius > 0f)
        {
            // Vòng tròn dây
            Gizmos.color = new Color(0.2f, 0.85f, 0.3f, 0.9f);
            Gizmos.DrawWireSphere(center, grabRadius);

            // Vùng bán kính mờ trong 2D / 3D
            UnityEditor.Handles.color = new Color(0.2f, 0.85f, 0.3f, 0.08f);
            UnityEditor.Handles.DrawSolidDisc(center, discNormal, grabRadius);

            if (showGizmoLabels)
            {
                GUIStyle grabStyle = new GUIStyle
                {
                    alignment = TextAnchor.LowerCenter,
                    normal = { textColor = new Color(0.2f, 0.9f, 0.4f, 1f) },
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                };
                Vector3 labelOffset = is2DMode ? new Vector3(0f, -(grabRadius + 0.05f), 0f) : new Vector3(0f, 0.05f, -grabRadius);
                UnityEditor.Handles.Label(center + labelOffset, $"Grab Radius: {grabRadius:F2}", grabStyle);
            }
        }
    }
#endif
}
