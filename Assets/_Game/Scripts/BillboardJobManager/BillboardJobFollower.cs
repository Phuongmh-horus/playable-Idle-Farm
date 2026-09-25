using UnityEngine;

public enum UpdateFrequency
{
    Continuous = 0,
    OnceOnEnable = 1,
    Disabled = 2
}

/// <summary>
/// Optional world-space UI follower. Use OnceOnEnable for static-camera scenes;
/// choose Continuous only for labels that must track a moving target.
/// </summary>
public class BillboardJobFollower : MonoBehaviour
{
    [Header("Target Follow")]
    [SerializeField] private UpdateFrequency positionUpdateMode = UpdateFrequency.OnceOnEnable;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    [Header("Camera Billboard")]
    [SerializeField] private UpdateFrequency rotationUpdateMode = UpdateFrequency.OnceOnEnable;
    [SerializeField] private BillboardMode billboardMode = BillboardMode.ParallelToCamera;
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;

    [Header("Scale")]
    [SerializeField] private bool manageScale;
    [SerializeField] private bool flipX;
    [SerializeField] private Vector3 customScale = Vector3.one;

    private bool isRegistered;

    private void OnEnable()
    {
        if (positionUpdateMode == UpdateFrequency.OnceOnEnable)
            ApplyPositionOnce();
        if (rotationUpdateMode == UpdateFrequency.OnceOnEnable)
            ApplyRotationOnce();

        if (positionUpdateMode == UpdateFrequency.Continuous ||
            rotationUpdateMode == UpdateFrequency.Continuous || manageScale)
            RegisterToManager();
    }

    private void OnDisable()
    {
        if (isRegistered && BillboardJobManager.Instance != null)
            BillboardJobManager.Instance.Unregister(transform);
        isRegistered = false;
    }

    public void SetTarget(Transform newTarget, Vector3 newOffset = default(Vector3))
    {
        target = newTarget;
        positionOffset = newOffset;
        if (positionUpdateMode == UpdateFrequency.OnceOnEnable)
            ApplyPositionOnce();
        if (isRegistered && BillboardJobManager.Instance != null)
            BillboardJobManager.Instance.UpdateTarget(transform, target, positionOffset);
    }

    public void ApplyPositionOnce()
    {
        if (target != null)
            transform.position = target.position + positionOffset;
    }

    public void ApplyRotationOnce()
    {
        BillboardJobManager manager = BillboardJobManager.Instance;
        Quaternion rotation = manager != null
            ? manager.GetBillboardRotation(billboardMode)
            : GetFallbackRotation();
        transform.rotation = rotation * Quaternion.Euler(rotationOffsetEuler);
    }

    private void RegisterToManager()
    {
        BillboardJobManager manager = BillboardJobManager.Instance;
        if (manager == null)
            return;

        Vector3 scale = manageScale ? customScale : transform.localScale;
        if (flipX)
            scale.x = -Mathf.Abs(scale.x);

        manager.Register(transform, target, positionOffset, Quaternion.Euler(rotationOffsetEuler), scale,
            positionUpdateMode == UpdateFrequency.Continuous,
            rotationUpdateMode == UpdateFrequency.Continuous,
            manageScale, billboardMode);
        isRegistered = true;
    }

    private Quaternion GetFallbackRotation()
    {
        if (billboardMode == BillboardMode.GroundFlat)
            return Quaternion.Euler(90f, 0f, 0f);

        Camera cameraToUse = CameraManager.Instance != null ? CameraManager.Instance.MainCamera : Camera.main;
        return cameraToUse != null ? cameraToUse.transform.rotation : Quaternion.identity;
    }
}
