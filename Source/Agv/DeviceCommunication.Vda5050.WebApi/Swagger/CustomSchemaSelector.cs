
namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Swagger;

/// <summary>
/// Class for generating type names
/// </summary>
public static class CustomSchemaSelector
{
    /// <summary>
    /// Returns a unique schema ID for a type. Internal types (Gebhardt.Acc.*) use the simple name
    /// without any DTO suffix. External types are prefixed with their last namespace segment when
    /// the name does not already start with it, preventing schema ID collisions between types from
    /// different assemblies that happen to share a simple name (e.g. Vdma.Lif.BlockingType vs
    /// Gebhardt.Acc.Common.Domain.Models.BlockingType).
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>Unique schema ID</returns>
    public static string GetSchemaId(Type type)
    {
        if (type.Namespace?.StartsWith("Gebhardt.Acc") == true)
        {
            return RemoveDtoEnding(type.Name);
        }

        // Qualify external types with last namespace segment to avoid name collisions.
        string ns = type.Namespace?.Split('.').LastOrDefault() ?? string.Empty;
        string name = type.Name;
        return name.StartsWith(ns, StringComparison.OrdinalIgnoreCase) ? name : $"{ns}{name}";
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