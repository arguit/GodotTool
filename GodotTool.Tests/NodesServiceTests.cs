using System.Collections.Generic;
using GodotTool.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace GodotTool.Tests;

public class NodesServiceTests
{
    private NodesService _nodesService;

    [SetUp]
    public void Setup()
    {
        var mock = new Mock<ILogger<NodesService>>();
        var logger = mock.Object;

        _nodesService = new NodesService(logger);
    }

    [Test]
    public void GeneratePartialClassFileContent_NoNamespace()
    {
        // Arrange
        var scriptInto = new NodesService.ScriptInfo(
            NamespaceName: "TestNamespaceName",
            ClassName: "TestClassName",
            IsPartial: true,
            hasNamespace: false,
            IsFileScopedNamespace: false);
        var nodeUsings = new List<string> { "Godot" };
        var nodeProperties = new List<NodesService.NodeProperty> { new("MyNodeType", "MyNodeName") };
        var nodeInitializations = new List<NodesService.NodeInitialization> { new("MyNodeName", "MyNodeType", ".") };
        var nodesInfo = new NodesService.NodesInfo(
            nodeUsings,
            nodeProperties,
            nodeInitializations);
        var expectedPartialClassFileContent = @"using Godot;

public partial class TestClassName
{
    public MyNodeType MyNodeName { get; set; }

    public void Initialize()
    {
        MyNodeName = GetNode<MyNodeType>(""."");
    }
}";

        // Act
        var partialClassFileContent = _nodesService.GeneratePartialClassFileContent(scriptInto, nodesInfo);

        // Assert
        Assert.AreEqual(partialClassFileContent, expectedPartialClassFileContent);
    }
    
    [Test]
    public void GeneratePartialClassFileContent_BlockScopedNamespace()
    {
        // Arrange
        var scriptInto = new NodesService.ScriptInfo(
            NamespaceName: "TestNamespaceName",
            ClassName: "TestClassName",
            IsPartial: true,
            hasNamespace: true,
            IsFileScopedNamespace: false);
        var nodeUsings = new List<string> { "Godot" };
        var nodeProperties = new List<NodesService.NodeProperty> { new("MyNodeType", "MyNodeName") };
        var nodeInitializations = new List<NodesService.NodeInitialization> { new("MyNodeName", "MyNodeType", ".") };
        var nodesInfo = new NodesService.NodesInfo(
            nodeUsings,
            nodeProperties,
            nodeInitializations);
        var expectedPartialClassFileContent = @"using Godot;

namespace TestNamespaceName
{
    public partial class TestClassName
    {
        public MyNodeType MyNodeName { get; set; }

        public void Initialize()
        {
            MyNodeName = GetNode<MyNodeType>(""."");
        }
    }
}";

        // Act
        var partialClassFileContent = _nodesService.GeneratePartialClassFileContent(scriptInto, nodesInfo);

        // Assert
        Assert.AreEqual(partialClassFileContent, expectedPartialClassFileContent);
    }

    [Test]
    public void GeneratePartialClassFileContent_FileScopedNamespace()
    {
        // Arrange
        var scriptInto = new NodesService.ScriptInfo(
            NamespaceName: "TestNamespaceName",
            ClassName: "TestClassName",
            IsPartial: true,
            hasNamespace: true,
            IsFileScopedNamespace: true);
        var nodeUsings = new List<string> { "Godot" };
        var nodeProperties = new List<NodesService.NodeProperty> { new("MyNodeType", "MyNodeName") };
        var nodeInitializations = new List<NodesService.NodeInitialization> { new("MyNodeName", "MyNodeType", ".") };
        var nodesInfo = new NodesService.NodesInfo(
            nodeUsings,
            nodeProperties,
            nodeInitializations);
        var expectedPartialClassFileContent = @"using Godot;

namespace TestNamespaceName;

public partial class TestClassName
{
    public MyNodeType MyNodeName { get; set; }

    public void Initialize()
    {
        MyNodeName = GetNode<MyNodeType>(""."");
    }
}";

        // Act
        var partialClassFileContent = _nodesService.GeneratePartialClassFileContent(scriptInto, nodesInfo);

        // Assert
        Assert.AreEqual(partialClassFileContent, expectedPartialClassFileContent);
    }
}