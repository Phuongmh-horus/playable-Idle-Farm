using System;
using System.Collections.Generic;
using System.Text;

internal static class PlayableExportJsonWriter
{
    public static string Serialize(PlayableNetworkExportReport report)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));

        var builder = new StringBuilder(2048);
        builder.AppendLine("{");
        AppendString(builder, 1, "network", report.network, true);
        AppendString(builder, 1, "sourceZipPath", report.sourceZipPath, true);
        AppendString(builder, 1, "outputDirectory", report.outputDirectory, true);
        AppendString(builder, 1, "workingDirectory", report.workingDirectory, true);
        AppendString(builder, 1, "singleHtmlPath", report.singleHtmlPath, true);
        AppendString(builder, 1, "artifactPath", report.artifactPath, true);
        AppendString(builder, 1, "artifactKind", report.artifactKind, true);
        AppendString(builder, 1, "orientation", report.orientation, true);
        AppendString(builder, 1, "iosStoreUrl", report.iosStoreUrl, true);
        AppendString(builder, 1, "androidStoreUrl", report.androidStoreUrl, true);
        AppendNumber(builder, 1, "sourceFileCount", report.sourceFileCount, true);
        AppendNumber(builder, 1, "inlinedJsonCount", report.inlinedJsonCount, true);
        AppendNumber(builder, 1, "inlinedBlobCount", report.inlinedBlobCount, true);
        AppendNumber(builder, 1, "inlinedSoundCount", report.inlinedSoundCount, true);
        AppendNumber(builder, 1, "inlinedImageCount", report.inlinedImageCount, true);
        AppendNumber(builder, 1, "inlinedVideoCount", report.inlinedVideoCount, true);
        AppendNumber(builder, 1, "singleHtmlSizeBytes", report.singleHtmlSizeBytes, true);
        AppendNumber(builder, 1, "artifactSizeBytes", report.artifactSizeBytes, true);
        AppendBoolean(builder, 1, "usesSingleHtml", report.usesSingleHtml, true);
        AppendBoolean(builder, 1, "hasStorageApiUsage", report.hasStorageApiUsage, true);
        AppendBoolean(builder, 1, "hasForbiddenExternalReferences", report.hasForbiddenExternalReferences, true);
        AppendBoolean(builder, 1, "artifactWithinRecommendedLimit", report.artifactWithinRecommendedLimit, true);
        AppendBoolean(builder, 1, "qcReady", report.qcReady, true);
        AppendString(builder, 1, "recommendedLimitLabel", report.recommendedLimitLabel, true);
        AppendStringArray(builder, 1, "sourceWarnings", report.sourceWarnings, true);
        AppendStringArray(builder, 1, "patchSummary", report.patchSummary, true);
        AppendStringArray(builder, 1, "requiredPatchFailures", report.requiredPatchFailures, true);
        AppendStringArray(builder, 1, "qcChecks", report.qcChecks, true);
        AppendStringArray(builder, 1, "qcFailures", report.qcFailures, true);
        AppendStringArray(builder, 1, "externalReferences", report.externalReferences, true);
        AppendStringArray(builder, 1, "warnings", report.warnings, true);
        AppendStringArray(builder, 1, "patchesApplied", report.patchesApplied, false);
        builder.Append('}');
        return builder.ToString();
    }

    public static string Serialize(PlayableBatchExportReport report)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));

        var builder = new StringBuilder(2048);
        builder.AppendLine("{");
        AppendString(builder, 1, "sourceZipPath", report.sourceZipPath, true);
        AppendString(builder, 1, "outputDirectory", report.outputDirectory, true);
        AppendString(builder, 1, "bundleZipPath", report.bundleZipPath, true);
        AppendNetworkReports(builder, 1, "networkReports", report.networkReports, true);
        AppendStringArray(builder, 1, "warnings", report.warnings, false);
        builder.Append('}');
        return builder.ToString();
    }

    private static void AppendNetworkReports(StringBuilder builder, int indentLevel, string name, List<PlayableNetworkExportReport> reports, bool trailingComma)
    {
        AppendIndent(builder, indentLevel);
        builder.Append('"').Append(name).AppendLine("\": [");
        for (var index = 0; index < reports.Count; index++)
        {
            AppendIndentedBlock(builder, indentLevel + 1, Serialize(reports[index]));
            if (index + 1 < reports.Count)
            {
                builder.Append(',');
            }

            builder.AppendLine();
        }

        AppendIndent(builder, indentLevel);
        builder.Append(']');
        if (trailingComma)
        {
            builder.Append(',');
        }

        builder.AppendLine();
    }

    private static void AppendStringArray(StringBuilder builder, int indentLevel, string name, List<string> values, bool trailingComma)
    {
        AppendIndent(builder, indentLevel);
        builder.Append('"').Append(name).AppendLine("\": [");
        for (var index = 0; index < values.Count; index++)
        {
            AppendIndent(builder, indentLevel + 1);
            builder.Append('"').Append(Escape(values[index])).Append('"');
            if (index + 1 < values.Count)
            {
                builder.Append(',');
            }

            builder.AppendLine();
        }

        AppendIndent(builder, indentLevel);
        builder.Append(']');
        if (trailingComma)
        {
            builder.Append(',');
        }

        builder.AppendLine();
    }

    private static void AppendString(StringBuilder builder, int indentLevel, string name, string value, bool trailingComma)
    {
        AppendIndent(builder, indentLevel);
        builder.Append('"').Append(name).Append("\": ");
        builder.Append('"').Append(Escape(value)).Append('"');
        if (trailingComma)
        {
            builder.Append(',');
        }

        builder.AppendLine();
    }

    private static void AppendBoolean(StringBuilder builder, int indentLevel, string name, bool value, bool trailingComma)
    {
        AppendIndent(builder, indentLevel);
        builder.Append('"').Append(name).Append("\": ").Append(value ? "true" : "false");
        if (trailingComma)
        {
            builder.Append(',');
        }

        builder.AppendLine();
    }

    private static void AppendNumber(StringBuilder builder, int indentLevel, string name, long value, bool trailingComma)
    {
        AppendIndent(builder, indentLevel);
        builder.Append('"').Append(name).Append("\": ").Append(value);
        if (trailingComma)
        {
            builder.Append(',');
        }

        builder.AppendLine();
    }

    private static void AppendIndent(StringBuilder builder, int indentLevel)
    {
        builder.Append(' ', indentLevel * 2);
    }

    private static void AppendIndentedBlock(StringBuilder builder, int indentLevel, string block)
    {
        var lines = block.Replace("\r\n", "\n").Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            AppendIndent(builder, indentLevel);
            builder.Append(lines[index]);
            if (index + 1 < lines.Length)
            {
                builder.AppendLine();
            }
        }
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length + 8);
        foreach (var character in value)
        {
            switch (character)
            {
                case '\\': builder.Append("\\\\"); break;
                case '"': builder.Append("\\\""); break;
                case '\r': builder.Append("\\r"); break;
                case '\n': builder.Append("\\n"); break;
                case '\t': builder.Append("\\t"); break;
                default: builder.Append(character); break;
            }
        }

        return builder.ToString();
    }
}
