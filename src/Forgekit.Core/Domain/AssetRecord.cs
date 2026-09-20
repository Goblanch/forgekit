namespace Forgekit.Core.Domain;

public sealed record AssetRecord(
    Guid Guid,
    string path,
    AssetType AssetType,
    long FileSizeBytes,
    string ContentHash
);