namespace Forgekit.Core.Auditing;

public sealed record AuditOptions
{
    public int MaxTextureSize { get; init; } = 2048;

    public IReadOnlyList<string> ExcludePathFragments { get; init; } =
        new[] { "/Resources/", "/Addressables/" };
}