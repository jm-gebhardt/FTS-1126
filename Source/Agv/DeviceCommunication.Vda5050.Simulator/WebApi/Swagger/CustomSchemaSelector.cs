
namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Swagger;

/// <summary>
/// Class for generating type names
/// </summary>
public static class CustomSchemaSelector
{
    /// <summary>
    /// Returns a unique schema ID for a type. Types use the simple name
    /// without any DTO suffix.
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>Unique schema ID</returns>
    public static string GetSchemaId(Type type)
    {
        return RemoveDtoEnding(type.Name);
    }

    private static string RemoveDtoEnding(string name)
    {
        const string suffix = "DTO";
        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^suffix.Length];
        }
        return name;
    }
}