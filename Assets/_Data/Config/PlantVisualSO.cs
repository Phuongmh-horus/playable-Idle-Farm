using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlantVisual", menuName = "Farm/PlantVisual")]
public class PlantVisualSO : ScriptableObject
{
    [Header("Identity & Localization")]
    [Tooltip("Tên cây (dùng hiển thị mặc định nếu không có localization)")]
    public string plantName;

    [Tooltip("Key I2 Localization (ví dụ: ui_tomato, ui_potato...)")]
    public string localizedKey;

    [Header("UI Icons")]
    public Sprite plantIcon;
    public Sprite fruitIcon;

    [Header("World Prefab & Growth Sprites")]
    public Plant plantPrefab;

    [Tooltip("Danh sách Sprite theo số lượng quả (Index 0: 0 quả, Index 1-6: tương ứng 1-6 quả)")]
    public List<Sprite> plantSprites = new();

    [Header("Mega Visuals")]
    public Sprite megaPlantIcon;
    public Sprite megaFruitIcon;
    public Sprite megaUnwateredSprite;
    public Sprite megaWateredSprite;
}
