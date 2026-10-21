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

            if (context.Options.IsExcludedFromOrphanCheck(asset.Path)
                || context.Options.IsSkippedFromOrphanCheck(asset.Path))
                continue;

            yield return new Finding(
                FindingCategory.OrphanAsset,
                Severity.Warning,
                asset.Path,
                "Asset is not referenced by any scene reachable from Build Settings.",
                new Dictionary<string, string> { ["guid"] = asset.Guid.ToString("N") });
        }
    }
}