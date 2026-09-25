using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class TransportVehicle : MonoBehaviour
{
    [SerializeField] private Transform cargoRoot;
    [SerializeField] private float spawnRadius = 0.4f;

    private readonly List<Fruit> _spawnedFruitVisuals = new List<Fruit>();
    private Tween _moveTween;
    public Transform CargoRoot { get { return cargoRoot; } }

    private void OnDestroy()
    {
        CancelMotions();
        ClearFruitVisuals();
    }

    public void PlaceAt(Vector3 position)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    public void MoveTo(Vector3 startPosition, Vector3 targetPosition, float duration, Ease ease, Action onComplete)
    {
        CancelMotions();
        float fixedY = transform.position.y;
        float fixedZ = transform.position.z;
        transform.position = new Vector3(startPosition.x, fixedY, fixedZ);
        _moveTween = transform.DOMoveX(targetPosition.x, Mathf.Max(0.01f, duration))
            .SetEase(ease)
            .OnComplete(() =>
            {
                transform.position = new Vector3(targetPosition.x, fixedY, fixedZ);
                onComplete?.Invoke();
            });
    }

    public void CancelMotions()
    {
        if (_moveTween != null) _moveTween.Kill();
        _moveTween = null;
    }

    public void SpawnFruitVisuals(int plantId, int count)
    {
        int capacity = ConfigHolder.Instance != null && ConfigHolder.Instance.FarmConfig != null
            ? ConfigHolder.Instance.FarmConfig.transport.GetCapacity(PlayableFarmState.GetTransportSaveData().currentLevel)
            : 10;
        int toSpawn = Mathf.Min(count, capacity - _spawnedFruitVisuals.Count);
        for (int i = 0; i < toSpawn; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Fruit fruit = FruitSpawner.SpawnFruit(plantId, cargoRoot, new Vector3(offset.x, offset.y, 0f), Quaternion.identity);
            if (fruit == null) continue;
            fruit.SetLayer("Fruit", 20 + _spawnedFruitVisuals.Count);
            fruit.transform.localScale = Vector3.zero;
            fruit.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            _spawnedFruitVisuals.Add(fruit);
        }
    }

    public void ClearFruitVisuals()
    {
        for (int i = 0; i < _spawnedFruitVisuals.Count; i++)
        {
            if (_spawnedFruitVisuals[i] != null) FruitSpawner.DespawnFruit(_spawnedFruitVisuals[i]);
        }
        _spawnedFruitVisuals.Clear();
    }
}
