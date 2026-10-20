using System.Text;
using Forgekit.Cli.Rendering;
using Forgekit.Core.Auditing;
using Forgekit.Core.Domain;
using Forgekit.Core.Reporting;
using Spectre.Console;

namespace Forgekit.Cli.Commands;

public sealed record AssetAuditRequest(
    string Project,
    string? output,
    string? Format,
    int MaxTextureSize,
    IReadOnlyList<string> Excludes,
    string? SeverityThreshold,
    string? FailOn);

public sealed class AssetAuditRunner
{
    private readonly IAnsiConsole _console;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;

    public AssetAuditRunner(IAnsiConsole console, TextWriter stdout, TextWriter stderr)
    {
        _console = console;
        _stdout = stdout;
        _stderr = stderr;
    }

    public int Run(AssetAuditRequest request)
    {
        if (!TryValidate(request, out var settings, out var error))
        {
            _stderr.WriteLine($"Error: {error}");
            return ExitCode.UsageError;
        }

        try
        {
            var report = new AssetAuditor().Run(settings!.ProjectPath, settings.Options);
            Emit(settings, report.WithMinimumSeverity(settings.Threshold));

            var exceeded = settings.FailOn is { } failOn
                && report.Findings.Any(f => f.Severity >= failOn);

            return exceeded ? ExitCode.FindingExceeded : ExitCode.Success;
        }
        catch (Exception ex)
        {
            _stderr.WriteLine($"Error: {ex.Message}");
            return ExitCode.RuntimeError;
        }
    }

    private void Emit(Settings settings, AuditReport report)
    {
        if (settings.Format == OutputFormat.Console)
        {
            new ConsoleReportRenderer(_console).Render(report);
            return;
        }

        IReportFormatter formatter = settings.Format == OutputFormat.Json
            ? new JsonReportFormatter()
            : new MarkdownReportFormatter();

        var text = formatter.Format(report);
        if (!text.EndsWith('\n'))
            text += "\n";

        if (settings.OutputPath is null)
        {
            _stdout.Write(text);
            return;
        }

        var fullPath = Path.GetFullPath(settings.OutputPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(fullPath, text, new UTF8Encoding(false));
        _stdout.WriteLine($"Report written to {settings.OutputPath}");
    }

    private sealed record Settings(
        string ProjectPath,
        string? OutputPath,
        OutputFormat Format,
        AuditOptions Options,
        Severity Threshold,
        Severity? FailOn);

    private static bool TryValidate(AssetAuditRequest request, out Settings? settings, out string error)
    {
        settings = null;
        error = string.Empty;

        var buildSettings = Path.Combine(request.Project, "ProjectSettings", "EditorBuildSettings.asset");
        if (!Directory.Exists(Path.Combine(request.Project, "Assets")) || !File.Exists(buildSettings))
        {
            error = $"'{request.Project}' is not a Unity project root " +
                    "(expected Assets/ and ProjectSettings/EditorBuildSettings.asset)";
            return false;
        }

        if (request.MaxTextureSize <= 0)
        {
            error = "--max-texture-size must be greater than 0.";
            return false;
        }

        if (!TryResolveFormat(request.Format, request.output, out var format, out error))
            return false;

        if (format == OutputFormat.Console && request.output is not null)
        {
            error = "--output cannot be used with the console format. " +
                    "Use --format json|markdown, or give --output a .json or .md extension.";
            return false;
        }

        if (request.output is not null && !HasExtensionFor(format, request.output))
        {
            error = $"--output '{request.output}' does not match the {format.ToString().ToLowerInvariant()} format " +
                    $"(expected {string.Join(" or ", ExtensionOf(format))}).";
            return false;
        }

        if (!TryParseSeverity(request.SeverityThreshold, "--severity-threshold", Severity.Info, out var threshold, out error))
            return false;

        Severity? failOn = null;
        if (request.FailOn is not null)
        {
            if (!TryParseSeverity(request.FailOn, "--fail-on", Severity.Warning, out var parsed, out error))
                return false;

            if (parsed == Severity.Info)
            {
                error = "--fail-on accepts 'warning' or 'error'";
                return false;
            }

            failOn = parsed;
        }

        var options = new AuditOptions
        {
            MaxTextureSize = request.MaxTextureSize,
            ExcludePathFragments = request.Excludes.Select(e => e.Replace('\\', '/')).ToList()
        };

        settings = new Settings(request.Project, request.output, format, options, threshold, failOn);
        return true;
    }

    private static bool TryResolveFormat(
        string? explicitFormat, string? output, out OutputFormat format, out string error)
    {
        error = string.Empty;

        if (explicitFormat is not null)
        {
            OutputFormat? parsed = explicitFormat.ToLowerInvariant() switch
            {
                "console" => OutputFormat.Console,
                "json" => OutputFormat.Json,
                "markdown" or "md" => OutputFormat.Markdown,
                _ => null
            };

            format = parsed ?? OutputFormat.Console;
            if (parsed is null)
                error = $"Unknown format '{explicitFormat}'. Use console, json or markdown.";

            return parsed is not null;
        }

        format = output is null
            ? OutputFormat.Console
            : Path.GetExtension(output).ToLowerInvariant() switch
            {
                ".json" => OutputFormat.Json,
                ".md" or ".markdown" => OutputFormat.Markdown,
                _ => OutputFormat.Console
            };

        return true;
    }

    private static string[] ExtensionOf(OutputFormat format) => format switch
    {
        OutputFormat.Json => new[] { ".json" },
        OutputFormat.Markdown => new[] { ".md", ".markdown" },
        _ => Array.Empty<string>()
    };

    private static bool HasExtensionFor(OutputFormat format, string path) =>
        ExtensionOf(format).Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static bool TryParseSeverity(
        string? text, string optionName, Severity fallback, out Severity severity, out string error)
    {
        error = string.Empty;

        Severity? parsed = text?.ToLowerInvariant() switch
        {
            null => fallback,
            "info" => Severity.Info,
            "warning" => Severity.Warning,
            "error" => Severity.Error,
            _ => null
        };

        severity = parsed ?? fallback;
        if (parsed is null)
            error = $"Invalid value '{text}' for {optionName}. Use info, warning or error.";

        return parsed is not null;
    }
}