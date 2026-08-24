#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class FindAssetBundles
{
    [MenuItem("Tools/Find AssetBundles")]
    static void Run()
    {
        var names = AssetDatabase.GetAllAssetBundleNames();

        Debug.Log("Bundle Count = " + names.Length);

        foreach (var bundle in names)
        {
            Debug.Log(bundle);

            foreach (var asset in AssetDatabase.GetAssetPathsFromAssetBundle(bundle))
            {
                Debug.Log("    " + asset);
            }
        }
    }
}
#endif