namespace Forgekit.Core.Domain;

public sealed record Finding(
    FindingCategory Category,
    Severity Severity,
    string AssetPath,
    string Message,
    IReadOnlyDictionary<string, string> Details
);