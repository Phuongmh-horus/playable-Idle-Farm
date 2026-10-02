using System;
using System.Collections.Generic;
using System.Threading;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FXManager : MonoSingleton<FXManager>
{
    [SerializeField] private FXConfigSO config;

    private readonly Dictionary<UIParticle, CancellationTokenSource> _pooledParticleCtsMap = new Dictionary<UIParticle, CancellationTokenSource>();
    private CancellationTokenSource _masterCts;

    protected override void Awake()
    {
        base.Awake();
        _masterCts = new CancellationTokenSource();

        if (config == null)
        {
            Debug.LogError("[FXManager] FXConfigSO is null! Please assign FXConfigSO in the Inspector.", this);
        }
    }

    protected override void OnDestroy()
    {
        ClearAll();
        if (_masterCts != null)
        {
            _masterCts.Cancel();
            _masterCts.Dispose();
            _masterCts = null;
        }
        base.OnDestroy();
    }

    public async UniTask<bool> PlayHoeDigAsync(Vector3 worldPosition, Transform parent = null, CancellationToken cancellationToken = default)
    {
        if (config == null || config.hoePrefab == null)
        {
            Debug.LogError("[FXManager] Cannot PlayHoeDigAsync because FXConfigSO or hoePrefab is null!", this);
            return false;
        }

        using var scope = CreateLinkedTokenScope(cancellationToken);
        try
        {
            await AnimatedHoe.SpawnAndPlayAsync(config.hoePrefab, worldPosition, parent, scope.Token);
            return !scope.Token.IsCancellationRequested;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public void PlayCashUIParticle(Transform parent, Vector3 localPosition = default, float duration = -1f, CancellationToken cancellationToken = default)
    {
        if (config == null)
        {
            Debug.LogError("[FXManager] Cannot PlayCashUIParticle because FXConfigSO is null!", this);
            return;
        }

        if (config.cashUIParticlePrefab == null)
        {
            Debug.LogError("[FXManager] cashUIParticlePrefab in FXConfigSO is null!", config);
            return;
        }

        PlayUIParticle(config.cashUIParticlePrefab, parent, localPosition, duration, cancellationToken);
    }

    public void PlayUIParticle(
        UIParticle prefab,
        Transform parent,
        Vector3 localPosition = default,
        float duration = -1f,
        CancellationToken cancellationToken = default)
    {
        if (prefab == null)
        {
            Debug.LogError("[FXManager] UIParticle prefab is null!", this);
            return;
        }

        UIParticle instance = PoolHolder.Instance != null
            ? PoolHolder.Instance.Get(prefab, parent)
            : null;

        if (instance == null) return;

        Transform instTransform = instance.transform;
        instTransform.localPosition = localPosition;
        instTransform.localScale = Vector3.one;

        // Huỷ timer cũ của instance này nếu còn tồn tại
        if (_pooledParticleCtsMap.TryGetValue(instance, out var existingCts))
        {
            existingCts?.Cancel();
            existingCts?.Dispose();
            _pooledParticleCtsMap.Remove(instance);
        }

        var instanceCts = CancellationTokenSource.CreateLinkedTokenSource(
            _masterCts != null ? _masterCts.Token : CancellationToken.None,
            cancellationToken
        );
        _pooledParticleCtsMap[instance] = instanceCts;

        instance.StartEmission();
        instance.Play();

        float defaultDuration = config != null ? config.defaultUIParticleDuration : 1.0f;
        float playDuration = duration > 0f ? duration : defaultDuration;

        AutoDespawnPooledUIParticleAsync(instance, playDuration, instanceCts).Forget();
    }

    private async UniTaskVoid AutoDespawnPooledUIParticleAsync(
        UIParticle instance,
        float duration,
        CancellationTokenSource instanceCts)
    {
        try
        {
            await UniTask.WaitForSeconds(duration, ignoreTimeScale: true, cancellationToken: instanceCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        finally
        {
            // Chỉ thu hồi particle nếu timer này khớp với CTS hiện hành của instance
            if (_pooledParticleCtsMap.TryGetValue(instance, out var registeredCts) && registeredCts == instanceCts)
            {
                _pooledParticleCtsMap.Remove(instance);
                DespawnUIParticle(instance);
            }

            instanceCts.Dispose();
        }
    }

    private void DespawnUIParticle(UIParticle instance)
    {
        if (instance == null) return;

        instance.Stop();
        instance.Clear();

        if (PoolHolder.Instance != null)
        {
            instance.transform.SetParent(null);
            PoolHolder.Instance.Release(instance);
        }
    }

    public void ClearAll()
    {
        if (_pooledParticleCtsMap.Count == 0) return;

        // Copy danh sách để tránh thay đổi collection khi đang duyệt
        var activeParticles = new List<UIParticle>(_pooledParticleCtsMap.Keys);

        foreach (var particle in activeParticles)
        {
            if (_pooledParticleCtsMap.TryGetValue(particle, out var cts))
            {
                if (cts != null)
                {
                    cts.Cancel();
                    cts.Dispose();
                }
            }

            DespawnUIParticle(particle);
        }

        _pooledParticleCtsMap.Clear();
    }

    private readonly struct LinkedTokenScope : IDisposable
    {
        private readonly CancellationTokenSource _cts;

        public CancellationToken Token { get; }

        public LinkedTokenScope(CancellationToken token1, CancellationToken token2)
        {
            bool c1 = token1.CanBeCanceled;
            bool c2 = token2.CanBeCanceled;

            if (c1 && c2)
            {
                _cts = CancellationTokenSource.CreateLinkedTokenSource(token1, token2);
                Token = _cts.Token;
            }
            else if (c1)
            {
                _cts = null;
                Token = token1;
            }
            else if (c2)
            {
                _cts = null;
                Token = token2;
            }
            else
            {
                _cts = null;
                Token = CancellationToken.None;
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
        }
    }

    private LinkedTokenScope CreateLinkedTokenScope(CancellationToken callerToken)
    {
        CancellationToken masterToken = _masterCts != null ? _masterCts.Token : CancellationToken.None;
        return new LinkedTokenScope(masterToken, callerToken);
    }
}
