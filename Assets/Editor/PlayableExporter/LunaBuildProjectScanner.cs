using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal enum LunaBuildProjectAssetKind
{
    IndexHtml,
    EngineScript,
    CacheScript,
    JsonPayload,
    BlobPayload,
    SoundPayload,
    ImagePayload,
    VideoPayload,
    SourceSupport,
    Unknown
}

[Serializable]
internal sealed class LunaBuildProjectEntryInfo
{
    public string relativePath;
    public LunaBuildProjectAssetKind kind;
    public long sizeBytes;
    public bool requiredForRuntime;
}

[Serializable]
internal sealed class LunaBuildProjectTopology
{
    public string sourceDirectory;
    public string indexHtmlPath;
    public string engineScriptPath;
    public bool hasPrimaryBundleBlob;
    public bool hasSecondaryBundleBlob;
    public bool isSecondaryBundleBlobEmpty;
    public int totalFileCount;
    public int runtimeJsonCount;
    public int runtimeBlobCount;
    public int runtimeSoundCount;
    public int runtimeImageCount;
    public int runtimeVideoCount;
    public readonly List<string> cacheVersions = new List<string>();
    public readonly List<LunaBuildProjectEntryInfo> entries = new List<LunaBuildProjectEntryInfo>();
    public readonly List<string> warnings = new List<string>();

    public IEnumerable<LunaBuildProjectEntryInfo> RuntimePayloadEntries()
    {
        return entries.Where(entry => entry.requiredForRuntime);
    }
}

internal static class LunaBuildProjectScanner
{
    private static readonly string[] RequiredRuntimeJsons =
    {
        "assets/bundles.json",
        "assets/bundles/-1/bundle.json",
        "assets/bundles/-2/bundle.json",
        "assets/shaders.json"
    };

