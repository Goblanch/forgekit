using System.Text.RegularExpressions;

namespace Forgekit.Core.Scanning;

public sealed partial class BuildSettingsReader
{
    [GeneratedRegex(@"-\s*enabled:\s*1\s*\r?\n\s*path:.*\r?\n\s*guid:\s*([0-9a-fA-F]{32})")]
    private static partial Regex EnabledSceneEntryPattern();

    public IReadOnlySet<Guid> ReadEnabledSceneGuids(string projectRoot)
    {
        var path = Path.Combine(projectRoot, "ProjectSettings", "EditorBuildSettings.asset");
        var text = File.ReadAllText(path);

        var guids = new HashSet<Guid>();
        foreach (Match match in EnabledSceneEntryPattern().Matches(text))
        {
            if (Guid.TryParse(match.Groups[1].Value, out var guid))
                guids.Add(guid);
        }

        return guids;
    }
}