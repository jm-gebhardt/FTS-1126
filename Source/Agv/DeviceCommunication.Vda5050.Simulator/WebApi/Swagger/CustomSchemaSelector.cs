namespace DeviceCommunication.Vda5050.Simulator.WebApi.Swagger;

/// <summary>
/// Class for generating type names
/// </summary>
public static class CustomSchemaSelector
{
    /// <summary>
    /// Returns a unique schema ID for a type. Types use the simple name without any DTO suffix.
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>Unique schema ID</returns>
    public static string GetSchemaId(Type type)
    {
        const string suffix = "DTO";
        string name = type.Name;
        return name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? name[..^suffix.Length] : name;
    }
}
