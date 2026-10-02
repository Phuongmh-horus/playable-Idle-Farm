using System.Collections.Generic;
using UnityEngine;

public class WorldSinglePopupAutoCloseHandler : MonoBehaviour
{
    [Header("─── Settings ───")]
    [SerializeField] private bool animate = true;
    [SerializeField] private bool closeOnCameraDrag = true;
    [SerializeField] private bool closeOnSelectableClick = true;

    [Header("─── Auto-Close Popup Group ───")]
    [Tooltip("Danh sách các WorldLayerType thuộc nhóm tự động đóng khi click ra ngoài, click selectable khác hoặc mở popup mới cùng nhóm.")]
    [SerializeField] private List<WorldLayerType> autoClosePopupTypes = new()
    {
        WorldLayerType.PopupUpgradePlantLayer,
    };

    public List<WorldLayerType> AutoClosePopupTypes => autoClosePopupTypes;

    private void OnEnable()
    {
        JobSelectionManager.OnNothingClicked += HandleNothingClicked;
        JobSelectionManager.OnSelectableClicked += HandleSelectableClicked;
        WorldLayerManager.OnLayerShown += HandleLayerShown;

        if (closeOnCameraDrag)
        {
            GameEventBus.OnCameraDragStart += HandleCameraDragStart;
        }
    }

    private void OnDisable()
    {
        JobSelectionManager.OnNothingClicked -= HandleNothingClicked;
        JobSelectionManager.OnSelectableClicked -= HandleSelectableClicked;
        WorldLayerManager.OnLayerShown -= HandleLayerShown;

        if (closeOnCameraDrag)
        {
            GameEventBus.OnCameraDragStart -= HandleCameraDragStart;
        }
    }

    private void HandleNothingClicked()
    {
        CloseTargetPopups();
    }

    private void HandleCameraDragStart()
    {
        CloseTargetPopups();
    }

    private void HandleSelectableClicked(GameObject selectable)
    {
        if (!closeOnSelectableClick) return;

        CloseTargetPopups();
    }

    private void HandleLayerShown(WorldLayerBase shownLayer)
    {
        if (shownLayer == null) return;

        // Nếu layer vừa hiện thuộc nhóm cấu hình, đóng tất cả các layer khác trong nhóm
        if (autoClosePopupTypes != null && autoClosePopupTypes.Contains(shownLayer.LayerType))
        {
            CloseTargetPopups(exceptLayer: shownLayer);
        }
    }

    public void CloseTargetPopups(WorldLayerBase exceptLayer = null)
    {
        if (WorldLayerManager.Instance != null && autoClosePopupTypes != null && autoClosePopupTypes.Count > 0)
        {
            WorldLayerManager.Instance.HideLayersByTypes(autoClosePopupTypes, exceptLayer, animate);
        }
    }

    // Giữ lại hàm cũ để backward compatible nếu có code bên ngoài gọi
    public void CloseSinglePopups()
    {
        CloseTargetPopups();
    }
}
