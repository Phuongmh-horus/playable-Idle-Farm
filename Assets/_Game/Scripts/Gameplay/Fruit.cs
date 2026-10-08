using System.Collections;
using UnityEngine;

public class Fruit : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Coroutine _scaleRoutine;

    public void Init(int id)
    {
        Sprite spriteToUse = ConfigHolder.Instance?.PlantConfig?.GetPlant(id)?.GetHarvestIcon();
        SetSprite(spriteToUse);
    }

    public void SetLayer(string layerName, int sortingOrder)
    {
        spriteRenderer.sortingLayerName = layerName;
        spriteRenderer.sortingOrder = sortingOrder;
    }

    public void PlaySpawnScale()
    {
        StopScaleAnimation();
        transform.localScale = Vector3.zero;
        _scaleRoutine = StartCoroutine(ScaleInRoutine());
    }

    private IEnumerator ScaleInRoutine()
    {
        const float duration = 0.25f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.one * (1f - (1f - progress) * (1f - progress));
            yield return null;
        }
        transform.localScale = Vector3.one;
        _scaleRoutine = null;
    }

    private void OnDisable()
    {
        StopScaleAnimation();
    }

    private void StopScaleAnimation()
    {
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = null;
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    public void ResetForPool()
    {
        StopScaleAnimation();
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
}
