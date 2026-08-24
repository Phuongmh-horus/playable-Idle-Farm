using System;
using System.Reflection;
using UnityEngine;

public sealed class LunaPlayableAdapter : IPlayablePlatformAdapter
{
    private bool _started;
    private bool _ended;

    public void GameStarted()
    {
        if (_started) return;
        _started = true;

        if (!LunaReflectionBridge.TryInvokeGameStarted())
        {
            Debug.LogWarning("[CTA] Luna GameStarted() is unavailable. Falling back to no-op.");
        }
    }

    public void GameEnded()
    {
        if (_ended) return;
        _ended = true;

        if (!LunaReflectionBridge.TryInvokeGameEnded())
        {
            Debug.LogWarning("[CTA] Luna GameEnded() is unavailable. Falling back to no-op.");
        }
    }

    public void InstallFullGame()
    {
        if (!LunaReflectionBridge.TryInvokeInstallFullGame())
        {
            // Debug.Log(gameObject.name);
            Debug.LogWarning("[CTA] Luna InstallFullGame() is unavailable. Falling back to no-op.");
        }
    }
}

internal static class LunaReflectionBridge
{
    private const string LifeCycleTypeName = "Luna.Unity.LifeCycle";
    private const string PlayableTypeName = "Luna.Unity.Playable";

    private static bool _resolved;
    private static MethodInfo _gameStartedMethod;
    private static MethodInfo _gameEndedMethod;
    private static MethodInfo _installMethod;

    public static bool TryInvokeGameStarted()
    {
        EnsureResolved();
        return TryInvoke(_gameStartedMethod);
    }

    public static bool TryInvokeGameEnded()
    {
        EnsureResolved();
        return TryInvoke(_gameEndedMethod);
    }

    public static bool TryInvokeInstallFullGame()
    {
        EnsureResolved();
        return TryInvoke(_installMethod);
    }

    private static void EnsureResolved()
    {
        if (_resolved) return;

        _resolved = true;
        _gameStartedMethod = ResolveStaticMethod(LifeCycleTypeName, "GameStarted");
        _gameEndedMethod = ResolveStaticMethod(LifeCycleTypeName, "GameEnded");
        _installMethod = ResolveStaticMethod(PlayableTypeName, "InstallFullGame");
    }

    private static MethodInfo ResolveStaticMethod(string typeName, string methodName)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (var i = 0; i < assemblies.Length; i++)
        {
            var type = assemblies[i].GetType(typeName);
            if (type == null) continue;

            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method != null) return method;
        }

        return null;
    }

    private static bool TryInvoke(MethodInfo method)
    {
        if (method == null) return false;

        try
        {
            method.Invoke(null, null);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[CTA] Failed to invoke Luna API: {exception.Message}");
            return false;
        }
    }
}

