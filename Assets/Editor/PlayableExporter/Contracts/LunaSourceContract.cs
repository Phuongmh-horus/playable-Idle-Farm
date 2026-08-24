using System.Text.RegularExpressions;

internal static class LunaSourceContract
{
    public const string PiApplyMarker = "<script>window.pi.apply(window,window.LUNA_PI_SETTINGS||[])</script>";
    public const string ClosingHeadMarker = "</head>";
    public const string ClosingBodyMarker = "</body>";

    public static readonly Regex EnvironmentScriptRegex = new Regex("<script>window\\.\\$environment=.*?</script>", RegexOptions.Singleline);
    public static readonly Regex HiddenPreloaderStyleRegex = new Regex("<style>\\s*\\.preloader\\s*\\{\\s*display\\s*:\\s*none\\s*;?\\s*\\}\\s*</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    public static readonly Regex BootstrapSeedScriptRegex = new Regex("<script>\\s*window\\.LUNA_PLAYGROUND_CREATIVE_ID\\s*,\\s*window\\.LUNA_PLAYGROUND_OVERRIDES\\s*,\\s*window\\.LUNA_PLAYGROUND_BUNDLES\\s*</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    public static readonly Regex PreloaderIconRegex = new Regex("<img\\s+class=\\\"preloader__icon\\s+wrapper\\\"\\s+id=\\\"asset/preloader/icon\\\"\\s+src=\\\"[^\\\"]*\\\">", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    public static readonly Regex PreloaderNamePlaceholderRegex = new Regex("<span\\s+class=\\\"preloader__name\\s+wrapper\\\"\\s+id=\\\"parameter/preloader/name\\\"\\s*>\\s*</span>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    public static readonly Regex ExternalEngineScriptRegex = new Regex("<script\\b[^>]*\\bsrc=(?:\\\"engine/scripts\\.js\\\"|'engine/scripts\\.js')[^>]*></script>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    public static readonly Regex SimpleBuildBridgeRegex = new Regex("<script>window\\.addEventListener\\(\\\"luna:ready\\\".*?window\\.dispatchEvent\\(new Event\\(\\\"luna:build\\\"\\)\\).*?</script>", RegexOptions.Singleline);
    public static readonly Regex DebugInfoScriptRegex = new Regex("<script>window\\.addEventListener\\(\\\"luna:started\\\",\\(\\)=>\\{Luna\\.LogDebugInfo=.*?</script>", RegexOptions.Singleline);
    public static readonly Regex StartupOnlyScriptRegex = new Regex("<script\\b[^>]*data-startup-only[^>]*>.*?</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    public static readonly Regex PlaygroundBridgeScriptRegex = new Regex("<script>(?:(?!</script>).)*(window\\.parent\\.postMessage|playground:started|setPlaygroundOverrides|editor:pi)(?:(?!</script>).)*</script>", RegexOptions.Singleline);
    public static readonly Regex DebuggerStatementRegex = new Regex("(?<![\\w$])debugger\\s*;?", RegexOptions.IgnoreCase);
}
