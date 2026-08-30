using AlzaLogistics.Console.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("AlzaLogistics");
    
    config.AddCommand<PlanCommand>("plan")
        .WithDescription("Plans delivery trips (default mode if run without arguments for backward compatibility).");
        
    config.AddCommand<GenerateCommand>("generate")
        .WithDescription("Generates synthetic packages and saves them to a file.");
});

// For backward compatibility, if arguments don't match commands, we could inject "plan" 
// but Spectre.Console.Cli handles routing well. Let's run it directly.
return await app.RunAsync(args);