    private static readonly HashSet<string> SourceSupportFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "engine/manifest.json",
        "luna.json",
        "playable_health_check.json",
        "playground.json",
        "resources/data.json"
    };

    public static LunaBuildProjectTopology Scan(string sourceDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory)) throw new ArgumentException("Source directory is required.", nameof(sourceDirectory));

        var fullSourceDirectory = Path.GetFullPath(sourceDirectory);
        if (!Directory.Exists(fullSourceDirectory)) throw new DirectoryNotFoundException($"Source directory was not found: {fullSourceDirectory}");

        var topology = new LunaBuildProjectTopology
        {
            sourceDirectory = fullSourceDirectory
        };

        var files = Directory.GetFiles(fullSourceDirectory, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        topology.totalFileCount = files.Length;

        foreach (var filePath in files)
        {
            var relativePath = NormalizeRelativePath(fullSourceDirectory, filePath);
            var fileInfo = new FileInfo(filePath);
            var entry = new LunaBuildProjectEntryInfo
            {
                relativePath = relativePath,
                kind = Classify(relativePath),
                sizeBytes = fileInfo.Length,
                requiredForRuntime = IsRequiredForRuntime(relativePath)
            };

            topology.entries.Add(entry);
            UpdateTopology(topology, entry);
        }

        topology.indexHtmlPath = topology.entries.FirstOrDefault(entry => entry.kind == LunaBuildProjectAssetKind.IndexHtml)?.relativePath ?? string.Empty;
        topology.engineScriptPath = topology.entries.FirstOrDefault(entry => entry.kind == LunaBuildProjectAssetKind.EngineScript)?.relativePath ?? string.Empty;

        Validate(topology);
        return topology;
    }

    private static void UpdateTopology(LunaBuildProjectTopology topology, LunaBuildProjectEntryInfo entry)
    {
        switch (entry.kind)
        {
            case LunaBuildProjectAssetKind.CacheScript:
                var cacheVersion = ExtractCacheVersion(entry.relativePath);
                if (!string.IsNullOrEmpty(cacheVersion) && !topology.cacheVersions.Contains(cacheVersion, StringComparer.OrdinalIgnoreCase))
                {
                    topology.cacheVersions.Add(cacheVersion);
                }
                break;
            case LunaBuildProjectAssetKind.JsonPayload:
                if (entry.requiredForRuntime)
                {
                    topology.runtimeJsonCount++;
                }
                break;
            case LunaBuildProjectAssetKind.BlobPayload:
                if (entry.requiredForRuntime)
                {
                    topology.runtimeBlobCount++;
                }

                if (entry.relativePath.Equals("assets/bundles/-1/data.blob", StringComparison.OrdinalIgnoreCase))
                {
                    topology.hasPrimaryBundleBlob = true;
                }
                else if (entry.relativePath.Equals("assets/bundles/-2/data.blob", StringComparison.OrdinalIgnoreCase))
                {
                    topology.hasSecondaryBundleBlob = true;
                    topology.isSecondaryBundleBlobEmpty = entry.sizeBytes == 0;
                }
                break;
            case LunaBuildProjectAssetKind.SoundPayload:
                if (entry.requiredForRuntime)
                {
                    topology.runtimeSoundCount++;
                }
                break;
            case LunaBuildProjectAssetKind.ImagePayload:
                if (entry.requiredForRuntime)
                {
                    topology.runtimeImageCount++;
                }
                break;
            case LunaBuildProjectAssetKind.VideoPayload:
                if (entry.requiredForRuntime)
                {
                    topology.runtimeVideoCount++;
                }
                break;
        }
    }

    private static void Validate(LunaBuildProjectTopology topology)
    {
        if (string.IsNullOrEmpty(topology.indexHtmlPath))
        {
            topology.warnings.Add("Missing index.html at the build-project root.");
        }

        if (string.IsNullOrEmpty(topology.engineScriptPath))
        {
            topology.warnings.Add("Missing engine/scripts.js.");
        }

        foreach (var requiredJson in RequiredRuntimeJsons)
        {
            if (!topology.entries.Any(entry => entry.relativePath.Equals(requiredJson, StringComparison.OrdinalIgnoreCase)))
            {
                topology.warnings.Add($"Missing required runtime json: {requiredJson}.");
            }
        }

        if (!topology.hasPrimaryBundleBlob)
        {
            topology.warnings.Add("Missing assets/bundles/-1/data.blob.");
        }

        if (topology.cacheVersions.Count == 0)
        {
            topology.warnings.Add("No cache/<version> scripts were found.");
        }
    }

    private static LunaBuildProjectAssetKind Classify(string relativePath)
    {
        if (relativePath.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            return LunaBuildProjectAssetKind.IndexHtml;
        }

        if (relativePath.Equals("engine/scripts.js", StringComparison.OrdinalIgnoreCase))
        {
            return LunaBuildProjectAssetKind.EngineScript;
        }

        if (relativePath.StartsWith("cache/", StringComparison.OrdinalIgnoreCase) && relativePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        {
            return LunaBuildProjectAssetKind.CacheScript;
        }

        if (SourceSupportFiles.Contains(relativePath))
        {
            return LunaBuildProjectAssetKind.SourceSupport;
        }

        var extension = Path.GetExtension(relativePath).ToLowerInvariant();
        switch (extension)
        {
            case ".json":
                return LunaBuildProjectAssetKind.JsonPayload;
            case ".blob":
                return LunaBuildProjectAssetKind.BlobPayload;
            case ".mp3":
            case ".wav":
            case ".ogg":
                return LunaBuildProjectAssetKind.SoundPayload;
            case ".png":
            case ".jpg":
            case ".jpeg":
            case ".gif":
            case ".webp":
                return LunaBuildProjectAssetKind.ImagePayload;
            case ".mp4":
            case ".webm":
            case ".ogv":
                return LunaBuildProjectAssetKind.VideoPayload;
            default:
                return LunaBuildProjectAssetKind.Unknown;
        }
    }

    private static bool IsRequiredForRuntime(string relativePath)
    {
        if (relativePath.Equals("engine/scripts.js", StringComparison.OrdinalIgnoreCase) ||
            relativePath.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (relativePath.StartsWith("cache/", StringComparison.OrdinalIgnoreCase) ||
            SourceSupportFiles.Contains(relativePath))
        {
            return false;
        }

        if (relativePath.Equals("assets/bundles/-2/data.blob", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Classify(relativePath) switch
        {
            LunaBuildProjectAssetKind.JsonPayload => relativePath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase),
            LunaBuildProjectAssetKind.BlobPayload => relativePath.Equals("assets/bundles/-1/data.blob", StringComparison.OrdinalIgnoreCase),
            LunaBuildProjectAssetKind.SoundPayload => true,
            LunaBuildProjectAssetKind.ImagePayload => true,
            LunaBuildProjectAssetKind.VideoPayload => true,
            _ => false
        };
    }

    private static string ExtractCacheVersion(string relativePath)
    {
        var segments = relativePath.Split('/');
        return segments.Length >= 3 ? segments[1] : string.Empty;
    }

    private static string NormalizeRelativePath(string rootDirectory, string filePath)
    {
        return Path.GetRelativePath(rootDirectory, filePath).Replace('\\', '/');
    }
}
