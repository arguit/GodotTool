using System.Text;
using GodotTool.TscnFileFormat;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace GodotTool.Services;

public class NodesService : INodesService
{
    private readonly ILogger<NodesService> _logger;

    public NodesService(ILogger<NodesService> logger)
    {
        _logger = logger;
    }

    public void Run(IEnumerable<string>? filePaths)
    {
        if (filePaths == null)
        {
            filePaths = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.tscn", SearchOption.AllDirectories);
        }

        foreach (var filePath in filePaths)
        {
            GenerateNodesPartialClass(filePath);
        }
    }

    private void GenerateNodesPartialClass(string filePath)
    {
        if (!IsCurrentDirectoryGodotProjectRoot()) return;
        if (!DoesGivenTscnFilePathExist(filePath)) return;

        var tscn = ParseTscnFile(filePath);

        _logger.LogInformation($"{filePath} file parsed successfully.");

        if (!DoesTscnHaveScripAssociated(tscn)) return;
        if (DoesAssociatedScriptExist(tscn, out var scriptPath)) return;

        _logger.LogInformation($"{scriptPath} file found.");

        var (namespaceName, className) = GetNamespaceAndClassName(scriptPath);

        _logger.LogInformation($"{scriptPath} file parsed successfully.");

        var (nodeUsings, nodeProperties, nodeInitializations) = PrepareNodePropertiesAndInitializations(tscn);

        _logger.LogInformation($"Nodes properties and initializations prepared successfully.");

        var partialClassFileContent =
            GeneratePartialClassFileContent(namespaceName, className, nodeUsings, nodeProperties, nodeInitializations);

        _logger.LogInformation($"Nodes partial class content generated successfully.");

        var partialClassFilePath = Path.Join(
            Path.GetDirectoryName(scriptPath),
            $"{Path.GetFileNameWithoutExtension(scriptPath)}.Nodes.cs");

        File.WriteAllText(partialClassFilePath, partialClassFileContent);

        _logger.LogInformation($"{partialClassFilePath} file created successfully.");
    }

    private bool IsCurrentDirectoryGodotProjectRoot()
    {
        var projectGodotPath = Path.Join(Directory.GetCurrentDirectory(), "project.godot");

        if (!File.Exists(projectGodotPath))
        {
            _logger.LogError($"The command 'nodes' must be executed from a godot project root directory.");
            return false;
        }

        return true;
    }

    private bool DoesGivenTscnFilePathExist(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError($"The given file at '{filePath}' does not exist.");
            return false;
        }

