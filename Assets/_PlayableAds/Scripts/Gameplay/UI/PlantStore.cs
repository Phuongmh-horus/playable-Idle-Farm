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

    public void SetupStore(List<PlantConfigData> plantConfigs)
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
                plantItem.plantConfigData = plantConfigs[i * maxItemPerColumn + j]; 
            }
        }
        UpdateLayout();
    }

    private void UpdateLayout()
    {
       LayoutRebuilder.ForceRebuildLayoutImmediate(Panel.GetComponent<RectTransform>());
    }


    public void HideStore()
    {
        Panel.SetActive(false);
    }
}

