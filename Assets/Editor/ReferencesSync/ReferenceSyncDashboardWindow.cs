#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PlayableAd.Editor
{
    public sealed class ReferenceSyncDashboardWindow : EditorWindow
    {
        internal const string NewFilesSessionKey = "PLA.ReferenceSync.NewFiles";
        private readonly List<string> changedFiles = new List<string>();
        private Vector2 scroll;
        private string excludeInput = "";
        private string status = "Chưa kiểm tra.";

        [MenuItem("REFERENCE SYNC/Dashboard", false, 0)]
        public static void Open()
        {
            ReferenceSyncDashboardWindow window = GetWindow<ReferenceSyncDashboardWindow>("Reference Sync");
            window.minSize = new Vector2(560f, 500f);
            window.Show();
            window.RefreshStatus();
        }

        private void OnEnable() { EditorApplication.delayCall += RefreshStatus; }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("REFERENCE SYNC", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Mirror script vào staging, audit feature, hoặc tạo asset migration plan. Tool không ghi đè asset/.meta trong playable project.", MessageType.Info);
            DrawConfiguration();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Kiểm tra")) RefreshStatus();
                if (GUILayout.Button("Sync script reference")) OriginalGameReferenceSyncTool.SyncReferenceCode();
                if (GUILayout.Button("Git history")) OriginalGameReferenceSyncTool.ShowGitLog();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Feature audit + prompt")) ReferenceFeatureAuditPopup.Open();
                if (GUILayout.Button("Asset diff + plan")) ReferenceAssetDiffExperimentalWindow.Open();
                if (GUILayout.Button("Open latest AI prompt")) OpenLatestAiPrompt();
            }
            EditorGUILayout.HelpBox(status, changedFiles.Count > 0 ? MessageType.Warning : MessageType.Info);
            DrawChangedFiles();
            DrawExcludes();
        }

        private void DrawConfiguration()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Cấu hình local", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                string sourceRoot = EditorGUILayout.TextField("Game gốc", ReferenceSyncConfiguration.SourceRoot);
                string sourceScripts = EditorGUILayout.TextField("Script folder", ReferenceSyncConfiguration.SourceScriptsRelativePath);
                string mirrorFolder = EditorGUILayout.TextField("Mirror staging", ReferenceSyncConfiguration.MirrorFolder);
                if (EditorGUI.EndChangeCheck())
                {
                    ReferenceSyncConfiguration.SourceRoot = sourceRoot;
                    ReferenceSyncConfiguration.SourceScriptsRelativePath = sourceScripts;
                    ReferenceSyncConfiguration.MirrorFolder = mirrorFolder;
                    RefreshStatus();
                }
                EditorGUILayout.SelectableLabel("Source: " + ReferenceSyncConfiguration.SourceScriptsPath, EditorStyles.miniLabel, GUILayout.Height(18));
                EditorGUILayout.SelectableLabel("Staging: " + ReferenceSyncConfiguration.MirrorPath, EditorStyles.miniLabel, GUILayout.Height(18));
            }
        }

        private void DrawChangedFiles()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Script cần cập nhật: " + changedFiles.Count, EditorStyles.boldLabel);
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(120));
                for (int i = 0; i < changedFiles.Count; i++) EditorGUILayout.LabelField(changedFiles[i], EditorStyles.miniLabel);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawExcludes()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Exclude rules", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    excludeInput = EditorGUILayout.TextField(excludeInput);
                    if (GUILayout.Button("Add", GUILayout.Width(55)) && ReferenceSyncExcludes.Add(excludeInput)) { excludeInput = ""; RefreshStatus(); }
                }
                List<string> rules = ReferenceSyncExcludes.Rules;
                for (int i = 0; i < rules.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(rules[i], EditorStyles.miniLabel);
                        if (GUILayout.Button("Remove", GUILayout.Width(65))) { ReferenceSyncExcludes.RemoveAt(i); RefreshStatus(); break; }
                    }
                }
            }
        }

        private void RefreshStatus()
        {
            changedFiles.Clear();
            string source = ReferenceSyncConfiguration.SourceScriptsPath;
            if (!Directory.Exists(source)) { status = "Không tìm thấy script folder của game gốc."; Repaint(); return; }
            string mirror = ReferenceSyncConfiguration.MirrorPath;
            string[] files = Directory.GetFiles(source, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string relative = files[i].Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!ReferenceSyncExcludes.IsExcluded(relative) && !ReferenceSyncConfiguration.SameContent(files[i], Path.Combine(mirror, relative))) changedFiles.Add(relative);
            }
            status = changedFiles.Count == 0 ? "Mirror đồng nhất." : changedFiles.Count + " script cần sync.";
            Repaint();
        }

        private static void OpenLatestAiPrompt()
        {
            string path = ReferenceSyncConfiguration.LatestAiPromptPath;
            if (File.Exists(path)) EditorUtility.OpenWithDefaultApp(path);
            else EditorUtility.DisplayDialog("Reference Sync", "Chưa có asset migration run. Tạo migration plan trước.", "OK");
        }
    }

    internal sealed class ReferenceFeatureAuditPopup : EditorWindow
    {
        private static readonly Regex TypeRegex = new Regex(@"\b(class|interface|struct|enum)\s+([A-Za-z_]\w*)", RegexOptions.Compiled);
        private static readonly Regex MethodRegex = new Regex(@"\b(public|protected|internal|private)\s+(?:static\s+|virtual\s+|override\s+|abstract\s+|async\s+)*(?:[A-Za-z_]\w*(?:\s*<[^>]+>)?|void)\s+([A-Za-z_]\w*)\s*\(", RegexOptions.Compiled);
        private readonly List<Row> rows = new List<Row>();
        private Vector2 scroll;
        private string filter = "";
        private string status = "Chưa quét.";

        private sealed class Symbol { public string name, file; public readonly HashSet<string> methods = new HashSet<string>(StringComparer.Ordinal); }
        private sealed class Row { public Symbol reference, candidate; public string status; public bool selected; }

        public static void Open()
        {
            ReferenceFeatureAuditPopup window = GetWindow<ReferenceFeatureAuditPopup>("Feature Audit");
            window.minSize = new Vector2(760f, 480f);
            window.Show();
            window.Scan();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("FEATURE AUDIT", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Heuristic chọn feature cần AI review; method overlap không chứng minh hai implementation tương đương.", MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                filter = EditorGUILayout.TextField("Filter", filter);
                if (GUILayout.Button("Rescan", GUILayout.Width(70))) Scan();
                if (GUILayout.Button("Select actionable", GUILayout.Width(115))) for (int i = 0; i < rows.Count; i++) rows[i].selected = rows[i].status != "Exact method set";
                if (GUILayout.Button("Create AI prompt", GUILayout.Width(115))) CreatePrompt();
            }
            EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < rows.Count; i++) DrawRow(rows[i]);
            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(Row row)
        {
            if (!string.IsNullOrEmpty(filter) && row.reference.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) return;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                row.selected = EditorGUILayout.Toggle(row.selected, GUILayout.Width(18));
                EditorGUILayout.LabelField(row.reference.name, GUILayout.Width(180));
                EditorGUILayout.LabelField(row.status, GUILayout.Width(125));
                EditorGUILayout.LabelField(row.candidate == null ? "(none)" : row.candidate.name, GUILayout.Width(180));
                EditorGUILayout.LabelField(row.reference.methods.Count + " ref / " + (row.candidate == null ? 0 : row.candidate.methods.Count) + " target", EditorStyles.miniLabel);
            }
        }

        private void Scan()
        {
            rows.Clear();
            Dictionary<string, Symbol> reference = ScanFolder(ReferenceSyncConfiguration.MirrorPath);
            Dictionary<string, Symbol> target = ScanFolder(Path.Combine(ReferenceSyncConfiguration.ProjectRoot, "Assets", "_Game", "Scripts"));
            foreach (KeyValuePair<string, Symbol> pair in reference)
            {
                Symbol candidate;
                if (!target.TryGetValue(pair.Key, out candidate)) candidate = FindCandidate(pair.Value, target);
                rows.Add(new Row { reference = pair.Value, candidate = candidate, status = Status(pair.Value, candidate) });
            }
            rows.Sort((a, b) => string.Compare(a.reference.name, b.reference.name, StringComparison.OrdinalIgnoreCase));
            status = "Đã quét " + rows.Count + " symbol.";
            Repaint();
        }

        private static Dictionary<string, Symbol> ScanFolder(string folder)
        {
            Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>(StringComparer.Ordinal);
            if (!Directory.Exists(folder)) return symbols;
            string[] files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string relative = files[i].Substring(folder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (ReferenceSyncExcludes.IsExcluded(relative)) continue;
                string text = File.ReadAllText(files[i]);
                MatchCollection types = TypeRegex.Matches(text);
                MatchCollection methods = MethodRegex.Matches(text);
                for (int typeIndex = 0; typeIndex < types.Count; typeIndex++)
                {
                    string name = types[typeIndex].Groups[2].Value;
                    Symbol symbol;
                    if (!symbols.TryGetValue(name, out symbol)) { symbol = new Symbol { name = name, file = files[i].Replace('\\', '/') }; symbols.Add(name, symbol); }
                    int start = types[typeIndex].Index;
                    int end = typeIndex + 1 < types.Count ? types[typeIndex + 1].Index : text.Length;
                    for (int methodIndex = 0; methodIndex < methods.Count; methodIndex++) if (methods[methodIndex].Index > start && methods[methodIndex].Index < end) symbol.methods.Add(methods[methodIndex].Groups[2].Value);
                }
            }
            return symbols;
        }

        private static Symbol FindCandidate(Symbol reference, Dictionary<string, Symbol> target)
        {
            Symbol best = null;
            int bestScore = 0;
            foreach (KeyValuePair<string, Symbol> pair in target)
            {
                int score = 0;
                foreach (string method in reference.methods) if (pair.Value.methods.Contains(method)) score++;
                if (score > bestScore) { best = pair.Value; bestScore = score; }
            }
            return best;
        }

        private static string Status(Symbol reference, Symbol candidate)
        {
            if (candidate == null) return "Missing";
            if (!string.Equals(reference.name, candidate.name, StringComparison.Ordinal)) return "Name differs";
            return reference.methods.SetEquals(candidate.methods) ? "Exact method set" : "Method mismatch";
        }

        private void CreatePrompt()
        {
            List<Row> selected = new List<Row>();
            for (int i = 0; i < rows.Count; i++) if (rows[i].selected) selected.Add(rows[i]);
            if (selected.Count == 0) { EditorUtility.DisplayDialog("Feature Audit", "Chọn ít nhất một feature trước.", "OK"); return; }
            StringBuilder prompt = new StringBuilder();
            prompt.AppendLine("# Playable migration review");
            prompt.AppendLine("Return JSON: decision (port/defer/rebuild), confidence, dependencies, incompatible APIs, minimal patch plan, Unity validation steps.");
            prompt.AppendLine("Never copy source blindly. Exclude backend, analytics, ads, IAP, network, and full app lifecycle unless required for playable flow.");
            prompt.AppendLine("| Feature | Reference | Candidate | Status | Methods |");
            prompt.AppendLine("|---|---|---|---|---|");
            for (int i = 0; i < selected.Count; i++)
            {
                Row row = selected[i];
                prompt.AppendLine("| `" + row.reference.name + "` | `" + row.reference.file + "` | `" + (row.candidate == null ? "(none)" : row.candidate.file) + "` | " + row.status + " | " + string.Join(", ", new List<string>(row.reference.methods).ToArray()) + " |");
            }
            string folder = Path.Combine(ReferenceSyncConfiguration.ProjectRoot, "Library", "ReferenceSync");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "feature-prompt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".md");
            string text = prompt.ToString();
            File.WriteAllText(path, text, Encoding.UTF8);
            GUIUtility.systemCopyBuffer = text;
            status = "Đã tạo prompt: " + path;
        }
    }

    internal static class ReferenceSyncExcludes
    {
        private const string PreferencesKey = "PLA.ReferenceSync.ExcludeRules";
        private static readonly List<string> rules = new List<string>(EditorPrefs.GetString(PreferencesKey, "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        internal static List<string> Rules { get { return rules; } }
        internal static bool Add(string rule) { string normalized = Normalize(rule); if (normalized.Length == 0 || rules.Contains(normalized)) return false; rules.Add(normalized); Save(); return true; }
        internal static void RemoveAt(int index) { if (index >= 0 && index < rules.Count) { rules.RemoveAt(index); Save(); } }
        internal static bool IsExcluded(string relativePath)
        {
            string path = Normalize(relativePath);
            for (int i = 0; i < rules.Count; i++) if (path == rules[i] || path.StartsWith(rules[i] + "/", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(path).Equals(rules[i], StringComparison.OrdinalIgnoreCase) || Path.GetFileNameWithoutExtension(path).Equals(rules[i], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static string Normalize(string value) { return (value ?? "").Replace('\\', '/').Trim().Trim('/').ToLowerInvariant(); }
        private static void Save() { EditorPrefs.SetString(PreferencesKey, string.Join(";", rules.ToArray())); }
    }
}
#endif
