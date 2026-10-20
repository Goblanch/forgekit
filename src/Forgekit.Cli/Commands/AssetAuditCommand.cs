using System.CommandLine;
using Forgekit.Core.Auditing;
using Forgekit.Core.Domain;
using Spectre.Console;

namespace Forgekit.Cli.Commands;

public static class AssetAuditCommand
{
    public static Command Create()
    {
        Option<string> project = new("--project")
        {
            Description = "Path to the Unity project root (the folder containing Assets/ and ProjectSettings/).",
            Required = true
        };

        Option<string?> output = new("--output")
        {
            Description = "File to write the report to (json or markdown formats only)."
        };

        Option<string?> format = new("--format")
        {
            Description = "console, json or markdown. Inferred from the --output extension when omitted; defaults to console."
        };

        Option<int> maxTextureSize = new("--max-texture-size")
        {
            Description = "Textures with a larger max size are reported.",
            DefaultValueFactory = _ => new AuditOptions().MaxTextureSize
        };

        Option<string[]> exclude = new("--exclude")
        {
            Description = "Path fragment of assets to ignore in all checks, e.g. Assets/ThirdParty/. Ignored assets still count as references. Repeatable."
        };

        Option<string?> threshold = new("--severity-threshold")
        {
            Description = "Only show findings at or above this level: info, warning or error. Defaults to info."
        };

        Option<string?> failOn = new("--fail-on")
        {
            Description = "Exit with code 1 if any finding is at or above this level: warning or error."
        };

        Command command = new(
            "assetaudit",
            "Audit a Unity project's assets: orphans, unoptimized textures and duplicates.")
        {
            project, output, format, maxTextureSize, exclude, threshold, failOn
        };

        command.SetAction(parseResult =>
        {
            var request = new AssetAuditRequest(
                parseResult.GetValue(project) ?? string.Empty,
                parseResult.GetValue(output),
                parseResult.GetValue(format),
                parseResult.GetValue(maxTextureSize),
                parseResult.GetValue(exclude) ?? Array.Empty<string>(),
                parseResult.GetValue(threshold),
                parseResult.GetValue(failOn));

            return new AssetAuditRunner(AnsiConsole.Console, Console.Out, Console.Error).Run(request);
        });

        return command;
    }
}