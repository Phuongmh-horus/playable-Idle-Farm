using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlantStore : MonoBehaviour
{
    private const int maxItemPerColumn = 4;

    [SerializeField, Min(1)] private int prewarmItemCount = maxItemPerColumn;

    private readonly List<GameObject> _columns = new List<GameObject>();
    private readonly List<GameObject> _itemObjects = new List<GameObject>();
    private readonly List<PlantItem> _plantItems = new List<PlantItem>();
    private readonly List<bool> _itemListenersInitialized = new List<bool>();

    [Header ("Prefab")]
    public GameObject ColumnPrefab;

    public GameObject ItemInColumnPrefab;

    public GameObject Panel;

    public bool IsGridStore
    {
        get { return ColumnPrefab != null && ColumnPrefab.name.Contains("Grid"); }
    }

    private void Awake()
    {
        if (Panel != null && ColumnPrefab != null && ItemInColumnPrefab != null)
        {
            EnsurePoolCapacity(prewarmItemCount);
        }
    }

    public void ClearStore()
    {
        for (int i = 0; i < _columns.Count; i++)
        {
            if (_columns[i] != null)
            {
                _columns[i].SetActive(false);
            }
        }

        Panel?.SetActive(false);
    }

    public void Prewarm(int requiredItemCount)
    {
        if (Panel == null || ColumnPrefab == null || ItemInColumnPrefab == null)
        {
            return;
        }

        EnsurePoolCapacity(Mathf.Max(prewarmItemCount, requiredItemCount));
    }

    /// <summary>Builds choices in the provided order; scenarios must not randomize this list.</summary>
    public void SetupStore(IReadOnlyList<PlantConfigData> plantConfigs)
    {
        if (Panel == null || ColumnPrefab == null || ItemInColumnPrefab == null)
        {
            Debug.LogError("PlantStore: Panel, ColumnPrefab, or ItemInColumnPrefab is not assigned.");
            return;
        }
        if (plantConfigs == null || plantConfigs.Count == 0)
        {
            Debug.LogWarning("PlantStore: No plant configurations provided.");
            return;
        }
        int count = plantConfigs.Count;
        int columnCount = Mathf.CeilToInt((float)count / maxItemPerColumn);
        EnsurePoolCapacity(Mathf.Max(count, prewarmItemCount));

        ClearStore();
        gameObject.SetActive(true);
        Panel.SetActive(true);

        for (int i = 0; i < columnCount; i++)
        {
            _columns[i].SetActive(true);
        }

        for (int i = 0; i < _itemObjects.Count; i++)
        {
            GameObject itemObject = _itemObjects[i];
            bool isUsed = i < count;
            itemObject.SetActive(isUsed);
            if (!isUsed)
            {
                continue;
            }

            BindItem(i, plantConfigs[i]);
        }
        UpdateLayout();
    }

    private void EnsurePoolCapacity(int requiredItemCount)
    {
        int requiredColumnCount = Mathf.CeilToInt((float)Mathf.Max(1, requiredItemCount) / maxItemPerColumn);
        while (_columns.Count < requiredColumnCount)
        {
            GameObject column = Instantiate(ColumnPrefab, Panel.transform);
            column.SetActive(false);
            _columns.Add(column);

            for (int i = 0; i < maxItemPerColumn; i++)
            {
                GameObject itemObject = Instantiate(ItemInColumnPrefab, column.transform);
                itemObject.SetActive(false);
                _itemObjects.Add(itemObject);
                _plantItems.Add(itemObject.GetComponent<PlantItem>());
                _itemListenersInitialized.Add(false);
            }
        }
    }

    private void BindItem(int index, PlantConfigData data)
    {
        PlantItem plantItem = _plantItems[index];
        if (plantItem == null)
        {
            Debug.LogError("PlantStore: ItemInColumnPrefab needs a PlantItem component.");
            return;
        }

        // Rebind every time the modal opens. The same pooled item can represent
        // a different plant in the next unlock, so never leave stale name/art.
        plantItem.SetData(data);
        _itemListenersInitialized[index] = true;
    }

    private void UpdateLayout()
    {
        if (Panel == null) return;
        RectTransform panelRect = Panel.GetComponent<RectTransform>();
        if (panelRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }


    public void HideStore()
    {
        gameObject.SetActive(false);
    }
}

