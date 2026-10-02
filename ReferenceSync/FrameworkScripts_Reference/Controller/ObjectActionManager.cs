using System;
using System.Collections.Generic;

public class ObjectActionManager<T>
{
    private readonly Dictionary<int, ActionSealed<int,T>> _objectHolders = new();

    private static ObjectActionManager<T> _instance;
    public static ObjectActionManager<T> Instance => _instance ??= new ObjectActionManager<T>();

    public void Bind(int key, T @object)
    {
        _objectHolders.TryAdd(key, new ActionSealed<int,T>());
        _objectHolders[key].Invoke(key,@object);
    }

    public void Append(int key, Action<int,T> onChangedAction)
    {
        if (_objectHolders.TryGetValue(key, out var objectHolder))
        {
            objectHolder.Register(onChangedAction);
        }
        else
        {
            var newObjectHolder = new ActionSealed<int,T>();
            newObjectHolder.Register(onChangedAction);
            _objectHolders.Add(key, newObjectHolder);
        }
    }

    public void Remove(int key, Action<int,T> onChangedAction)
    {
        if (_objectHolders.TryGetValue(key, out var objectHolder))
        {
            objectHolder.UnRegister(onChangedAction);
        }
    }
}
