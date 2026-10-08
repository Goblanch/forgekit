namespace Forgekit.Core.Domain;

public sealed record AssetRecord(
    Guid Guid,
    string Path,
    AssetType AssetType,
    long FileSizeBytes,
    string ContentHash
);