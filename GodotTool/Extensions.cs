namespace GodotTool;

public static class Extensions
{
    public static string FirstCharToUpper(this string input) =>
        string.IsNullOrEmpty(input)
            ? String.Empty
            : $"{input[0].ToString().ToUpper()}{input.Substring(1)}";
}