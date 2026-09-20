using Forgekit.Core.Parsing;
using Xunit;

namespace Forgekit.Core.Tests;

public class UnityYamlParserMetaTests
{
    private readonly UnityYamlParser _parser = new();

    [Fact]
    public void ParseMeta_HeroMaterial_ReturnsExpectedGuid()
    {
        var metaPath = FixturePaths.Of("Assets", "Materials", "HeroMaterial.mat.meta");

        var result = _parser.ParseMeta(metaPath);

        Assert.Equal("985979071f4d750350b3eab4c6302abb", result.Guid);
    }

    [Fact]
    public void ParseMeta_UncompressedTexture_ReportsUnoptimizedSettings()
    {
        var metaPath = FixturePaths.Of("Assets", "Textures", "hero_diffuse.png.meta");

        var result = _parser.ParseMeta(metaPath);

        Assert.NotNull(result.TextureImporter);
        var defaultPlatform = result.TextureImporter!.PlatformSettings!
            .Single(p => p.BuildTarget == "DefaultTexturePlatform");

        Assert.Equal(4096, defaultPlatform.MaxTextureSize);
        Assert.Equal(0, defaultPlatform.TextureCompression);
    }

    [Fact]
    public void ParseMeta_CompressedTexture_ReportsOptimizedSettings()
    {
        var metaPath = FixturePaths.Of("Assets", "Textures", "icon_compressed.png.meta");

        var result = _parser.ParseMeta(metaPath);

        var defaultPlatform = result.TextureImporter!.PlatformSettings!
            .Single(p => p.BuildTarget == "DefaultTexturePlatform");

        Assert.Equal(128, defaultPlatform.MaxTextureSize);
        Assert.NotEqual(0, defaultPlatform.TextureCompression);
    }

    [Fact]
    public void ParseMeta_PrefabMeta_HasNoTextureImporterSection()
    {
        var metaPath = FixturePaths.Of("Assets", "Prefabs", "HeroPrefab.prefab.meta");

        var result = _parser.ParseMeta(metaPath);

        Assert.Null(result.TextureImporter);
    }
}