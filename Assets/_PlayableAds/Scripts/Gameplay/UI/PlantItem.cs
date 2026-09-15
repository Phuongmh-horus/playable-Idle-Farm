using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlantItem : MonoBehaviour
{
    private Button button;
    
    public Image fruitIconImage;

    public PlantConfigData plantConfigData;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
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
        GameEventBus.ActionOnPlantSelected?.Invoke(plantConfigData.id);
    }


}

