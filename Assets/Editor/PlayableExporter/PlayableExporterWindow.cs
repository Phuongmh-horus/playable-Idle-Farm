using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class PlayableExporterWindow : EditorWindow
{
    private const string SourceZipPrefKey = "PlayableExporter.SourceZipPath";
    private const string OutputRootPrefKey = "PlayableExporter.OutputRootPath";
    private const string LogoImagePrefKey = "PlayableExporter.LogoImagePath";
    private const string ApplicationNamePrefKey = "PlayableExporter.ApplicationName";
    private const string OrientationPrefKey = "PlayableExporter.Orientation";
    private const string StripDebugPrefKey = "PlayableExporter.StripDebug";
    private const string IosStoreUrlPrefKey = "PlayableExporter.IosStoreUrl";
    private const string AndroidStoreUrlPrefKey = "PlayableExporter.AndroidStoreUrl";
    private const string SelectedNetworksPrefKey = "PlayableExporter.SelectedNetworks";

    private string _sourceZipPath;
    private string _outputRootPath;
    private string _logoImagePath;
    private Texture2D _logoImageAsset;
    private string _applicationName;
    private string _orientation = "portrait,landscape";
    private bool _stripLunaDebugScripts = true;
    private string _iosStoreUrl;
    private string _androidStoreUrl;
    private Vector2 _scrollPosition;
    private readonly Dictionary<PlayableAdNetwork, bool> _selectedNetworks = new Dictionary<PlayableAdNetwork, bool>();

    [MenuItem("Tools/Playable Ads/Exporter")]
    public static void OpenWindow()
    {
        var window = GetWindow<PlayableExporterWindow>("Playable Exporter");
        window.minSize = new Vector2(760f, 520f);
        window.Show();
    }

    [MenuItem("Tools/Playable Ads/Export Paid Networks From Latest Luna Zip")]
    public static void ExportPaidNetworksFromLatestLunaZip()
    {
        var latestZip = PlayableAdsExporter.TryFindLatestLunaZip();
        if (string.IsNullOrEmpty(latestZip))
        {
            EditorUtility.DisplayDialog("Playable Exporter", "No Luna create-hub zip was found under LunaTemp/stage4/create-hub.", "OK");
            return;
        }

        var options = new PlayableExportOptions
        {
            sourceZipPath = latestZip,
            outputRootPath = GetDefaultOutputRoot(),
            logoImagePath = string.Empty,
            applicationName = string.Empty,
            orientation = "portrait,landscape",
            stripLunaDebugScripts = true,
            iosStoreUrl = string.Empty,
            androidStoreUrl = string.Empty
        };

        ExecuteBatchExport(options);
    }

    [MenuItem("Tools/Playable Ads/Clean Generated Artifacts")]
    public static void CleanGeneratedArtifacts()
    {
        ExecuteCleanup();
    }

    private void OnEnable()
    {
        _sourceZipPath = EditorPrefs.GetString(SourceZipPrefKey, PlayableAdsExporter.TryFindLatestLunaZip());
        _outputRootPath = EditorPrefs.GetString(OutputRootPrefKey, GetDefaultOutputRoot());
        _logoImagePath = EditorPrefs.GetString(LogoImagePrefKey, string.Empty);
        _applicationName = EditorPrefs.GetString(ApplicationNamePrefKey, string.Empty);
        _orientation = EditorPrefs.GetString(OrientationPrefKey, "portrait,landscape");
        _stripLunaDebugScripts = EditorPrefs.GetBool(StripDebugPrefKey, true);
        _iosStoreUrl = EditorPrefs.GetString(IosStoreUrlPrefKey, string.Empty);
        _androidStoreUrl = EditorPrefs.GetString(AndroidStoreUrlPrefKey, string.Empty);
        SyncLogoAssetFromPath();
        LoadSelectedNetworks();
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Paid Network Export", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool replaces the LunaX paid-network packaging step. It takes a Luna create-hub ZIP, packs it into a network-specific artifact, then exports Google Ads, Mintegral, AppLovin, Facebook, IronSource, Moloco, TikTok, Unity, and Vungle variants from the same Unity-side pipeline.",
            MessageType.Info);

        DrawPathField("Source Luna ZIP", ref _sourceZipPath, PickSourceZipPath, "Latest", UseLatestLunaZip);
        DrawPathField("Output Root", ref _outputRootPath, PickOutputRootPath, null, null);
        DrawLogoField();

        EditorGUILayout.Space();
        _applicationName = EditorGUILayout.TextField("Game Name", _applicationName);
        _orientation = EditorGUILayout.TextField("Orientation", _orientation);
        _iosStoreUrl = EditorGUILayout.TextField("iOS Store URL", _iosStoreUrl);
        _androidStoreUrl = EditorGUILayout.TextField("Android Store URL", _androidStoreUrl);
        _stripLunaDebugScripts = EditorGUILayout.Toggle("Strip Debug Scripts", _stripLunaDebugScripts);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Store URLs are injected into $environment.packageConfig for CTA fallback on AppLovin, Facebook, IronSource, Moloco, Mintegral, TikTok, Unity, and Vungle. Leave them blank only if your gameplay passes explicit URLs into InstallFullGame().",
            MessageType.Warning);

        EditorGUILayout.Space();
        DrawNetworkSelectionSection();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Export All Paid Networks", GUILayout.Height(34f)))
            {
                ExportAllPaidNetworks();
            }

            if (GUILayout.Button("Export Selected Networks", GUILayout.Height(34f)))
            {
                ExportSelectedNetworks();
            }

            if (GUILayout.Button("Clean Generated Temp", GUILayout.Height(34f)))
            {
                ExecuteCleanup();
            }

            if (GUILayout.Button("Reveal Output Root", GUILayout.Height(34f)))
            {
                var root = string.IsNullOrWhiteSpace(_outputRootPath) ? GetDefaultOutputRoot() : _outputRootPath;
                Directory.CreateDirectory(root);
                EditorUtility.RevealInFinder(root);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Current exporter produces Google Ads and Mintegral as ZIPs containing one HTML, AppLovin/Facebook/IronSource/Moloco/Unity as standalone HTML, TikTok as a ZIP containing index.html plus config.json, and Vungle as a ZIP containing index.html plus external runtime media files. Reports are written next to each artifact.",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void ExportAllPaidNetworks()
    {
        RefreshSourceZipPathIfNeeded();
        PersistPreferences();
        ExecuteBatchExport(BuildOptions());
    }

    private void ExportSelectedNetworks()
    {
        RefreshSourceZipPathIfNeeded();
        PersistPreferences();
        var selectedNetworks = GetSelectedNetworks();
        if (selectedNetworks.Length == 0)
        {
            EditorUtility.DisplayDialog("Playable Exporter", "Select at least one network before running a selected batch export.", "OK");
            return;
        }

        ExecuteBatchExport(BuildOptions(), selectedNetworks);
    }

    private static void ExecuteBatchExport(PlayableExportOptions options)
    {
        ExecuteBatchExport(options, null);
    }

    private static void ExecuteBatchExport(PlayableExportOptions options, IEnumerable<PlayableAdNetwork> selectedNetworks)
    {
        try
        {
            var report = selectedNetworks == null
                ? PlayableAdsExporter.ExportPaidNetworks(options)
                : PlayableAdsExporter.ExportPaidNetworks(options, selectedNetworks);
            Debug.Log($"[PlayableExporter] Paid network batch export completed: {report.bundleZipPath}");
            ShowBatchCompletionDialog(report);
            EditorUtility.RevealInFinder(report.outputDirectory);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Playable Exporter", exception.Message, "OK");
        }
    }

    private static void ExecuteCleanup()
    {
        var report = PlayableExportCleanup.CleanGeneratedArtifacts();
        Debug.Log($"[PlayableExporter] Cleanup removed {report.removedDirectories.Count} directories and {report.removedFiles.Count} files.");
        ShowCleanupDialog(report);
    }

    private PlayableExportOptions BuildOptions()
    {
        return new PlayableExportOptions
        {
            sourceZipPath = _sourceZipPath,
            outputRootPath = _outputRootPath,
            logoImagePath = _logoImagePath ?? string.Empty,
            applicationName = _applicationName ?? string.Empty,
            orientation = string.IsNullOrWhiteSpace(_orientation) ? "portrait,landscape" : _orientation,
            stripLunaDebugScripts = _stripLunaDebugScripts,
            iosStoreUrl = _iosStoreUrl ?? string.Empty,
            androidStoreUrl = _androidStoreUrl ?? string.Empty
        };
    }

    private void PersistPreferences()
    {
        EditorPrefs.SetString(SourceZipPrefKey, _sourceZipPath ?? string.Empty);
        EditorPrefs.SetString(OutputRootPrefKey, _outputRootPath ?? string.Empty);
        EditorPrefs.SetString(LogoImagePrefKey, _logoImagePath ?? string.Empty);
        EditorPrefs.SetString(ApplicationNamePrefKey, _applicationName ?? string.Empty);
        EditorPrefs.SetString(OrientationPrefKey, _orientation ?? "portrait,landscape");
        EditorPrefs.SetBool(StripDebugPrefKey, _stripLunaDebugScripts);
        EditorPrefs.SetString(IosStoreUrlPrefKey, _iosStoreUrl ?? string.Empty);
        EditorPrefs.SetString(AndroidStoreUrlPrefKey, _androidStoreUrl ?? string.Empty);
        EditorPrefs.SetString(SelectedNetworksPrefKey, string.Join(",", GetSelectedNetworks().Select(network => network.ToString()).ToArray()));
    }

    private void PickSourceZipPath()
    {
        var startDirectory = string.IsNullOrWhiteSpace(_sourceZipPath)
            ? Path.Combine(PlayableAdsExporter.GetProjectRoot(), "LunaTemp", "stage4", "create-hub")
            : Path.GetDirectoryName(_sourceZipPath);

        var selectedPath = EditorUtility.OpenFilePanel("Select Luna Create-Hub ZIP", startDirectory, "zip");
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            _sourceZipPath = selectedPath;
            Repaint();
        }
    }

    private void UseLatestLunaZip()
    {
        _sourceZipPath = PlayableAdsExporter.TryFindLatestLunaZip();
        Repaint();
    }

    private void RefreshSourceZipPathIfNeeded()
    {
        var latestZip = PlayableAdsExporter.TryFindLatestLunaZip();
        if (string.IsNullOrWhiteSpace(latestZip))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_sourceZipPath) || !File.Exists(_sourceZipPath))
        {
            _sourceZipPath = latestZip;
            return;
        }

        var currentFullPath = Path.GetFullPath(_sourceZipPath);
        var latestFullPath = Path.GetFullPath(latestZip);
        if (string.Equals(currentFullPath, latestFullPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var createHubDirectory = Path.Combine(PlayableAdsExporter.GetProjectRoot(), "LunaTemp", "stage4", "create-hub");
        var currentDirectory = Path.GetDirectoryName(currentFullPath) ?? string.Empty;
        if (string.Equals(Path.GetFullPath(currentDirectory), Path.GetFullPath(createHubDirectory), StringComparison.OrdinalIgnoreCase))
        {
            _sourceZipPath = latestFullPath;
        }
    }

    private void PickOutputRootPath()
    {
        var startDirectory = string.IsNullOrWhiteSpace(_outputRootPath) ? GetDefaultOutputRoot() : _outputRootPath;
        var selectedPath = EditorUtility.OpenFolderPanel("Select Export Output Root", startDirectory, string.Empty);
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            _outputRootPath = selectedPath;
            Repaint();
        }
    }

    private void PickLogoImagePath()
    {
        var startDirectory = string.IsNullOrWhiteSpace(_logoImagePath)
            ? Path.Combine(PlayableAdsExporter.GetProjectRoot(), "Assets")
            : Path.GetDirectoryName(_logoImagePath);

        var selectedPath = EditorUtility.OpenFilePanel("Select Game Logo Image", startDirectory, "png,jpg,jpeg,webp,gif");
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            _logoImagePath = selectedPath;
            SyncLogoAssetFromPath();
            Repaint();
        }
    }

    private void ClearLogoImagePath()
    {
        _logoImagePath = string.Empty;
        _logoImageAsset = null;
        Repaint();
    }

    private void SetLogoAsset(Texture2D logoAsset)
    {
        _logoImageAsset = logoAsset;
        if (_logoImageAsset == null)
        {
            _logoImagePath = string.Empty;
            return;
        }

        var assetPath = AssetDatabase.GetAssetPath(_logoImageAsset);
        _logoImagePath = string.IsNullOrWhiteSpace(assetPath) ? string.Empty : Path.GetFullPath(assetPath);
    }

    private void SyncLogoAssetFromPath()
    {
        _logoImageAsset = null;
        if (string.IsNullOrWhiteSpace(_logoImagePath))
        {
            return;
        }

        var assetPath = TryGetProjectRelativeAssetPath(_logoImagePath);
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return;
        }

        _logoImageAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static string TryGetProjectRelativeAssetPath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return string.Empty;
        }

        var normalizedFullPath = Path.GetFullPath(fullPath).Replace('\\', '/');
        var normalizedProjectPath = PlayableAdsExporter.GetProjectRoot().Replace('\\', '/').TrimEnd('/');
        if (!normalizedFullPath.StartsWith(normalizedProjectPath + "/", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var relativePath = normalizedFullPath.Substring(normalizedProjectPath.Length + 1);
        return relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || string.Equals(relativePath, "Assets", StringComparison.OrdinalIgnoreCase)
            ? relativePath
            : string.Empty;
    }

    private static void DrawPathField(string label, ref string value, Action browseAction, string secondaryButtonLabel, Action secondaryAction)
    {
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            value = EditorGUILayout.TextField(value);
            if (GUILayout.Button("Browse", GUILayout.Width(80f)))
            {
                browseAction?.Invoke();
            }

            if (secondaryAction != null && GUILayout.Button(secondaryButtonLabel, GUILayout.Width(80f)))
            {
                secondaryAction();
            }
        }
    }

    private void DrawLogoField()
    {
        EditorGUILayout.LabelField("Game Logo", EditorStyles.miniBoldLabel);

        EditorGUI.BeginChangeCheck();
        var selectedLogo = (Texture2D)EditorGUILayout.ObjectField(_logoImageAsset, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck())
        {
            SetLogoAsset(selectedLogo);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(_logoImagePath ?? string.Empty);
            }

            if (GUILayout.Button("Browse", GUILayout.Width(80f)))
            {
                PickLogoImagePath();
            }

            if (GUILayout.Button("Clear", GUILayout.Width(80f)))
            {
                ClearLogoImagePath();
            }
        }

        if (_logoImageAsset != null)
        {
            var previewRect = GUILayoutUtility.GetRect(120f, 120f, GUILayout.Width(120f), GUILayout.Height(120f));
            EditorGUI.DrawPreviewTexture(previewRect, _logoImageAsset, null, ScaleMode.ScaleToFit);
        }
    }

    private void DrawNetworkSelectionSection()
    {
        EditorGUILayout.LabelField("Batch Network Selection", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Choose which networks to include in the batch export. Use 'Export Selected Networks' to build only the checked networks and reduce export time.",
            MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Select All", GUILayout.Width(100f)))
            {
                SetAllNetworkSelections(true);
            }

            if (GUILayout.Button("Clear All", GUILayout.Width(100f)))
            {
                SetAllNetworkSelections(false);
            }
        }

        var paidNetworks = PlayableAdsExporter.GetPaidNetworks();
        const int columnCount = 3;
        for (var index = 0; index < paidNetworks.Count; index += columnCount)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (var column = 0; column < columnCount; column++)
                {
                    var currentIndex = index + column;
                    if (currentIndex >= paidNetworks.Count)
                    {
                        GUILayout.FlexibleSpace();
                        continue;
                    }

                    var network = paidNetworks[currentIndex];
                    _selectedNetworks[network] = GUILayout.Toggle(_selectedNetworks[network], GetNetworkDisplayName(network), GUILayout.MinWidth(140f));
                }
            }
        }
    }

    private void LoadSelectedNetworks()
    {
        _selectedNetworks.Clear();
        foreach (var network in PlayableAdsExporter.GetPaidNetworks())
        {
            _selectedNetworks[network] = true;
        }

        var rawValue = EditorPrefs.GetString(SelectedNetworksPrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        SetAllNetworkSelections(false);
        foreach (var token in rawValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!Enum.TryParse(token, true, out PlayableAdNetwork network))
            {
                continue;
            }

            if (_selectedNetworks.ContainsKey(network))
            {
                _selectedNetworks[network] = true;
            }
        }
    }

    private void SetAllNetworkSelections(bool isSelected)
    {
        foreach (var network in PlayableAdsExporter.GetPaidNetworks())
        {
            _selectedNetworks[network] = isSelected;
        }
    }

    private PlayableAdNetwork[] GetSelectedNetworks()
    {
        return PlayableAdsExporter.GetPaidNetworks()
            .Where(network => _selectedNetworks.ContainsKey(network) && _selectedNetworks[network])
            .ToArray();
    }

    private static string GetNetworkDisplayName(PlayableAdNetwork network)
    {
        switch (network)
        {
            case PlayableAdNetwork.GoogleAds:
                return "Google Ads";
            case PlayableAdNetwork.IronSource:
                return "IronSource";
            case PlayableAdNetwork.TikTok:
                return "TikTok";
            default:
                return network.ToString();
        }
    }

    private static void ShowBatchCompletionDialog(PlayableBatchExportReport report)
    {
        var lines = report.networkReports
            .Select(networkReport => $"{networkReport.network}: {networkReport.artifactSizeBytes / 1024f / 1024f:0.00} MB | Limit OK: {networkReport.artifactWithinRecommendedLimit}")
            .ToArray();
        var summary =
            $"Bundle ZIP: {report.bundleZipPath}\n\n" +
            string.Join("\n", lines);

        EditorUtility.DisplayDialog("Paid Network Export Complete", summary, "OK");
    }

    private static void ShowCleanupDialog(GeneratedArtifactCleanupReport report)
    {
        var summary =
            $"Removed directories: {report.removedDirectories.Count}\n" +
            $"Removed files: {report.removedFiles.Count}\n" +
            $"Warnings: {report.warnings.Count}";

        EditorUtility.DisplayDialog("Playable Exporter Cleanup", summary, "OK");
    }

    private static string GetDefaultOutputRoot()
    {
        return Path.Combine(PlayableAdsExporter.GetProjectRoot(), "PlayableExports", "Official");
    }
}
