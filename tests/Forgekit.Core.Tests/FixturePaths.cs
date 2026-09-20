namespace Forgekit.Core.Tests;

internal static class FixturePaths
{
    private static readonly string Root = Path.Combine(
        AppContext.BaseDirectory, "Fixtures", "SampleUnityProject"
    );

    public static string Of(params string[] relativeSegments) =>
        Path.Combine(new[] { Root }.Concat(relativeSegments).ToArray());
}