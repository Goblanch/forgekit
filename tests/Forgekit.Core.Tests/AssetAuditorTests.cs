using Forgekit.Core.Auditing;
using Forgekit.Core.Domain;
using Xunit;

namespace Forgekit.Core.Tests;

public class AssetAuditorTests
{
    [Fact]
    public void Run_OnSampleProject_ProducesExpectedFindings()
    {
        var report = new AssetAuditor().Run(FixturePaths.Of());

        Assert.Equal(8, report.TotalAssetsScanned);
        Assert.Equal(3, report.Findings.Count(f => f.Category == FindingCategory.OrphanAsset));
        Assert.Equal(1, report.Findings.Count(f => f.Category == FindingCategory.UnoptimizedTexture));
        Assert.Equal(1, report.Findings.Count(f => f.Category == FindingCategory.DuplicateAsset));
        Assert.Equal(4, report.SummaryBySeverity[Severity.Warning]);
        Assert.Equal(1, report.SummaryBySeverity[Severity.Info]);
    }

    [Fact]
    public void Run_FlagsHeroDiffuseAsUnoptimized()
    {
        var report = new AssetAuditor().Run(FixturePaths.Of());

        var finding = Assert.Single(report.Findings, f => f.Category == FindingCategory.UnoptimizedTexture);
        Assert.Equal("Assets/Textures/hero_diffuse.png", finding.AssetPath);
        Assert.Equal(Severity.Warning, finding.Severity);
    }
}