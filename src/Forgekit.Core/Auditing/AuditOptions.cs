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

    /// <summary>
    /// Extensions never judged as orphans: code, shaders and docs are not referenced
    /// by GUID from scenes/prefabs, so "not reachable" says nothing about them.
    /// </summary>
    public IReadOnlyList<string> OrphanSkipExtensions { get; init; } = new[]
    {
        ".cs", ".asmdef", ".asmref", ".dll",
        ".shader", ".cginc", ".hlsl", ".compute", ".shadergraph", ".shadersubgraph",
        ".inputactions", ".txt", ".md", ".pdf", ".json", ".wlt"
    };

    public bool IsExcluded(string path) => Matches(path, ExcludePathFragments);

    public bool IsExcludedFromOrphanCheck(string path) => Matches(path, OrphanExcludePathFragments);

    public static bool Matches(string path, IReadOnlyList<string> fragments) =>
        fragments.Any(f => path.Contains(f, StringComparison.OrdinalIgnoreCase));

    public bool IsSkippedFromOrphanCheck(string path) =>
        OrphanSkipExtensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase));
}