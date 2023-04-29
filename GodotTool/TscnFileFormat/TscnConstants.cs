namespace GodotTool.TscnFileFormat;

public static class TscnConstants
{
    public const string TSCN_ELEMENT_FORMAT = @"\[([a-zA-Z_][a-zA-Z0-9_]*)(?:\s+[a-zA-Z_][a-zA-Z0-9_]*=(.+))+\]";
    public const string TSCN_ELEMENT_KEY_VALUE_FORMAT = @"([a-zA-Z_][a-zA-Z0-9_]*)=""([^""]*)""";
    public const string TSCN_ELEMENT_KEY_EXT_RESOURCE_FORMAT = @"([a-zA-Z_][a-zA-Z0-9_]*)=ExtResource\(""(.*?)""\)";
}
