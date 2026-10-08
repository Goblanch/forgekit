using Forgekit.Core.Domain;

namespace Forgekit.Core.Auditing;

public interface IAuditRule
{
    IEnumerable<Finding> Evaluate(AuditContext context);
}