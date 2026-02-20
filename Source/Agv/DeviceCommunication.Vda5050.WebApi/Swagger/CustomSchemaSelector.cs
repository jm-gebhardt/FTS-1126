
namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Swagger;

/// <summary>
/// Class for generating type names
/// </summary>
public static class CustomSchemaSelector
{
    /// <summary>
    /// Returns the name of a type without a DTO suffix
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>Type name without DTO suffix</returns>
    public static string RemoveDtoSuffix(Type type)
    {
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