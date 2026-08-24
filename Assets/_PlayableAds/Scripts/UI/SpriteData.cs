using UnityEngine;

[System.Serializable]
public class SpriteData
{
    public SpriteType type;
    public Sprite sprite;
}
public enum SpriteType
{
    image_good = 0,
    image_excellent = 1,
    image_great = 2,
    image_amazing = 3,
}