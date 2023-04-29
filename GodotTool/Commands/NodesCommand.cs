using System.CommandLine;
using System.CommandLine.Invocation;
using GodotTool.Services;

namespace GodotTool.Commands;

public class NodesCommand : Command
{
    public NodesCommand()
        : base("nodes", "Generates partial class with all nodes for the given *.tscn file with existing associated script.")
    {
        AddArgument(new Argument<IEnumerable<string>>("filepath", () => null!, "Filepath to a *.tscn file."));
    }

    public new class Handler : ICommandHandler
    {
        private readonly NodesService _nodesService;

        public IEnumerable<string>? FilePath { get; set; }

        public Handler(NodesService nodesService)
        {
            _nodesService = nodesService;
        }

        public int Invoke(InvocationContext context)
        {
            _nodesService.Run(FilePath);
            return 0;
        }

        public Task<int> InvokeAsync(InvocationContext context)
            => Task.FromResult(Invoke(context));
    }
}