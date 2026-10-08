using Forgekit.Core.Domain;

namespace Forgekit.Core.Auditing;

public sealed class TextureImpoortRule : IAuditRule
{
    public IEnumerable<Finding> Evaluate(AuditContext context)
    {
        var limit = context.Options.MaxTextureSize;

        foreach (var asset in context.Assets)
        {
            if (!context.Textures.TryGetValue(asset.Guid, out var texture))
                continue;

            if (texture.MaxSize <= limit)
                continue;

            var severity = texture.CompressionEnabled ? Severity.Info : Severity.Warning;
            var state = texture.CompressionEnabled ? "compressed" : "uncompressed";

            yield return new Finding(
                FindingCategory.UnoptimizedTexture,
                severity,
                asset.Path,
                $"Texture max size {texture.MaxSize}px exceeds {limit}px ({state}).",
                new Dictionary<string, string>
                {
                    ["maxSize"] = texture.MaxSize.ToString(),
                    ["compressed"] = texture.CompressionEnabled.ToString()
                }
            );
        }
    }
}