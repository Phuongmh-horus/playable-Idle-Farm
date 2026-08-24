using System;
using System.Collections.Generic;
using UnityEngine;

public static class SceneObjectFinder
{
    public static T[] FindIncludingInactive<T>() where T : UnityEngine.Object
    {
        T[] allObjects = Resources.FindObjectsOfTypeAll<T>();
        if (allObjects == null || allObjects.Length == 0)
        {
            return new T[0];
        }

        List<T> sceneObjects = new List<T>(allObjects.Length);
        for (int i = 0; i < allObjects.Length; i++)
        {
            T candidate = allObjects[i];
            if (candidate == null || !IsLoadedSceneObject(candidate))
            {
                continue;
            }

            sceneObjects.Add(candidate);
        }

        return sceneObjects.ToArray();
    }

    private static bool IsLoadedSceneObject(UnityEngine.Object candidate)
    {
        GameObject gameObject = candidate as GameObject;
        if (gameObject != null)
        {
            return gameObject.scene.IsValid() && gameObject.scene.isLoaded;
        }

        Component component = candidate as Component;
        if (component != null && component.gameObject != null)
        {
            return component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded;
        }

        return false;
    }
}
