using UnityEngine;

public class Fruit : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    public void Init(int id)
    {
        Sprite spriteToUse = ConfigHolder.Instance?.PlantConfig?.GetPlant(id)?.fruitIcon;
        SetSprite(spriteToUse);
    }

    public void SetLayer(string layerName, int sortingOrder)
    {
        spriteRenderer.sortingLayerName = layerName;
        spriteRenderer.sortingOrder = sortingOrder;
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    public void ResetForPool()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
}
