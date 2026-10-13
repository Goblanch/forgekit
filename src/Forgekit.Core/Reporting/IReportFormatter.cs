using Forgekit.Core.Domain;

namespace Forgekit.Core.Reporting;

public interface IReportFormatter
{
    string Format(AuditReport report);
}