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

        // The picker deliberately uses the big plant art, independently of
        // gameplay's fruit/plant sprites and their progression visuals.
        Sprite icon = data != null ? data.GetPlantStoreIcon() : null;
        if (fruitIconImage != null) fruitIconImage.sprite = icon;

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

