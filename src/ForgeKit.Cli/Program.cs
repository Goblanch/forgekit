using System.CommandLine;
using Forgekit.Cli;
using Forgekit.Cli.Commands;

var root = new RootCommand("forgekit - developer tools for game projects");
root.Subcommands.Add(AssetAuditCommand.Create());

var parseResult = root.Parse(args);

if (parseResult.Errors.Count > 0)
{
    foreach (var error in parseResult.Errors)
        Console.Error.WriteLine(error.Message);

    Console.Error.WriteLine("Run 'forgekit --help' for usage.");
    return ExitCode.UsageError;
}

return parseResult.Invoke();