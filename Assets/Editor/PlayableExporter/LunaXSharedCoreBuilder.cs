using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal static class LunaXSharedCoreBuilder
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly int[] Base122IllegalValues = { 0, 10, 13, 34, 38, 92 };
    private const int Base122ShortenedMarker = 7;
    private const string BrotliDecoderAssetPath = "Assets/Editor/PlayableExporter/Templates/brotli_decoder.txt";
    private const string WorkerBootstrapAssetPath = "Assets/Editor/PlayableExporter/Templates/lunax_worker_bootstrap.txt";
    private const string PreloaderShellAssetPath = "Assets/Editor/PlayableExporter/Templates/lunax_preloader_shell.txt";
    private const string LegacyTemplatesDirectory = "Assets/_Game/Scripts/Editor/PlayableExporter/Templates";
    private const string ApplicationNamePlaceholder = "__APPLICATION_NAME__";
    private const string HiddenAssetContainerId = "playable-inline-assets";
    private const string HiddenAssetContainerMarkupStart = "<div id=\"playable-inline-assets\" style=\"display:none !important\">";
    private const string HiddenAssetContainerMarkupEnd = "</div>";
    public static LunaSingleHtmlPackResult Build(LunaSingleHtmlPackOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.sourceDirectory)) throw new InvalidOperationException("Source directory is required.");

        var sourceDirectory = Path.GetFullPath(options.sourceDirectory);
        if (!Directory.Exists(sourceDirectory)) throw new DirectoryNotFoundException($"Source directory was not found: {sourceDirectory}");

        var plan = LunaXCoreBuildPlanner.CreatePlan(sourceDirectory);
        if (string.IsNullOrWhiteSpace(plan.indexHtmlPath)) throw new InvalidOperationException("The Luna build-project does not contain index.html.");
        if (string.IsNullOrWhiteSpace(plan.engineScriptPath)) throw new InvalidOperationException("The Luna build-project does not contain engine/scripts.js.");

        var result = new LunaSingleHtmlPackResult
        {
            sourceFileCount = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories).Length
        };

        var indexPath = Path.Combine(sourceDirectory, plan.indexHtmlPath.Replace('/', Path.DirectorySeparatorChar));
        var engineScriptPath = Path.Combine(sourceDirectory, plan.engineScriptPath.Replace('/', Path.DirectorySeparatorChar));
        var html = File.ReadAllText(indexPath, Utf8NoBom);
        var engineScripts = File.ReadAllText(engineScriptPath, Utf8NoBom);

        result.sourceWarnings.AddRange(plan.warnings);
        PatchIndexHtml(ref html, options, result);
        PatchEngineScripts(ref engineScripts, result.patchesApplied);

        var sharedCoreInjection = BuildSharedCoreInjection(plan, sourceDirectory, engineScripts, options, result);
        html = ReplaceEngineScriptTag(html, sharedCoreInjection, result.patchesApplied);

        if (options.network == PlayableAdNetwork.Mintegral)
        {
            html = InjectAfter(
                html,
                LunaSourceContract.PiApplyMarker,
                "<script>window.pi.injectMintegralAdData=function(){if(\"mintegral\"===this.env.adNetwork&&window.MW_INIT)try{const rewardState=window.MW_INIT.isRewardAds();!0===rewardState?this.env.isRewarded=1:!1===rewardState&&(this.env.isRewarded=0)}catch(ex){}};window.pi.originalLogEvent=window.pi.logEvent;window.pi.logEvent=function(eventName,isTimed,options){return this.injectMintegralAdData(),this.originalLogEvent(eventName,isTimed,options)};</script>",
                "Injected Mintegral analytics compatibility bridge.",
                result.patchesApplied);
        }

        var networkBridgeScript = BuildNetworkBridgeScript(options.network);
        if (!string.IsNullOrEmpty(networkBridgeScript))
        {
            html = InjectBeforeClosingBody(html, networkBridgeScript, $"Injected {options.network} network tail.", result.patchesApplied);
        }

        html = Regex.Replace(html, "</body>\\s*</html>\\s*</head>\\s*</html>\\s*$", "</body></html>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        result.html = html;
        return result;
    }

    private static void PatchIndexHtml(ref string html, LunaSingleHtmlPackOptions options, LunaSingleHtmlPackResult result)
    {
        var patchesApplied = result.patchesApplied;
        var patchOperations = new List<HtmlPatchOperation>
        {
            new HtmlPatchOperation
            {
                name = "Remove Luna debug logger",
                kind = HtmlPatchKind.ReplaceRegex,
                regex = LunaSourceContract.DebugInfoScriptRegex,
                replacement = string.Empty,
                patchDescription = "Removed Luna playground debug logger."
            },
            new HtmlPatchOperation
            {
                name = "Remove playground bridge scripts",
                kind = HtmlPatchKind.ReplaceRegex,
                regex = LunaSourceContract.PlaygroundBridgeScriptRegex,
                replacement = string.Empty,
                patchDescription = "Removed playground/editor bridge scripts."
            },
            new HtmlPatchOperation
            {
                name = "Remove direct ready/build bridge",
                kind = HtmlPatchKind.ReplaceRegex,
                regex = LunaSourceContract.SimpleBuildBridgeRegex,
                replacement = string.Empty,
                patchDescription = "Removed direct luna:ready to luna:build bridge."
            },
            new HtmlPatchOperation
            {
                name = "Keep preloader visible",
                kind = HtmlPatchKind.ReplaceRegex,
                regex = LunaSourceContract.HiddenPreloaderStyleRegex,
                replacement = string.Empty,
                patchDescription = "Preserved the visible preloader shell instead of hiding it up-front."
            },
            new HtmlPatchOperation
            {
                name = "Replace bootstrap seed globals",
                required = true,
                kind = HtmlPatchKind.ReplaceRegex,
                regex = LunaSourceContract.BootstrapSeedScriptRegex,
                replacement = BuildBootstrapSeedScript(),
                patchDescription = "Replaced Luna playground bootstrap globals with LunaX-style seed globals."
            },
            new HtmlPatchOperation
            {
                name = "Apply preloader name",
                required = true,
                kind = HtmlPatchKind.ReplaceRegex,
                regex = LunaSourceContract.PreloaderNamePlaceholderRegex,
                replacement = BuildPreloaderNameMarkup(options),
                patchDescription = "Applied the custom game name to the built-in Luna preloader name slot."
            },
            new HtmlPatchOperation
            {
                name = "Inject preloader name style",
                required = true,
                kind = HtmlPatchKind.InjectBefore,
                marker = LunaSourceContract.ClosingHeadMarker,
                replacement = BuildPreloaderNameStyleOverride() + LunaSourceContract.ClosingHeadMarker,
                patchDescription = "Adjusted the built-in Luna preloader layout so the game name renders below the slider."
            }
        };

        if (options == null || options.stripLunaDebugScripts)
        {
            patchOperations.Insert(1, new HtmlPatchOperation
            {
                name = "Remove startup-only scripts",
                kind = HtmlPatchKind.ReplaceRegex,
                regex = LunaSourceContract.StartupOnlyScriptRegex,
                replacement = string.Empty,
                patchDescription = "Removed develop-only startup helper scripts."
            });
            patchOperations.Insert(3, new HtmlPatchOperation
            {
                name = "Remove debugger statements from HTML shell",
                kind = HtmlPatchKind.ReplaceRegex,
                regex = LunaSourceContract.DebuggerStatementRegex,
                replacement = string.Empty,
                patchDescription = "Removed stray debugger statements from the final HTML shell."
            });
        }

        var preloaderIconMarkup = BuildPreloaderIconMarkup(options);
        if (!string.IsNullOrEmpty(preloaderIconMarkup))
        {
            patchOperations.Add(new HtmlPatchOperation
            {
                name = "Apply preloader icon",
                required = true,
                kind = HtmlPatchKind.ReplaceRegex,
                regex = LunaSourceContract.PreloaderIconRegex,
                replacement = preloaderIconMarkup,
                patchDescription = "Applied the custom game logo to the built-in Luna preloader icon slot."
            });
        }

        HtmlPatchExecutionReport patchReport;
        html = HtmlPatchExecutor.Apply(html, patchOperations, patchesApplied, out patchReport);
        RecordPatchReport(patchReport, result);
        if (patchReport.HasMissingRequiredOperations())
        {
            throw new InvalidOperationException(patchReport.BuildFailureMessage());
        }

        var environmentScript = BuildEnvironmentScript(options);
        if (LunaSourceContract.EnvironmentScriptRegex.IsMatch(html))
        {
            HtmlPatchExecutionReport environmentPatchReport;
            html = HtmlPatchExecutor.Apply(
                html,
                new[]
                {
                    new HtmlPatchOperation
                    {
                        name = "Replace environment config",
                        required = true,
                        kind = HtmlPatchKind.ReplaceRegex,
                        regex = LunaSourceContract.EnvironmentScriptRegex,
                        replacement = environmentScript,
                        patchDescription = "Replaced Luna environment config with LunaX-style inline config."
                    }
                },
                patchesApplied,
                out environmentPatchReport);
            RecordPatchReport(environmentPatchReport, result);
            if (environmentPatchReport.HasMissingRequiredOperations())
            {
                throw new InvalidOperationException(environmentPatchReport.BuildFailureMessage());
            }
        }
        else
        {
            HtmlPatchExecutionReport environmentPatchReport;
            html = HtmlPatchExecutor.Apply(
                html,
                new[]
                {
                    new HtmlPatchOperation
                    {
                        name = "Inject environment config",
                        required = true,
                        kind = HtmlPatchKind.InjectBefore,
                        marker = LunaSourceContract.ClosingHeadMarker,
                        replacement = environmentScript + LunaSourceContract.ClosingHeadMarker,
                        patchDescription = "Injected LunaX-style environment config."
                    }
                },
                patchesApplied,
                out environmentPatchReport);
            RecordPatchReport(environmentPatchReport, result);
            if (environmentPatchReport.HasMissingRequiredOperations())
            {
                throw new InvalidOperationException(environmentPatchReport.BuildFailureMessage());
            }
        }

        var headMarkup = BuildHeadMarkup(options);
        if (!string.IsNullOrEmpty(headMarkup))
        {
            HtmlPatchExecutionReport headPatchReport;
            html = HtmlPatchExecutor.Apply(
                html,
                new[]
                {
                    new HtmlPatchOperation
                    {
                        name = $"Inject {options.network} head markup",
                        required = true,
                        kind = HtmlPatchKind.InjectBefore,
                        marker = LunaSourceContract.ClosingHeadMarker,
                        replacement = headMarkup + LunaSourceContract.ClosingHeadMarker,
                        patchDescription = $"Injected {options.network} head markup."
                    }
                },
                patchesApplied,
                out headPatchReport);
            RecordPatchReport(headPatchReport, result);
            if (headPatchReport.HasMissingRequiredOperations())
            {
                throw new InvalidOperationException(headPatchReport.BuildFailureMessage());
            }
        }
    }

    private static void PatchEngineScripts(ref string engineScripts, List<string> patchesApplied)
    {
        ReplaceLiteral(ref engineScripts, "https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0", "about:blank#tmp", "Sanitized TextMeshPro help URL in engine/scripts.js.", patchesApplied);
        ReplaceLiteral(ref engineScripts, "https://via.placeholder.com/128", "data:image/gif;base64,R0lGODlhAQABAAAAACw=", "Sanitized placeholder image URL in engine/scripts.js.", patchesApplied);
        var sanitizedEngineScripts = ReplaceRegex(
            engineScripts,
            LunaSourceContract.DebuggerStatementRegex,
            string.Empty,
            "Removed stray debugger statements from engine/scripts.js.",
            patchesApplied);
        engineScripts = sanitizedEngineScripts;
    }

    private static string BuildBootstrapSeedScript()
    {
        return "<script>window.LUCID=\"00000000-0000-0000-0000-000000000000\",window.playgroundOverrides={},window.playgroundAssetOverrides={},window.LUNA_PLAYGROUND_BUNDLES</script>";
    }
    private static string BuildPreloaderNameMarkup(LunaSingleHtmlPackOptions options)
    {
        var applicationName = ResolveApplicationName(options);
        return "<span class=\"preloader__name wrapper\" id=\"parameter/preloader/name\">" + EscapeHtmlContent(applicationName) + "</span>";
    }

    private static string BuildPreloaderIconMarkup(LunaSingleHtmlPackOptions options)
    {
        if (options == null || string.IsNullOrWhiteSpace(options.logoImagePath))
        {
            return string.Empty;
        }

        var fullPath = Path.GetFullPath(options.logoImagePath);
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"Custom game logo was not found: {fullPath}");
        }

        var mimeType = GetImageMimeType(fullPath);
        var dataUri = "data:" + mimeType + ";base64," + Convert.ToBase64String(File.ReadAllBytes(fullPath));
        return "<img class=\"preloader__icon wrapper\" id=\"asset/preloader/icon\" src=\"" + EscapeHtmlAttribute(dataUri) + "\">";
    }

    private static string BuildPreloaderNameStyleOverride()
    {
        return "<style>.preloader__icon.wrapper{order:0}.preloader__container.wrapper{order:1}.preloader__name.wrapper{order:2;margin:1.2vmax 0 0;padding:0 1.2vmax;max-width:80vw;font-size:1.55vmax;line-height:1.25;letter-spacing:.04em;text-align:center;display:block}</style>";
    }
    private static string BuildSharedCoreInjection(
        LunaXCoreBuildPlan plan,
        string sourceDirectory,
        string engineScripts,
        LunaSingleHtmlPackOptions options,
        LunaSingleHtmlPackResult result)
    {
        var builder = new StringBuilder(1 << 16);
        builder.Append(BuildWorkerBootstrapScript(result.patchesApplied));
        builder.Append(BuildHiddenAssetMarkup(plan, sourceDirectory, options, result));
        if (HasCacheTransport(plan))
        {
            builder.Append(BuildCacheTransportScript(plan.cacheScriptsPath, sourceDirectory, "scripts", result));
            builder.Append(BuildCacheTransportScript(plan.cacheJsonsPath, sourceDirectory, "jsons", result));
            builder.Append(BuildCacheTransportScript(plan.cacheBlobsPath, sourceDirectory, "blobs", result));
            result.inlinedJsonCount = plan.runtimeJsonPaths.Count;
            result.inlinedBlobCount = plan.runtimeBlobPaths.Count;
            result.inlinedSoundCount = plan.runtimeSoundPaths.Count;
        }
        else
        {
            builder.Append(BuildSoundPayloadScript(plan, sourceDirectory, result));
            builder.Append(BuildJsonPayloadScript(plan, sourceDirectory, result));
            builder.Append(BuildBlobPayloadScript(plan, sourceDirectory, result));
            builder.Append(BuildEnginePayloadScript(engineScripts, result.patchesApplied));
        }

        return builder.ToString();
    }

    private static bool HasCacheTransport(LunaXCoreBuildPlan plan)
    {
        return plan != null &&
               !string.IsNullOrWhiteSpace(plan.cacheScriptsPath) &&
               !string.IsNullOrWhiteSpace(plan.cacheJsonsPath) &&
               !string.IsNullOrWhiteSpace(plan.cacheBlobsPath);
    }

    private static string BuildCacheTransportScript(
        string relativePath,
        string sourceDirectory,
        string transportRole,
        LunaSingleHtmlPackResult result)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var filePath = Path.Combine(sourceDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var script = File.ReadAllText(filePath, Utf8NoBom).Trim();
        if (string.IsNullOrWhiteSpace(script))
        {
            return string.Empty;
        }

        result.patchesApplied.Add($"Inlined Luna cache transport '{relativePath}' for {transportRole} hydration instead of reconstructing payloads.");
        return "<script>" + script + "</script>";
    }

    private static string BuildWorkerBootstrapScript(List<string> patchesApplied)
    {
        var decoder = LoadTextAsset(BrotliDecoderAssetPath).Replace("`", string.Empty).Trim();
        var workerBootstrap = LoadTextAsset(WorkerBootstrapAssetPath).Trim();
        patchesApplied.Add("Injected LunaX-style worker/decompression bootstrap.");
        return string.Concat(
            "<script data-startup-only>window.makeBrotliDecodeStr=`",
            decoder,
            "`;</script>",
            "<script data-startup-only>",
            workerBootstrap,
            "</script>");
    }

    private static string BuildEnginePayloadScript(string engineScripts, List<string> patchesApplied)
    {
        var payload = ConvertToBrotliBase64(Utf8NoBom.GetBytes(engineScripts));
        patchesApplied.Add("Packed engine/scripts.js into the compressed asset queue.");
        return string.Concat(
            "<script>window._compressedAssets=window._compressedAssets||[];window._compressedAssets.push(decompressString(",
            ToSingleQuotedJavaScriptString(payload),
            ",false).then(function(code){window.eval(code)}));</script>");
    }

    private static string BuildJsonPayloadScript(LunaXCoreBuildPlan plan, string sourceDirectory, LunaSingleHtmlPackResult result)
    {
        if (plan.runtimeJsonPaths.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("<script>window.jsons=window.jsons||{};window._compressedAssets=window._compressedAssets||[];");
        foreach (var relativePath in plan.runtimeJsonPaths)
        {
            var filePath = Path.Combine(sourceDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var json = File.ReadAllText(filePath, Utf8NoBom);
            builder.Append("window.jsons[");
            builder.Append(ToSingleQuotedJavaScriptString(relativePath));
            builder.Append("]=JSON.parse(");
            builder.Append(ToSingleQuotedJavaScriptString(json));
            builder.Append(");window._compressedAssets.push(Promise.resolve());");
            result.inlinedJsonCount++;
        }

        builder.Append("</script>");
        result.patchesApplied.Add($"Materialized {result.inlinedJsonCount} runtime json payloads directly into window.jsons to preserve valid JSON text.");
        return builder.ToString();
    }

    private static string BuildBlobPayloadScript(LunaXCoreBuildPlan plan, string sourceDirectory, LunaSingleHtmlPackResult result)
    {
        if (plan.runtimeBlobPaths.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("<script>window.blobs=window.blobs||{};window._compressedAssets=window._compressedAssets||[];");
        foreach (var relativePath in plan.runtimeBlobPaths)
        {
            var filePath = Path.Combine(sourceDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var payload = Convert.ToBase64String(File.ReadAllBytes(filePath));
            builder.Append("window.blobs[");
            builder.Append(ToSingleQuotedJavaScriptString(relativePath));
            builder.Append("]=_base64ToArrayBuffer(");
            builder.Append(ToSingleQuotedJavaScriptString(payload));
            builder.Append(").buffer;window._compressedAssets.push(Promise.resolve());");
            result.inlinedBlobCount++;
        }

        builder.Append("</script>");
        result.patchesApplied.Add($"Materialized {result.inlinedBlobCount} runtime blob payloads directly into window.blobs to preserve binary integrity.");
        return builder.ToString();
    }

    private static string BuildSoundPayloadScript(LunaXCoreBuildPlan plan, string sourceDirectory, LunaSingleHtmlPackResult result)
    {
        if (plan.runtimeSoundPaths.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("<script>window.sounds=window.sounds||{};window._compressedAssets=window._compressedAssets||[];");
        foreach (var relativePath in plan.runtimeSoundPaths)
        {
            var filePath = Path.Combine(sourceDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var payload = Convert.ToBase64String(File.ReadAllBytes(filePath));
            builder.Append("window.sounds[");
            builder.Append(ToSingleQuotedJavaScriptString(relativePath));
            builder.Append("]=_base64ToArrayBuffer(");
            builder.Append(ToSingleQuotedJavaScriptString(payload));
            builder.Append(").buffer;window._compressedAssets.push(Promise.resolve());");
            result.inlinedSoundCount++;
        }

        builder.Append("</script>");
        result.patchesApplied.Add($"Materialized {result.inlinedSoundCount} runtime sound payloads directly into window.sounds to preserve binary integrity.");
        return builder.ToString();
    }

    private static string BuildHiddenAssetMarkup(LunaXCoreBuildPlan plan, string sourceDirectory, LunaSingleHtmlPackOptions options, LunaSingleHtmlPackResult result)
    {
        if (plan.runtimeImagePaths.Count == 0 && plan.runtimeVideoPaths.Count == 0)
        {
            return HiddenAssetContainerMarkupStart + HiddenAssetContainerMarkupEnd;
        }

        var builder = new StringBuilder();
        builder.Append(HiddenAssetContainerMarkupStart);

        foreach (var relativePath in plan.runtimeImagePaths)
        {
            var filePath = Path.Combine(sourceDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            builder.Append("<img id=\"");
            builder.Append(EscapeHtmlAttribute(relativePath));
            builder.Append("\" alt=\"\" src=\"");
            if (options != null && options.externalizeRuntimeMedia)
            {
                builder.Append(EscapeHtmlAttribute(relativePath));
            }
            else
            {
                var mimeType = GetMimeType(relativePath);
                var dataUri = "data:" + mimeType + ";base64," + Convert.ToBase64String(File.ReadAllBytes(filePath));
                builder.Append(EscapeHtmlAttribute(dataUri));
            }
            builder.Append("\" style=\"display:none\" crossorigin=\"\">");
            result.inlinedImageCount++;
        }

        foreach (var relativePath in plan.runtimeVideoPaths)
        {
            var filePath = Path.Combine(sourceDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            builder.Append("<video id=\"");
            builder.Append(EscapeHtmlAttribute(relativePath));
            builder.Append("\" style=\"display:none\" muted playsinline src=\"");
            if (options != null && options.externalizeRuntimeMedia)
            {
                builder.Append(EscapeHtmlAttribute(relativePath));
            }
            else
            {
                var mimeType = GetMimeType(relativePath);
                var dataUri = "data:" + mimeType + ";base64," + Convert.ToBase64String(File.ReadAllBytes(filePath));
                builder.Append(EscapeHtmlAttribute(dataUri));
            }
            builder.Append("\" crossorigin=\"\"></video>");
            result.inlinedVideoCount++;
        }

        builder.Append(HiddenAssetContainerMarkupEnd);
        if (options != null && options.externalizeRuntimeMedia)
        {
            result.patchesApplied.Add($"Materialized {result.inlinedImageCount} images and {result.inlinedVideoCount} videos as DOM-backed external runtime assets.");
        }
        else
        {
            result.patchesApplied.Add($"Materialized {result.inlinedImageCount} images and {result.inlinedVideoCount} videos as DOM-backed inline assets.");
        }
        return builder.ToString();
    }

    private static string BuildEnvironmentScript(LunaSingleHtmlPackOptions options)
    {
        var applicationName = EscapeJavaScriptStringLiteral(ResolveApplicationName(options));
        var iosLink = EscapeJavaScriptStringLiteral(options.iosStoreUrl ?? string.Empty);
        var androidLink = EscapeJavaScriptStringLiteral(options.androidStoreUrl ?? string.Empty);
        var orientation = EscapeJavaScriptStringLiteral(string.IsNullOrWhiteSpace(options.orientation) ? "unspecified" : options.orientation);
        var targetPlatform = EscapeJavaScriptStringLiteral(GetTargetPlatform(options.network));
        var imageTransport = options != null && options.externalizeRuntimeMedia ? "external" : "inline";
        var videoTransport = options != null && options.externalizeRuntimeMedia ? "external" : "inline";

        return string.Concat(
            "<script>window.$environment = { baseUrl: \"./\", resourceConfig: { json: \"inline\", image: \"", imageTransport, "\", video: \"", videoTransport, "\", sound: \"compress\", blob: \"compress\" }, packageConfig: Object.assign({",
            "\"applicationName\":\"", applicationName, "\",",
            "\"iosLink\":\"", iosLink, "\",",
            "\"androidLink\":\"", androidLink, "\",",
            "\"orientation\":\"", orientation, "\",",
            "\"languages\":[\"en\",\"en\"],",
            "\"inlineScripts\":!0,\"inlineJsons\":!0,\"inlineBlobs\":!0,\"inlineImages\":!0,\"inlineSounds\":!0,\"inlineVideos\":!0 }, window.LUNA_PLAYGROUND_PACKAGE_CONFIG || {}), playerPrefs: !0, usingWebWorkers: \"off\", targetPlatform: \"",
            targetPlatform,
            "\", runtimeAnalysisModules: [\"physics3d\", \"physics2d\", \"particle_system\", \"reflection\", \"prefabs\", \"mecanim-wasm\"] }</script>");
    }

    private static string BuildHeadMarkup(LunaSingleHtmlPackOptions options)
    {
        var builder = NetworkMarkupBuilderFactory.Get(options.network);
        if (builder == null)
        {
            return string.Empty;
        }

        return builder.BuildHeadMarkup(EscapeHtmlAttribute(string.IsNullOrWhiteSpace(options.orientation) ? "unspecified" : options.orientation));
    }

    private static string BuildNetworkBridgeScript(PlayableAdNetwork network)
    {
        var builder = NetworkMarkupBuilderFactory.Get(network);
        return builder != null ? builder.BuildTailScript() : string.Empty;
    }

    private static void RecordPatchReport(HtmlPatchExecutionReport patchReport, LunaSingleHtmlPackResult result)
    {
        if (patchReport == null || result == null)
        {
            return;
        }

        for (var index = 0; index < patchReport.operations.Count; index++)
        {
            var operation = patchReport.operations[index];
            result.patchSummary.Add((operation.applied ? "APPLIED: " : "SKIPPED: ") + operation.message);
            if (operation.required && !operation.applied)
            {
                result.requiredPatchFailures.Add(operation.name);
            }
        }
    }

    private static string ReplaceEngineScriptTag(string html, string replacement, List<string> patchesApplied)
    {
        var output = LunaSourceContract.ExternalEngineScriptRegex.Replace(html, _ => replacement, 1);
        if (string.Equals(output, html, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Could not locate engine/scripts.js in index.html while building the LunaX replacement output.");
        }

        patchesApplied.Add("Replaced external engine/scripts.js with LunaX-style inline shared core.");
        return output;
    }

    private static string InjectBeforeClosingHead(string html, string markup, string patchDescription, List<string> patchesApplied)
    {
        return InjectBefore(html, "</head>", markup + "</head>", patchDescription, patchesApplied);
    }

    private static string InjectBeforeClosingBody(string html, string markup, string patchDescription, List<string> patchesApplied)
    {
        return InjectBefore(html, "</body>", markup + "</body>", patchDescription, patchesApplied);
    }

    private static string InjectBefore(string html, string marker, string replacement, string patchDescription, List<string> patchesApplied)
    {
        var markerIndex = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException($"Could not find marker '{marker}' while patching the LunaX replacement HTML.");
        }

        patchesApplied.Add(patchDescription);
        return html.Remove(markerIndex, marker.Length).Insert(markerIndex, replacement);
    }

    private static string InjectAfter(string html, string marker, string markup, string patchDescription, List<string> patchesApplied)
    {
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return html;
        }

        patchesApplied.Add(patchDescription);
        return html.Insert(markerIndex + marker.Length, markup);
    }

    private static string ReplaceRegex(string input, Regex regex, string replacement, string patchDescription, List<string> patchesApplied)
    {
        var output = regex.Replace(input, _ => replacement);
        if (!string.Equals(output, input, StringComparison.Ordinal))
        {
            patchesApplied.Add(patchDescription);
        }

        return output;
    }

    private static bool ReplaceLiteral(ref string input, string oldValue, string newValue, string patchDescription, List<string> patchesApplied)
    {
        if (input.IndexOf(oldValue, StringComparison.Ordinal) < 0)
        {
            return false;
        }

        input = input.Replace(oldValue, newValue);
        patchesApplied.Add(patchDescription);
        return true;
    }

    private static string ConvertToBrotliBase64(byte[] bytes)
    {
        using (var output = new MemoryStream())
        {
            using (var brotli = new BrotliStream(output, System.IO.Compression.CompressionLevel.Optimal, true))
            {
                brotli.Write(bytes, 0, bytes.Length);
            }

            return Convert.ToBase64String(output.ToArray());
        }
    }

    private static string EncodeBase122(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return string.Empty;
        }

        var chunks = ExtractSevenBitChunks(bytes);
        var builder = new StringBuilder(chunks.Count);
        for (var index = 0; index < chunks.Count; index++)
        {
            var current = chunks[index];
            var illegalIndex = Array.IndexOf(Base122IllegalValues, current);
            if (illegalIndex < 0)
            {
                builder.Append((char)current);
                continue;
            }

            var marker = illegalIndex;
            int nextValue;
            if (index + 1 < chunks.Count)
            {
                nextValue = chunks[++index];
            }
            else
            {
                marker = Base122ShortenedMarker;
                nextValue = current;
            }

            builder.Append((char)(0x80 | (marker << 8) | nextValue));
        }

        return builder.ToString();
    }

    private static List<int> ExtractSevenBitChunks(byte[] bytes)
    {
        var totalBits = bytes.Length * 8;
        var chunks = new List<int>((totalBits + 6) / 7);
        var bitIndex = 0;
        while (bitIndex < totalBits)
        {
            var bitsToRead = Math.Min(7, totalBits - bitIndex);
            var value = 0;
            for (var offset = 0; offset < 7; offset++)
            {
                value <<= 1;
                if (offset >= bitsToRead)
                {
                    continue;
                }

                var globalBitIndex = bitIndex + offset;
                var sourceByte = bytes[globalBitIndex / 8];
                var sourceBitIndex = 7 - (globalBitIndex % 8);
                value |= (sourceByte >> sourceBitIndex) & 1;
            }

            chunks.Add(value);
            bitIndex += bitsToRead;
        }

        return chunks;
    }

    private static string GetMimeType(string relativePath)
    {
        switch (Path.GetExtension(relativePath).ToLowerInvariant())
        {
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".webp": return "image/webp";
            case ".mp4": return "video/mp4";
            case ".webm": return "video/webm";
            case ".ogv": return "video/ogg";
            default: return "application/octet-stream";
        }
    }

    private static string GetImageMimeType(string filePath)
    {
        switch (Path.GetExtension(filePath).ToLowerInvariant())
        {
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".webp": return "image/webp";
            default: return "application/octet-stream";
        }
    }

    private static string GetTargetPlatform(PlayableAdNetwork network)
    {
        switch (network)
        {
            case PlayableAdNetwork.GoogleAds:
                return "google";
            case PlayableAdNetwork.Mintegral:
                return "mintegral";
            case PlayableAdNetwork.AppLovin:
                return "applovin";
            case PlayableAdNetwork.Facebook:
            case PlayableAdNetwork.Moloco:
                return "facebook";
            case PlayableAdNetwork.IronSource:
                return "ironsource";
            case PlayableAdNetwork.TikTok:
                return "tiktok";
            case PlayableAdNetwork.Unity:
                return "unity";
            case PlayableAdNetwork.Vungle:
                return "vungle";
            default:
                return "playable";
        }
    }

    private static string ResolveApplicationName(LunaSingleHtmlPackOptions options)
    {
        if (options != null && !string.IsNullOrWhiteSpace(options.applicationName))
        {
            return options.applicationName;
        }

        var environmentOverride = Environment.GetEnvironmentVariable("PLAYABLE_EXPORT_APP_NAME");
        if (!string.IsNullOrWhiteSpace(environmentOverride))
        {
            return environmentOverride;
        }

        try
        {
            var property = typeof(PlayerSettings).GetProperty("productName", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var value = property?.GetValue(null) as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        catch
        {
        }

        try
        {
            return new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string LoadTextAsset(string assetPath)
    {
        foreach (var candidatePath in EnumerateTemplateCandidatePaths(assetPath))
        {
            var fullPath = Path.GetFullPath(candidatePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(Directory.GetCurrentDirectory(), candidatePath.Replace('/', Path.DirectorySeparatorChar));
            }

            if (File.Exists(fullPath))
            {
                return File.ReadAllText(fullPath, Utf8NoBom);
            }
        }

        throw new InvalidOperationException($"Required LunaX replacement template is missing: {assetPath}");
    }

    private static IEnumerable<string> EnumerateTemplateCandidatePaths(string assetPath)
    {
        yield return assetPath;

        if (!assetPath.StartsWith("Assets/Editor/PlayableExporter/Templates/", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var fileName = Path.GetFileName(assetPath);
        if (string.IsNullOrEmpty(fileName))
        {
            yield break;
        }

        yield return $"{LegacyTemplatesDirectory}/{fileName}";
    }

    private static string ToSingleQuotedJavaScriptString(string value)
    {
        return "'" + EscapeJavaScriptStringLiteral(value) + "'";
    }

    private static string EscapeJavaScriptStringLiteral(string value)
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
                case '\'': builder.Append("\\'"); break;
                case '\r': builder.Append("\\r"); break;
                case '\n': builder.Append("\\n"); break;
                case '\t': builder.Append("\\t"); break;
                default: builder.Append(character); break;
            }
        }

        return builder.ToString();
    }

    private static string EscapeHtmlAttribute(string value)
    {
        return (value ?? string.Empty)
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static string EscapeHtmlContent(string value)
    {
        return (value ?? string.Empty)
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}






