using Forgekit.Core.Domain;
using Forgekit.Core.Graph;
using Forgekit.Core.Scanning;

namespace Forgekit.Core.Auditing;

public sealed class AssetAuditor
{
    private readonly IReadOnlyList<IAuditRule> _rules;

    public AssetAuditor()
        : this(new IAuditRule[] { new OrphanRule(), new TextureImportRule(), new DuplicateRule() })
    {
    }

    public AssetAuditor(IReadOnlyList<IAuditRule> rules) => _rules = rules;

    public AuditReport Run(string projectRoot, AuditOptions? options = null)
    {
        options ??= new AuditOptions();

        var assets = new AssetScanner().ScanAssets(projectRoot);

        var roots = new HashSet<Guid>(new BuildSettingsReader().ReadEnabledSceneGuids(projectRoot));
        roots.UnionWith(new ProjectSettingsReader().ReadReferencedGuids(projectRoot));
        roots.UnionWith(assets
            .Where(a => options.IsExcludedFromOrphanCheck(a.Path))
            .Select(a => a.Guid));

        var reachable = new ReferenceGraphBuilder().Build(assets, projectRoot).ReachableFrom(roots);
        var textures = new TextureImportReader().Read(assets, projectRoot);

        var audited = assets.Where(a => !options.IsExcluded(a.Path)).ToList();
        var context = new AuditContext(audited, reachable, textures, options);

        var findings = _rules
            .SelectMany(rule => rule.Evaluate(context))
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.Category)
            .ThenBy(f => f.AssetPath, StringComparer.Ordinal)
            .ToList();

        return new AuditReport
        {
            ProjectPath = projectRoot,
            Timestamp = DateTimeOffset.UtcNow,
            Findings = findings,
            TotalAssetsScanned = assets.Count
        };
    }
}