using Forgekit.Core.Parsing;

namespace Forgekit.Core.Scanning;

public sealed class ProjectSettingsReader
{
    private readonly UnityYamlParser _parser = new();

    /// <summary>
    /// GUIDs referenced from ProjectSettings/*.asset (e.g. the URP asset in GraphicsSettings).
    /// EditorBuildSettings is skipped: it also lists disabled scenes.
    /// </summary>
    /// <param name="projectRoot"></param>
    /// <returns></returns>
    public IReadOnlySet<Guid> ReadReferencedGuids(string projectRoot)
    {
        var guids = new HashSet<Guid>();
        var dir = Path.Combine(projectRoot, "ProjectSettings");
        if (!Directory.Exists(dir))
            return guids;

        foreach (var file in Directory.EnumerateFiles(dir, "*.asset"))
        {
            if (Path.GetFileName(file).Equals("EditorBuildSettings.asset", StringComparison.OrdinalIgnoreCase))
                continue;

            guids.UnionWith(_parser.ExtractReferencedGuids(file));
        }

        return guids;
    }
}