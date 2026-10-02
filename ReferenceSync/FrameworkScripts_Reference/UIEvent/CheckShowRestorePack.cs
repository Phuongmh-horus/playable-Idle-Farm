using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CheckShowRestorePack : UIEventBase
{
    public static List<Action<Action>> CachedEvents = new List<Action<Action>>();

    public override UniTask<bool> CheckRequire()
    {
        return UniTask.FromResult(CachedEvents.Count > 0);
    }

    public override void Play()
    {
        ExecuteNextEvent();
    }

    private async void ExecuteNextEvent()
    {
        await UniTask.NextFrame();
        if (CachedEvents.Count > 0)
        {
            var nextEvent = CachedEvents[0];
            CachedEvents.RemoveAt(0);

            nextEvent?.Invoke(() =>
            {
                if (CachedEvents.Count > 0)
                {
                    ExecuteNextEvent();
                }
                else
                {
                    OnEndEvent();
                }
            });
        }
        else
        {
            OnEndEvent();
        }
    }
}
