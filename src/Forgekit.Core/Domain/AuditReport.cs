namespace Forgekit.Core.Domain;

public sealed class AuditReport
{
    public required string ProjectPath { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required IReadOnlyList<Finding> Findings { get; init; }
    public required int TotalAssetsScanned { get; init; }

    public IReadOnlyDictionary<Severity, int> SummaryBySeverity =>
        Findings
            .GroupBy(f => f.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

    public int CountOf(Severity severity) => Findings.Count(f => f.Severity == severity);
}