#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace PlayableAd.Editor
{
    /// <summary>Centralizes local-only Reference Sync settings and its audit trail.</summary>
    internal static class ReferenceSyncConfiguration
    {
        private const string SourceRootKey = "PLA.ReferenceSync.SourceRoot";
        private const string SourceScriptsKey = "PLA.ReferenceSync.SourceScripts";
        private const string MirrorFolderKey = "PLA.ReferenceSync.MirrorFolder";
        internal const string DefaultSourceRoot = @"E:\real-war";
        internal const string DefaultSourceScripts = @"Assets\_Game\Scripts";
        internal const string DefaultMirrorFolder = @"Library\ReferenceSync\GameplayScripts_Reference";

        internal static string SourceRoot { get { return EditorPrefs.GetString(SourceRootKey, DefaultSourceRoot); } set { EditorPrefs.SetString(SourceRootKey, NormalizeAbsolutePath(value)); } }
        internal static string SourceScriptsRelativePath { get { return EditorPrefs.GetString(SourceScriptsKey, DefaultSourceScripts); } set { EditorPrefs.SetString(SourceScriptsKey, NormalizeRelativePath(value)); } }
        internal static string MirrorFolder { get { return EditorPrefs.GetString(MirrorFolderKey, DefaultMirrorFolder); } set { EditorPrefs.SetString(MirrorFolderKey, NormalizeRelativePath(value)); } }
        internal static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
        internal static string SourceScriptsPath { get { return Path.Combine(SourceRoot, SourceScriptsRelativePath); } }
        internal static string MirrorPath { get { return Path.Combine(ProjectRoot, MirrorFolder); } }
        internal static string ManifestPath { get { return Path.Combine(ProjectRoot, "Library", "ReferenceSync", "last-script-sync.json"); } }
        internal static string RunsPath { get { return Path.Combine(ProjectRoot, "Library", "ReferenceSync", "Runs"); } }
        internal static string LatestPlanPath { get { return Path.Combine(ProjectRoot, "Library", "ReferenceSync", "latest-asset-plan.json"); } }
        internal static string LatestAiPromptPath { get { return Path.Combine(ProjectRoot, "Library", "ReferenceSync", "latest-ai-review-prompt.md"); } }

        internal static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }

        internal static bool SameContent(string first, string second)
        {
            return File.Exists(first) && File.Exists(second) && new FileInfo(first).Length == new FileInfo(second).Length && string.Equals(HashFile(first), HashFile(second), StringComparison.Ordinal);
        }

        private static string NormalizeAbsolutePath(string value) { return string.IsNullOrWhiteSpace(value) ? DefaultSourceRoot : Path.GetFullPath(value.Trim()); }
        private static string NormalizeRelativePath(string value) { return (value ?? string.Empty).Trim().TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
    }

    [Serializable]
    internal sealed class ReferenceSyncManifest
    {
        public string sourceRoot, sourceCommit, createdUtc;
        public int copiedCount, unchangedCount, excludedCount;
        public ReferenceSyncManifestFile[] files;
    }

    [Serializable]
    internal sealed class ReferenceSyncManifestFile
    {
        public string relativePath, sourceHash, action;
    }

    [Serializable]
    internal sealed class ReferenceAssetMigrationPlan
    {
        public string runId, createdUtc, sourceRoot, targetRoot, policy, readiness, planPath, aiPromptPath;
        public ReferenceAssetMigrationItem[] items;
        public ReferenceAssetDependency[] dependencies;
        public string[] unresolvedGuids;
        public string[] warnings;
        public ReferenceMigrationCheck[] checks;
    }

    [Serializable]
    internal sealed class ReferenceAssetMigrationItem
    {
        public string relativePath, kind, detail, sourceHash;
    }

    [Serializable]
    internal sealed class ReferenceAssetDependency
    {
        public string relativePath, guid, sourceHash;
    }

    [Serializable]
    internal sealed class ReferenceMigrationCheck
    {
        public string severity, code, path, message;
    }
}
#endif
