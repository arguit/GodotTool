using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using GodotTool.Commands;
using GodotTool.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var parser = BuildCommandLine()
    .UseHost(_ => Host.CreateDefaultBuilder(args), builder => builder
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton<INodesService, NodesService>();
        })
        .UseCommandHandler<NodesCommand, NodesCommand.Handler>())
    .UseDefaults()
    .Build();

return await parser.InvokeAsync(args);

static CommandLineBuilder BuildCommandLine()
{
    var rootCommand = new RootCommand();

    rootCommand.AddCommand(new NodesCommand());

    return new CommandLineBuilder(rootCommand);
}