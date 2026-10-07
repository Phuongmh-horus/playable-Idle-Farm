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

    public Sprite GetPlantSprite(int fruitCount = 0)
    {
        if (plantSprites == null || plantSprites.Count == 0)
            return plantIcon != null ? plantIcon : fruitIcon;

        int index = Mathf.Clamp(fruitCount, 0, plantSprites.Count - 1);
        Sprite sprite = plantSprites[index];
        return sprite != null ? sprite : (plantIcon != null ? plantIcon : fruitIcon);
    }
}
