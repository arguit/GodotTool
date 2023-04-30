using GodotTool.TscnFileFormat;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace GodotTool.Services;

public class NodesService : INodesService
{
    private readonly ILogger<NodesService> _logger;

    private record ScriptInfo(string NamespaceName, string ClassName, bool IsPartial, bool IsFileScopedNamespace);

    private record NodeProperty(string Type, string Name);

    private record NodeInitialization(string Name, string Type, string Path);

    private record NodesInfo(List<string> NodeUsings, List<NodeProperty> NodeProperties,
        List<NodeInitialization> NodeInitializations);

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

        var scriptInfo = GetScriptInfo(scriptPath);

        _logger.LogInformation($"{scriptPath} file parsed successfully.");

        var nodesInfo = GetNodesInfo(tscn);

        _logger.LogInformation($"Nodes properties and initializations prepared successfully.");

        var partialClassFileContent = GeneratePartialClassFileContent(scriptInfo, nodesInfo);

        _logger.LogInformation($"Nodes partial class content generated successfully.");

        var partialClassFilePath = Path.Join(
            Path.GetDirectoryName(scriptPath),
            $"{Path.GetFileNameWithoutExtension(scriptPath)}.Nodes.cs");

        File.WriteAllText(partialClassFilePath, partialClassFileContent);

        _logger.LogInformation($"{partialClassFilePath} file created successfully.");

        if (!scriptInfo.IsPartial)
        {
            ChangeToPartialClass(scriptPath);
        }
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

