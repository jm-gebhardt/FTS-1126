namespace Gebhardt.Acc.TransportOrder.WebApi.Swagger;

/// <summary>
/// Class for generating type names
/// </summary>
public static class CustomSchemaSelector
{
    private const string ModeSchemaId = "Mode";

    private const string PagedResult = "PagedResult";

    /// <summary>
    /// Returns the name of a type without a DTO suffix
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>Type name without DTO suffix</returns>
    public static string RemoveDtoSuffix(Type type)
    {
        if (type == typeof(Dtos.Agv.AgvModeChangedDto))
        {
            return ModeSchemaId;
        }
        if (type.Name.StartsWith(PagedResult))
        {
            string genericType = RemoveDtoEnding(type.GenericTypeArguments.First().Name);
            return $"{PagedResult}<{genericType}>";
        }

        string name = RemoveDtoEnding(type.Name);
        return name;
    }

    private static string RemoveDtoEnding(string name)
    {
        const string suffix = "DTO";
        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^suffix.Length]; // Remove DTO suffix
        }
        return name;
    }
}