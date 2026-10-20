namespace Forgekit.Core.Auditing;

public sealed record AuditOptions
{
    public int MaxTextureSize { get; init; } = 2048;

    /// <summary>
    /// Fragments of assets to ignore in every rule (from --exclude)
    /// </summary>
    public IReadOnlyList<string> ExcludePathFragments { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Fragments excluded only from orphan detection: assets loaded by string at runtime
    /// never appear in the reference graph.
    /// </summary>
    public IReadOnlyList<string> OrphanExcludePathFragments { get; init; } =
        new[] { "/Resources/", "/Addressables/" };

    public bool IsExcluded(string path) => Matches(path, ExcludePathFragments);

    public bool IsExcludedFromOrphanCheck(string path) => Matches(path, OrphanExcludePathFragments);

    public static bool Matches(string path, IReadOnlyList<string> fragments) =>
        fragments.Any(f => path.Contains(f, StringComparison.OrdinalIgnoreCase));
}