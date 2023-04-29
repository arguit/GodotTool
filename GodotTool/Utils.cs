namespace GodotTool;

public static class Utils
{
    public static string GetResPath(string resPath) =>
        Path.Join(Directory.GetCurrentDirectory(), resPath.Substring("res://".Length));
}