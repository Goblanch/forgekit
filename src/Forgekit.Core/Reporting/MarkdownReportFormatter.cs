using System.Text;
using Forgekit.Core.Domain;

namespace Forgekit.Core.Reporting;

public sealed class MarkdownReportFormatter : IReportFormatter
{
    public string Format(AuditReport report)
    {
        var sb = new StringBuilder();
        void Line(string text = "") => sb.Append(text).Append('\n');

        Line($"# forgekit assetaudit - {report.ProjectPath}");
        Line();
        Line($"**Assets scanned:** {report.TotalAssetsScanned} · " +
             $"**Errors:** {report.CountOf(Severity.Error)} · " +
             $"**Warnings:** {report.CountOf(Severity.Warning)} · " +
             $"**Info:** {report.CountOf(Severity.Info)}");
        Line();

        if (report.Findings.Count == 0)
        {
            Line("No findings.");
            return sb.ToString();
        }

        var groups = report.Findings
            .GroupBy(f => (f.Severity, f.Category))
            .OrderByDescending(g => g.Key.Severity)
            .ThenBy(g => g.Key.Category);

        foreach (var group in groups)
        {
            Line($"## {group.Key.Severity} — {group.Key.Category} ({group.Count()})");
            Line();
            Line("| Asset | Message |");
            Line("|---|---|");

            foreach (var finding in group)
                Line($"| {AssetCell(finding)} | {Escape(finding.Message)} |");

            Line();
        }

        return sb.ToString();
    }

    private static string AssetCell(Finding finding) =>
        finding.Category == FindingCategory.DuplicateAsset
        && finding.Details.TryGetValue("paths", out var paths)
            ? string.Join("<br>", paths.Split(';').Select(Code))
            : Code(finding.AssetPath);

    private static string Code(string text) => $"`{Escape(text)}`";

    private static string Escape(string text) => text.Replace("|", "\\|");
}