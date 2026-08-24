using System;
using UnityEngine;

public sealed class GameplayCtaTriggerSource : MonoBehaviour
{
    public event Action Win;
    public event Action Lose;
    public event Action PreLose;
    public event Action ClickThresholdReached;

    public void NotifyWin()
    {
        Win?.Invoke();
    }

    public void NotifyLose()
    {
        Lose?.Invoke();
    }

    public void NotifyPreLose()
    {
        PreLose?.Invoke();
    }

    public void NotifyClickThresholdReached()
    {
        ClickThresholdReached?.Invoke();
    }
}
