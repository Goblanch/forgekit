using Forgekit.Core.Domain;
using Spectre.Console;

namespace Forgekit.Cli.Rendering;

public sealed class ConsoleReportRenderer
{
    private readonly IAnsiConsole _console;

    public ConsoleReportRenderer(IAnsiConsole console) => _console = console;

    /// <param name="report">The findings to list,</param>
    /// <param name="unfiltered">The full report when <paramref name="report"/> was filtrered by severity. The summary
    /// counts it, so hidden findings are never silently dropped.
    /// </param>
    public void Render(AuditReport report, AuditReport? unfiltered = null)
    {
        var totals = unfiltered ?? report;
        var hidden = totals.Findings.Count - report.Findings.Count;

        _console.MarkupLine($"[bold]forgekit assetaudit[/] — {Markup.Escape(report.ProjectPath)}");
        _console.WriteLine();

        if (totals.Findings.Count == 0)
        {
            _console.MarkupLine("[green]No findings.[/]");
            _console.WriteLine();
        }

        var groups = report.Findings
            .GroupBy(f => (f.Severity, f.Category))
            .OrderByDescending(g => g.Key.Severity)
            .ThenBy(g => g.Key.Category);

        foreach (var group in groups)
        {
            var color = ColorOf(group.Key.Severity);
            var label = group.Key.Severity.ToString().ToUpperInvariant();

            _console.MarkupLine($"[{color} bold][[{label}]][/] [bold]{group.Key.Category}[/] ({group.Count()})");

            var messages = group.Select(f => f.Message).Distinct().ToList();
            var sharedMessage = messages.Count == 1 ? messages[0] : null;
            if (sharedMessage is not null)
                _console.MarkupLine($"  [grey]{Markup.Escape(sharedMessage)}[/]");

            foreach (var finding in group)
            {
                var paths = string.Join(" == ", PathsOf(finding).Select(Markup.Escape));
                var suffix = sharedMessage is null
                    ? $"[grey]— {Markup.Escape(finding.Message)}[/]"
                    : string.Empty;
                _console.MarkupLine($"    {paths}{suffix}");
            }

            _console.WriteLine();
        }

        _console.MarkupLine(
            $"Summary: [{CountColor(totals.CountOf(Severity.Error), "red")}]{totals.CountOf(Severity.Error)} errors[/], " +
            $"[{CountColor(totals.CountOf(Severity.Warning), "yellow")}]{totals.CountOf(Severity.Warning)} warnings[/], " +
            $"[{CountColor(totals.CountOf(Severity.Info), "aqua")}]{totals.CountOf(Severity.Info)} info[/] — " +
            $"{totals.Findings.Count} findings across {totals.TotalAssetsScanned} assets scanned.");

        if (hidden > 0)
            _console.MarkupLine($"[grey]{hidden} finding(s) hidden by --severity-threshold.[/]");
    }

    private static string ColorOf(Severity severity) => severity switch
    {
        Severity.Error => "red",
        Severity.Warning => "yellow",
        _ => "aqua"
    };

    private static string CountColor(int count, string color) => count == 0 ? "grey" : color;

    private static IEnumerable<string> PathsOf(Finding finding) =>
        finding.Category == FindingCategory.DuplicateAsset
        && finding.Details.TryGetValue("paths", out var paths)
            ? paths.Split(';')
            : new[] { finding.AssetPath };
}