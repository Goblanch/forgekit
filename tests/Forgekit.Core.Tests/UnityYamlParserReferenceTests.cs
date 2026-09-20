using Forgekit.Core.Parsing;
using Xunit;

namespace Forgekit.Core.Tests;

public class UnityYamlParserReferenceTests
{
    private readonly UnityYamlParser _parser = new();

    [Fact]
    public void ExtractReferencedGuids_HeroMaterial_ContainsTextureGuid()
    {
        var matPath = FixturePaths.Of("Assets", "Materials", "HeroMaterial.mat");

        var guids = _parser.ExtractReferencedGuids(matPath);

        Assert.Contains(Guid.Parse("1fb81bbffba5e53a9da84b90221a129d"), guids);
    }

    [Fact]
    public void ExtractReferencedGuids_HeroPrefab_ContainsMaterialGuid()
    {
        var prefabPath = FixturePaths.Of("Assets", "Prefabs", "HeroPrefab.prefab");

        var guids = _parser.ExtractReferencedGuids(prefabPath);

        Assert.Contains(Guid.Parse("985979071f4d750350b3eab4c6302abb"), guids);
    }

    [Fact]
    public void ExtractReferencedGuids_PrefabVariant_ContainsBasePrefabGuid()
    {
        var variantPath = FixturePaths.Of("Assets", "Prefabs", "HeroPrefab_Variant.prefab");

        var guids = _parser.ExtractReferencedGuids(variantPath);

        Assert.Contains(Guid.Parse("a0aae538467ca7c3255b3e326f6af907"), guids);
    }

    [Fact]
    public void ExtractReferencedGuids_OrphanAsset_ReturnsEmptySet()
    {
        var orphanPath = FixturePaths.Of("Assets", "Prefabs", "OrphanDebugMarker.prefab");

        var guids = _parser.ExtractReferencedGuids(orphanPath);

        Assert.Empty(guids);
    }

    [Fact]
    public void ExtractReferencedGuids_MainScene_ContainsVariantGuid()
    {
        var scenePath = FixturePaths.Of("Assets", "Scenes", "MainScene.unity");

        var guids = _parser.ExtractReferencedGuids(scenePath);

        Assert.Contains(Guid.Parse("b83dd0af413d43a71c20cba7518b61bb"), guids);
    }
}