using Forgekit.Core.Domain;

namespace Forgekit.Core.Auditing;

public sealed class DuplicateRule : IAuditRule
{
    public IEnumerable<Finding> Evaluate(AuditContext context)
    {
        var duplicateGroups = context.Assets
            .Where(a => a.FileSizeBytes > 0)
            .GroupBy(a => a.ContentHash)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            var paths = group
                .Select(a => a.Path)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            yield return new Finding(
                FindingCategory.DuplicateAsset,
                Severity.Info,
                paths[0],
                $"{paths.Count} files share identical content.",
                new Dictionary<string, string>
                {
                    ["contentHash"] = group.Key,
                    ["paths"] = string.Join(";", paths)
                }
            );
        }
    }
}