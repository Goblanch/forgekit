using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Forgekit.Core.Parsing;

public sealed partial class UnityYamlParser
{
    [GeneratedRegex(@"guid:\s*([0-9a-fA-F]{32})")]
    private static partial Regex GuidPattern();

    private readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    public UnityMetaDocument ParseMeta(string metaFilePath)
    {
        var text = File.ReadAllText(metaFilePath);
        return _yamlDeserializer.Deserialize<UnityMetaDocument>(text)
            ?? throw new InvalidDataException($"No se puede parsear el .meta: {metaFilePath}");
    }

    public IReadOnlySet<Guid> ExtractReferencedGuids(string assetFilePath)
    {
        var text = File.ReadAllText(assetFilePath);
        var found = new HashSet<Guid>();

        foreach (Match match in GuidPattern().Matches(text))
        {
            if (Guid.TryParse(match.Groups[1].Value, out var guid))
                found.Add(guid);
        }

        return found;
    }
}