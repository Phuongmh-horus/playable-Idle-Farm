using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class GeneratedArtifactCleanupReport
{
    public readonly List<string> removedDirectories = new List<string>();
    public readonly List<string> removedFiles = new List<string>();
    public readonly List<string> warnings = new List<string>();
}

public static class PlayableExportCleanup
{
    private static readonly string[] GeneratedDirectoryPaths =
    {
        "PlayableExports/BatchTest",
        "PlayableExports/Cli",
        "PlayableExports/GoogleAds",
        "PlayableExports/HeadlessTrial",
        "PlayableExports/Inspect",
        "PlayableExports/Recovered",
        "PlayableExports/ReflectionSmoke",
        "PlayableExports/SmokePaidNetworks",
        "TempRuntimeInspect"
    };

    private static readonly string[] GeneratedDirectoryPatterns =
    {
        "TempChromeProfile*"
    };

    private static readonly string[] GeneratedFilePaths =
    {
        "build_diag.log",
        "TempBase122Test.cs"
    };

    public static GeneratedArtifactCleanupReport CleanGeneratedArtifacts()
    {
        var report = new GeneratedArtifactCleanupReport();
        var projectRoot = PlayableAdsExporter.GetProjectRoot();

        foreach (var relativePath in GeneratedDirectoryPaths)
        {
            TryDeleteDirectory(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)), report);
        }

        foreach (var pattern in GeneratedDirectoryPatterns)
        {
            foreach (var directory in Directory.GetDirectories(projectRoot, pattern, SearchOption.TopDirectoryOnly))
            {
                TryDeleteDirectory(directory, report);
            }
        }

        foreach (var relativePath in GeneratedFilePaths)
        {
            TryDeleteFile(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)), report);
        }

        return report;
    }

    private static void TryDeleteDirectory(string directoryPath, GeneratedArtifactCleanupReport report)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            Directory.Delete(directoryPath, true);
            report.removedDirectories.Add(directoryPath);
        }
        catch (Exception exception)
        {
            report.warnings.Add($"Failed to remove directory '{directoryPath}': {exception.Message}");
        }
    }

    private static void TryDeleteFile(string filePath, GeneratedArtifactCleanupReport report)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            File.Delete(filePath);
            report.removedFiles.Add(filePath);
        }
        catch (Exception exception)
        {
            report.warnings.Add($"Failed to remove file '{filePath}': {exception.Message}");
        }
    }
}
