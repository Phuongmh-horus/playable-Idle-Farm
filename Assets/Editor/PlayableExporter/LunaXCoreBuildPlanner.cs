using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
internal sealed class LunaXCoreBuildPlan
{
    public string sourceDirectory;
    public string indexHtmlPath;
    public string engineScriptPath;
    public string primaryCacheVersion;
    public string cacheScriptsPath;
    public string cacheJsonsPath;
    public string cacheBlobsPath;
    public bool hasDualCacheVersions;
    public bool hasSecondaryBundleBlob;
    public bool isSecondaryBundleBlobEmpty;
    public readonly List<string> cacheVersions = new List<string>();
    public readonly List<string> runtimeJsonPaths = new List<string>();
    public readonly List<string> runtimeBlobPaths = new List<string>();
    public readonly List<string> runtimeSoundPaths = new List<string>();
    public readonly List<string> runtimeImagePaths = new List<string>();
    public readonly List<string> runtimeVideoPaths = new List<string>();
    public readonly List<string> sourceSupportPaths = new List<string>();
    public readonly List<string> warnings = new List<string>();
}

internal static class LunaXCoreBuildPlanner
{
    public static LunaXCoreBuildPlan CreatePlan(string sourceDirectory)
    {
        var topology = LunaBuildProjectScanner.Scan(sourceDirectory);
        var plan = new LunaXCoreBuildPlan
        {
            sourceDirectory = topology.sourceDirectory,
            indexHtmlPath = topology.indexHtmlPath,
            engineScriptPath = topology.engineScriptPath,
            hasDualCacheVersions = topology.cacheVersions.Count > 1,
            hasSecondaryBundleBlob = topology.hasSecondaryBundleBlob,
            isSecondaryBundleBlobEmpty = topology.isSecondaryBundleBlobEmpty
        };

        plan.cacheVersions.AddRange(topology.cacheVersions.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        plan.warnings.AddRange(topology.warnings);
        plan.primaryCacheVersion = plan.cacheVersions.LastOrDefault() ?? string.Empty;

        if (!string.IsNullOrEmpty(plan.primaryCacheVersion))
        {
            var cachePrefix = $"cache/{plan.primaryCacheVersion}/";
            plan.cacheScriptsPath = topology.entries
                .Select(entry => entry.relativePath)
                .FirstOrDefault(path => path.Equals(cachePrefix + "scripts.js", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            plan.cacheJsonsPath = topology.entries
                .Select(entry => entry.relativePath)
                .FirstOrDefault(path => path.Equals(cachePrefix + "jsons.js", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            plan.cacheBlobsPath = topology.entries
                .Select(entry => entry.relativePath)
                .FirstOrDefault(path => path.Equals(cachePrefix + "blobs.js", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        }

        foreach (var entry in topology.entries.OrderBy(item => item.relativePath, StringComparer.OrdinalIgnoreCase))
        {
            switch (entry.kind)
            {
                case LunaBuildProjectAssetKind.JsonPayload when entry.requiredForRuntime:
                    plan.runtimeJsonPaths.Add(entry.relativePath);
                    break;
                case LunaBuildProjectAssetKind.BlobPayload when entry.requiredForRuntime:
                    plan.runtimeBlobPaths.Add(entry.relativePath);
                    break;
                case LunaBuildProjectAssetKind.SoundPayload when entry.requiredForRuntime:
                    plan.runtimeSoundPaths.Add(entry.relativePath);
                    break;
                case LunaBuildProjectAssetKind.ImagePayload when entry.requiredForRuntime:
                    plan.runtimeImagePaths.Add(entry.relativePath);
                    break;
                case LunaBuildProjectAssetKind.VideoPayload when entry.requiredForRuntime:
                    plan.runtimeVideoPaths.Add(entry.relativePath);
                    break;
                case LunaBuildProjectAssetKind.SourceSupport:
                case LunaBuildProjectAssetKind.CacheScript:
                    plan.sourceSupportPaths.Add(entry.relativePath);
                    break;
            }
        }

        if (plan.runtimeJsonPaths.Count == 0)
        {
            plan.warnings.Add("No runtime json payloads were planned.");
        }

        if (plan.runtimeBlobPaths.Count == 0)
        {
            plan.warnings.Add("No runtime blob payloads were planned.");
        }

        if (string.IsNullOrWhiteSpace(plan.cacheScriptsPath) ||
            string.IsNullOrWhiteSpace(plan.cacheJsonsPath) ||
            string.IsNullOrWhiteSpace(plan.cacheBlobsPath))
        {
            plan.warnings.Add("The Luna cache transport scripts are incomplete. The builder will fall back to reconstructed runtime payloads.");
        }

        return plan;
    }
}

