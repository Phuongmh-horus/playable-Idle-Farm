using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

internal static class PlayableExportVerifier
{
    private const long FiveMegabytes = 5L * 1024L * 1024L;
    private const string EmptyApplicationNameMarker = "\"applicationName\":\"\"";
    private const string EmptyPreloaderNameMarker = "id=\"parameter/preloader/name\"></span>";
    private const string StartupOnlyMarker = "data-startup-only";

    public static PlayableNetworkExportReport BuildNetworkReport(
        PlayableAdNetwork network,
        string sourceZipPath,
        string exportDirectory,
        string workingDirectory,
        string singleHtmlPath,
        string artifactPath,
        string artifactKind,
        PlayableExportOptions options,
        LunaSingleHtmlPackResult packResult)
    {
        var html = File.ReadAllText(singleHtmlPath, new UTF8Encoding(false));
        var externalReferences = Regex.Matches(html, "https?://[^\"' )<>]+", RegexOptions.IgnoreCase)
            .Cast<Match>()
            .Select(match => match.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var forbiddenExternalReferences = externalReferences
            .Where(reference => !IsAllowedExternalReference(network, reference))
            .ToList();

        var report = new PlayableNetworkExportReport
        {
            network = GetNetworkDirectoryName(network),
            sourceZipPath = sourceZipPath,
            outputDirectory = exportDirectory,
            workingDirectory = workingDirectory,
            singleHtmlPath = singleHtmlPath,
            artifactPath = artifactPath,
            artifactKind = artifactKind,
            orientation = string.IsNullOrWhiteSpace(options.orientation) ? "portrait,landscape" : options.orientation,
            iosStoreUrl = options.iosStoreUrl ?? string.Empty,
            androidStoreUrl = options.androidStoreUrl ?? string.Empty,
            sourceFileCount = packResult.sourceFileCount,
            inlinedJsonCount = packResult.inlinedJsonCount,
            inlinedBlobCount = packResult.inlinedBlobCount,
            inlinedSoundCount = packResult.inlinedSoundCount,
            inlinedImageCount = packResult.inlinedImageCount,
            inlinedVideoCount = packResult.inlinedVideoCount,
            singleHtmlSizeBytes = File.Exists(singleHtmlPath) ? new FileInfo(singleHtmlPath).Length : 0L,
            artifactSizeBytes = File.Exists(artifactPath) ? new FileInfo(artifactPath).Length : 0L,
            usesSingleHtml = network != PlayableAdNetwork.Vungle,
            hasStorageApiUsage = html.IndexOf("localStorage", StringComparison.OrdinalIgnoreCase) >= 0 || html.IndexOf("sessionStorage", StringComparison.OrdinalIgnoreCase) >= 0,
            hasForbiddenExternalReferences = forbiddenExternalReferences.Count > 0,
            qcReady = true,
            recommendedLimitLabel = "5 MB artifact size"
        };

        report.externalReferences.AddRange(forbiddenExternalReferences);
        report.sourceWarnings.AddRange(packResult.sourceWarnings);
        report.patchSummary.AddRange(packResult.patchSummary);
        report.requiredPatchFailures.AddRange(packResult.requiredPatchFailures);
        report.patchesApplied.AddRange(packResult.patchesApplied);

        report.artifactWithinRecommendedLimit = report.artifactSizeBytes <= FiveMegabytes;
        if (!report.artifactWithinRecommendedLimit)
        {
            report.warnings.Add($"Artifact exceeds 5 MB: {FormatBytes(report.artifactSizeBytes)}.");
        }

        if (report.hasForbiddenExternalReferences)
        {
            report.warnings.Add("Found external references outside the allowed list for this network.");
        }

        if (report.hasStorageApiUsage)
        {
            report.warnings.Add("Found localStorage/sessionStorage symbols in the final single-html artifact.");
        }

        if (html.IndexOf(StartupOnlyMarker, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            report.warnings.Add("Found residual data-startup-only helper scripts in the final single-html artifact.");
        }

        if (html.IndexOf(EmptyApplicationNameMarker, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            report.warnings.Add("packageConfig.applicationName is empty in the final single-html artifact.");
        }

        if (html.IndexOf(EmptyPreloaderNameMarker, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            report.warnings.Add("The built-in preloader name slot is still empty in the final single-html artifact.");
        }

        if (string.IsNullOrWhiteSpace(report.androidStoreUrl))
        {
            report.warnings.Add("Android store URL is empty. CTA fallback may not open the store on Android devices.");
        }

        if (string.IsNullOrWhiteSpace(report.iosStoreUrl))
        {
            report.warnings.Add("iOS store URL is empty. CTA fallback may not open the store on iOS devices.");
        }

        if (network == PlayableAdNetwork.GoogleAds && !string.Equals(report.artifactKind, "zip", StringComparison.OrdinalIgnoreCase))
        {
            report.warnings.Add("Google Ads output should be a ZIP containing the playable HTML.");
        }

        if (network == PlayableAdNetwork.TikTok && !string.Equals(report.artifactKind, "zip", StringComparison.OrdinalIgnoreCase))
        {
            report.warnings.Add("TikTok output should be a ZIP containing index.html and config.json.");
        }

        if (network == PlayableAdNetwork.Vungle && !string.Equals(report.artifactKind, "zip", StringComparison.OrdinalIgnoreCase))
        {
            report.warnings.Add("Vungle output should be a ZIP containing index.html plus external runtime media files.");
        }

        EvaluateQcReadiness(network, artifactPath, singleHtmlPath, html, report);
        return report;
    }

    public static string BuildHumanReadableReport(PlayableNetworkExportReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Network: {report.network}");
        builder.AppendLine($"Source ZIP: {report.sourceZipPath}");
        builder.AppendLine($"Artifact: {report.artifactPath}");
        builder.AppendLine($"Artifact Kind: {report.artifactKind}");
        builder.AppendLine($"Artifact Size: {FormatBytes(report.artifactSizeBytes)}");
        builder.AppendLine($"Single HTML Size: {FormatBytes(report.singleHtmlSizeBytes)}");
        builder.AppendLine($"Source Files: {report.sourceFileCount}");
        builder.AppendLine($"JSONs: {report.inlinedJsonCount}");
        builder.AppendLine($"Blobs: {report.inlinedBlobCount}");
        builder.AppendLine($"Sounds: {report.inlinedSoundCount}");
        builder.AppendLine($"Images: {report.inlinedImageCount}");
        builder.AppendLine($"Videos: {report.inlinedVideoCount}");
        builder.AppendLine($"Within Limit: {report.artifactWithinRecommendedLimit} ({report.recommendedLimitLabel})");
        builder.AppendLine($"Forbidden External Refs: {report.hasForbiddenExternalReferences}");
        builder.AppendLine($"Storage API Usage: {report.hasStorageApiUsage}");
        builder.AppendLine($"QC Ready: {report.qcReady}");
        builder.AppendLine();

        if (report.sourceWarnings.Count > 0)
        {
            builder.AppendLine("Source Warnings:");
            foreach (var warning in report.sourceWarnings)
            {
                builder.AppendLine($"- {warning}");
            }

            builder.AppendLine();
        }

        if (report.patchSummary.Count > 0)
        {
            builder.AppendLine("Patch Summary:");
            foreach (var patchSummary in report.patchSummary)
            {
                builder.AppendLine($"- {patchSummary}");
            }

            builder.AppendLine();
        }

        if (report.requiredPatchFailures.Count > 0)
        {
            builder.AppendLine("Required Patch Failures:");
            foreach (var failure in report.requiredPatchFailures)
            {
                builder.AppendLine($"- {failure}");
            }

            builder.AppendLine();
        }

        if (report.qcChecks.Count > 0)
        {
            builder.AppendLine("QC Checks:");
            foreach (var check in report.qcChecks)
            {
                builder.AppendLine($"- {check}");
            }

            builder.AppendLine();
        }

        if (report.warnings.Count > 0)
        {
            builder.AppendLine("Warnings:");
            foreach (var warning in report.warnings)
            {
                builder.AppendLine($"- {warning}");
            }

            builder.AppendLine();
        }

        if (report.externalReferences.Count > 0)
        {
            builder.AppendLine("Forbidden External References:");
            foreach (var reference in report.externalReferences)
            {
                builder.AppendLine($"- {reference}");
            }

            builder.AppendLine();
        }

        builder.AppendLine("Patches Applied:");
        foreach (var patch in report.patchesApplied.Distinct(StringComparer.Ordinal))
        {
            builder.AppendLine($"- {patch}");
        }

        return builder.ToString();
    }

    public static string BuildHumanReadableBatchReport(PlayableBatchExportReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Source ZIP: {report.sourceZipPath}");
        builder.AppendLine($"Output Directory: {report.outputDirectory}");
        builder.AppendLine($"Bundle ZIP: {report.bundleZipPath}");
        builder.AppendLine();

        foreach (var networkReport in report.networkReports)
        {
            builder.AppendLine($"[{networkReport.network}] {networkReport.artifactPath}");
            builder.AppendLine($"  Size: {FormatBytes(networkReport.artifactSizeBytes)}");
            builder.AppendLine($"  Within Limit: {networkReport.artifactWithinRecommendedLimit}");
            builder.AppendLine($"  QC Ready: {networkReport.qcReady}");
            builder.AppendLine($"  Forbidden External Refs: {networkReport.hasForbiddenExternalReferences}");
            builder.AppendLine($"  Storage API Usage: {networkReport.hasStorageApiUsage}");
            builder.AppendLine();
        }

        if (report.warnings.Count > 0)
        {
            builder.AppendLine("Warnings:");
            foreach (var warning in report.warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        return builder.ToString();
    }

    private static void EvaluateQcReadiness(
        PlayableAdNetwork network,
        string artifactPath,
        string singleHtmlPath,
        string html,
        PlayableNetworkExportReport report)
    {
        AddQcCheck(report, report.artifactWithinRecommendedLimit, "Artifact size is within the recommended limit.", "Artifact size exceeds the recommended limit.");
        AddQcCheck(report, !report.hasForbiddenExternalReferences, "No forbidden external references were found.", "Forbidden external references are still present.");
        AddQcCheck(report, !report.hasStorageApiUsage, "No storage API symbols were found in the final HTML.", "Storage API symbols were found in the final HTML.");
        AddQcCheck(report, html.IndexOf(StartupOnlyMarker, StringComparison.OrdinalIgnoreCase) < 0, "No startup-only helper scripts remain in the final HTML.", "Residual startup-only helper scripts remain in the final HTML.");
        AddQcCheck(report, html.IndexOf(EmptyApplicationNameMarker, StringComparison.OrdinalIgnoreCase) < 0, "Application name is populated in packageConfig.", "Application name is still empty in packageConfig.");
        AddQcCheck(report, html.IndexOf(EmptyPreloaderNameMarker, StringComparison.OrdinalIgnoreCase) < 0, "Preloader name slot is populated.", "Preloader name slot is still empty.");

        if (network == PlayableAdNetwork.Mintegral)
        {
            AddQcCheck(report, string.Equals(report.artifactKind, "zip", StringComparison.OrdinalIgnoreCase), "Mintegral artifact is packaged as ZIP.", "Mintegral artifact is not packaged as ZIP.");

            if (!File.Exists(artifactPath))
            {
                AddQcCheck(report, false, string.Empty, "Mintegral ZIP artifact was not found on disk.");
                return;
            }

            using (var archive = ZipFile.OpenRead(artifactPath))
            {
                var fileEntries = archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)).ToList();
                AddQcCheck(report, fileEntries.Count == 1, "Mintegral ZIP contains exactly one file.", $"Mintegral ZIP should contain exactly one file, found {fileEntries.Count}.");

                var htmlEntry = fileEntries.FirstOrDefault();
                if (htmlEntry == null)
                {
                    AddQcCheck(report, false, string.Empty, "Mintegral ZIP does not contain an HTML file.");
                    return;
                }

                AddQcCheck(report, htmlEntry.FullName.EndsWith(".html", StringComparison.OrdinalIgnoreCase), "Mintegral ZIP entry is an HTML file.", $"Mintegral ZIP entry should be an HTML file, found '{htmlEntry.FullName}'.");
                AddQcCheck(report, string.Equals(htmlEntry.Name, Path.GetFileName(singleHtmlPath), StringComparison.OrdinalIgnoreCase), "Mintegral ZIP entry name matches the exported HTML file.", $"Mintegral ZIP entry name '{htmlEntry.Name}' does not match the exported HTML file name.");
                AddQcCheck(report, htmlEntry.Length == report.singleHtmlSizeBytes, "Mintegral ZIP entry size matches the exported HTML.", "Mintegral ZIP entry size does not match the exported HTML size.");
            }

            return;
        }

        if (network == PlayableAdNetwork.Vungle)
        {
            AddQcCheck(report, string.Equals(report.artifactKind, "zip", StringComparison.OrdinalIgnoreCase), "Vungle artifact is packaged as ZIP.", "Vungle artifact is not packaged as ZIP.");

            if (!File.Exists(artifactPath))
            {
                AddQcCheck(report, false, string.Empty, "Vungle ZIP artifact was not found on disk.");
                return;
            }

            using (var archive = ZipFile.OpenRead(artifactPath))
            {
                var fileEntries = archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)).ToList();
                var htmlEntry = fileEntries.FirstOrDefault(entry => string.Equals(entry.Name, "index.html", StringComparison.OrdinalIgnoreCase));
                AddQcCheck(report, htmlEntry != null, "Vungle ZIP contains index.html.", "Vungle ZIP does not contain index.html.");
                AddQcCheck(report, fileEntries.Count > 1, "Vungle ZIP contains external runtime media files.", "Vungle ZIP does not contain any external runtime media files.");

                if (htmlEntry != null)
                {
                    AddQcCheck(report, htmlEntry.Length == report.singleHtmlSizeBytes, "Vungle index.html size matches the exported HTML.", "Vungle index.html size does not match the exported HTML size.");
                }
            }

            return;
        }

        if (network != PlayableAdNetwork.TikTok)
        {
            return;
        }

        AddQcCheck(report, string.Equals(report.artifactKind, "zip", StringComparison.OrdinalIgnoreCase), "TikTok artifact is packaged as ZIP.", "TikTok artifact is not packaged as ZIP.");

        if (!File.Exists(artifactPath))
        {
            AddQcCheck(report, false, string.Empty, "TikTok ZIP artifact was not found on disk.");
            return;
        }

        using (var archive = ZipFile.OpenRead(artifactPath))
        {
            var fileEntries = archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)).ToList();
            AddQcCheck(report, fileEntries.Count == 2, "TikTok ZIP contains index.html and config.json.", $"TikTok ZIP should contain exactly two files, found {fileEntries.Count}.");

            var htmlEntry = fileEntries.FirstOrDefault(entry => string.Equals(entry.Name, "index.html", StringComparison.OrdinalIgnoreCase));
            var configEntry = fileEntries.FirstOrDefault(entry => string.Equals(entry.Name, "config.json", StringComparison.OrdinalIgnoreCase));
            AddQcCheck(report, htmlEntry != null, "TikTok ZIP contains index.html.", "TikTok ZIP does not contain index.html.");
            AddQcCheck(report, configEntry != null, "TikTok ZIP contains config.json.", "TikTok ZIP does not contain config.json.");

            if (htmlEntry != null)
            {
                AddQcCheck(report, htmlEntry.Length == report.singleHtmlSizeBytes, "TikTok index.html size matches the exported HTML.", "TikTok index.html size does not match the exported HTML size.");
            }
        }
    }

    private static void AddQcCheck(PlayableNetworkExportReport report, bool passed, string passMessage, string failMessage)
    {
        var message = passed ? passMessage : failMessage;
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        report.qcChecks.Add((passed ? "PASS: " : "FAIL: ") + message);
        if (passed)
        {
            return;
        }

        report.qcReady = false;
        report.qcFailures.Add(message);
    }

    private static bool IsAllowedExternalReference(PlayableAdNetwork network, string reference)
    {
        if (reference.StartsWith("http://www.w3.org/1999/xlink", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("http://www.w3.org/2000/svg", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("https://apps.apple.com/", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("https://play.google.com/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (network == PlayableAdNetwork.GoogleAds && reference.StartsWith("https://tpc.googlesyndication.com/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (network == PlayableAdNetwork.TikTok && reference.StartsWith("https://sf16-muse-va.ibytedtos.com/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
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

    private static string FormatBytes(long bytes)
    {
        return $"{bytes / 1024f / 1024f:0.###} MB ({bytes} bytes)";
    }
}
