using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlantStore : MonoBehaviour
{
    private const int maxItemPerColumn = 4;

    [Header ("Prefab")]
    public GameObject ColumnPrefab;

    public GameObject ItemInColumnPrefab;

    public GameObject Panel;
    public void ClearStore()
    {
        if (Panel != null)
        {
            foreach (Transform child in Panel.transform)
            {
                Destroy(child.gameObject);
            }
        }
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
        // The scene keeps SelectPlantStore inactive except in scenario 125.
        gameObject.SetActive(true);
        Panel.SetActive(true);
        ClearStore();
        int count = plantConfigs.Count;
        int columnCount = Mathf.CeilToInt((float)count / maxItemPerColumn);

        for (int i = 0; i < columnCount; i++)
        {
            GameObject column = Instantiate(ColumnPrefab, Panel.transform);
            for (int j = 0; j < maxItemPerColumn; j++)
            {
                // if (count >= count) break;
                int index = i * maxItemPerColumn + j;
                if (index >= plantConfigs.Count) break;

                GameObject item = Instantiate(ItemInColumnPrefab, column.transform);
                // count++;
                PlantItem plantItem = item.GetComponent<PlantItem>();
                if (plantItem != null) plantItem.SetData(plantConfigs[index]);
            }
        }
        UpdateLayout();
    }

    private void UpdateLayout()
    {
       if (Panel != null) LayoutRebuilder.ForceRebuildLayoutImmediate(Panel.GetComponent<RectTransform>());
    }


    public void HideStore()
    {
        gameObject.SetActive(false);
    }
}

