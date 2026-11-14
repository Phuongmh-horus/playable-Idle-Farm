using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlantStore : MonoBehaviour
{
    private const int fallbackItemsPerColumn = 4;

    [SerializeField, Min(1)] private int prewarmItemCount = 2;

    private readonly List<GameObject> _columns = new List<GameObject>();
    private readonly List<GameObject> _itemObjects = new List<GameObject>();
    private readonly List<PlantItem> _plantItems = new List<PlantItem>();
    private readonly List<bool> _itemListenersInitialized = new List<bool>();

    [Header("Prefab")]
    public GameObject ColumnPrefab;

    public List<GameObject> ItemInColumnPrefab;

    public GameObject Panel;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI selectText;
    [SerializeField] private TextMeshProUGUI describeText;

    [Header("Texts - First Plant (Playable Start)")]
    [SerializeField] private string firstPlantTitleText = "Select your first plant!";
    [SerializeField] private string firstPlantDescribeText = "Your farm start with this plant";

    [Header("Texts - New Plot")]
    [SerializeField] private string newPlotTitleText = "Select your plant!";
    [SerializeField] private string newPlotDescribeText = "Expand your farm with this plant";

    private bool _isFirstPlantStage = true;

    public bool IsGridStore
    {
        get { return ColumnPrefab != null && ColumnPrefab.name.Contains("Grid"); }
    }

    public TextMeshProUGUI SelectText => selectText;
    public TextMeshProUGUI DescribeText => describeText;

    public string FirstPlantTitleText
    {
        get => firstPlantTitleText;
        set => firstPlantTitleText = value;
    }

    public string FirstPlantDescribeText
    {
        get => firstPlantDescribeText;
        set => firstPlantDescribeText = value;
    }

    public string NewPlotTitleText
    {
        get => newPlotTitleText;
        set => newPlotTitleText = value;
    }

    public string NewPlotDescribeText
    {
        get => newPlotDescribeText;
        set => newPlotDescribeText = value;
    }

    public bool IsFirstPlantStage => _isFirstPlantStage;

    private int ItemsPerColumn
    {
        get
        {
            if (ColumnPrefab != null)
            {
                PlantItem[] authoredItems = ColumnPrefab.GetComponentsInChildren<PlantItem>(true);
                if (authoredItems != null && authoredItems.Length > 0)
                {
                    return authoredItems.Length;
                }
            }

            return fallbackItemsPerColumn;
        }
    }

    private void Awake()
    {
        ApplyTexts();
        if (Panel != null && ColumnPrefab != null && ItemInColumnPrefab != null)
        {
            EnsurePoolCapacity(prewarmItemCount);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyTexts();
    }
#endif

    public void SetStage(bool isFirstPlant)
    {
        _isFirstPlantStage = isFirstPlant;
        ApplyTexts();
    }

    public void ApplyTexts()
    {
        if (selectText != null)
        {
            selectText.text = _isFirstPlantStage ? firstPlantTitleText : newPlotTitleText;
        }

        if (describeText != null)
        {
            describeText.text = _isFirstPlantStage ? firstPlantDescribeText : newPlotDescribeText;
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

    public void SetupStore(IReadOnlyList<PlantConfigData> plantConfigs)
    {
        SetupStore(plantConfigs, _isFirstPlantStage);
    }

    /// <summary>Builds choices in the provided order; scenarios must not randomize this list.</summary>
    public void SetupStore(IReadOnlyList<PlantConfigData> plantConfigs, bool isFirstPlant)
    {
        SetStage(isFirstPlant);
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
        int columnCount = Mathf.CeilToInt((float)count / ItemsPerColumn);
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
        int requiredColumnCount = Mathf.CeilToInt((float)Mathf.Max(1, requiredItemCount) / ItemsPerColumn);
        while (_columns.Count < requiredColumnCount)
        {
            GameObject column = Instantiate(ColumnPrefab, Panel.transform);
            column.SetActive(false);
            _columns.Add(column);

            PlantItem[] authoredItems = column.GetComponentsInChildren<PlantItem>(true);
            if (authoredItems != null && authoredItems.Length > 0)
            {
                for (int i = 0; i < authoredItems.Length; i++)
                {
                    RegisterItem(authoredItems[i]);
                }
            }
            else if (ItemInColumnPrefab != null && ItemInColumnPrefab.Count > 0)
            {
                for (int i = 0; i < ItemsPerColumn; i++)
                {
                    GameObject itemObject = Instantiate(ItemInColumnPrefab[i % ItemInColumnPrefab.Count], column.transform);
                    itemObject.SetActive(false);
                    RegisterItem(itemObject.GetComponent<PlantItem>());
                }
            }
        }
    }

    private void RegisterItem(PlantItem plantItem)
    {
        if (plantItem == null)
        {
            Debug.LogError("PlantStore: every item slot needs a PlantItem component.", this);
            return;
        }

        plantItem.gameObject.SetActive(false);
        _itemObjects.Add(plantItem.gameObject);
        _plantItems.Add(plantItem);
        _itemListenersInitialized.Add(false);
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
        plantItem.gameObject.name = data != null ? "Item_PlantStore_" + data.PlantName : "Item_PlantStore_Empty";
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

