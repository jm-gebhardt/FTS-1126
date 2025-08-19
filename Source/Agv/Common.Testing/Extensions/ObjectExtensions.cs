using System.Text.Json;

namespace Gebhardt.Acc.Common.Testing.Extensions;

/// <summary>
/// Provides extension methods for object manipulation.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Creates a deep copy of the specified object.
    /// </summary>
    /// <param name="obj">The object to copy.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns>A deep copy of the object.</returns>
    public static T DeepCopy<T>(this T obj)
    {
        string json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<T>(json)!;
    }
}