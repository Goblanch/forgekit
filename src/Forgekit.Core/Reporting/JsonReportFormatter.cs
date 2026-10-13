using System.Text.Encodings.Web;
using System.Text.Json;
using Forgekit.Core.Domain;

namespace Forgekit.Core.Reporting;

public sealed class JsonReportFormatter : IReportFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string Format(AuditReport report)
    {
        var dto = new ReportDto(
            report.ProjectPath,
            report.Timestamp,
            new SummaryDto(
                report.CountOf(Severity.Error),
                report.CountOf(Severity.Warning),
                report.CountOf(Severity.Info),
                report.TotalAssetsScanned),
            report.Findings.Select(ToDto).ToList()
        );

        return JsonSerializer.Serialize(dto, Options);
    }

    private static FindingDto ToDto(Finding finding) => new(
        finding.Category.ToString(),
        finding.Severity.ToString(),
        finding.AssetPath,
        finding.Message,
        finding.Details
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToDictionary(kv => kv.Key, kv => kv.Value)
    );

    private sealed record ReportDto(
        string projectPath, DateTimeOffset TimeStamp, SummaryDto Summary, IReadOnlyList<FindingDto> Findings);

    private sealed record SummaryDto(int Error, int Warning, int Info, int TotalAssetsScanned);

    private sealed record FindingDto(
        string Category, string Severity, string AssetPath, string Message,
        IReadOnlyDictionary<string, string> Details);
}