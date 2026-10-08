using Forgekit.Core.Domain;

namespace Forgekit.Core.Auditing;

public sealed record AuditContext(
    IReadOnlyList<AssetRecord> Assets,
    IReadOnlySet<Guid> ReachableGuids,
    IReadOnlyDictionary<Guid, TextureImportInfo> Textures,
    AuditOptions Options);