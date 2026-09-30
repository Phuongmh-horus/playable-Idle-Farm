#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PlayableAd.Editor
{
    /// <summary>
    /// Experimental reference asset diff. It compares the original project with this project
    /// by relative path and exposes explicit, per-file overwrite actions.
    /// </summary>
    internal sealed class ReferenceAssetDiffExperimentalWindow : EditorWindow
    {
        private const string ExperimentalTitle = "Reference Asset Diff (Experimental)";
        private static readonly string[] DefaultExtensions =
        {
            ".prefab", ".unity", ".mat", ".fbx", ".png", ".jpg", ".jpeg", ".psd", ".asset",
            ".controller", ".anim", ".shader", ".shadergraph", ".vfx", ".spriteatlas", ".wav", ".mp3", ".ogg"
        };
        private static readonly string[] AllowedAssetRoots =
        {
            "Assets/_Game/Prefabs",
            "Assets/_FrameWork",
            "Assets/_Game",
            "Assets/_AnimVFX",
            "Assets/Art_2D",
            "Assets/Material"
        };
        private static readonly string[] AllowedAssetRootLabels =
        {

            "_Game/Prefabs",
            "_FrameWork",
            "_Game",
            "_AnimVFX",
            "Art_2D",
            "Material"
        };

        private readonly List<AssetDiffRow> rows = new List<AssetDiffRow>();
        private Vector2 scroll;
        private int sourceRootPreset;
        private int currentRootPreset;
        private string filter = "";
        private bool showOnlyChanges = true;
        private string result = "Chưa quét.";
        private bool showChanged = true;
        private bool showMissingCurrent = true;
        private bool showMissingReference;
        private readonly Dictionary<string, Texture2D> previewCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> pendingPaths = new List<string>();
        private int pendingIndex;
        private bool scanInProgress;
        private string pendingSourceRoot;
        private string pendingCurrentRoot;

        private sealed class AssetDiffRow
        {
            public string RelativePath;
            public string SourcePath;
            public string CurrentPath;
            public string Kind;
            public string Detail;
            public bool Selected;
        }

        [MenuItem("REFERENCE SYNC/🧪 Experimental Asset Diff", false, 2)]
        public static void Open()
        {
            ReferenceAssetDiffExperimentalWindow window = GetWindow<ReferenceAssetDiffExperimentalWindow>(ExperimentalTitle);
            window.minSize = new Vector2(900f, 560f);
            window.Show();
            window.BeginScan();
        }

        private void OnDisable()
        {
            EditorApplication.update -= ProcessScanBatch;
            scanInProgress = false;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("REFERENCE ASSET DIFF", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Read-only migration inventory. .meta không được copy giữa hai project; các item được chọn chỉ tạo migration plan trong Library/ReferenceSync.", MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                sourceRootPreset = EditorGUILayout.Popup("Game gốc folder", sourceRootPreset, AllowedAssetRootLabels);
                currentRootPreset = EditorGUILayout.Popup("Game hiện tại folder", currentRootPreset, AllowedAssetRootLabels);
                if (GUILayout.Button(scanInProgress ? "Scanning..." : "Scan", GUILayout.Width(90f)) && !scanInProgress) BeginScan();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                filter = EditorGUILayout.TextField("Filter", filter);
                EditorGUILayout.LabelField(".meta luôn bị loại trừ", EditorStyles.miniLabel, GUILayout.Width(150f));
                showOnlyChanges = EditorGUILayout.ToggleLeft("Changes only", showOnlyChanges, GUILayout.Width(110f));
            }
            EditorGUILayout.LabelField("Đối chiếu theo relative path bên trong hai folder đã chọn. Ưu tiên Runtime để kiểm tra level 10.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Reference: " + ReferenceSyncConfiguration.SourceRoot, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Current: " + ProjectRoot(), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(result, EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Select all visible", EditorStyles.toolbarButton)) SetVisibleSelection(true);
                if (GUILayout.Button("Clear visible", EditorStyles.toolbarButton)) SetVisibleSelection(false);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create migration plan", EditorStyles.toolbarButton, GUILayout.Width(180f))) CreateMigrationPlan();
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                showChanged = GUILayout.Toggle(showChanged, "Changed existing", EditorStyles.toolbarButton);
                showMissingCurrent = GUILayout.Toggle(showMissingCurrent, "Missing in current", EditorStyles.toolbarButton);
                showMissingReference = GUILayout.Toggle(showMissingReference, "Only in current", EditorStyles.toolbarButton);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < rows.Count; i++)
            {
                AssetDiffRow row = rows[i];
                if (!IsVisible(row)) continue;
                DrawRow(row);
            }
            EditorGUILayout.EndScrollView();
        }

        private void BeginScan()
        {
            if (scanInProgress) return;
            rows.Clear();
            pendingPaths.Clear();
            pendingIndex = 0;
            string sourceRelativeRoot = GetSelectedRoot(sourceRootPreset);
            string currentRelativeRoot = GetSelectedRoot(currentRootPreset);
            string sourceRoot = Path.Combine(ReferenceSyncConfiguration.SourceRoot, sourceRelativeRoot.Replace('/', Path.DirectorySeparatorChar));
            string currentRoot = Path.Combine(ProjectRoot(), currentRelativeRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(sourceRoot) && !Directory.Exists(currentRoot))
            {
                result = "Không tìm thấy folder đã chọn ở một trong hai project.";
                return;
            }
            pendingSourceRoot = sourceRoot;
            pendingCurrentRoot = currentRoot;

            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddRelativeFiles(sourceRoot, "", paths);
            AddRelativeFiles(currentRoot, "", paths);
            pendingPaths.AddRange(paths);
            pendingPaths.Sort(StringComparer.OrdinalIgnoreCase);
            scanInProgress = true;
            result = "Đang quét " + pendingPaths.Count + " file theo batch...";
            EditorApplication.update -= ProcessScanBatch;
            EditorApplication.update += ProcessScanBatch;
            Repaint();
        }

        private void ProcessScanBatch()
        {
            const int batchSize = 24;
            int end = Math.Min(pendingIndex + batchSize, pendingPaths.Count);
            for (; pendingIndex < end; pendingIndex++)
            {
                string relativePath = pendingPaths[pendingIndex];
                string source = Path.Combine(pendingSourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string current = Path.Combine(pendingCurrentRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                bool sourceExists = File.Exists(source);
                bool currentExists = File.Exists(current);
                if (!sourceExists || !currentExists)
                {
                    rows.Add(new AssetDiffRow
                    {
                        RelativePath = relativePath,
                        SourcePath = source,
                        CurrentPath = current,
                        Kind = sourceExists ? "Missing current" : "Only current",
                        Detail = sourceExists ? "Reference có, current thiếu" : "Current có, reference thiếu",
                        Selected = false
                    });
                    continue;
                }
                if (SameContent(source, current)) continue;
                string detail = BuildDetail(source, current);
                rows.Add(new AssetDiffRow
                {
                    RelativePath = relativePath,
                    SourcePath = source,
                    CurrentPath = current,
                    Kind = "Changed",
                    Detail = detail,
                    Selected = false
                });
            }
            if (pendingIndex >= pendingPaths.Count)
            {
                EditorApplication.update -= ProcessScanBatch;
                scanInProgress = false;
                rows.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
                result = "Đã quét " + pendingPaths.Count + " file, phát hiện " + rows.Count + " khác biệt. Chưa ghi đè file nào.";
            }
            else result = "Đang quét " + pendingIndex + "/" + pendingPaths.Count + " file...";
            Repaint();
        }

        private void AddRelativeFiles(string root, string normalizedRoot, HashSet<string> output)
        {
            if (!Directory.Exists(root)) return;
            string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string extension = Path.GetExtension(files[i]);
                if (string.Equals(extension, ".meta", StringComparison.OrdinalIgnoreCase)) continue;
                if (!IsSupportedExtension(extension)) continue;
                string local = files[i].Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                output.Add(string.IsNullOrEmpty(normalizedRoot) ? local : normalizedRoot.TrimEnd('/') + "/" + local);
            }
        }

        private string GetSelectedRoot(int preset)
        {
            if (preset < 0 || preset >= AllowedAssetRoots.Length) preset = 0;
            return AllowedAssetRoots[preset];
        }

        private static bool IsSupportedExtension(string extension)
        {
            for (int i = 0; i < DefaultExtensions.Length; i++)
                if (string.Equals(extension, DefaultExtensions[i], StringComparison.OrdinalIgnoreCase)) return true;
            return string.Equals(extension, ".meta", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildDetail(string source, string current)
        {
            string extension = Path.GetExtension(source);
            if (!string.Equals(extension, ".prefab", StringComparison.OrdinalIgnoreCase) && !string.Equals(extension, ".unity", StringComparison.OrdinalIgnoreCase))
                return "Binary/content khác nhau";

            string sourceText = File.ReadAllText(source);
            string currentText = File.ReadAllText(current);
            List<string> changes = new List<string>();
            AddChangeLabel(changes, "fileID", sourceText, currentText);
            AddChangeLabel(changes, "guid", sourceText, currentText);
            AddChangeLabel(changes, "m_LocalPosition", sourceText, currentText);
            AddChangeLabel(changes, "m_LocalRotation", sourceText, currentText);
            AddChangeLabel(changes, "m_LocalScale", sourceText, currentText);
            return changes.Count == 0 ? "YAML khác, chưa phân loại" : string.Join(", ", changes.ToArray());
        }

        private static void AddChangeLabel(List<string> changes, string token, string source, string current)
        {
            if (ExtractLines(source, token) != ExtractLines(current, token)) changes.Add(token);
        }

        private static string ExtractLines(string text, string token)
        {
            StringBuilder builder = new StringBuilder();
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++) if (lines[i].IndexOf(token, StringComparison.Ordinal) >= 0) builder.AppendLine(lines[i].Trim());
            return builder.ToString();
        }

        private void DrawRow(AssetDiffRow row)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.MinHeight(58f)))
            {
                row.Selected = EditorGUILayout.Toggle(row.Selected, GUILayout.Width(18f));
                Texture2D preview = GetPreview(row);
                if (preview != null) GUILayout.Label(preview, GUILayout.Width(52f), GUILayout.Height(52f));
                else GUILayout.Label(GetExtensionLabel(row.RelativePath), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(52f), GUILayout.Height(52f));
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(row.Kind + "  " + row.RelativePath, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(row.Detail, EditorStyles.miniLabel);
                }
                if (GUILayout.Button("Ping", GUILayout.Width(48f))) PingCurrent(row);
            }
        }

        private Texture2D GetPreview(AssetDiffRow row)
        {
            string key = row.RelativePath + "|" + row.Kind;
            Texture2D cached;
            if (previewCache.TryGetValue(key, out cached)) return cached;
            string extension = Path.GetExtension(row.RelativePath);
            if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                string path = File.Exists(row.CurrentPath) ? row.CurrentPath : row.SourcePath;
                if (File.Exists(path))
                {
                    Texture2D image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (image.LoadImage(File.ReadAllBytes(path))) { previewCache[key] = image; return image; }
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
            if (File.Exists(row.CurrentPath))
            {
                string assetPath = ToAssetPath(row.CurrentPath);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                Texture2D preview = asset == null ? null : AssetPreview.GetAssetPreview(asset);
                if (preview != null) { previewCache[key] = preview; return preview; }
            }
            return null;
        }

        private static string GetExtensionLabel(string path)
        {
            string extension = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
            return extension.Length == 0 ? "FILE" : extension;
        }

        private void CreateMigrationPlan()
        {
            List<AssetDiffRow> selected = new List<AssetDiffRow>();
            for (int i = 0; i < rows.Count; i++) if (rows[i].Selected && File.Exists(rows[i].SourcePath)) selected.Add(rows[i]);
            if (selected.Count == 0) { EditorUtility.DisplayDialog(ExperimentalTitle, "Chưa chọn file reference hợp lệ.", "OK"); return; }
            List<ReferenceAssetMigrationItem> items = new List<ReferenceAssetMigrationItem>();
            for (int i = 0; i < selected.Count; i++)
            {
                AssetDiffRow row = selected[i];
                items.Add(new ReferenceAssetMigrationItem
                {
                    relativePath = row.RelativePath,
                    kind = row.Kind,
                    detail = row.Detail,
                    sourceHash = ReferenceSyncConfiguration.HashFile(row.SourcePath)
                });
            }
            ReferenceAssetMigrationPlan plan = new ReferenceAssetMigrationPlan
            {
                createdUtc = DateTime.UtcNow.ToString("o"),
                sourceRoot = pendingSourceRoot,
                targetRoot = pendingCurrentRoot,
                policy = "read-only; requires dependency resolution, GUID remapping, staging validation, and explicit merge",
                items = items.ToArray()
            };
            ReferenceMigrationPipeline.FinalizePlan(plan);
            result = "Plan: " + plan.readiness + " | roots: " + selected.Count + " | dependencies: " + plan.dependencies.Length + " | unresolved GUID: " + plan.unresolvedGuids.Length;
            EditorUtility.DisplayDialog(ExperimentalTitle, result + "\nPrompt: " + plan.aiPromptPath + "\nKhông file nào bị copy hoặc ghi đè.", "OK");
        }

        private void SetVisibleSelection(bool value) { for (int i = 0; i < rows.Count; i++) if (IsVisible(rows[i])) rows[i].Selected = value; }
        private bool IsVisible(AssetDiffRow row)
        {
            if (showOnlyChanges && row.Kind == "") return false;
            if (row.Kind == "Changed" && !showChanged) return false;
            if (row.Kind == "Missing current" && !showMissingCurrent) return false;
            if (row.Kind == "Only current" && !showMissingReference) return false;
            return string.IsNullOrEmpty(filter) || row.RelativePath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        private static string ProjectRoot() { return Directory.GetParent(Application.dataPath).FullName; }
        private static bool SameContent(string first, string second)
        {
            if (!File.Exists(first) || !File.Exists(second)) return false;
            FileInfo a = new FileInfo(first); FileInfo b = new FileInfo(second);
            if (a.Length != b.Length) return false;
            using (SHA256 sha = SHA256.Create())
            using (FileStream left = File.OpenRead(first))
            using (FileStream right = File.OpenRead(second))
            {
                byte[] leftHash = sha.ComputeHash(left); sha.Initialize(); byte[] rightHash = sha.ComputeHash(right);
                for (int i = 0; i < leftHash.Length; i++) if (leftHash[i] != rightHash[i]) return false;
            }
            return true;
        }
        private static void PingCurrent(AssetDiffRow row)
        {
            string assetPath = ToAssetPath(row.CurrentPath);
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset != null) EditorGUIUtility.PingObject(asset);
        }

        private static string ToAssetPath(string absolutePath)
        {
            string projectRoot = ProjectRoot().Replace('\\', '/').TrimEnd('/') + "/";
            string normalized = absolutePath.Replace('\\', '/');
            return normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(projectRoot.Length)
                : normalized;
        }
    }
}
#endif
