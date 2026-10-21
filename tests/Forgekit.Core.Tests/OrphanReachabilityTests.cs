using Forgekit.Core.Auditing;
using Forgekit.Core.Domain;
using Xunit;

namespace Forgekit.Core.Tests;

public sealed class OrphanReachabilityTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "forgekit-" + Guid.NewGuid().ToString("N"));

    private static string G() => Guid.NewGuid().ToString("N");
    private static string Ref(string guid) => $"{{fileID: 1, guid: {guid}, type: 2}}";

    private void Write(string relativePath, string guid, string content = "")
    {
        var full = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        File.WriteAllText(full + ".meta", $"fileFormatVersion: 2\nguid: {guid}\n");
    }

    private void WriteSettings(string name, string content)
    {
        var dir = Path.Combine(_root, "ProjectSettings");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, name), content);
    }

    [Fact]
    public void Run_FindsOnlyRealOrphans()
    {
        var scene = G(); var sceneMat = G();
        var resPrefab = G(); var resTexture = G();
        var urp = G(); var fbx = G(); var fbxMat = G();
        var orphanPng = G(); var script = G();

        Write("Assets/Main.unity", scene, $"m_Material: {Ref(sceneMat)}\nm_Mesh: {Ref(fbx)}");
        Write("Assets/Mat/Scene.mat", sceneMat);
        Write("Assets/Resources/P.prefab", resPrefab, $"m_Texture: {Ref(resTexture)}");
        Write("Assets/Tex/FromResources.png", resTexture);
        Write("Assets/Settings/Urp.asset", urp);
        Write("Assets/Models/Thing.fbx", fbx);
        File.WriteAllText(Path.Combine(_root, "Assets/Models/Thing.fbx.meta"),
            $"fileFormatVersion: 2\nguid: {fbx}\nexternalObjects:\n  material: {Ref(fbxMat)}\n");
        Write("Assets/Models/Thing.mat", fbxMat);
        Write("Assets/Tex/Unused.png", orphanPng);
        Write("Assets/Code/Player.cs", script);

        WriteSettings("EditorBuildSettings.asset",
            $"EditorBuildSettings:\n  m_Scenes:\n  - enabled: 1\n    path: Assets/Main.unity\n    guid: {scene}\n");
        WriteSettings("GraphicsSettings.asset", $"m_CustomRenderPipeline: {Ref(urp)}");

        var report = new AssetAuditor().Run(_root);

        var orphans = report.Findings
            .Where(f => f.Category == FindingCategory.OrphanAsset)
            .Select(f => f.AssetPath)
            .ToList();

        // fbx is used by the scene and its material is only linked from the fbx .meta
        Assert.Equal(new[] { "Assets/Tex/Unused.png" }, orphans);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}