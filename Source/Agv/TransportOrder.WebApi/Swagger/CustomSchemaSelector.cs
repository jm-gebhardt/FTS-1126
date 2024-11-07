namespace Gebhardt.Acc.TransportOrder.WebApi.Swagger;

/// <summary>
/// Class for generating type names
/// </summary>
public static class CustomSchemaSelector
{
    private const string ModeSchemaId = "Mode";

    /// <summary>
    /// Returns the name of a type without a DTO suffix
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>Type name without DTO suffix</returns>
    public static string RemoveDtoSuffix(Type type)
    {
        const string suffix = "DTO";
        if (type == typeof(Dtos.Agv.AgvModeChangedDto))
        {
            return ModeSchemaId;
        }
        string name = type.Name;
        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^suffix.Length]; // Remove DTO suffix
        }
        return name;
    }
}