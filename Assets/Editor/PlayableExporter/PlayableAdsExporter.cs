using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;

public static class PlayableAdsExporter
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly PlayableAdNetwork[] PaidNetworks =
    {
        PlayableAdNetwork.GoogleAds,
        PlayableAdNetwork.Mintegral,
        PlayableAdNetwork.AppLovin,
        PlayableAdNetwork.Facebook,
        PlayableAdNetwork.IronSource,
        PlayableAdNetwork.Moloco,
        PlayableAdNetwork.TikTok,
        PlayableAdNetwork.Unity,
        PlayableAdNetwork.Vungle
    };

    public static IReadOnlyList<PlayableAdNetwork> GetPaidNetworks()
    {
        return PaidNetworks;
    }

    public static PlayableBatchExportReport ExportPaidNetworks(PlayableExportOptions options)
    {
        return ExportPaidNetworks(options, PaidNetworks);
    }

    public static PlayableBatchExportReport ExportPaidNetworks(PlayableExportOptions options, IEnumerable<PlayableAdNetwork> selectedNetworks)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        var sourceZipPath = Path.GetFullPath(options.sourceZipPath);
        if (!File.Exists(sourceZipPath)) throw new FileNotFoundException("Luna build zip was not found.", sourceZipPath);

        var networks = (selectedNetworks ?? Array.Empty<PlayableAdNetwork>())
            .Distinct()
            .ToList();
        if (networks.Count == 0)
        {
            throw new InvalidOperationException("At least one paid network must be selected for batch export.");
        }

        var outputRoot = ResolveOutputRoot(options.outputRootPath);
        Directory.CreateDirectory(outputRoot);

        var baseName = Path.GetFileNameWithoutExtension(sourceZipPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var batchDirectory = Path.Combine(outputRoot, "PaidNetworks", $"{baseName}_paid_{timestamp}");
        ResetDirectory(batchDirectory);

        var report = new PlayableBatchExportReport
        {
            sourceZipPath = sourceZipPath,
            outputDirectory = batchDirectory
        };

        foreach (var network in networks)
        {
            report.networkReports.Add(ExportInternal(network, options, batchDirectory, timestamp, true));
        }

        report.bundleZipPath = Path.Combine(batchDirectory, $"{baseName}_paid_networks.zip");
        CreateBatchBundle(report.bundleZipPath, report.networkReports);

        foreach (var networkReport in report.networkReports)
        {
            if (!networkReport.qcReady)
            {
                report.warnings.Add($"{networkReport.network} failed the built-in QC gate.");
            }

            if (!networkReport.artifactWithinRecommendedLimit)
            {
                report.warnings.Add($"{networkReport.network} exceeds {networkReport.recommendedLimitLabel}.");
            }

            if (networkReport.hasForbiddenExternalReferences)
            {
                report.warnings.Add($"{networkReport.network} still contains external references outside the allowed list.");
            }
        }

        return report;
    }

    public static PlayableNetworkExportReport Export(PlayableAdNetwork network, PlayableExportOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return ExportInternal(network, options, ResolveOutputRoot(options.outputRootPath), timestamp, false);
    }

    public static string TryFindLatestLunaZip()
    {
        var createHubDirectory = Path.Combine(GetProjectRoot(), "LunaTemp", "stage4", "create-hub");
        if (!Directory.Exists(createHubDirectory))
        {
            return string.Empty;
        }

        return Directory.GetFiles(createHubDirectory, "*.zip", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault() ?? string.Empty;
    }

    public static void ExportPaidNetworksHeadless()
    {
        var sourceZipPath = Environment.GetEnvironmentVariable("PLAYABLE_EXPORT_SOURCE_ZIP");
        if (string.IsNullOrWhiteSpace(sourceZipPath))
        {
            sourceZipPath = TryFindLatestLunaZip();
        }

        if (string.IsNullOrWhiteSpace(sourceZipPath))
        {
            throw new InvalidOperationException("No Luna create-hub zip was found for headless export.");
        }

        var options = new PlayableExportOptions
        {
            sourceZipPath = sourceZipPath,
            outputRootPath = Environment.GetEnvironmentVariable("PLAYABLE_EXPORT_OUTPUT_ROOT") ?? Path.Combine(GetProjectRoot(), "PlayableExports", "Official"),
            logoImagePath = Environment.GetEnvironmentVariable("PLAYABLE_EXPORT_LOGO_PATH") ?? string.Empty,
            applicationName = Environment.GetEnvironmentVariable("PLAYABLE_EXPORT_APP_NAME") ?? string.Empty,
            orientation = Environment.GetEnvironmentVariable("PLAYABLE_EXPORT_ORIENTATION") ?? "portrait,landscape",
            stripLunaDebugScripts = ParseBooleanEnvironmentVariable("PLAYABLE_EXPORT_STRIP_DEBUG", true),
            iosStoreUrl = Environment.GetEnvironmentVariable("PLAYABLE_EXPORT_IOS_URL") ?? string.Empty,
            androidStoreUrl = Environment.GetEnvironmentVariable("PLAYABLE_EXPORT_ANDROID_URL") ?? string.Empty
        };

        var report = ExportPaidNetworks(options);
        Debug.Log($"[PlayableExporter] Headless paid network export completed: {report.bundleZipPath}");
    }
    public static string GetProjectRoot()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (currentDirectory != null)
        {
            if (Directory.Exists(Path.Combine(currentDirectory.FullName, "Assets")) &&
                Directory.Exists(Path.Combine(currentDirectory.FullName, "ProjectSettings")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static PlayableNetworkExportReport ExportInternal(
        PlayableAdNetwork network,
        PlayableExportOptions options,
        string outputRoot,
        string timestamp,
        bool useBatchLayout)
    {
        var sourceZipPath = Path.GetFullPath(options.sourceZipPath);
        if (!File.Exists(sourceZipPath)) throw new FileNotFoundException("Luna build zip was not found.", sourceZipPath);

        var baseName = Path.GetFileNameWithoutExtension(sourceZipPath);
        var exportDirectory = useBatchLayout
            ? Path.Combine(outputRoot, GetNetworkDirectoryName(network))
            : Path.Combine(outputRoot, GetNetworkDirectoryName(network), $"{baseName}_{GetNetworkSlug(network)}_{timestamp}");
        var workingDirectory = Path.Combine(exportDirectory, "working");
        var sourceDirectory = Path.Combine(workingDirectory, "source");
        var singleHtmlPath = network == PlayableAdNetwork.Vungle
            ? Path.Combine(exportDirectory, "index.html")
            : Path.Combine(exportDirectory, $"{baseName}.html");
        var reportTextPath = Path.Combine(exportDirectory, $"{GetNetworkSlug(network)}-report.txt");

        ResetDirectory(exportDirectory);
        try
        {
            Directory.CreateDirectory(workingDirectory);
            ZipFile.ExtractToDirectory(sourceZipPath, sourceDirectory);

            var packResult = LunaXSharedCoreBuilder.Build(new LunaSingleHtmlPackOptions
            {
                sourceDirectory = sourceDirectory,
                network = network,
                applicationName = options.applicationName ?? string.Empty,
                logoImagePath = options.logoImagePath ?? string.Empty,
                orientation = string.IsNullOrWhiteSpace(options.orientation) ? "portrait,landscape" : options.orientation,
                iosStoreUrl = options.iosStoreUrl ?? string.Empty,
                androidStoreUrl = options.androidStoreUrl ?? string.Empty,
                stripLunaDebugScripts = options.stripLunaDebugScripts,
                externalizeRuntimeMedia = network == PlayableAdNetwork.Vungle
            });

            File.WriteAllText(singleHtmlPath, packResult.html, Utf8NoBom);

            var artifactPath = singleHtmlPath;
            var artifactKind = "html";
            if (UsesSingleFileZipArtifact(network))
            {
                artifactKind = "zip";
                artifactPath = Path.Combine(exportDirectory, $"{baseName}.zip");
                CreateSingleFileZip(artifactPath, singleHtmlPath, Path.GetFileName(singleHtmlPath));
            }
            else if (network == PlayableAdNetwork.TikTok)
            {
                artifactKind = "zip";
                artifactPath = Path.Combine(exportDirectory, $"{baseName}.zip");
                var configJsonPath = Path.Combine(exportDirectory, "config.json");
                File.WriteAllText(configJsonPath, BuildTikTokConfigJson(options), Utf8NoBom);
                CreateTikTokZip(artifactPath, singleHtmlPath, configJsonPath);
            }
            else if (network == PlayableAdNetwork.Vungle)
            {
                artifactKind = "zip";
                artifactPath = Path.Combine(exportDirectory, $"{baseName}.zip");
                CreateVungleZip(sourceDirectory, exportDirectory, artifactPath);
            }

            var report = PlayableExportVerifier.BuildNetworkReport(network, sourceZipPath, exportDirectory, string.Empty, singleHtmlPath, artifactPath, artifactKind, options, packResult);
            File.WriteAllText(reportTextPath, PlayableExportVerifier.BuildHumanReadableReport(report), Utf8NoBom);
            CleanupExportDirectory(exportDirectory, artifactPath, reportTextPath);
            return report;
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, true);
            }
        }
    }

    private static void CreateBatchBundle(string bundleZipPath, IEnumerable<PlayableNetworkExportReport> reports)
    {
        if (File.Exists(bundleZipPath))
        {
            File.Delete(bundleZipPath);
        }

        using (var archive = ZipFile.Open(bundleZipPath, ZipArchiveMode.Create))
        {
            foreach (var report in reports)
            {
                var entryName = $"{report.network}/{Path.GetFileName(report.artifactPath)}";
                archive.CreateEntryFromFile(report.artifactPath, entryName, System.IO.Compression.CompressionLevel.Optimal);
            }
        }
    }

    private static void CreateSingleFileZip(string zipPath, string filePath, string entryName)
    {
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(filePath, entryName, System.IO.Compression.CompressionLevel.Optimal);
        }
    }

    private static void CreateTikTokZip(string zipPath, string htmlPath, string configJsonPath)
    {
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(htmlPath, "index.html", System.IO.Compression.CompressionLevel.Optimal);
            archive.CreateEntryFromFile(configJsonPath, "config.json", System.IO.Compression.CompressionLevel.Optimal);
        }
    }

    private static void CreateVungleZip(string sourceDirectory, string exportDirectory, string zipPath)
    {
        var packageDirectory = Path.Combine(exportDirectory, "vungle-package");
        ResetDirectory(packageDirectory);

        var htmlSourcePath = Path.Combine(exportDirectory, "index.html");
        var htmlPackagePath = Path.Combine(packageDirectory, "index.html");
        File.Copy(htmlSourcePath, htmlPackagePath, true);

        var plan = LunaXCoreBuildPlanner.CreatePlan(sourceDirectory);
        CopyRuntimeMediaFiles(sourceDirectory, packageDirectory, plan.runtimeImagePaths);
        CopyRuntimeMediaFiles(sourceDirectory, packageDirectory, plan.runtimeVideoPaths);

        CreateDirectoryZip(zipPath, packageDirectory);
        Directory.Delete(packageDirectory, true);
    }

    private static void CopyRuntimeMediaFiles(string sourceDirectory, string packageDirectory, IEnumerable<string> relativePaths)
    {
        foreach (var relativePath in relativePaths)
        {
            var sourcePath = Path.Combine(sourceDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(sourcePath))
            {
                continue;
            }

            var destinationPath = Path.Combine(packageDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourcePath, destinationPath, true);
        }
    }

    private static void CreateDirectoryZip(string zipPath, string sourceDirectory)
    {
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (var filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(sourceDirectory, filePath).Replace('\\', '/');
                archive.CreateEntryFromFile(filePath, entryName, System.IO.Compression.CompressionLevel.Optimal);
            }
        }
    }

    private static void CleanupExportDirectory(string exportDirectory, string artifactPath, string reportTextPath)
    {
        var keepPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.GetFullPath(artifactPath),
            Path.GetFullPath(reportTextPath)
        };

        foreach (var filePath in Directory.GetFiles(exportDirectory, "*", SearchOption.AllDirectories))
        {
            var fullPath = Path.GetFullPath(filePath);
            if (!keepPaths.Contains(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        DeleteEmptyDirectories(exportDirectory);
    }

    private static void DeleteEmptyDirectories(string rootDirectory)
    {
        foreach (var directoryPath in Directory.GetDirectories(rootDirectory, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            if (Directory.EnumerateFileSystemEntries(directoryPath).Any())
            {
                continue;
            }

            Directory.Delete(directoryPath);
        }
    }

    private static bool ParseBooleanEnvironmentVariable(string variableName, bool defaultValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        if (bool.TryParse(rawValue, out var parsedValue))
        {
            return parsedValue;
        }

        return defaultValue;
    }
    private static string ResolveOutputRoot(string outputRootPath)
    {
        return string.IsNullOrWhiteSpace(outputRootPath)
            ? Path.Combine(GetProjectRoot(), "PlayableExports", "Official")
            : Path.GetFullPath(outputRootPath);
    }

    private static string GetNetworkDirectoryName(PlayableAdNetwork network)
    {
        switch (network)
        {
            case PlayableAdNetwork.GoogleAds:
                return "GoogleAds";
            case PlayableAdNetwork.Mintegral:
                return "Mintegral";
            case PlayableAdNetwork.AppLovin:
                return "AppLovin";
            case PlayableAdNetwork.Facebook:
                return "Facebook";
            case PlayableAdNetwork.IronSource:
                return "IronSource";
            case PlayableAdNetwork.Moloco:
                return "Moloco";
            case PlayableAdNetwork.TikTok:
                return "Tiktok";
            case PlayableAdNetwork.Unity:
                return "Unity";
            case PlayableAdNetwork.Vungle:
                return "Vungle";
            default:
                return network.ToString();
        }
    }

    private static string GetNetworkSlug(PlayableAdNetwork network)
    {
        switch (network)
        {
            case PlayableAdNetwork.GoogleAds:
                return "googleads";
            case PlayableAdNetwork.Mintegral:
                return "mintegral";
            case PlayableAdNetwork.AppLovin:
                return "applovin";
            case PlayableAdNetwork.Facebook:
                return "facebook";
            case PlayableAdNetwork.IronSource:
                return "ironsource";
            case PlayableAdNetwork.Moloco:
                return "moloco";
            case PlayableAdNetwork.TikTok:
                return "tiktok";
            case PlayableAdNetwork.Unity:
                return "unity";
            case PlayableAdNetwork.Vungle:
                return "vungle";
            default:
                return network.ToString().ToLowerInvariant();
        }
    }

    private static bool UsesSingleFileZipArtifact(PlayableAdNetwork network)
    {
        return network == PlayableAdNetwork.GoogleAds || network == PlayableAdNetwork.Mintegral;
    }

    private static string BuildTikTokConfigJson(PlayableExportOptions options)
    {
        return "{\"playable_orientation\":" + GetTikTokOrientationCode(options != null ? options.orientation : string.Empty) + ",\"playable_languages\":[\"en\",\"en\"]}";
    }

    private static int GetTikTokOrientationCode(string orientation)
    {
        if (string.IsNullOrWhiteSpace(orientation))
        {
            return 0;
        }

        var normalized = orientation.ToLowerInvariant();
        var hasPortrait = normalized.Contains("portrait");
        var hasLandscape = normalized.Contains("landscape");
        if (hasPortrait && !hasLandscape)
        {
            return 1;
        }

        if (hasLandscape && !hasPortrait)
        {
            return 2;
        }

        return 0;
    }

    private static void ResetDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }

        Directory.CreateDirectory(directoryPath);
    }

}








