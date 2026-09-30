#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PlayableAd.Editor
{
    /// <summary>
    /// Công cụ đồng bộ mã nguồn tham chiếu từ game gốc (Original Game Reference Sync Tool).
    /// - Sao chép toàn bộ mã nguồn Assets/_Game/Scripts từ E:\real-war sang GameplayScripts_Reference.
    /// - Hiển thị 15 Git commit mới nhất của game gốc để theo dõi các cập nhật physics, mechanics mới.
    /// - Phục vụ quy trình Daily Sync cho dev/QA cập nhật logic Playable Ads theo game gốc.
    /// </summary>
    public static class OriginalGameReferenceSyncTool
    {
        [MenuItem("Tools/Reference Sync/🔄 Sync Script Reference From Original Game", priority = 10)]
        public static void SyncReferenceCode()
        {
            string sourcePath = ReferenceSyncConfiguration.SourceScriptsPath;
            string targetPath = ReferenceSyncConfiguration.MirrorPath;

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError($"<color=red>[ReferenceSync] ❌ Không tìm thấy thư mục game gốc tại: {sourcePath}</color>");
                return;
            }

            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            int copiedFiles = 0;
            int skippedFiles = 0;
            int excludedFiles = 0;
            List<string> newlyCreatedFiles = new List<string>();
            List<ReferenceSyncManifestFile> manifestFiles = new List<ReferenceSyncManifestFile>();

            try
            {
                EditorUtility.DisplayProgressBar("Sync Reference Code", "Đang đồng bộ mã nguồn từ game gốc...", 0.1f);

                // Quét toàn bộ file .cs trong thư mục game gốc
                string[] allSourceFiles = Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories);
                int total = allSourceFiles.Length;

                for (int i = 0; i < total; i++)
                {
                    string srcFile = allSourceFiles[i];
                    string relative = srcFile.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (ReferenceSyncExcludes.IsExcluded(relative))
                    {
                        skippedFiles++;
                        excludedFiles++;
                        continue;
                    }
                    string destFile = Path.Combine(targetPath, relative);

                    string destDir = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    bool shouldCopy = !ReferenceSyncConfiguration.SameContent(srcFile, destFile);

                    if (shouldCopy)
                    {
                        if (!File.Exists(destFile)) newlyCreatedFiles.Add(relative.Replace('\\', '/'));
                        File.Copy(srcFile, destFile, true);
                        copiedFiles++;
                    }
                    else skippedFiles++;

                    manifestFiles.Add(new ReferenceSyncManifestFile
                    {
                        relativePath = relative.Replace('\\', '/'),
                        sourceHash = ReferenceSyncConfiguration.HashFile(srcFile),
                        action = shouldCopy ? "copied" : "unchanged"
                    });

                    float progress = (float)(i + 1) / total;
                    EditorUtility.DisplayProgressBar("Sync Reference Code", $"Đang sao chép: {relative}", progress);
                }

                AssetDatabase.Refresh();
                SessionState.SetString(ReferenceSyncDashboardWindow.NewFilesSessionKey, string.Join("\n", newlyCreatedFiles.ToArray()));

                string gitLog = GetOriginalGameGitLog(5);
                WriteManifest(sourcePath, gitLog, copiedFiles, skippedFiles, excludedFiles, manifestFiles);
                Debug.Log($"<color=green><b>[ReferenceSync] ✅ ĐỒNG BỘ THÀNH CÔNG!</b></color>\n" +
                          $"📁 Nguồn: <i>{sourcePath}</i>\n" +
                          $"📂 Đích: <i>{targetPath}</i>\n" +
                          $"📊 Tổng số: <b>{total}</b> file .cs | Cập nhật mới: <b>{copiedFiles}</b> | Giữ nguyên: <b>{skippedFiles}</b>\n\n" +
                          $"<b>📜 5 Commit gần nhất của Game Gốc:</b>\n{gitLog}");

                string statusMsg = (copiedFiles > 0)
                    ? $"Đã đồng bộ {copiedFiles} file mã nguồn mới (trong tổng số {total} file) từ game gốc vào 'GameplayScripts_Reference/'."
                    : $"Toàn bộ {total} file mã nguồn trong 'GameplayScripts_Reference/' đã ở trạng thái MỚI NHẤT trùng khớp 100% với game gốc (không có file nào bị thay đổi).";

                EditorUtility.DisplayDialog(
                    "Đồng Bộ Reference Code Hoàn Tất",
                    $"{statusMsg}\n\nXem Console để theo dõi commit log chi tiết.",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red>[ReferenceSync] ❌ Lỗi trong quá trình đồng bộ: {ex.Message}</color>\n{ex.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/Reference Sync/📜 Show Original Game Git Commits", priority = 11)]
        public static void ShowGitLog()
        {
            string gitLog = GetOriginalGameGitLog(15);
            Debug.Log($"<b><color=cyan>[Original Game Git History (15 Commits)]</color></b>\n{gitLog}");
        }

        private static string GetOriginalGameGitLog(int count)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                Arguments = $"-C \"{ReferenceSyncConfiguration.SourceRoot}\" log -n {count} --oneline",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit(3000);
                        return string.IsNullOrEmpty(output) ? "(Không lấy được git log)" : output.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                return $"(Lỗi đọc git: {ex.Message})";
            }

            return "(Không tìm thấy git)";
        }

        private static void WriteManifest(string sourcePath, string gitLog, int copied, int unchanged, int excluded, List<ReferenceSyncManifestFile> files)
        {
            ReferenceSyncManifest manifest = new ReferenceSyncManifest
            {
                sourceRoot = sourcePath,
                sourceCommit = string.IsNullOrEmpty(gitLog) ? "unknown" : gitLog.Split('\n')[0],
                createdUtc = DateTime.UtcNow.ToString("o"),
                copiedCount = copied,
                unchangedCount = unchanged,
                excludedCount = excluded,
                files = files.ToArray()
            };
            string manifestPath = ReferenceSyncConfiguration.ManifestPath;
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
        }
    }
}
#endif
