using System.ComponentModel;
using Spectre.Console.Cli;

namespace AlzaLogistics.Console.Commands;

public class GenerateSettings : CommandSettings
{
    [CommandArgument(0, "<OutputFilePath>")]
    [Description("Path to save the generated packages (e.g. packages.json).")]
    public string OutputFilePath { get; set; } = string.Empty;

    [CommandOption("-c|--count")]
    [Description("Number of packages to generate.")]
    [DefaultValue(200_000)]
    public int Count { get; set; } = 200_000;
}
