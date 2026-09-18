#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PlayableAd.Editor
{
    /// <summary>Builds a read-only dependency closure from Unity YAML GUID references.</summary>
    internal static class ReferenceAssetDependencyAnalyzer
    {
        private const int MaxDependencies = 500;
        private static readonly Regex GuidRegex = new Regex(@"guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);
        private static readonly HashSet<string> YamlExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".prefab", ".unity", ".mat", ".asset", ".controller", ".anim", ".spriteatlas"
        };

        internal static void Populate(ReferenceAssetMigrationPlan plan)
        {
            Dictionary<string, string> guidIndex = BuildGuidIndex();
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Queue<string> pending = new Queue<string>();
            List<ReferenceAssetDependency> dependencies = new List<ReferenceAssetDependency>();
            HashSet<string> unresolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> warnings = new List<string>();

            for (int i = 0; i < plan.items.Length; i++) pending.Enqueue(Path.Combine(plan.sourceRoot, plan.items[i].relativePath));

            while (pending.Count > 0 && dependencies.Count < MaxDependencies)
            {
                string assetPath = pending.Dequeue();
                if (!visited.Add(assetPath) || !CanReadYaml(assetPath)) continue;
                string assetText;
                try { assetText = File.ReadAllText(assetPath); }
                catch (Exception exception)
                {
                    warnings.Add("Could not inspect dependency references: " + ToSourceRelativePath(assetPath) + " (" + exception.GetType().Name + ").");
                    continue;
                }
                MatchCollection matches = GuidRegex.Matches(assetText);
                for (int i = 0; i < matches.Count; i++)
                {
                    string guid = matches[i].Groups[1].Value.ToLowerInvariant();
                    string dependencyPath;
                    if (!guidIndex.TryGetValue(guid, out dependencyPath))
                    {
                        unresolved.Add(guid);
                        continue;
                    }
                    if (!File.Exists(dependencyPath))
                    {
                        unresolved.Add(guid);
                        continue;
                    }
                    if (visited.Contains(dependencyPath)) continue;
                    dependencies.Add(new ReferenceAssetDependency
                    {
                        guid = guid,
                        relativePath = ToSourceRelativePath(dependencyPath),
                        sourceHash = ReferenceSyncConfiguration.HashFile(dependencyPath)
                    });
                    pending.Enqueue(dependencyPath);
                    if (dependencies.Count >= MaxDependencies) break;
                }
            }

            if (dependencies.Count >= MaxDependencies) warnings.Add("Dependency closure reached the safety limit of " + MaxDependencies + " assets.");
            if (unresolved.Count > 0) warnings.Add("Some GUIDs resolve outside the source project or package cache.");
            plan.dependencies = dependencies.ToArray();
            plan.unresolvedGuids = ToArray(unresolved);
            plan.warnings = warnings.ToArray();
            plan.readiness = unresolved.Count > 0 || dependencies.Count >= MaxDependencies ? "blocked_needs_mapping" : "ready_for_staging_review";
        }

        private static Dictionary<string, string> BuildGuidIndex()
        {
            Dictionary<string, string> index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string assetsRoot = Path.Combine(ReferenceSyncConfiguration.SourceRoot, "Assets");
            if (!Directory.Exists(assetsRoot)) return index;
            string[] metaFiles = Directory.GetFiles(assetsRoot, "*.meta", SearchOption.AllDirectories);
            for (int i = 0; i < metaFiles.Length; i++)
            {
                string text;
                try { text = File.ReadAllText(metaFiles[i]); }
                catch (Exception) { continue; }
                Match match = GuidRegex.Match(text);
                if (!match.Success) continue;
                index[match.Groups[1].Value.ToLowerInvariant()] = metaFiles[i].Substring(0, metaFiles[i].Length - ".meta".Length);
            }
            return index;
        }

        private static bool CanReadYaml(string path)
        {
            return File.Exists(path) && YamlExtensions.Contains(Path.GetExtension(path));
        }

        private static string ToSourceRelativePath(string path)
        {
            string root = ReferenceSyncConfiguration.SourceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path.Substring(root.Length).Replace('\\', '/') : path.Replace('\\', '/');
        }

        private static string[] ToArray(HashSet<string> values)
        {
            string[] output = new string[values.Count];
            values.CopyTo(output);
            Array.Sort(output, StringComparer.Ordinal);
            return output;
        }
    }
}
#endif
