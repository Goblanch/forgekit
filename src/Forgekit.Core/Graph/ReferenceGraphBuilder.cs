using Forgekit.Core.Domain;
using Forgekit.Core.Parsing;
using Forgekit.Core.Scanning;

namespace Forgekit.Core.Graph;

public sealed class ReferenceGraphBuilder
{
    private readonly UnityYamlParser _parser = new();

    public ReferenceGraph Build(IReadOnlyList<AssetRecord> assets, string projectRoot)
    {
        var graph = new ReferenceGraph();

        foreach (var asset in assets)
        {
            var absolutePath = Path.Combine(projectRoot, asset.Path);
            if (!AssetScanner.IsYamlParsable(absolutePath))
                continue;

            foreach (var referencedGuid in _parser.ExtractReferencedGuids(absolutePath))
            {
                graph.AddEdge(asset.Guid, referencedGuid);
            }
        }

        return graph;
    }
}