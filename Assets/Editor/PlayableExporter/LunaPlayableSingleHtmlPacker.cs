using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal static class LunaPlayableSingleHtmlPacker
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly int[] Base122IllegalValues = { 0, 10, 13, 34, 38, 92 };
    private const int Base122ShortenedMarker = 7;
    private const string BrotliDecoderAssetPath = "Assets/Editor/PlayableExporter/Templates/brotli_decoder.txt";
    private const string HiddenAssetContainerMarkup = "<div id=\"playable-inline-assets\" style=\"display:none !important\"></div>";

    public static LunaSingleHtmlPackResult Build(LunaSingleHtmlPackOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.sourceDirectory)) throw new InvalidOperationException("Source directory is required.");

        var sourceDirectory = Path.GetFullPath(options.sourceDirectory);
        if (!Directory.Exists(sourceDirectory)) throw new DirectoryNotFoundException($"Source directory was not found: {sourceDirectory}");

        var allFiles = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories)
            .ToDictionary(path => NormalizeRelativePath(sourceDirectory, path), path => path, StringComparer.OrdinalIgnoreCase);

        if (!allFiles.TryGetValue("index.html", out var indexPath))
        {
            throw new InvalidOperationException("The Luna package does not contain index.html at the root.");
        }

        if (!allFiles.TryGetValue("engine/scripts.js", out var engineScriptsPath))
        {
            throw new InvalidOperationException("The Luna package does not contain engine/scripts.js.");
        }

        var result = new LunaSingleHtmlPackResult
        {
            sourceFileCount = allFiles.Count
        };

        var html = File.ReadAllText(indexPath, Utf8NoBom);
        var engineScripts = File.ReadAllText(engineScriptsPath, Utf8NoBom);

        PatchIndexHtml(ref html, options, result.patchesApplied);
        PatchEngineScripts(ref engineScripts, result.patchesApplied);

        var inlineAssetScript = BuildInlineAssetScript(allFiles, engineScripts, result);
        var bootstrapScript = BuildCompressionBootstrapScript();
        var readyBootstrapScript = BuildReadyBootstrapScript();
        html = InjectBefore(
            html,
            "<canvas id=\"application-canvas\"></canvas>",
            bootstrapScript + inlineAssetScript + HiddenAssetContainerMarkup + "<canvas id=\"application-canvas\"></canvas>" + readyBootstrapScript,
            "Injected compression bootstrap, packed asset payload, and ready orchestration ahead of the canvas.",
            result.patchesApplied);

        var networkBridgeScripts = BuildNetworkBridgeScripts(options);
        if (!string.IsNullOrEmpty(networkBridgeScripts))
        {
            html = InjectBeforeClosingBody(html, networkBridgeScripts, "Injected network-specific playable bridge.", result.patchesApplied);
        }

        result.html = html;
        return result;
    }

    private static void PatchIndexHtml(ref string html, LunaSingleHtmlPackOptions options, List<string> patchesApplied)
    {
        if (options.stripLunaDebugScripts)
        {
            html = ReplaceRegex(
                html,
                "<script\\b[^>]*data-startup-only[^>]*>.*?</script>",
                string.Empty,
                RegexOptions.Singleline | RegexOptions.IgnoreCase,
                "Removed Luna startup-only debug scripts.",
                patchesApplied);

            html = ReplaceRegex(
                html,
                "<script>window\\.addEventListener\\(\\\"luna:started\\\",\\(\\)=>\\{Luna\\.LogDebugInfo=.*?</script>",
                string.Empty,
                RegexOptions.Singleline,
                "Removed Luna playground debug logger.",
                patchesApplied);

            html = ReplaceRegex(
                html,
                "<script>window\\.addEventListener\\(\\\"luna:ready\\\",\\(\\)=>\\{window\\.pi\\.logStarted\\(\\);.*?</script>",
                string.Empty,
                RegexOptions.Singleline,
                "Removed editor-only Luna postMessage bridge.",
                patchesApplied);
        }

        html = ReplaceRegex(
            html,
            "<script\\s+src=\\\"engine/scripts\\.js\\\"[^>]*></script>",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.IgnoreCase,
            "Removed external engine/scripts.js reference.",
            patchesApplied);

        var environmentScript = BuildEnvironmentScript(options);
        html = ReplaceRegex(
            html,
            "<script>window\\.\\$environment=.*?</script>",
            environmentScript,
            RegexOptions.Singleline,
            "Replaced Luna playground environment config with single-html export config.",
            patchesApplied);

        if (options.network == PlayableAdNetwork.Mintegral)
        {
            var mintegralScript = "<script>window.pi.injectMintegralAdData=function(){if(\"mintegral\"===this.env.adNetwork&&window.MW_INIT)try{const i=window.MW_INIT.isRewardAds();!0===i?this.env.isRewarded=1:!1===i&&(this.env.isRewarded=0)}catch(i){}},window.pi.originalLogEvent=window.pi.logEvent,window.pi.logEvent=function(i,n,t){return this.injectMintegralAdData(),this.originalLogEvent(i,n,t)};</script>";
            html = InjectAfter(
                html,
                "<script>window.pi.apply(window,window.LUNA_PI_SETTINGS||[])</script>",
                mintegralScript,
                "Injected Mintegral analytics compatibility bridge.",
                patchesApplied);
        }

        var headMarkup = BuildHeadMarkup(options);
        if (!string.IsNullOrEmpty(headMarkup))
        {
            html = InjectBeforeClosingHead(html, headMarkup, $"Injected {options.network} head markup.", patchesApplied);
        }

        html = ReplaceRegex(
            html,
            "</body>\\s*</html>\\s*</head>\\s*</html>\\s*$",
            "</body></html>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase,
            "Normalized duplicated closing HTML tags from the Luna package.",
            patchesApplied);
    }

    private static void PatchEngineScripts(ref string engineScripts, List<string> patchesApplied)
    {
        ReplaceLiteral(ref engineScripts, "https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0", "about:blank#tmp", "Sanitized TextMeshPro help URLs inside engine/scripts.js.", patchesApplied);
        ReplaceLiteral(ref engineScripts, "https://via.placeholder.com/128", "data:image/gif;base64,R0lGODlhAQABAAAAACw=", "Sanitized placeholder image URLs inside engine/scripts.js.", patchesApplied);
        ReplaceLiteral(ref engineScripts, "sessionStorage", "__playableStorage", "Disabled sessionStorage-backed player prefs in packed engine/scripts.js.", patchesApplied);
    }

    private static string BuildEnvironmentScript(LunaSingleHtmlPackOptions options)
    {
        var applicationName = EscapeJavaScriptStringLiteral(PlayerSettings.productName ?? string.Empty);
        var iosLink = EscapeJavaScriptStringLiteral(options.iosStoreUrl ?? string.Empty);
        var androidLink = EscapeJavaScriptStringLiteral(options.androidStoreUrl ?? string.Empty);
        var orientation = EscapeJavaScriptStringLiteral(string.IsNullOrWhiteSpace(options.orientation) ? "portrait,landscape" : options.orientation);
        var targetPlatform = EscapeJavaScriptStringLiteral(GetTargetPlatform(options.network));

        return string.Concat(
            "<script>window.$environment = { baseUrl: \"./\", resourceConfig: { json: \"inline\", image: \"inline\", video: \"inline\", sound: \"compress\", blob: \"compress\" }, packageConfig: Object.assign({",
            "\"applicationName\":\"", applicationName, "\",",
            "\"iosLink\":\"", iosLink, "\",",
            "\"androidLink\":\"", androidLink, "\",",
            "\"orientation\":\"", orientation, "\",",
            "\"languages\":[\"en\",\"en\"],",
            "\"inlineScripts\":!0,\"inlineJsons\":!0,\"inlineBlobs\":!0,\"inlineImages\":!0,\"inlineSounds\":!0,\"inlineVideos\":!0 }, window.LUNA_PLAYGROUND_PACKAGE_CONFIG || {}), playerPrefs: !1, usingWebWorkers: \"off\", targetPlatform: \"",
            targetPlatform,
            "\", buildId: \"\", runtimeAnalysisModules: [\"physics3d\", \"physics2d\", \"particle_system\", \"reflection\", \"prefabs\", \"mecanim-wasm\"] }</script>");
    }

    private static string BuildHeadMarkup(LunaSingleHtmlPackOptions options)
    {
        var builder = NetworkMarkupBuilderFactory.Get(options.network);
        if (builder == null)
        {
            return string.Empty;
        }

        return builder.BuildHeadMarkup(EscapeHtmlAttribute(string.IsNullOrWhiteSpace(options.orientation) ? "portrait,landscape" : options.orientation));
    }

    private static string BuildCompressionBootstrapScript()
    {
        var decoder = LoadBrotliDecoderTemplate();
        var builder = new StringBuilder();
        builder.Append("<script data-startup-only>");
        builder.Append("window.makeBrotliDecodeStr=`");
        builder.Append(decoder);
        builder.Append("`;window.sharedBase122Buffer=null;(()=>{let curByte=0;let bitOfByte=0;let decodedIndex=0;const kIllegals=[0,10,13,34,38,92];const kShortened=7;function push7(byte,decoded){byte<<=1;curByte|=byte>>>bitOfByte;bitOfByte+=7;if(bitOfByte>=8){decoded[decodedIndex++]=curByte;bitOfByte-=8;curByte=byte<<7-bitOfByte&255}}window._base122ToArrayBuffer=(strData,useSharedBuffer=false)=>{const maxOutputLength=1.75*strData.length|0;let decoded=null;if(useSharedBuffer){if(!window.sharedBase122Buffer||window.sharedBase122Buffer.length<maxOutputLength){window.sharedBase122Buffer=new Uint8Array(maxOutputLength)}decoded=window.sharedBase122Buffer}else{decoded=new Uint8Array(maxOutputLength)}curByte=0;bitOfByte=0;decodedIndex=0;for(let i=0;i<strData.length;i++){const c=strData.charCodeAt(i);if(c>127){const illegalIndex=c>>>8&7;if(illegalIndex!==kShortened){push7(kIllegals[illegalIndex],decoded)}push7(c&127,decoded)}else{push7(c,decoded)}}return new Uint8Array(decoded.buffer,0,decodedIndex)}})();window._base64ToArrayBuffer=function(base64){const binary=atob(base64);const length=binary.length;const bytes=new Uint8Array(length);for(let i=0;i<length;i++){bytes[i]=binary.charCodeAt(i)}return bytes};");
        builder.Append("window.PlayableInlineAssets={getContainer:function(){let container=document.getElementById('playable-inline-assets');if(!container){container=document.createElement('div');container.id='playable-inline-assets';container.style.display='none';document.body.appendChild(container)}return container},blobToDataUrl:function(blob){return new Promise(function(resolve){const reader=new FileReader();reader.onloadend=function(){resolve(reader.result)};reader.readAsDataURL(blob)})},shouldUseDataUrlFallback:function(){const chromeMatch=navigator.userAgent.match(/Chrom(e|ium)\\/([0-9]+)\\./);const chromeVersion=chromeMatch?parseInt(chromeMatch[2],10):0;const iosMatch=navigator.userAgent.match(/(iPod|iPhone|iPad).*OS\\s([0-9_]*)\\s/);const iosVersion=iosMatch?iosMatch[2].replaceAll('_','.'):'';return chromeVersion&&chromeVersion<52||iosVersion&&(iosVersion.indexOf('15.4')===0||iosVersion.indexOf('17.4')===0)},bufferToUrl:function(buffer,mime){const blob=new Blob([buffer],{type:mime});return this.shouldUseDataUrlFallback()?this.blobToDataUrl(blob):Promise.resolve(URL.createObjectURL(blob))},appendImage:function(id,mime,buffer){return this.bufferToUrl(buffer,mime).then(function(url){return new Promise(function(resolve){const img=document.createElement('img');img.id=id;img.alt='';img.style.display='none';img.onload=function(){resolve(img)};img.onerror=function(){resolve(img)};img.src=url;window.PlayableInlineAssets.getContainer().appendChild(img);if(img.complete){resolve(img)}})})},appendVideo:function(id,mime,buffer){return this.bufferToUrl(buffer,mime).then(function(url){return new Promise(function(resolve){const video=document.createElement('video');video.id=id;video.style.display='none';video.src=url;video.autoplay=!1;video.loop=!1;video.muted=!0;video.crossOrigin='anonymous';video.setAttribute('playsinline','');video.setAttribute('type',mime);video.setAttribute('muted','');video.onloadeddata=function(){resolve(video)};video.onerror=function(){resolve(video)};window.PlayableInlineAssets.getContainer().appendChild(video);video.load();if(video.readyState>=2){resolve(video)}})})}};");
        builder.Append("window.unbrotli=Function('\"use strict\"; return ('+window.makeBrotliDecodeStr+')();')();window.decompressArrayBuffer=function(asset,isBase122){return Promise.resolve().then(function(){const encoded=isBase122===!1?window._base64ToArrayBuffer(asset):window._base122ToArrayBuffer(asset);let result=window.unbrotli(encoded);if(!(result instanceof Uint8Array)){result=new Uint8Array(result)}if(result.byteOffset===0&&result.byteLength===result.buffer.byteLength){return result.buffer}return result.buffer.slice(result.byteOffset,result.byteOffset+result.byteLength)})};window.decompressString=function(asset,isBase122){return window.decompressArrayBuffer(asset,isBase122).then(function(buffer){return new TextDecoder('utf-8').decode(new Uint8Array(buffer))})};window._compressedAssets=window._compressedAssets||[];");
        builder.Append("</script>");
        return builder.ToString();
    }

    private static string BuildReadyBootstrapScript()
    {
        return "<script>window._bridgeReady=!1,window._domReady=!1,window._readyEventEmitted=!1,window._emitPlayableReady=function(){window._domReady&&window._bridgeReady&&!window._readyEventEmitted&&(window._readyEventEmitted=!0,window.dispatchEvent(new Event(\"luna:ready\")))};window.addEventListener(\"DOMContentLoaded\",function(){window._domReady=!0;const pending=window._compressedAssets&&window._compressedAssets.length?Promise.all(window._compressedAssets):Promise.resolve();pending.then(function(){window.dispatchEvent(new Event(\"bridge:ready\"))});window._emitPlayableReady()});window.addEventListener(\"bridge:ready\",function(){window._bridgeReady=!0;window._emitPlayableReady()})</script>";
    }

    private static string BuildInlineAssetScript(Dictionary<string, string> allFiles, string engineScripts, LunaSingleHtmlPackResult result)
    {
        var builder = new StringBuilder();
        builder.Append("<script>");
        builder.Append("window.jsons=window.jsons||{};window.blobs=window.blobs||{};window.sounds=window.sounds||{};window._compressedAssets=window._compressedAssets||[];");

        AppendCompressedScript(builder, engineScripts, "window.eval(code);", result.patchesApplied, "Packed engine/scripts.js into inline compressed payload.");

        foreach (var pair in allFiles.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(pair.Key, "index.html", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Key, "engine/scripts.js", StringComparison.OrdinalIgnoreCase) ||
                pair.Key.StartsWith("cache/", StringComparison.OrdinalIgnoreCase) ||
                pair.Key.EndsWith(".map", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Key, "luna.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsJsonAsset(pair.Key))
            {
                var bytes = Utf8NoBom.GetBytes(File.ReadAllText(pair.Value, Utf8NoBom));
                var payload = ConvertToBrotliBase122(bytes);
                builder.Append("window._compressedAssets.push(window.decompressString(");
                builder.Append(ToSingleQuotedJavaScriptString(payload));
                builder.Append(",!0).then(function(json){window.jsons[");
                builder.Append(ToSingleQuotedJavaScriptString(pair.Key));
                builder.Append("]=JSON.parse(json);}));");
                result.inlinedJsonCount++;
                continue;
            }

            if (IsBlobAsset(pair.Key))
            {
                var payload = ConvertToBrotliBase122(File.ReadAllBytes(pair.Value));
                builder.Append("window._compressedAssets.push(window.decompressArrayBuffer(");
                builder.Append(ToSingleQuotedJavaScriptString(payload));
                builder.Append(",!0).then(function(buffer){window.blobs[");
                builder.Append(ToSingleQuotedJavaScriptString(pair.Key));
                builder.Append("]=buffer;}));");
                result.inlinedBlobCount++;
                continue;
            }

            if (IsSoundAsset(pair.Key))
            {
                var payload = ConvertToBrotliBase122(File.ReadAllBytes(pair.Value));
                builder.Append("window._compressedAssets.push(window.decompressArrayBuffer(");
                builder.Append(ToSingleQuotedJavaScriptString(payload));
                builder.Append(",!0).then(function(buffer){window.sounds[");
                builder.Append(ToSingleQuotedJavaScriptString(pair.Key));
                builder.Append("]=buffer;}));");
                result.inlinedSoundCount++;
                continue;
            }

            if (IsImageAsset(pair.Key))
            {
                var payload = ConvertToBrotliBase122(File.ReadAllBytes(pair.Value));
                builder.Append("window._compressedAssets.push(window.decompressArrayBuffer(");
                builder.Append(ToSingleQuotedJavaScriptString(payload));
                builder.Append(",!0).then(function(buffer){return window.PlayableInlineAssets.appendImage(");
                builder.Append(ToSingleQuotedJavaScriptString(pair.Key));
                builder.Append(",");
                builder.Append(ToSingleQuotedJavaScriptString(GetMimeType(pair.Key)));
                builder.Append(",buffer);}));");
                result.inlinedImageCount++;
                continue;
            }

            if (IsVideoAsset(pair.Key))
            {
                var payload = ConvertToBrotliBase122(File.ReadAllBytes(pair.Value));
                builder.Append("window._compressedAssets.push(window.decompressArrayBuffer(");
                builder.Append(ToSingleQuotedJavaScriptString(payload));
                builder.Append(",!0).then(function(buffer){return window.PlayableInlineAssets.appendVideo(");
                builder.Append(ToSingleQuotedJavaScriptString(pair.Key));
                builder.Append(",");
                builder.Append(ToSingleQuotedJavaScriptString(GetMimeType(pair.Key)));
                builder.Append(",buffer);}));");
                result.inlinedVideoCount++;
            }
        }

        builder.Append("</script>");
        return builder.ToString();
    }

    private static void AppendCompressedScript(StringBuilder builder, string scriptBody, string onResolvedBody, List<string> patchesApplied, string patchDescription)
    {
        var payload = ConvertToBrotliBase122(Utf8NoBom.GetBytes(scriptBody));
        builder.Append("window._compressedAssets.push(window.decompressString(");
        builder.Append(ToSingleQuotedJavaScriptString(payload));
        builder.Append(",!0).then(function(code){");
        builder.Append(onResolvedBody);
        builder.Append("}));");
        patchesApplied.Add(patchDescription);
    }

    private static string BuildNetworkBridgeScripts(LunaSingleHtmlPackOptions options)
    {
        var builder = NetworkMarkupBuilderFactory.Get(options.network);
        return builder != null ? builder.BuildTailScript() : string.Empty;
    }

    private static string LoadBrotliDecoderTemplate()
    {
        var fullPath = Path.GetFullPath(BrotliDecoderAssetPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            fullPath = Path.Combine(Directory.GetCurrentDirectory(), BrotliDecoderAssetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"Brotli decoder template is missing: {BrotliDecoderAssetPath}");
        }

        return File.ReadAllText(fullPath, Utf8NoBom).Replace("`", string.Empty);
    }

    private static string ConvertToBrotliBase122(byte[] bytes)
    {
        using (var output = new MemoryStream())
        {
            using (var brotli = new BrotliStream(output, System.IO.Compression.CompressionLevel.Optimal, true))
            {
                brotli.Write(bytes, 0, bytes.Length);
            }

            return EncodeBase122(output.ToArray());
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

    private static string NormalizeRelativePath(string rootDirectory, string filePath)
    {
        var relativePath = Path.GetRelativePath(rootDirectory, filePath);
        return relativePath.Replace('\\', '/');
    }

    private static bool IsJsonAsset(string relativePath)
    {
        return relativePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && relativePath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBlobAsset(string relativePath)
    {
        return relativePath.EndsWith(".blob", StringComparison.OrdinalIgnoreCase) && relativePath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSoundAsset(string relativePath)
    {
        return relativePath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) &&
               (relativePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsImageAsset(string relativePath)
    {
        return relativePath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) &&
               (relativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsVideoAsset(string relativePath)
    {
        return relativePath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) &&
               (relativePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".webm", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetMimeType(string relativePath)
    {
        var extension = Path.GetExtension(relativePath).ToLowerInvariant();
        switch (extension)
        {
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".webp": return "image/webp";
            case ".mp3": return "audio/mpeg";
            case ".wav": return "audio/wav";
            case ".ogg": return "audio/ogg";
            case ".m4a": return "audio/mp4";
            case ".mp4": return "video/mp4";
            case ".webm": return "video/webm";
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
            case PlayableAdNetwork.Vungle:
                return "vungle";
            default:
                return "playable";
        }
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
            throw new InvalidOperationException($"Could not find marker '{marker}' while patching single-html playable output.");
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

    private static string ReplaceRegex(string input, string pattern, string replacement, RegexOptions options, string patchDescription, List<string> patchesApplied)
    {
        var output = Regex.Replace(input, pattern, replacement, options);
        if (!ReferenceEquals(output, input) && !string.Equals(output, input, StringComparison.Ordinal))
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
}






