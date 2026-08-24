using System.Collections.Generic;

internal sealed class LunaSingleHtmlPackOptions
{
    public string sourceDirectory;
    public PlayableAdNetwork network;
    public string applicationName;
    public string logoImagePath;
    public string orientation;
    public string iosStoreUrl;
    public string androidStoreUrl;
    public bool stripLunaDebugScripts;
    public bool externalizeRuntimeMedia;
}

internal sealed class LunaSingleHtmlPackResult
{
    public string html;
    public int sourceFileCount;
    public int inlinedJsonCount;
    public int inlinedBlobCount;
    public int inlinedSoundCount;
    public int inlinedImageCount;
    public int inlinedVideoCount;
    public readonly List<string> sourceWarnings = new List<string>();
    public readonly List<string> patchSummary = new List<string>();
    public readonly List<string> requiredPatchFailures = new List<string>();
    public readonly List<string> patchesApplied = new List<string>();
}
