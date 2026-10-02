using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

public class ActionSealed
{
    public Dictionary<int, Action> Actions = new();

    public void Register(Action action)
    {
        Actions.TryAdd(action.GetHashCode(), action);
    }

    public void UnRegister(Action action)
    {
        Actions.Remove(action.GetHashCode());
    }

    public void Invoke()
    {
        if (Actions.Values.Count == 0) return;
        var newActions = Actions.Values.ToList();
        foreach (var newAction in newActions)
        {
            try
            {
                newAction?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError(e + newAction?.Method.Name);
            }
        }
    }

    public async UniTask InvokeAsync()
    {
        await UniTask.Yield();
        Invoke();
    }
}

public class ActionSealed<T>
{
    public Dictionary<int, Action<T>> Actions = new();

    public void Register(Action<T> action)
    {
        Actions.TryAdd(action.GetHashCode(), action);
    }

    public void UnRegister(Action<T> action)
    {
        Actions.Remove(action.GetHashCode());
    }

    public void Invoke(T value)
    {
        if (Actions.Values.Count == 0) return;
        var newActions = Actions.Values.ToList();
        foreach (var newAction in newActions)
        {
            try
            {
                newAction?.Invoke(value);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    public async UniTask InvokeAsync(T value)
    {
        await UniTask.Yield();
        Invoke(value);
    }
}

public class ActionSealed<T,U>
{
    public Dictionary<int, Action<T,U>> Actions = new();

    public void Register(Action<T,U> action)
    {
        Actions.TryAdd(action.GetHashCode(), action);
    }

    public void UnRegister(Action<T,U> action)
    {
        Actions.Remove(action.GetHashCode());
    }

    public void Invoke(T value, U value2)
    {
        if (Actions.Values.Count == 0) return;
        var newActions = Actions.Values.ToList();
        foreach (var newAction in newActions)
        {
            try
            {
                newAction?.Invoke(value,value2);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    public async UniTask InvokeAsync(T value, U value2)
    {
        await UniTask.Yield();
        Invoke(value, value2);
    }
}