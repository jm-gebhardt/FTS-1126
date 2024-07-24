namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;

/// <summary>
/// Provides a parser for Prismat telegrams
/// </summary>
public static class TelegramParser
{
    private const string EndSymbol = "$$:";
    private const string FieldSeparator = "/";
    private const string KeyValueSeparator = ":";

    /// <summary>
    /// Parse a telegram into a dictionary of its fields.
    /// </summary>
    /// <param name="telegram">The telegram.</param>
    /// <returns>A dictionary with the fields keys and values.</returns>
    public static Dictionary<string, string> Parse(string telegram)
    {
        Dictionary<string, string> content = telegram
            .Split([EndSymbol], StringSplitOptions.None)[0]
            .Split([FieldSeparator], StringSplitOptions.RemoveEmptyEntries)
            .Select(field => field.Split([KeyValueSeparator], StringSplitOptions.None))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());
        return content;
    }
}