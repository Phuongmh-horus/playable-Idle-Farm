using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlantVisual", menuName = "Farm/PlantVisual")]
public class PlantVisualSO : ScriptableObject
{
    [Header("UI Icons")]
    public Sprite plantIcon;
    public Sprite fruitIcon;

    [Header("World Prefab & Growth Sprites")]
    public Plant plantPrefab;
    public List<Sprite> plantSprites = new List<Sprite>();
    public Sprite giantSkin;

    /// <summary>Icon authored for the plant picker; falls back to the legacy UI art.</summary>
    public Sprite GetPlantStoreIcon()
    {
        return giantSkin != null ? giantSkin : (plantIcon != null ? plantIcon : fruitIcon);
    }

    public Sprite GetPlantSprite(int fruitCount = 0)
    {
        if (plantSprites == null || plantSprites.Count == 0)
            return plantIcon != null ? plantIcon : fruitIcon;

        int index = Mathf.Clamp(fruitCount, 0, plantSprites.Count - 1);
        Sprite sprite = plantSprites[index];
        return sprite != null ? sprite : (plantIcon != null ? plantIcon : fruitIcon);
    }
}
