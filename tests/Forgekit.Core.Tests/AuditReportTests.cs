using Forgekit.Core.Domain;
using Xunit;

namespace Forgekit.Core.Tests;

public class AuditReportTests
{
    private static Finding FindingOf(Severity severity) =>
        new(FindingCategory.OrphanAsset, severity, "Assets/x.png", "m", new Dictionary<string, string>());

    [Fact]
    public void WithMinimumSeverity_KeepsOnlyFindingsAtOrAboveLevel()
    {
        var report = new AuditReport
        {
            ProjectPath = "p",
            Timestamp = DateTimeOffset.UnixEpoch,
            TotalAssetsScanned = 5,
            Findings = new[] { FindingOf(Severity.Info), FindingOf(Severity.Warning), FindingOf(Severity.Error) }
        };

        var filtered = report.WithMinimumSeverity(Severity.Warning);

        Assert.Equal(new[] { Severity.Warning, Severity.Error }, filtered.Findings.Select(f => f.Severity));
        Assert.Equal(5, filtered.TotalAssetsScanned);
    }
}