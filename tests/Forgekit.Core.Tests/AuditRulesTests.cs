using Forgekit.Core.Auditing;
using Forgekit.Core.Domain;
using Xunit;

namespace Forgekit.Core.Tests;

public class AuditRulesTests
{
    private static AuditContext ContextOf(
        IReadOnlyList<AssetRecord> assets,
        IReadOnlyDictionary<Guid, TextureImportInfo>? textures = null,
        AuditOptions? options = null) =>
            new(assets, new HashSet<Guid>(), textures ?? new Dictionary<Guid, TextureImportInfo>(),
                options ?? new AuditOptions());

    [Fact]
    public void OrphanRule_SkipAssetsUnderResources()
    {
        var inResources = new AssetRecord(Guid.NewGuid(), "Assets/Resources/ui.png", AssetType.Texture, 10, "H1");
        var plain = new AssetRecord(Guid.NewGuid(), "Assets/Old/x.png", AssetType.Texture, 10, "H2");

        var findings = new OrphanRule().Evaluate(ContextOf(new[] { inResources, plain })).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal("Assets/Old/x.png", finding.AssetPath);
    }

    [Theory]
    [InlineData(4096, false, Severity.Warning)]
    [InlineData(4096, true, Severity.Info)]
    public void TextureImportRule_OverLimit_SeverityDependsOnCompression(
        int maxSize, bool compressed, Severity expected)
    {
        var asset = new AssetRecord(Guid.NewGuid(), "Assets/t.png", AssetType.Texture, 10, "H");
        var textures = new Dictionary<Guid, TextureImportInfo>
        {
            [asset.Guid] = new(asset.Guid, maxSize, compressed)
        };

        var findings = new TextureImportRule().Evaluate(ContextOf(new[] { asset }, textures)).ToList();

        Assert.Equal(expected, Assert.Single(findings).Severity);
    }

    [Fact]
    public void TextureImportRule_UnderLimit_ReturnsNothing()
    {
        var asset = new AssetRecord(Guid.NewGuid(), "Assets/t.png", AssetType.Texture, 10, "H");
        var textures = new Dictionary<Guid, TextureImportInfo>
        {
            [asset.Guid] = new(asset.Guid, 128, false)
        };

        Assert.Empty(new TextureImportRule().Evaluate(ContextOf(new[] { asset }, textures)));
    }

    [Fact]
    public void DuplicateRule_GroupsIdenticalContent_AndIgnoresEmptyFiles()
    {
        var a = new AssetRecord(Guid.NewGuid(), "Assets/a.png", AssetType.Texture, 10, "SAME");
        var b = new AssetRecord(Guid.NewGuid(), "Assets/b.png", AssetType.Texture, 10, "SAME");
        var empty1 = new AssetRecord(Guid.NewGuid(), "Assets/e1.txt", AssetType.Other, 0, "EMPTY");
        var empty2 = new AssetRecord(Guid.NewGuid(), "Assets/e2.txt", AssetType.Other, 0, "EMPTY");

        var findings = new DuplicateRule().Evaluate(ContextOf(new[] { a, b, empty1, empty2 })).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal("Assets/a.png", finding.AssetPath);
    }
}
