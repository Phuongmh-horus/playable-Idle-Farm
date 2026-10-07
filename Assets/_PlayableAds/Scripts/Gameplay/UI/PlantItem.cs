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
        plantConfigData = data;
        if (button == null)
        {
            Debug.LogError("PlantItem requires a Button component.", this);
            return;
        }

        if (fruitIconImage != null)
        {
            fruitIconImage.sprite = data != null ? (data.plantIcon != null ? data.plantIcon : data.fruitIcon) : null;
        }

        if (fruitNameText != null)
        {
            fruitNameText.text = data != null ? data.PlantName : string.Empty;
        }

        button.onClick.RemoveListener(OnPlantItemClicked);
        button.onClick.AddListener(OnPlantItemClicked);
        button.interactable = data != null;
    }
    private void OnPlantItemClicked()
    {
        if (plantConfigData != null)
        {
            GameEventBus.ActionOnPlantSelected?.Invoke(plantConfigData.ID);
        }
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnPlantItemClicked);
    }


}

