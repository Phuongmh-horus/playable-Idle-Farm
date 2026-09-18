#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace PlayableAd.Editor
{
    /// <summary>Single orchestration boundary for a read-only migration run.</summary>
    internal static class ReferenceMigrationPipeline
    {
        internal static void FinalizePlan(ReferenceAssetMigrationPlan plan)
        {
            plan.runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            ReferenceAssetDependencyAnalyzer.Populate(plan);
            List<ReferenceMigrationCheck> checks = Validate(plan);
            plan.checks = checks.ToArray();
            bool hasErrors = false;
            for (int i = 0; i < checks.Count; i++) if (checks[i].severity == "error") { hasErrors = true; break; }
            plan.readiness = hasErrors ? "blocked_needs_mapping" : "ready_for_staging_review";
            Persist(plan);
        }

        private static List<ReferenceMigrationCheck> Validate(ReferenceAssetMigrationPlan plan)
        {
            List<ReferenceMigrationCheck> checks = new List<ReferenceMigrationCheck>();
            if (plan.items == null || plan.items.Length == 0) checks.Add(Check("error", "empty_plan", "", "No root assets were selected."));
            if (!Directory.Exists(plan.sourceRoot)) checks.Add(Check("error", "source_missing", plan.sourceRoot, "The source root is unavailable."));
            if (plan.unresolvedGuids != null && plan.unresolvedGuids.Length > 0) checks.Add(Check("error", "unresolved_guid", "", plan.unresolvedGuids.Length + " GUID references need an explicit mapping."));
            if (plan.dependencies != null)
            {
                for (int i = 0; i < plan.dependencies.Length; i++)
                {
                    ReferenceAssetDependency dependency = plan.dependencies[i];
                    string sourcePath = Path.Combine(ReferenceSyncConfiguration.SourceRoot, dependency.relativePath.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(sourcePath)) checks.Add(Check("error", "dependency_missing", dependency.relativePath, "Dependency disappeared after analysis."));
                }
            }
            for (int i = 0; i < plan.items.Length; i++)
            {
                ReferenceAssetMigrationItem item = plan.items[i];
                string source = Path.Combine(plan.sourceRoot, item.relativePath);
                string target = Path.Combine(plan.targetRoot, item.relativePath);
                if (!File.Exists(source)) checks.Add(Check("error", "root_missing", item.relativePath, "Selected source asset is unavailable."));
                else if (!string.Equals(ReferenceSyncConfiguration.HashFile(source), item.sourceHash, StringComparison.Ordinal)) checks.Add(Check("error", "source_changed", item.relativePath, "Source changed while the plan was being built; rescan it."));
                if (File.Exists(target) && !ReferenceSyncConfiguration.SameContent(source, target)) checks.Add(Check("warning", "target_conflict", item.relativePath, "Target already has different content; staging merge and review are required."));
            }
            if (plan.warnings != null)
            {
                for (int i = 0; i < plan.warnings.Length; i++)
                {
                    string warning = plan.warnings[i];
                    bool truncated = warning.StartsWith("Dependency closure reached", StringComparison.Ordinal);
                    checks.Add(Check(truncated ? "error" : "warning", truncated ? "closure_truncated" : "analysis_warning", "", warning));
                }
            }
            return checks;
        }

        private static ReferenceMigrationCheck Check(string severity, string code, string path, string message)
        {
            return new ReferenceMigrationCheck { severity = severity, code = code, path = path, message = message };
        }

        private static void Persist(ReferenceAssetMigrationPlan plan)
        {
            string runFolder = Path.Combine(ReferenceSyncConfiguration.RunsPath, plan.runId);
            Directory.CreateDirectory(runFolder);
            plan.planPath = Path.Combine(runFolder, "migration-plan.json");
            plan.aiPromptPath = Path.Combine(runFolder, "ai-review-prompt.md");
            string prompt = BuildAiPrompt(plan);
            string serializedPlan = JsonUtility.ToJson(plan, true);
            File.WriteAllText(plan.aiPromptPath, prompt, Encoding.UTF8);
            File.WriteAllText(plan.planPath, serializedPlan, Encoding.UTF8);
            Directory.CreateDirectory(Path.GetDirectoryName(ReferenceSyncConfiguration.LatestPlanPath));
            File.WriteAllText(ReferenceSyncConfiguration.LatestAiPromptPath, prompt, Encoding.UTF8);
            File.WriteAllText(ReferenceSyncConfiguration.LatestPlanPath, serializedPlan, Encoding.UTF8);
        }

        private static string BuildAiPrompt(ReferenceAssetMigrationPlan plan)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine("# Playable asset migration review");
            output.AppendLine("Run: `" + plan.runId + "`  ");
            output.AppendLine("Readiness: `" + plan.readiness + "`");
            output.AppendLine();
            output.AppendLine("Return JSON: decision (port/defer/rebuild), confidence, required mappings, incompatible dependencies, minimal conversion steps, and Unity validation steps.");
            output.AppendLine("Do not copy .meta, overwrite target assets, or use proprietary/backend/ads/network code without an explicit mapping.");
            output.AppendLine();
            output.AppendLine("## Roots");
            for (int i = 0; i < plan.items.Length; i++) output.AppendLine("- `" + plan.items[i].relativePath + "` — " + plan.items[i].kind);
            output.AppendLine("## Dependencies");
            for (int i = 0; plan.dependencies != null && i < plan.dependencies.Length; i++) output.AppendLine("- `" + plan.dependencies[i].relativePath + "` (" + plan.dependencies[i].guid + ")");
            output.AppendLine("## Checks");
            for (int i = 0; plan.checks != null && i < plan.checks.Length; i++) output.AppendLine("- [" + plan.checks[i].severity + "] " + plan.checks[i].code + ": " + plan.checks[i].message);
            return output.ToString();
        }
    }
}
#endif
