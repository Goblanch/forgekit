using System.Security.Cryptography;
using Forgekit.Core.Domain;
using Forgekit.Core.Parsing;

namespace Forgekit.Core.Scanning;

public sealed class AssetScanner
{
    private static readonly HashSet<string> YamlPareseableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".unity", ".prefab", ".asset", ".mat", ".controller"
    };

    private readonly UnityYamlParser _parser = new();

    public IReadOnlyList<AssetRecord> ScanAssets(string projectRoot)
    {
        var assetsDir = Path.Combine(projectRoot, "Assets");
        var records = new List<AssetRecord>();

        foreach (var filePath in Directory.EnumerateFiles(assetsDir, "*", SearchOption.AllDirectories))
        {
            if (filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;

            var metaPath = filePath + ".meta";
            if (!File.Exists(metaPath))
                continue;

            var meta = _parser.ParseMeta(metaPath);
            if (!Guid.TryParse(meta.Guid, out var guid))
                continue;

            var relativePath = Path.GetRelativePath(projectRoot, filePath).Replace('\\', '/');

            records.Add(new AssetRecord(
                Guid: guid,
                Path: relativePath,
                AssetType: DetermineAssetType(filePath),
                FileSizeBytes: new FileInfo(filePath).Length,
                ContentHash: ComputeContentHash(filePath)
            ));
        }

        return records;
    }

    public static bool IsYamlParsable(string filePath) =>
        YamlPareseableExtensions.Contains(Path.GetExtension(filePath));

    private static AssetType DetermineAssetType(string filePath) =>
            Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".prefab" => AssetType.Prefab,
                ".unity" => AssetType.Scene,
                ".mat" => AssetType.Material,
                ".png" or ".jpg" or ".jpeg" or ".tga" or ".psd" or ".exr" => AssetType.Texture,
                _ => AssetType.Other
            };

    private static string ComputeContentHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(stream));
    }


}
