using Forgekit.Core.Domain;

namespace Forgekit.Core.Auditing;

public sealed class OrphanRule : IAuditRule
{
    public IEnumerable<Finding> Evaluate(AuditContext context)
    {
        foreach (var asset in context.Assets)
        {
            if (context.ReachableGuids.Contains(asset.Guid))
                continue;

            if (IsExcluded(asset.Path, context.Options))
                continue;

            yield return new Finding(
                FindingCategory.OrphanAsset,
                Severity.Warning,
                asset.Path,
                "Asset is not referenced by any scene reachable from Build Settings.",
                new Dictionary<string, string> { ["guid"] = asset.Guid.ToString("N") });
        }
    }

    private static bool IsExcluded(string path, AuditOptions options) =>
        options.ExcludePathFragments.Any(f =>
            path.Contains(f, StringComparison.OrdinalIgnoreCase));
}