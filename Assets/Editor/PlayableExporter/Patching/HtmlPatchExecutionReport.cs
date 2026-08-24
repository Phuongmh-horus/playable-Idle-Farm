using System.Collections.Generic;

internal sealed class HtmlPatchExecutionReport
{
    public readonly List<HtmlPatchOperationResult> operations = new List<HtmlPatchOperationResult>();

    public bool HasMissingRequiredOperations()
    {
        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];
            if (operation.required && !operation.applied)
            {
                return true;
            }
        }

        return false;
    }

    public string BuildFailureMessage()
    {
        var missing = new List<string>();
        for (var index = 0; index < operations.Count; index++)
        {
            var operation = operations[index];
            if (operation.required && !operation.applied)
            {
                missing.Add(operation.name);
            }
        }

        return missing.Count == 0
            ? string.Empty
            : "Required Luna HTML patches were not applied: " + string.Join(", ", missing) + ".";
    }
}
