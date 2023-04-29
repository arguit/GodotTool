# GodotTool

Tool for Godot game developers.

- [GodotTool](#godottool)
  - [Install](#install)
  - [Usage](#usage)
  - [Features](#features)
    - [Generate Nodes](#generate-nodes)

## Install

```powershell
dotnet tool install --global GodotTool --version 1.0.1
```

## Usage

```text
Description:

Usage:
  GodotTool [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  nodes <filepaths>  Generates partial class with all nodes for the given *.tscn file with existing associated script. []
```

## Features

### Generate Nodes

Generates strongly typed properties for every node in the given *.tscn file.

Generates file with the following attributes:

- Name `[AssociatedScriptName].Nodes.cs`
  - Ex. for `Main.tscn` with `Main.cs` the generated file will be named `Main.Nodes.cs`
- `Usings`
  - Adds `Godot` using
  - Adds all necessary usings gathered from all dependant scripts
- `Namespace` that correspondents with the namespace found in the associated script
- `Partial class` for the associated script (class) with:
  - `Properies` for every found nodes in the source \*.tscn file
  - Public `Initialize()` method that should be call from `_Ready()` method in the main class
    - Contains `Initializations` for all nodes found

Command: `nodes`

Arguments: `list of strings` that are file paths of \*.tscn files or `nothing` to go through all \*.tscn files in project

Examples:

```text
godottool nodes
```

```text
godottool nodes .\Main.tscn
```

```text
godottool nodes .\Main.tscn .\Components\Component.tscn
```

Generated partial class format:

```csharp
using Godot;

//
// GENERATES: dependency usings
//

namespace MyProject;

public partial class Main
{
    //
    // GENERATES: node properties
    // 
    // Format:
    //
    // public [NodeType] [NodeName] { get; set; }
    //

    public Node MyNode { get; set;}

    public void Initialize()
    {
        //
        // GENERATES: node initializations
        // 
        // Format:
        //
        // [NodeName] = GetNode<[NodeType]>("[NodePath]");
        //

        MyNode = GetNode<Node>("./MyNode");
    }
}
```
