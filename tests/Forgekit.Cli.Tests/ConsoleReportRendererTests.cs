using Forgekit.Cli.Rendering;
using Forgekit.Core.Domain;
using Spectre.Console.Testing;
using Xunit;

namespace Forgekit.Cli.Tests;

public class ConsoleReportRendererTests
{
    private static string Render(AuditReport report)
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        new ConsoleReportRenderer(console).Render(report);
        return console.Output;
    }

    private static AuditReport ReportOf(params Finding[] findings) => new()
    {
        ProjectPath = "./MyGame",
        Timestamp = DateTimeOffset.UnixEpoch,
        TotalAssetsScanned = 10,
        Findings = findings
    };

    private static Finding Orphan(string path) =>
        new(FindingCategory.OrphanAsset, Severity.Warning, path, "Not referenced.",
            new Dictionary<string, string>());

    [Fact]
    public void Render_ShowsHeaderAndSeverityLabelledGroups()
    {
        var output = Render(ReportOf(Orphan("Assets/a.png"), Orphan("Assets/b.png")));

        Assert.Contains("forgekit assetaudit — ./MyGame", output);
        Assert.Contains("[WARNING] OrphanAsset (2)", output);
        Assert.Contains("Assets/a.png", output);
        Assert.Contains("Assets/b.png", output);
    }

    [Fact]
    public void Render_JoinsDuplicatePathsWithDoubleEquals()
    {
        var duplicate = new Finding(
            FindingCategory.DuplicateAsset, Severity.Info, "Assets/a.png",
            "2 files share identical content.",
            new Dictionary<string, string> { ["paths"] = "Assets/a.png;Assets/b.png" });

        var output = Render(ReportOf(duplicate));

        Assert.Contains("Assets/a.png == Assets/b.png", output);
    }

    [Fact]
    public void Render_PathsWithBrackets_DoNotBreakMarkup()
    {
        var output = Render(ReportOf(Orphan("Assets/[WIP]/x.png")));

        Assert.Contains("Assets/[WIP]/x.png", output);
    }

    [Fact]
    public void Render_EmptyReport_SaysNoFindings()
    {
        var output = Render(ReportOf());

        Assert.Contains("No findings.", output);
    }

    [Fact]
    public void Render_EndsWithSummaryLine()
    {
        var output = Render(ReportOf(Orphan("Assets/a.png")));

        Assert.Contains("Summary: 0 errors, 1 warnings, 0 info — 1 findings across 10 assets scanned.", output);
    }

    [Fact]
    public void Render_WithHiddenFindings_SummaryCountsEverything_AndMentionsHidden()
    {
        var full = ReportOf(Orphan("Assets/a.png"), Orphan("Assets/b.png"));
        var console = new TestConsole();
        console.Profile.Width = 200;

        new ConsoleReportRenderer(console).Render(ReportOf(), full);

        Assert.Contains("Summary: 0 errors, 2 warnings, 0 info — 2 findings across 10 assets scanned.", console.Output);
        Assert.Contains("2 finding(s) hidden by --severity-threshold.", console.Output);
        Assert.DoesNotContain("No findings.", console.Output);
    }
}