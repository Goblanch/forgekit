using System.Text.Json;
using Forgekit.Core.Domain;
using Forgekit.Core.Reporting;
using Xunit;

namespace Forgekit.Core.Tests;

public class ReportFormatterTests
{
    private static AuditReport SampleReport() => new()
    {
        ProjectPath = "./MyGame",
        Timestamp = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero),
        TotalAssetsScanned = 842,
        Findings = new[]
        {
            new Finding(FindingCategory.OrphanAsset, Severity.Warning, "Assets/old_logo.png",
                "Not referenced.", new Dictionary<string, string> { ["guid"] = "abc" }),
            new Finding(FindingCategory.DuplicateAsset, Severity.Info, "Assets/a.png",
                "2 files share identical content.",
                new Dictionary<string, string> { ["paths"] = "Assets/a.png;Assets/b.png" })
        }
    };

    private static AuditReport EmptyReport() => new()
    {
        ProjectPath = "./Empty",
        Timestamp = DateTimeOffset.UnixEpoch,
        TotalAssetsScanned = 3,
        Findings = Array.Empty<Finding>()
    };

    // ---------- JSON ----------

    [Fact]
    public void Json_SummaryIncludesZeroCounts_AndTotalAssets()
    {
        var json = new JsonReportFormatter().Format(SampleReport());

        using var doc = JsonDocument.Parse(json);
        var summary = doc.RootElement.GetProperty("summary");

        Assert.Equal(0, summary.GetProperty("error").GetInt32());
        Assert.Equal(1, summary.GetProperty("warning").GetInt32());
        Assert.Equal(1, summary.GetProperty("info").GetInt32());
        Assert.Equal(842, summary.GetProperty("totalAssetsScanned").GetInt32());
    }

    [Fact]
    public void Json_SerializesEnumsAsText_WithCamelCaseNames()
    {
        var json = new JsonReportFormatter().Format(SampleReport());

        using var doc = JsonDocument.Parse(json);
        var first = doc.RootElement.GetProperty("findings")[0];

        Assert.Equal("OrphanAsset", first.GetProperty("category").GetString());
        Assert.Equal("Warning", first.GetProperty("severity").GetString());
        Assert.Equal("Assets/old_logo.png", first.GetProperty("assetPath").GetString());
        Assert.Equal("abc", first.GetProperty("details").GetProperty("guid").GetString());
    }

    [Fact]
    public void Json_DoesNotEscapePlusSign()
    {
        var json = new JsonReportFormatter().Format(SampleReport());

        Assert.DoesNotContain("\\u002B", json);
    }

    // ---------- Markdown ----------

    [Fact]
    public void Markdown_StartsWithHeaderAndSummaryLine()
    {
        var md = new MarkdownReportFormatter().Format(SampleReport());

        Assert.StartsWith("# forgekit assetaudit - ./MyGame\n", md);
        Assert.Contains("**Assets scanned:** 842 · **Errors:** 0 · **Warnings:** 1 · **Info:** 1", md);
    }

    [Fact]
    public void Markdown_ListsMoreSevereGroupsFirst()
    {
        var md = new MarkdownReportFormatter().Format(SampleReport());

        var warning = md.IndexOf("## Warning — OrphanAsset (1)", StringComparison.Ordinal);
        var info = md.IndexOf("## Info — DuplicateAsset (1)", StringComparison.Ordinal);

        Assert.True(warning >= 0 && info >= 0);
        Assert.True(warning < info);
    }

    [Fact]
    public void Markdown_ListsAllPathsOfADuplicateGroup()
    {
        var md = new MarkdownReportFormatter().Format(SampleReport());

        Assert.Contains("| `Assets/a.png`<br>`Assets/b.png` |", md);
    }

    [Fact]
    public void Markdown_EscapesPipes_AndUsesOnlyLineFeeds()
    {
        var report = new AuditReport
        {
            ProjectPath = "./P",
            Timestamp = DateTimeOffset.UnixEpoch,
            TotalAssetsScanned = 1,
            Findings = new[]
            {
                new Finding(FindingCategory.OrphanAsset, Severity.Warning, "Assets/we|rd.png",
                    "a|b", new Dictionary<string, string>())
            }
        };

        var md = new MarkdownReportFormatter().Format(report);

        Assert.Contains("`Assets/we\\|rd.png`", md);
        Assert.Contains("a\\|b", md);
        Assert.DoesNotContain("\r", md);
    }

    [Fact]
    public void Markdown_EmptyReport_SaysNoFindings()
    {
        var md = new MarkdownReportFormatter().Format(EmptyReport());

        Assert.Contains("No findings.", md);
        Assert.DoesNotContain("##", md);
    }
}