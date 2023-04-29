using System.Text.RegularExpressions;

namespace GodotTool.TscnFileFormat;

public class Tscn
{
    public List<TscnElement> Elements { get; set; } = new();

    public Tscn(IEnumerable<string> lines)
    {
        Parse(lines);
    }

    public IEnumerable<TscnElement> ExtResources => Elements.Where(n => n.Type == "ext_resource");

    public IEnumerable<TscnElement> Nodes => Elements.Where(n => n.Type == "node");

    public TscnElement? Script => ExtResources.FirstOrDefault(n => n.Attributes["type"] == "Script");

    private void Parse(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, TscnConstants.TSCN_ELEMENT_FORMAT))
            {
                Elements.Add(new TscnElement(line));
            }
        }
    }
}
