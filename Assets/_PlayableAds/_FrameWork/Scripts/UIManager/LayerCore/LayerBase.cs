using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
#endif

[RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(RectTransform))]
public class LayerBase : MonoBehaviour
{
    [SerializeField] protected Canvas canvas;
    [SerializeField] protected CanvasGroup canvasGroup;

    [SerializeField] public bool keepPreActive;
    private List<int> _sortOrders = new List<int>();

    protected virtual void Reset()
    {
        canvas ??= GetComponent<Canvas>();
        canvas.overrideSorting = true;

        canvasGroup ??= GetComponent<CanvasGroup>();
        canvasGroup.gameObject.SetActive(false);
    }

    protected virtual void OnValidate()
    {
        if (!keepPreActive) gameObject.SetActive(false);
    }

    public int GetSortingOrder()
    {
        return _sortOrders.Count > 0 ? _sortOrders[^1] : 0;
    }

    public virtual void InitData()
    {

    }

    public UnityEvent OnShowLayer = new UnityEvent();
    public UnityEvent OnHideLayer = new UnityEvent();
    public virtual void ShowLayerAsync()
    {
        canvasGroup.gameObject.SetActive(true);
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        OnShowLayer?.Invoke();
    }

    public void ShowLayerWithoutEvent()
    {
        canvasGroup.gameObject.SetActive(true);
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
    }
    
    public virtual void HideLayerAsync()
    {
        canvasGroup.gameObject.SetActive(false);
        OnHideLayer?.Invoke();
    }
    public virtual void CloseLayerAsync(bool force = false)
    {
        if (force) _sortOrders.Clear();
        var order = -10000;
        if (_sortOrders.Count > 1)
        {
            _sortOrders.RemoveAt(_sortOrders.Count - 1);
            order = _sortOrders[^1];
        }
        else
        {
            _sortOrders.Clear();
        }
        SetSortOrder(order, false);
        if(order <= 0) HideLayerAsync();
    }
    public virtual void SetSortOrder(int order, bool save = true)
    {
        canvas.sortingOrder = order;
        if (save && (_sortOrders.Count == 0 || _sortOrders[^1] < order)) _sortOrders.Add(order);
    }

    public bool IsActive()
    {
        return canvas.sortingOrder > 0;
    }

    public void SetHighLight(bool highlight)
    {
        canvas.sortingOrder = highlight ? 32767 : (_sortOrders.Count > 0 ? _sortOrders[^1] : -10000);
        bool isActive = canvas.sortingOrder > 0;
        canvasGroup.interactable = isActive;
        canvasGroup.blocksRaycasts = isActive;
        canvasGroup.alpha = isActive ? 1 : 0;
    }
}
public class LayerGroup
{
    private Dictionary<LayerType, LayerBase> _layerBases = new Dictionary<LayerType, LayerBase>(4);

    public List<LayerType> LayerTypes => new List<LayerType>(_layerBases.Keys);
    public void CloseGroupAsync()
    {
        foreach (var layerBase in _layerBases.Values)
        {
            layerBase.CloseLayerAsync();
        }
    }
    public void AddLayer(LayerType layerType, LayerBase layerBase)
    {
        _layerBases.Add(layerType, layerBase);
    }
    public bool GetLayerBase(LayerType layerType, out LayerBase layerBase)
    {
        layerBase = _layerBases.GetValueOrDefault(layerType);
        return layerBase;
    }

    public void SetSortOrder(int order)
    {
        int subOrder = 1;
        foreach (var layerBase in _layerBases.Values)
        {
            layerBase.SetSortOrder(order + subOrder);
            subOrder++;
        }
    }
    public void ShowGroupAsync()
    {
        foreach (var layerBase in _layerBases.Values)
        {
            layerBase.ShowLayerAsync();
        }
    }
}