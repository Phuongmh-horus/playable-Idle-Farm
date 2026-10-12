using System.Collections.Generic;
using UnityEngine;

public enum BillboardMode
{
    ParallelToCamera = 0,
    GroundFlat = 1
}

/// <summary>
/// Updates the small set of world-space labels used by the playable.
/// This deliberately uses normal Transform operations: WebGL playable builds do
/// not benefit from the previous parallel-data setup, which also completed its
/// job on the main thread before rendering.
/// </summary>
public class BillboardJobManager : MonoSingleton<BillboardJobManager>
{
    private class FollowerItem
    {
        public Transform Follower;
        public Transform Target;
        public Vector3 PositionOffset;
        public Quaternion RotationOffset;
        public Vector3 LocalScale;
        public bool FollowPosition;
        public bool Billboard;
        public bool ApplyScale;
        public BillboardMode Mode;
    }

    [Header("Settings")]
    // Keeps the existing scene reference while removing the old Job System toggle.
    [SerializeField] private Camera _customCamera;

    private readonly List<FollowerItem> registeredItems = new List<FollowerItem>();
    private readonly Dictionary<Transform, int> itemIndices = new Dictionary<Transform, int>();
    private static readonly Quaternion GroundFlatRotation = Quaternion.Euler(90f, 0f, 0f);
    private Camera cachedCamera;
    private Transform _cachedCameraTransform;

    public int ActiveCount { get { return registeredItems.Count; } }
    public Camera ActiveCamera { get { return cachedCamera; } }

    protected override void Awake()
    {
        base.Awake();
        cachedCamera = _customCamera;
        if (cachedCamera == null && CameraManager.Instance != null)
            cachedCamera = CameraManager.Instance.MainCamera;
        if (cachedCamera == null)
            cachedCamera = Camera.main;
        if (cachedCamera != null)
            _cachedCameraTransform = cachedCamera.transform;
    }

    public void Register(Transform follower, Transform target, Vector3 positionOffset,
        Quaternion rotationOffset, Vector3 localScale, bool autoFollow, bool autoBillboard,
        bool autoScale, BillboardMode mode)
    {
        if (follower == null)
            return;

        int index;
        if (itemIndices.TryGetValue(follower, out index))
        {
            UpdateItem(registeredItems[index], target, positionOffset, rotationOffset, localScale,
                autoFollow, autoBillboard, autoScale, mode);
            return;
        }

        FollowerItem item = new FollowerItem();
        UpdateItem(item, target, positionOffset, rotationOffset, localScale,
            autoFollow, autoBillboard, autoScale, mode);
        item.Follower = follower;
        itemIndices.Add(follower, registeredItems.Count);
        registeredItems.Add(item);
    }

    public void Unregister(Transform follower)
    {
        int index;
        if (follower == null || !itemIndices.TryGetValue(follower, out index))
            return;
        RemoveAt(index);
    }

    public void UpdateTarget(Transform follower, Transform target, Vector3 positionOffset)
    {
        int index;
        if (follower != null && itemIndices.TryGetValue(follower, out index))
        {
            registeredItems[index].Target = target;
            registeredItems[index].PositionOffset = positionOffset;
        }
    }

    public void UpdatePositionOffset(Transform follower, Vector3 positionOffset)
    {
        int index;
        if (follower != null && itemIndices.TryGetValue(follower, out index))
            registeredItems[index].PositionOffset = positionOffset;
    }

    public void UpdateRotationOffset(Transform follower, Quaternion rotationOffset)
    {
        int index;
        if (follower != null && itemIndices.TryGetValue(follower, out index))
            registeredItems[index].RotationOffset = rotationOffset;
    }

    public void UpdateRotationOffsetEuler(Transform follower, Vector3 eulerAngles)
    {
        UpdateRotationOffset(follower, Quaternion.Euler(eulerAngles));
    }

    public void UpdateScale(Transform follower, Vector3 scale)
    {
        int index;
        if (follower != null && itemIndices.TryGetValue(follower, out index))
        {
            registeredItems[index].LocalScale = scale;
            registeredItems[index].ApplyScale = true;
        }
    }

    public void SetFlipX(Transform follower, bool flipX)
    {
        int index;
        if (follower == null || !itemIndices.TryGetValue(follower, out index))
            return;

        Vector3 scale = registeredItems[index].LocalScale;
        scale.x = flipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        registeredItems[index].LocalScale = scale;
        registeredItems[index].ApplyScale = true;
    }

    public void SetBillboardMode(Transform follower, BillboardMode mode)
    {
        int index;
        if (follower != null && itemIndices.TryGetValue(follower, out index))
            registeredItems[index].Mode = mode;
    }

    private void LateUpdate()
    {
        int count = registeredItems.Count;
        if (count == 0)
            return;

        Quaternion camRot = _cachedCameraTransform != null 
            ? _cachedCameraTransform.rotation 
            : (cachedCamera != null ? cachedCamera.transform.rotation : Quaternion.identity);

        for (int i = count - 1; i >= 0; i--)
        {
            FollowerItem item = registeredItems[i];
            if (item.Follower == null)
            {
                RemoveAt(i);
                continue;
            }

            if (item.FollowPosition && item.Target != null && item.Target.gameObject.activeInHierarchy)
                item.Follower.position = item.Target.position + item.PositionOffset;
            if (item.Billboard)
            {
                Quaternion baseRot = item.Mode == BillboardMode.GroundFlat ? GroundFlatRotation : camRot;
                item.Follower.rotation = baseRot * item.RotationOffset;
            }
            if (item.ApplyScale)
                item.Follower.localScale = item.LocalScale;
        }
    }

    public Quaternion GetBillboardRotation(BillboardMode mode)
    {
        if (mode == BillboardMode.GroundFlat)
            return GroundFlatRotation;
        return _cachedCameraTransform != null 
            ? _cachedCameraTransform.rotation 
            : (cachedCamera != null ? cachedCamera.transform.rotation : Quaternion.identity);
    }

    private static void UpdateItem(FollowerItem item, Transform target, Vector3 positionOffset,
        Quaternion rotationOffset, Vector3 localScale, bool autoFollow, bool autoBillboard,
        bool autoScale, BillboardMode mode)
    {
        item.Target = target;
        item.PositionOffset = positionOffset;
        item.RotationOffset = rotationOffset;
        item.LocalScale = localScale;
        item.FollowPosition = autoFollow;
        item.Billboard = autoBillboard;
        item.ApplyScale = autoScale;
        item.Mode = mode;
    }

    private void RemoveAt(int index)
    {
        int lastIndex = registeredItems.Count - 1;
        FollowerItem removedItem = registeredItems[index];
        if (index != lastIndex)
        {
            FollowerItem lastItem = registeredItems[lastIndex];
            registeredItems[index] = lastItem;
            if (lastItem.Follower != null)
                itemIndices[lastItem.Follower] = index;
        }

        registeredItems.RemoveAt(lastIndex);
        if (removedItem.Follower != null)
            itemIndices.Remove(removedItem.Follower);
    }
}