        return true;
    }

    private Tscn ParseTscnFile(string filePath)
    {
        // open the given file and read all its content
        var lines = File.ReadAllLines(filePath);

        // parse the file into Tscn object
        return new Tscn(lines);
    }

    private bool DoesTscnHaveScripAssociated(Tscn tscn)
    {
        if (tscn.Script == null)
        {
            _logger.LogError($"The given tscn file does not have a script associated.");
            return false;
        }

        return true;
    }

    private bool DoesAssociatedScriptExist(Tscn tscn, out string scriptPath)
    {
        scriptPath = Utils.GetResPath(tscn.Script!.Attributes["path"]);

        if (!File.Exists(scriptPath))
        {
            _logger.LogError($"The given script at '{scriptPath}' does not exist.");
            return true;
        }

        return false;
    }

    private (string namespaceName, string className) GetNamespaceAndClassName(string scriptPath)
    {
        // get the associated script content
        var scriptContent = File.ReadAllText(scriptPath);

        // open and parse it to obtain its namespace and class name
        var tree = SyntaxFactory.ParseSyntaxTree(scriptContent.Trim());
        var root = tree.GetCompilationUnitRoot();
        var namespaceName = string.Empty;
        var className = string.Empty;

        if (root.Members.Any(n => n.Kind() == SyntaxKind.NamespaceDeclaration))
        {
            var namespaceDeclarationSyntax = (NamespaceDeclarationSyntax)root.Members
                .FirstOrDefault(n => n.Kind() == SyntaxKind.NamespaceDeclaration)!;

            namespaceName = namespaceDeclarationSyntax.Name.ToFullString();

            var classDeclaration = (ClassDeclarationSyntax)namespaceDeclarationSyntax.Members
                .FirstOrDefault(n => n.Kind() == SyntaxKind.ClassDeclaration)!;

            className = classDeclaration.Identifier.ValueText;
        }

        if (root.Members.Any(n => n.Kind() == SyntaxKind.FileScopedNamespaceDeclaration))
        {
            var fileScopedNamespaceDeclarationSyntax = (FileScopedNamespaceDeclarationSyntax)root.Members
                .FirstOrDefault(n => n.Kind() == SyntaxKind.FileScopedNamespaceDeclaration)!;

            namespaceName = fileScopedNamespaceDeclarationSyntax.Name.ToFullString();

            var classDeclaration = (ClassDeclarationSyntax)fileScopedNamespaceDeclarationSyntax.Members
                .FirstOrDefault(n => n.Kind() == SyntaxKind.ClassDeclaration)!;

            className = classDeclaration.Identifier.ValueText;
        }

        return (namespaceName, className);
    }

    private (List<string> nodeUsings, List<string> nodeProperties, List<string> nodeInitializations)
        PrepareNodePropertiesAndInitializations(Tscn tscn)
    {
        var nodeUsings = new List<string>();
        var nodeProperties = new List<string>();
        var nodeInitializations = new List<string>();

        foreach (var node in tscn.Nodes)
        {
            // if node does not have parent then it is root node
            if (!node.Attributes.ContainsKey("parent"))
            {
                continue;
            }

            var nodeName = node.Attributes["name"].FirstCharToUpper();
            var nodeType = string.Empty;
            var nodePath = $"{node.Attributes["parent"]}/{nodeName}";

            if (node.Attributes.ContainsKey("type"))
            {
                nodeType = node.Attributes["type"];
            }
            else if (node.ExtResources.ContainsKey("instance"))
            {
                var extResourceId = node.ExtResources["instance"];
                var extResource = tscn.ExtResources.FirstOrDefault(n => n.Attributes["id"] == extResourceId);
                var extResourceTscnPath = Utils.GetResPath(extResource!.Attributes["path"]);

                if (!DoesGivenTscnFilePathExist(extResourceTscnPath))
                {
                    _logger.LogWarning($"Property for node '{nodeName}' will not be created.");
                    continue;
                }

                var extResourceTscn = ParseTscnFile(extResourceTscnPath);

                if (!DoesTscnHaveScripAssociated(extResourceTscn))
                {
                    _logger.LogWarning($"Property for node '{nodeName}' will not be created.");
                    continue;
                }

                if (DoesAssociatedScriptExist(extResourceTscn, out var extResourceScriptPath))
                {
                    _logger.LogWarning($"Property for node '{nodeName}' will not be created.");
                    continue;
                }

                var (namespaceName, className) = GetNamespaceAndClassName(extResourceScriptPath);

                nodeType = className;

                nodeUsings.Add($"using {namespaceName};");
            }

            nodeProperties.Add($"public {nodeType} {nodeName} {{ get; set; }}");
            nodeInitializations.Add($"{nodeName} = GetNode<{nodeType}>(\"{nodePath}\");");
        }

        return (nodeUsings.Distinct().ToList(), nodeProperties, nodeInitializations);
    }

    private static string GeneratePartialClassFileContent(
        string namespaceName,
        string className,
        List<string> nodeUsings,
        List<string> nodeProperties,
        List<string> nodeInitializations)
    {
        var builder = new StringBuilder();

        builder.AppendLine("using Godot;");

        foreach (var nodeUsing in nodeUsings)
        {
            builder.AppendLine(nodeUsing);
        }

        builder.AppendLine();
        builder.AppendLine($"namespace {namespaceName};");
        builder.AppendLine();
        builder.AppendLine($"public partial class {className}");
        builder.AppendLine("{");

        foreach (var nodeProperty in nodeProperties)
        {
            builder.AppendLine($"    {nodeProperty}");
        }

        builder.AppendLine();
        builder.AppendLine("    public void Initialize()");
        builder.AppendLine("    {");

        foreach (var nodeInitialization in nodeInitializations)
        {
            builder.AppendLine($"        {nodeInitialization}");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        return builder.ToString();
    }
}