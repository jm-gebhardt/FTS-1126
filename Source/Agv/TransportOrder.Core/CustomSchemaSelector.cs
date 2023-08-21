
namespace Gebhardt.StoreWare.Agv.TransportOrder.Core;

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
        const string suffix = "DTO";

        if (type.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return type.Name.Substring(0, type.Name.Length - suffix.Length); // Remove DTO suffix
        }

        return type.Name;
    }
}