    private void ChangeToPartialClass(string scriptPath)
    {
        var scriptContent = File.ReadAllText(scriptPath);
        
        // Parse the existing source code into a syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(scriptContent);

        // Get the root node of the syntax tree
        var root = syntaxTree.GetRoot();

        // Find the class declaration node in the syntax tree
        var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        // Check if the class declaration exists
        if (classDeclaration != null)
        {
            // Check if the class already has the 'partial' modifier
            if (!classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
            {
                // Add the 'partial' modifier to the class declaration
                var partialModifier = SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space);
                var newClassDeclaration = classDeclaration.AddModifiers(partialModifier);

                // Create a new root node with the updated class declaration
                var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);

                // Create a new syntax tree with the updated root node
                var newSyntaxTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);

                // Get the updated source code
                var updatedScriptContent = newSyntaxTree.ToString();
                
                File.WriteAllText(scriptPath, updatedScriptContent);

                _logger.LogInformation(
                    $"Class {classDeclaration.Identifier.ValueText} in {scriptPath} file changed to partial successfully.");
            }
        }
    }

    private ScriptInfo GetScriptInfo(string scriptPath)
    {
        // get the associated script content
        var scriptContent = File.ReadAllText(scriptPath);

        // open and parse it to obtain its namespace and class name
        var tree = SyntaxFactory.ParseSyntaxTree(scriptContent.Trim());
        var root = tree.GetCompilationUnitRoot();
        var namespaceName = string.Empty;
        var className = string.Empty;
        var isPartial = true;
        var isFileScopeNamespace = true;

        if (root.Members.Any(n => n.Kind() == SyntaxKind.NamespaceDeclaration))
        {
            var namespaceDeclarationSyntax = (NamespaceDeclarationSyntax)root.Members
                .FirstOrDefault(n => n.Kind() == SyntaxKind.NamespaceDeclaration)!;

            namespaceName = namespaceDeclarationSyntax.Name.ToFullString();

            var classDeclaration = (ClassDeclarationSyntax)namespaceDeclarationSyntax.Members
                .FirstOrDefault(n => n.Kind() == SyntaxKind.ClassDeclaration)!;

            className = classDeclaration.Identifier.ValueText;

            isPartial = classDeclaration.Modifiers.Any(n => n.IsKind(SyntaxKind.PartialKeyword));

            isFileScopeNamespace = false;
        }

        if (root.Members.Any(n => n.Kind() == SyntaxKind.FileScopedNamespaceDeclaration))
        {
            var fileScopedNamespaceDeclarationSyntax = (FileScopedNamespaceDeclarationSyntax)root.Members
                .FirstOrDefault(n => n.Kind() == SyntaxKind.FileScopedNamespaceDeclaration)!;

            namespaceName = fileScopedNamespaceDeclarationSyntax.Name.ToFullString();

            var classDeclaration = (ClassDeclarationSyntax)fileScopedNamespaceDeclarationSyntax
                .Members.FirstOrDefault(n => n.Kind() == SyntaxKind.ClassDeclaration)!;

            className = classDeclaration.Identifier.ValueText;

            isPartial = classDeclaration.Modifiers.Any(n => n.IsKind(SyntaxKind.PartialKeyword));

            isFileScopeNamespace = true;
        }

        return new ScriptInfo(namespaceName, className, isPartial, isFileScopeNamespace);
    }

    private NodesInfo GetNodesInfo(Tscn tscn)
    {
        var nodeUsings = new List<string> { "Godot" };
        var nodeProperties = new List<NodeProperty>();
        var nodeInitializations = new List<NodeInitialization>();

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

                var (namespaceName, className, _, _) = GetScriptInfo(extResourceScriptPath);

                nodeType = className;

                nodeUsings.Add(namespaceName);
            }

            nodeProperties.Add(new NodeProperty(nodeType, nodeName));
            nodeInitializations.Add(new NodeInitialization(nodeName, nodeType, nodePath));
        }

        return new NodesInfo(nodeUsings.Distinct().ToList(), nodeProperties, nodeInitializations);
    }

    private string GeneratePartialClassFileContent(ScriptInfo scriptInfo, NodesInfo nodesInfo)
    {
        var compilationUnit = SyntaxFactory.CompilationUnit();

        FileScopedNamespaceDeclarationSyntax? fileScopedNamespaceDeclaration = null;
        NamespaceDeclarationSyntax? namespaceDeclaration = null;

        // Add usings
        foreach (var nodeUsing in nodesInfo.NodeUsings)
        {
            compilationUnit = compilationUnit.AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(nodeUsing)));
        }

        // Add namespace
        if (scriptInfo.IsFileScopedNamespace)
        {
            fileScopedNamespaceDeclaration =
                SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.IdentifierName(scriptInfo.NamespaceName));
        }
        else
        {
            namespaceDeclaration =
                SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(scriptInfo.NamespaceName));
        }

        // Create class
        var classDeclaration = SyntaxFactory.ClassDeclaration(scriptInfo.ClassName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

        // Add properties to class
        foreach (var nodeProperty in nodesInfo.NodeProperties)
        {
            classDeclaration = classDeclaration.AddMembers(SyntaxFactory
                .PropertyDeclaration(SyntaxFactory.ParseTypeName(nodeProperty.Type), nodeProperty.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));
        }

        // Add initialization method
        var methodDeclaration = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Initialize")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        var statemenSyntaxes = new List<StatementSyntax>();

        foreach (var nodeInitialization in nodesInfo.NodeInitializations)
        {
            // Create the identifier for the left-hand side of the assignment
            var identifier = SyntaxFactory.IdentifierName(nodeInitialization.Name);

            // Create the right-hand side of the assignment
            var invocationExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("GetNode"))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName(nodeInitialization.Type)
                                )
                            )
                        )
                )
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal(nodeInitialization.Path)
                                )
                            )
                        )
                    )
                );

            // Create the assignment expression
            var assignmentExpression = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                identifier,
                invocationExpression
            );

            // Create the expression statement
            var expressionStatement = SyntaxFactory.ExpressionStatement(assignmentExpression);

            statemenSyntaxes.Add(expressionStatement);
        }

        methodDeclaration = methodDeclaration.WithBody(SyntaxFactory.Block(statemenSyntaxes));

        // Add method into class
        classDeclaration = classDeclaration.AddMembers(methodDeclaration);

        // Add class into namespace and namespace into compilation unit
        if (scriptInfo.IsFileScopedNamespace)
        {
            fileScopedNamespaceDeclaration = fileScopedNamespaceDeclaration!.AddMembers(classDeclaration);
            compilationUnit = compilationUnit.AddMembers(fileScopedNamespaceDeclaration);
        }
        else
        {
            namespaceDeclaration = namespaceDeclaration!.AddMembers(classDeclaration);
            compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);
        }

        var code = compilationUnit.NormalizeWhitespace().ToString();

        return code;
    }
}