using YamlDotNet.Serialization;

namespace Forgekit.Core.Parsing;

public sealed class UnityMetaDocument
{
    [YamlMember(Alias = "guid")]
    public string Guid { get; set; } = String.Empty;

    [YamlMember(Alias = "TextureImporter")]
    public TextureImporterSection? TextureImporter { get; set; }

}

public sealed class TextureImporterSection
{
    [YamlMember(Alias = "maxTextureSize")]
    public int? MaxTextureSize { get; set; }

    [YamlMember(Alias = "platformSettings")]
    public List<PlatformSettings>? PlatformSettings { get; set; }
}

public sealed class PlatformSettings
{
    [YamlMember(Alias = "buildTarget")]
    public string? BuildTarget { get; set; }

    [YamlMember(Alias = "maxTextureSize")]
    public int? MaxTextureSize { get; set; }

    [YamlMember(Alias = "textureCompression")]
    public int? TextureCompression { get; set; }
}