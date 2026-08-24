#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class FindSubAssets
{
    [MenuItem("Tools/Find Multi Asset Files")]
    static void Run()
    {
        foreach (var guid in AssetDatabase.FindAssets(""))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);

            if (assets.Length > 1)
            {
                Debug.Log($"{path} -> {assets.Length} assets");
            }
        }
    }
}
#endif