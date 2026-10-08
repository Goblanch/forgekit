namespace Forgekit.Core.Graph;

public sealed class ReferenceGraph
{
    private readonly Dictionary<Guid, List<Guid>> _edges = new();

    public void AddEdge(Guid from, Guid to)
    {
        if (!_edges.TryGetValue(from, out var neighbors))
        {
            neighbors = new List<Guid>();
            _edges[from] = neighbors;
        }
        neighbors.Add(to);
    }

    public IReadOnlySet<Guid> ReachableFrom(IEnumerable<Guid> roots)
    {
        var visited = new HashSet<Guid>(roots);
        var queue = new Queue<Guid>(visited);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!_edges.TryGetValue(current, out var neighbors))
                continue;

            foreach (var neighbor in neighbors)
            {
                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        return visited;
    }
}