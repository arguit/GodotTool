using System.CommandLine;
using System.CommandLine.Invocation;
using GodotTool.Services;

namespace GodotTool.Commands;

public class NodesCommand : Command
{
    public NodesCommand()
        : base("nodes", "Generates partial class with all nodes for the given *.tscn files with existing associated scripts.")
    {
        AddArgument(new Argument<IEnumerable<string>>("filepaths", () => null!, "Filepaths to a *.tscn file."));
    }

    public new class Handler : ICommandHandler
    {
        private readonly INodesService _nodesService;

        public IEnumerable<string>? FilePaths { get; set; }

        public Handler(INodesService nodesService)
        {
            _nodesService = nodesService;
        }

        public int Invoke(InvocationContext context)
        {
            _nodesService.Run(FilePaths);
            return 0;
        }

        public Task<int> InvokeAsync(InvocationContext context)
            => Task.FromResult(Invoke(context));
    }
}