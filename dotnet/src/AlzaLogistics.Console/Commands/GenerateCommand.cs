using System.Diagnostics;
using AlzaLogistics.Core.DataGeneration;
using AlzaLogistics.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AlzaLogistics.Console.Commands;

public class GenerateCommand : AsyncCommand<GenerateSettings>
{
    private readonly IPackageStorage _packageStorage;

    public GenerateCommand()
    {
        _packageStorage = new JsonPackageStorage();
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, GenerateSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[green]Generating {settings.Count:N0} packages...[/]");
        
        var genSw = Stopwatch.StartNew();
        var generator = new PackageGenerator();
        var packages = generator.Generate(settings.Count);
        genSw.Stop();
        
        AnsiConsole.MarkupLine($"[green]Generated in {genSw.ElapsedMilliseconds} ms[/]");

        AnsiConsole.MarkupLine($"[green]Saving to {settings.OutputFilePath}...[/]");
        var saveSw = Stopwatch.StartNew();
        await _packageStorage.SavePackagesAsync(packages, settings.OutputFilePath);
        saveSw.Stop();
        
        AnsiConsole.MarkupLine($"[green]Saved in {saveSw.ElapsedMilliseconds} ms[/]");

        return 0;
    }
}
