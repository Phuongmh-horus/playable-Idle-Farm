using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class FarmPlotScenePersistence
{
    private const string SnapshotKey = "PlayableIdleFarm.FarmPlotSceneSnapshot";

    [Serializable]
    private sealed class SnapshotCollection
    {
        public List<SystemSnapshot> systems = new List<SystemSnapshot>();
    }

    [Serializable]
    private sealed class SystemSnapshot
    {
        public string systemGlobalId;
        public List<PlotSnapshot> plots = new List<PlotSnapshot>();
    }

    [Serializable]
    private sealed class PlotSnapshot
    {
        public string prefabGuid;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public int siblingIndex;
    }

    static FarmPlotScenePersistence()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            CaptureRuntimePlots();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            RestoreScenePlots();
        }
    }

    private static void CaptureRuntimePlots()
    {
        SnapshotCollection collection = new SnapshotCollection();
        FarmPlotSystem[] systems = Resources.FindObjectsOfTypeAll<FarmPlotSystem>();

        for (int systemIndex = 0; systemIndex < systems.Length; systemIndex++)
        {
            FarmPlotSystem system = systems[systemIndex];
            if (!CanCapture(system))
            {
                continue;
            }

            SystemSnapshot systemSnapshot = new SystemSnapshot
            {
                systemGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(system).ToString()
            };

            IReadOnlyList<Plot> runtimePlots = system.RuntimeSpawnedPlots;
            for (int plotIndex = 0; plotIndex < runtimePlots.Count; plotIndex++)
            {
                Plot plot = runtimePlots[plotIndex];
                if (plot == null)
                {
                    continue;
                }

                Plot prefab = system.GetPlotPrefabForRing(plot.RingIndex);
                string prefabPath = prefab != null ? AssetDatabase.GetAssetPath(prefab.gameObject) : string.Empty;
                string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
                if (string.IsNullOrEmpty(prefabGuid))
                {
                    Debug.LogWarning($"[FarmPlotScenePersistence] Cannot resolve the source prefab for runtime plot '{plot.name}'. Scene sync skipped for this FarmPlotSystem.", system);
                    systemSnapshot.plots.Clear();
                    break;
                }

                Transform plotTransform = plot.transform;
                systemSnapshot.plots.Add(new PlotSnapshot
                {
                    prefabGuid = prefabGuid,
                    localPosition = plotTransform.localPosition,
                    localRotation = plotTransform.localRotation,
                    localScale = plotTransform.localScale,
                    siblingIndex = plotTransform.GetSiblingIndex()
                });
            }

            if (systemSnapshot.plots.Count > 0)
            {
                collection.systems.Add(systemSnapshot);
            }
        }

        if (collection.systems.Count == 0)
        {
            SessionState.EraseString(SnapshotKey);
            return;
        }

        SessionState.SetString(SnapshotKey, JsonUtility.ToJson(collection));
    }

    private static bool CanCapture(FarmPlotSystem system)
    {
        return system != null
               && !system.UseScenePlots
               && system.gameObject.scene.IsValid()
               && system.gameObject.scene.isLoaded
               && system.RuntimeSpawnedPlots.Count > 0;
    }

    private static void RestoreScenePlots()
    {
        string json = SessionState.GetString(SnapshotKey, string.Empty);
        SessionState.EraseString(SnapshotKey);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        SnapshotCollection collection = JsonUtility.FromJson<SnapshotCollection>(json);
        if (collection == null || collection.systems == null)
        {
            return;
        }

        for (int i = 0; i < collection.systems.Count; i++)
        {
            RestoreSystem(collection.systems[i]);
        }
    }

    private static void RestoreSystem(SystemSnapshot snapshot)
    {
        if (snapshot == null
            || snapshot.plots == null
            || snapshot.plots.Count == 0
            || !GlobalObjectId.TryParse(snapshot.systemGlobalId, out GlobalObjectId globalId))
        {
            return;
        }

        FarmPlotSystem system = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as FarmPlotSystem;
        if (system == null || system.UseScenePlots || !system.gameObject.scene.IsValid() || !system.gameObject.scene.isLoaded)
        {
            return;
        }

        List<GameObject> prefabAssets = ResolvePrefabAssets(snapshot.plots);
        if (prefabAssets == null)
        {
            Debug.LogError("[FarmPlotScenePersistence] One or more plot prefabs are missing. Existing scene plots were left unchanged.", system);
            return;
        }

        Transform root = system.SlotRoot;
        if (root == null)
        {
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Sync Farm Plot Prefabs After Play Mode");
        RemoveDirectScenePlots(root);

        for (int i = 0; i < snapshot.plots.Count; i++)
        {
            PlotSnapshot plotSnapshot = snapshot.plots[i];
            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAssets[i], root) as GameObject;
            if (instance == null)
            {
                continue;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create synced farm plot");
            Transform instanceTransform = instance.transform;
            instanceTransform.localPosition = plotSnapshot.localPosition;
            instanceTransform.localRotation = plotSnapshot.localRotation;
            instanceTransform.localScale = plotSnapshot.localScale;
            instanceTransform.SetSiblingIndex(Mathf.Clamp(plotSnapshot.siblingIndex, 0, root.childCount - 1));
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(system.gameObject.scene);
        Debug.Log($"[FarmPlotScenePersistence] Synced {snapshot.plots.Count} prefab-linked scene plots under '{root.name}'.", system);
    }

    private static List<GameObject> ResolvePrefabAssets(List<PlotSnapshot> snapshots)
    {
        List<GameObject> prefabAssets = new List<GameObject>(snapshots.Count);
        for (int i = 0; i < snapshots.Count; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(snapshots[i].prefabGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                return null;
            }
            prefabAssets.Add(prefab);
        }
        return prefabAssets;
    }

    private static void RemoveDirectScenePlots(Transform root)
    {
        for (int childIndex = root.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = root.GetChild(childIndex);
            if (child.GetComponent<Plot>() != null)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }
}
