using System;
using System.Collections.Generic;
using UnityEngine;

public enum SelectableBoundsMode
{
    Manual,
    Auto2D,
    Collider2D,
    SpriteRenderer
}

/// <summary>
/// Authored tap target. Manual bounds are the default because playable plot roots do not
/// need extra physics colliders solely for input.
/// </summary>
public class StaticSelectable : MonoBehaviour
{
    private static readonly List<StaticSelectable> ActiveSelectables = new List<StaticSelectable>();

    [Header("Selection Geometry")]
    public SelectableBoundsMode boundsMode = SelectableBoundsMode.Manual;
    public Vector3 boundsSize = new Vector3(2f, 2f, 2f);
    public Vector3 boundsCenter = Vector3.zero;

    [Header("Selectable State")]
    public bool selectable = true;
    public int priority;

    public event Action<StaticSelectable> OnSelected;
    public event Action<StaticSelectable> OnDeselected;
    public event Action<StaticSelectable> OnTapped;

    public bool IsSelected { get; private set; }
    private Collider2D _collider;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        if (boundsMode == SelectableBoundsMode.Auto2D || boundsMode == SelectableBoundsMode.Collider2D)
            _collider = GetComponent<Collider2D>();
        else if (boundsMode == SelectableBoundsMode.SpriteRenderer)
            _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (!ActiveSelectables.Contains(this))
            ActiveSelectables.Add(this);

        if (JobSelectionManager.Instance != null)
            JobSelectionManager.Instance.RegisterSelectable(this);
    }

    private void OnDisable()
    {
        ActiveSelectables.Remove(this);

        if (JobSelectionManager.Instance != null)
            JobSelectionManager.Instance.UnregisterSelectable(this);
    }

    public static void RegisterActiveSelectables(JobSelectionManager selectionManager)
    {
        if (selectionManager == null)
            return;

        for (int i = ActiveSelectables.Count - 1; i >= 0; i--)
        {
            StaticSelectable selectable = ActiveSelectables[i];
            if (selectable == null)
            {
                ActiveSelectables.RemoveAt(i);
                continue;
            }

            selectionManager.RegisterSelectable(selectable);
        }
    }

    public bool TryRaycast(Ray ray, out float distance)
    {
        return GetWorldBounds().IntersectRay(ray, out distance);
    }

    public void Select()
    {
        IsSelected = true;
        if (OnSelected != null)
            OnSelected(this);
    }

    public void Deselect()
    {
        IsSelected = false;
        if (OnDeselected != null)
            OnDeselected(this);
    }

    public void DeselectInstant()
    {
        Deselect();
    }

    public void NotifyTapped()
    {
        if (OnTapped != null)
            OnTapped(this);
    }

    public void SetSelectable(bool enabled)
    {
        selectable = enabled;
    }

    private Bounds GetWorldBounds()
    {
        if (boundsMode == SelectableBoundsMode.Auto2D || boundsMode == SelectableBoundsMode.Collider2D)
        {
            if (_collider != null)
                return _collider.bounds;
        }
        else if (boundsMode == SelectableBoundsMode.SpriteRenderer)
        {
            if (_spriteRenderer != null)
                return _spriteRenderer.bounds;
        }

        Vector3 scaledSize = Vector3.Scale(boundsSize, transform.lossyScale);
        scaledSize.x = Mathf.Abs(scaledSize.x);
        scaledSize.y = Mathf.Abs(scaledSize.y);
        scaledSize.z = Mathf.Abs(scaledSize.z);
        return new Bounds(transform.TransformPoint(boundsCenter), scaledSize);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Bounds bounds = GetWorldBounds();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
#endif
}
