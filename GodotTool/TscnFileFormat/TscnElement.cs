using System.Text.RegularExpressions;

namespace GodotTool.TscnFileFormat;

public class TscnElement
{
    public string Type { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
    public Dictionary<string, string> ExtResources { get; set; } = new();

    public TscnElement(string input)
    {
        var tscnElement = Regex.Match(input, TscnConstants.TSCN_ELEMENT_FORMAT);

        if (tscnElement.Success)
        {
            Type = tscnElement.Groups[1].Value;

            var keyValuePairs = Regex.Matches(input, TscnConstants.TSCN_ELEMENT_KEY_VALUE_FORMAT);

            foreach (Match keyValuePair in keyValuePairs)
            {
                var key = keyValuePair.Groups[1].Value;
                var value = keyValuePair.Groups[2].Value;

                Attributes.Add(key, value);
            }

            var extResources = Regex.Matches(input, TscnConstants.TSCN_ELEMENT_KEY_EXT_RESOURCE_FORMAT);

            foreach (Match extResource in extResources)
            {
                var key = extResource.Groups[1].Value;
                var value = extResource.Groups[2].Value;

                ExtResources.Add(key, value);
            }
        }
        else
        {
            throw new ArgumentException("Bad TscnElement input data format.");
        }
    }
}