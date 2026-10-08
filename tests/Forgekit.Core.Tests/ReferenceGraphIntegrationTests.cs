using Forgekit.Core.Domain;
using Forgekit.Core.Graph;
using Forgekit.Core.Scanning;
using Xunit;

namespace Forgekit.Core.Tests;

public class ReferenceGraphIntegrationTests
{
    private static readonly string ProjectRoot = FixturePaths.Of();

    private static readonly Guid MainScene = Guid.Parse("a35c35e6bc89d312a9740a75141e9bb2");
    private static readonly Guid HeroPrefabVariant = Guid.Parse("b83dd0af413d43a71c20cba7518b61bb");
    private static readonly Guid HeroPrefab = Guid.Parse("a0aae538467ca7c3255b3e326f6af907");
    private static readonly Guid HeroMaterial = Guid.Parse("985979071f4d750350b3eab4c6302abb");
    private static readonly Guid HeroDiffuse = Guid.Parse("1fb81bbffba5e53a9da84b90221a129d");
    private static readonly Guid OrphanDebugMarker = Guid.Parse("9e46647fd3f1f517f44dcdd8051a9d93");

    private static (IReadOnlyList<AssetRecord> Assets, IReadOnlySet<Guid> Reachable) Analyze()
    {
        var assets = new AssetScanner().ScanAssets(ProjectRoot);
        var roots = new BuildSettingsReader().ReadEnabledSceneGuids(ProjectRoot);
        var graph = new ReferenceGraphBuilder().Build(assets, ProjectRoot);
        return (assets, graph.ReachableFrom(roots));
    }

    [Fact]
    public void ScanAssets_ReturnsEightAssets_ExcludingMetaFiles()
    {
        var assets = new AssetScanner().ScanAssets(ProjectRoot);

        Assert.Equal(8, assets.Count);
        Assert.DoesNotContain(assets, a => a.Path.EndsWith(".meta"));
    }

    [Fact]
    public void ReadEnabledSceneGuids_ReturnsOnlyMainScene()
    {
        var roots = new BuildSettingsReader().ReadEnabledSceneGuids(ProjectRoot);

        Assert.Single(roots);
        Assert.Contains(MainScene, roots);
    }

    [Fact]
    public void Reachable_IncludesFullChain_FromSceneToTexture()
    {
        var (_, reachable) = Analyze();

        Assert.Contains(MainScene, reachable);
        Assert.Contains(HeroPrefabVariant, reachable);
        Assert.Contains(HeroPrefab, reachable);
        Assert.Contains(HeroMaterial, reachable);
        Assert.Contains(HeroDiffuse, reachable);
    }

    [Fact]
    public void Reachable_ExcludesOrphanDebugMarker()
    {
        var (_, reachable) = Analyze();

        Assert.DoesNotContain(OrphanDebugMarker, reachable);
    }

    [Fact]
    public void UnreachableAssets_AreExactlyTheThreeUnreferencedOnes()
    {
        var (assets, reachable) = Analyze();

        var orphanPaths = assets
            .Where(a => !reachable.Contains(a.Guid))
            .Select(a => a.Path)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        var expected = new[]
        {
            "Assets/Prefabs/OrphanDebugMarker.prefab",
            "Assets/Textures/Deprecated/icon_compressed_old.png",
            "Assets/Textures/icon_compressed.png"
        };

        Assert.Equal(expected, orphanPaths);
    }
}