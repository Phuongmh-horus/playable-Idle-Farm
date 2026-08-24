using System;
using System.Collections.Generic;

public enum PlayableAdNetwork
{
    GoogleAds,
    Mintegral,
    AppLovin,
    Facebook,
    IronSource,
    Moloco,
    TikTok,
    Unity,
    Vungle
}

[Serializable]
public sealed class PlayableExportOptions
{
    public string sourceZipPath;
    public string outputRootPath;
    public string logoImagePath = string.Empty;
    public string applicationName = string.Empty;
    public string orientation = "portrait,landscape";
    public bool stripLunaDebugScripts = true;
    public string iosStoreUrl = string.Empty;
    public string androidStoreUrl = string.Empty;
}

[Serializable]
public sealed class PlayableNetworkExportReport
{
    public string network;
    public string sourceZipPath;
    public string outputDirectory;
    public string workingDirectory;
    public string singleHtmlPath;
    public string artifactPath;
    public string artifactKind;
    public string orientation;
    public string iosStoreUrl;
    public string androidStoreUrl;
    public int sourceFileCount;
    public int inlinedJsonCount;
    public int inlinedBlobCount;
    public int inlinedSoundCount;
    public int inlinedImageCount;
    public int inlinedVideoCount;
    public long singleHtmlSizeBytes;
    public long artifactSizeBytes;
    public bool usesSingleHtml;
    public bool hasStorageApiUsage;
    public bool hasForbiddenExternalReferences;
    public bool artifactWithinRecommendedLimit;
    public bool qcReady;
    public string recommendedLimitLabel;
    public List<string> sourceWarnings = new List<string>();
    public List<string> patchSummary = new List<string>();
    public List<string> requiredPatchFailures = new List<string>();
    public List<string> qcChecks = new List<string>();
    public List<string> qcFailures = new List<string>();
    public List<string> externalReferences = new List<string>();
    public List<string> warnings = new List<string>();
    public List<string> patchesApplied = new List<string>();
}

[Serializable]
public sealed class PlayableBatchExportReport
{
    public string sourceZipPath;
    public string outputDirectory;
    public string bundleZipPath;
    public List<PlayableNetworkExportReport> networkReports = new List<PlayableNetworkExportReport>();
    public List<string> warnings = new List<string>();
}
