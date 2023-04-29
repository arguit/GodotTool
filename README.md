# GodotTool

Tool for Godot game developers.

## Usage

```text
Description:

Usage:
  GodotTool [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  nodes <filepath>  Generates partial class with all nodes for the given *.tscn file with existing associated script. []
```

## Generate Nodes

Command: `nodes`

Arguments: `list of strings` that are file paths of *.tscn files or `nothing` to go through all *.tscn files in project

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
