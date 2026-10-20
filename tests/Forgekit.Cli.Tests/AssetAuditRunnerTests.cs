using System.Text.Json;
using Forgekit.Cli.Commands;
using Forgekit.Core.Tests;
using Spectre.Console.Testing;
using Xunit;

namespace Forgekit.Cli.Tests;

public class AssetAuditRunnerTests
{
    private static AssetAuditRequest Request(
        string? format = null,
        string? output = null,
        string? threshold = null,
        string? failOn = null,
        int maxTextureSize = 2048,
        string[]? excludes = null,
        string? project = null) =>
        new(project ?? FixturePaths.Of(), output, format, maxTextureSize,
            excludes ?? Array.Empty<string>(), threshold, failOn);

    private static (int Code, string Stdout, string Stderr, string Rendered) Run(AssetAuditRequest request)
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var code = new AssetAuditRunner(console, stdout, stderr).Run(request);

        return (code, stdout.ToString(), stderr.ToString(), console.Output);
    }

    [Fact]
    public void Run_ConsoleFormat_ShowsFindings_AndSucceeds()
    {
        var result = Run(Request());

        Assert.Equal(ExitCode.Success, result.Code);
        Assert.Contains("[WARNING] OrphanAsset (3)", result.Rendered);
        Assert.Contains("[INFO] DuplicateAsset (1)", result.Rendered);
        Assert.Contains("Summary:", result.Rendered);
    }

    [Fact]
    public void Run_FailOnWarning_ReturnsFindingsExceeded()
    {
        Assert.Equal(ExitCode.FindingExceeded, Run(Request(failOn: "warning")).Code);
    }

    [Fact]
    public void Run_FailOnError_Succeeds_WhenThereAreNoErrors()
    {
        Assert.Equal(ExitCode.Success, Run(Request(failOn: "error")).Code);
    }

    [Fact]
    public void Run_JsonFormat_WritesValidJsonToStdout()
    {
        var result = Run(Request(format: "json"));

        Assert.Equal(ExitCode.Success, result.Code);
        using var doc = JsonDocument.Parse(result.Stdout);
        Assert.Equal(8, doc.RootElement.GetProperty("summary").GetProperty("totalAssetsScanned").GetInt32());
        Assert.Equal(5, doc.RootElement.GetProperty("findings").GetArrayLength());
    }

    [Fact]
    public void Run_OutputFile_InfersMarkdownFromExtension_AndCreatesDirectories()
    {
        var dir = Path.Combine(Path.GetTempPath(), "forgekit-tests-" + Guid.NewGuid().ToString("N"));
        var file = Path.Combine(dir, "sub", "report.md");

        try
        {
            var result = Run(Request(output: file));

            Assert.Equal(ExitCode.Success, result.Code);
            Assert.StartsWith("# forgekit assetaudit", File.ReadAllText(file));
            Assert.Contains("Report written to", result.Stdout);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Run_SeverityThresholdError_HidesWarnings_ButKeepsAssetCount()
    {
        var result = Run(Request(format: "json", threshold: "error"));

        using var doc = JsonDocument.Parse(result.Stdout);
        Assert.Equal(0, doc.RootElement.GetProperty("findings").GetArrayLength());
        Assert.Equal(8, doc.RootElement.GetProperty("summary").GetProperty("totalAssetsScanned").GetInt32());
    }

    [Fact]
    public void Run_ExcludeFragment_RemovesMatchingOrphans()
    {
        var result = Run(Request(format: "json", excludes: new[] { "Assets/Prefabs/" }));

        using var doc = JsonDocument.Parse(result.Stdout);
        var orphans = doc.RootElement.GetProperty("findings").EnumerateArray()
            .Count(f => f.GetProperty("category").GetString() == "OrphanAsset");
        Assert.Equal(2, orphans);
    }

    [Fact]
    public void Run_InvalidProject_ReturnsUsageError()
    {
        var result = Run(Request(project: Path.Combine(Path.GetTempPath(), "no-such-project")));

        Assert.Equal(ExitCode.UsageError, result.Code);
        Assert.Contains("is not a Unity project root", result.Stderr);
    }

    [Theory]
    [InlineData("xml", null, null, null, 2048)]
    [InlineData("console", "report.txt", null, null, 2048)]
    [InlineData(null, null, null, "info", 2048)]
    [InlineData(null, null, "fatal", null, 2048)]
    [InlineData(null, null, null, null, 0)]
    public void Run_InvalidArguments_ReturnUsageError(
        string? format, string? output, string? threshold, string? failOn, int maxTextureSize)
    {
        var result = Run(Request(format, output, threshold, failOn, maxTextureSize));

        Assert.Equal(ExitCode.UsageError, result.Code);
        Assert.StartsWith("Error:", result.Stderr);
    }

    [Fact]
    public void Run_ExcludeFragment_AppliesToEveryRule()
    {
        var result = Run(Request(format: "json", excludes: new[] { "/Deprecated/" }));

        using var doc = JsonDocument.Parse(result.Stdout);
        var categories = doc.RootElement.GetProperty("findings").EnumerateArray()
            .Select(f => f.GetProperty("category").GetString())
            .ToList();
        Assert.DoesNotContain("DuplicateAsset", categories);
        Assert.Equal(2, categories.Count(c => c == "OrphanAsset"));
    }

    [Fact]
    public void Run_OutputExtensionDoesNotMatchFormat_ReturnsUsageError_AndLeavesTheFileAlone()
    {
        var file = Path.Combine(Path.GetTempPath(), "forgekit-tests-" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllText(file, "original");

        try
        {
            var result = Run(Request(format: "json", output: file));

            Assert.Equal(ExitCode.UsageError, result.Code);
            Assert.Contains("does not match the json format", result.Stderr);
            Assert.Equal("original", File.ReadAllText(file));
        }
        finally
        {
            File.Delete(file);
        }
    }
}