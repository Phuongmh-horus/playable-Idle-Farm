using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TransportVehicle : MonoBehaviour
{
    [SerializeField] private Transform cargoRoot;
    [SerializeField] private float spawnRadius = 0.4f;

    private readonly List<Fruit> _spawnedFruitVisuals = new List<Fruit>();
    private Coroutine _moveRoutine;
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

    public void MoveTo(Vector3 startPosition, Vector3 targetPosition, float duration, Action onComplete)
    {
        CancelMotions();
        float fixedY = transform.position.y;
        float fixedZ = transform.position.z;
        transform.position = new Vector3(startPosition.x, fixedY, fixedZ);
        _moveRoutine = StartCoroutine(MoveRoutine(startPosition.x, targetPosition.x, fixedY, fixedZ, duration, onComplete));
    }

    private IEnumerator MoveRoutine(float startX, float targetX, float fixedY, float fixedZ, float duration, Action onComplete)
    {
        duration = Mathf.Max(0.01f, duration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = progress * progress * (3f - 2f * progress);
            transform.position = new Vector3(Mathf.Lerp(startX, targetX, eased), fixedY, fixedZ);
            yield return null;
        }
        transform.position = new Vector3(targetX, fixedY, fixedZ);
        _moveRoutine = null;
        onComplete?.Invoke();
    }

    public void CancelMotions()
    {
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = null;
    }

    public void SpawnFruitVisuals(int visualPlantId, int count)
    {
        int capacity = ConfigHolder.Instance != null && ConfigHolder.Instance.FarmConfig != null
            ? ConfigHolder.Instance.FarmConfig.transport.GetCapacity(PlayableFarmState.GetTransportSaveData().currentLevel)
            : 10;
        int toSpawn = Mathf.Min(count, capacity - _spawnedFruitVisuals.Count);
        for (int i = 0; i < toSpawn; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Fruit fruit = FruitSpawner.SpawnFruit(visualPlantId, cargoRoot, new Vector3(offset.x, offset.y, 0f), Quaternion.identity);
            if (fruit == null) continue;
            fruit.SetLayer("Fruit", 20 + _spawnedFruitVisuals.Count);
            fruit.PlaySpawnScale();
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
