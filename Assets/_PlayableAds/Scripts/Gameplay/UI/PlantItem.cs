using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlantItem : MonoBehaviour
{
    private Button button;

    public Image fruitIconImage;
    [SerializeField] private TMP_Text fruitNameText;

    public PlantConfigData plantConfigData;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void SetData(PlantConfigData data)
    {
        if (plantConfigData != null)
        {
            // Set the button image to the fruit icon
            if (fruitIconImage != null && plantConfigData.fruitIcon != null)
            {
                fruitIconImage.sprite = plantConfigData.fruitIcon;
            }

            // Add a listener to the button click event
            button.onClick.AddListener(OnPlantItemClicked);
        }
    }
    private void OnPlantItemClicked()
    {
        // Trigger the event when the plant item is clicked
        GameEventBus.ActionOnPlantSelected?.Invoke(plantConfigData.ID);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnPlantItemClicked);
    }


}

