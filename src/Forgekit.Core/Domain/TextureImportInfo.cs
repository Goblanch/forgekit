namespace Forgekit.Core.Domain;

public sealed record TextureImportInfo(
    Guid Guid,
    int MaxSize,
    bool CompressionEnabled
);