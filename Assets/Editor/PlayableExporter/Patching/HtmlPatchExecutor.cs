using System;
using System.Collections.Generic;

internal static class HtmlPatchExecutor
{
    public static string Apply(string input, IEnumerable<HtmlPatchOperation> operations, List<string> patchesApplied, out HtmlPatchExecutionReport report)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (patchesApplied == null) throw new ArgumentNullException(nameof(patchesApplied));

        var output = input;
        report = new HtmlPatchExecutionReport();

        foreach (var operation in operations)
        {
            var result = new HtmlPatchOperationResult
            {
                name = operation.name ?? operation.patchDescription ?? operation.kind.ToString(),
                required = operation.required
            };

            switch (operation.kind)
            {
                case HtmlPatchKind.ReplaceRegex:
                    var replaced = operation.regex.Replace(output, _ => operation.replacement);
                    result.applied = !string.Equals(replaced, output, StringComparison.Ordinal);
                    output = replaced;
                    break;

                case HtmlPatchKind.InjectBefore:
                    var beforeIndex = output.IndexOf(operation.marker, StringComparison.OrdinalIgnoreCase);
                    if (beforeIndex >= 0)
                    {
                        output = output.Remove(beforeIndex, operation.marker.Length).Insert(beforeIndex, operation.replacement);
                        result.applied = true;
                    }
                    break;

                case HtmlPatchKind.InjectAfter:
                    var afterIndex = output.IndexOf(operation.marker, StringComparison.Ordinal);
                    if (afterIndex >= 0)
                    {
                        output = output.Insert(afterIndex + operation.marker.Length, operation.replacement);
                        result.applied = true;
                    }
                    break;
            }

            if (result.applied && !string.IsNullOrWhiteSpace(operation.patchDescription))
            {
                patchesApplied.Add(operation.patchDescription);
            }

            result.message = result.applied
                ? operation.patchDescription ?? result.name
                : $"Skipped patch '{result.name}' because its source anchor was not found.";
            report.operations.Add(result);
        }

        return output;
    }
}
