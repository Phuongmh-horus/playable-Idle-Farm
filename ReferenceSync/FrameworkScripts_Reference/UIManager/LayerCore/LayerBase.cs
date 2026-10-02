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
    private List<int> _sortOrders = new();

    protected virtual void Reset()
    {
        canvas ??= GetComponent<Canvas>();
        canvas.overrideSorting = true;

        canvasGroup ??= GetComponent<CanvasGroup>();
        canvasGroup.SetActive(false);
        if (canvas) canvas.enabled = false;
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

    public UnityEvent OnShowLayer = new();
    public UnityEvent OnHideLayer = new();
    public virtual void ShowLayerAsync()
    {
        if (canvas) canvas.enabled = true;
        canvasGroup.SetActive(true);
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        SetVisibleInternal(true);
        OnShowLayer?.Invoke();
    }

    public void ShowLayerWithoutEvent()
    {
        if (canvas) canvas.enabled = true;
        canvasGroup.SetActive(true);
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        // Vẫn phải báo visibility dù không bắn OnShowLayer: "WithoutEvent" chỉ có nghĩa là
        // không chạy lại animation mở popup khi khôi phục layer nền, chứ layer thì đúng là
        // đang hiện trở lại. Bỏ qua ở đây thì handler đã tạm dừng sẽ không bao giờ chạy lại.
        SetVisibleInternal(true);
    }

    public virtual void HideLayerAsync()
    {
        if (canvas) canvas.enabled = false;
        canvasGroup.SetActive(false);
        SetVisibleInternal(false);
        OnHideLayer?.Invoke();
    }
    public virtual void CloseLayerAsync(bool force = false)
    {
        if (force)
        {
            _sortOrders.Clear();
            OnStackClear();
        }
        var order = -10000;
        if (_sortOrders.Count > 1)
        {
            _sortOrders.RemoveAt(_sortOrders.Count - 1);
            order = _sortOrders[^1];
            OnRemoveFromStack();
        }
        else
        {
            _sortOrders.Clear();
            OnStackClear();
        }
        SetSortOrder(order, false);
        if(order <= 0) HideLayerAsync();
    }
    public virtual void SetSortOrder(int order, bool save = true)
    {
        if(canvas.sortingOrder ==  order) return;
        canvas.sortingOrder = order;
        if (save && (_sortOrders.Count == 0 || _sortOrders[^1] < order))
        {
            _sortOrders.Add(order);
            OnAddToStack();
        }
    }

    public bool IsActive()
    {
        return canvas.sortingOrder > 0;
    }
    
    public virtual void OnAddToStack()
    {
    }

    public virtual void OnRemoveFromStack()
    {
    }

    public virtual void OnStackClear()
    {
    }

    public void SetHighLight(bool highlight)
    {
        canvas.sortingOrder = highlight ? 32767 : (_sortOrders.Count > 0 ? _sortOrders[^1] : -10000);
        bool isActive = canvas.sortingOrder > 0;
        if (canvas) canvas.enabled = isActive;
        canvasGroup.interactable = isActive;
        canvasGroup.blocksRaycasts = isActive;
        canvasGroup.alpha = isActive ? 1 : 0;
        SetVisibleInternal(isActive);
    }

    #region Layer Visibility

    /// <summary>
    /// Layer có đang thực sự hiển thị hay không.
    ///
    /// Đây là nguồn sự thật cho <see cref="ILayerVisibilityHandler"/>. Không đọc
    /// <c>canvas.enabled</c> thay cho nó: giá trị này chỉ đổi khi trạng thái thật sự đổi,
    /// nên handler không bị gọi lặp lại mỗi lần một group được show đè lên.
    /// </summary>
    public bool IsVisible { get; private set; }

    private readonly List<ILayerVisibilityHandler> _visibilityHandlers = new();
    private bool _handlersCollected;
    private bool _visibilityInitialized;

    /// <summary>
    /// Đăng ký một handler sinh ra sau khi layer đã khởi tạo (item lấy từ pool chẳng hạn).
    /// Handler được đồng bộ ngay trạng thái hiện tại, để object sinh ra lúc layer đang bị che
    /// biết là mình không được chạy.
    /// </summary>
    public void RegisterVisibilityHandler(ILayerVisibilityHandler handler)
    {
        if (handler == null) return;
        CollectVisibilityHandlers();
        if (_visibilityHandlers.Contains(handler)) return;
        _visibilityHandlers.Add(handler);
        if (_visibilityInitialized) SafeNotify(handler, IsVisible);
    }

    public void UnregisterVisibilityHandler(ILayerVisibilityHandler handler)
    {
        if (handler == null) return;
        _visibilityHandlers.Remove(handler);
    }

    private void SetVisibleInternal(bool visible)
    {
        if (_visibilityInitialized && IsVisible == visible) return;
        _visibilityInitialized = true;
        IsVisible = visible;

        CollectVisibilityHandlers();

        // Duyệt ngược để vừa dọn được handler đã bị destroy vừa an toàn nếu handler tự
        // Unregister chính nó ngay trong callback.
        for (int i = _visibilityHandlers.Count - 1; i >= 0; i--)
        {
            var handler = _visibilityHandlers[i];
            // So sánh qua UnityEngine.Object: biến kiểu interface không dùng toán tử == đã
            // override của Unity, nên component đã destroy vẫn khác null nếu check trực tiếp.
            if (handler == null || (handler is UnityEngine.Object uo && uo == null))
            {
                _visibilityHandlers.RemoveAt(i);
                continue;
            }

            SafeNotify(handler, visible);
        }
    }

    private void CollectVisibilityHandlers()
    {
        if (_handlersCollected) return;
        _handlersCollected = true;
        // Phải quét trước mọi lần Register thủ công: overload nhận List sẽ Clear() list trước
        // khi ghi kết quả, gọi sau sẽ xoá mất các handler đã đăng ký tay.
        // includeInactive để bắt cả component nằm trên object tắt sẵn trong prefab.
        GetComponentsInChildren(true, _visibilityHandlers);
    }

    private void SafeNotify(ILayerVisibilityHandler handler, bool visible)
    {
        // Một handler lỗi không được phép chặn các handler còn lại tạm dừng/chạy lại.
        try
        {
            handler.OnLayerVisibilityChanged(visible);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e, this);
        }
    }

    #endregion
}
public class LayerGroup
{
    private Dictionary<LayerType, LayerBase> _layerBases = new(4);

    public List<LayerType> LayerTypes => new(_layerBases.Keys);
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