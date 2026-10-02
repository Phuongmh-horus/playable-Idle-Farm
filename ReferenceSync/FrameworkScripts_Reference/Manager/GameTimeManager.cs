using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class GameTimeManager : MonoSingleton<GameTimeManager>
{
    private static readonly Queue<Action> _ExecuteOnMainThread = new();
    [SerializeField]
    private float TimeUpdateRoutine = 2f;
    public Action OnUpdateRoutine { get; set; }

    float _timeUpdate = 0f;

    protected void Update()
    {
        _timeUpdate += Time.deltaTime;
        if (_timeUpdate > TimeUpdateRoutine)
        {
            _timeUpdate = 0f;
            // Đảm bảo OnUpdateRoutine luôn chạy trong main thread
            ExecuteOnMainThread(() => OnUpdateRoutine?.Invoke());
        }

        try
        {
            lock (_ExecuteOnMainThread)
            {
                while (_ExecuteOnMainThread.Count > 0)
                {
                    var action = _ExecuteOnMainThread.Dequeue();
                    action?.Invoke();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public static void ExecuteOnMainThread(Action action)
    {
        if (action == null) return;
        lock (_ExecuteOnMainThread)
        {
            _ExecuteOnMainThread.Enqueue(action);
        }
    }

    public static async void DispatchDelayedOnMainThread(Action action, float delaySeconds)
    {
        if (Instance == null)
        {
            Debug.LogErrorFormat(
                "No Dispatcher instance found. " +
                "Delayed action cannot be dispatched on main thread."
            );
            return;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), delayType: DelayType.DeltaTime);
        ExecuteOnMainThread(action);
    }
}