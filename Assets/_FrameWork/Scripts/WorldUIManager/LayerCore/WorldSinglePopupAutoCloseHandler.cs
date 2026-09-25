using UnityEngine;

public class WorldSinglePopupAutoCloseHandler : MonoBehaviour
{
    [SerializeField] private bool animate = true;

    private void OnEnable()
    {
        JobSelectionManager.OnNothingClicked += HandleNothingClicked;
    }

    private void OnDisable()
    {
        JobSelectionManager.OnNothingClicked -= HandleNothingClicked;
    }

    private void HandleNothingClicked()
    {
        CloseSinglePopups();
    }

    public void CloseSinglePopups()
    {
        if (WorldLayerManager.Instance != null)
        {
            WorldLayerManager.Instance.HideAllSingleLayers(animate);
        }
    }
}
