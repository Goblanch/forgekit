using Forgekit.Core.Domain;
using Forgekit.Core.Parsing;

namespace Forgekit.Core.Scanning;

public sealed class TextureImportReader
{
    private readonly UnityYamlParser _parser = new();

    public IReadOnlyDictionary<Guid, TextureImportInfo> Read(
        IReadOnlyList<AssetRecord> assets, string projectRoot)
    {
        var result = new Dictionary<Guid, TextureImportInfo>();

        foreach (var asset in assets.Where(a => a.AssetType == AssetType.Texture))
        {
            var meta = _parser.ParseMeta(Path.Combine(projectRoot, asset.Path) + ".meta");
            var importer = meta.TextureImporter;
            if (importer is null) continue;

            var platform = importer.PlatformSettings?
                .FirstOrDefault(p => p.BuildTarget == "DefaultTexturePlatform");

            var maxSize = platform?.MaxTextureSize ?? importer.MaxTextureSize ?? 0;
            var compressed = (platform?.TextureCompression ?? 0) != 0;

            result[asset.Guid] = new TextureImportInfo(asset.Guid, maxSize, compressed);
        }

        return result;
    }
}