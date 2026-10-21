using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <summary>
/// Helper class providing utility methods for Entity Framework Core value conversion operations.
/// </summary>
public static class EfValueConverters
{
    /// <summary>
    /// Creates a value converter for converting between a list of enum values and their string representation.
    /// </summary>
    /// <typeparam name="T">The enum type of elements in the list.</typeparam>
    /// <returns>A ValueConverter that converts between List{T} and string representation.</returns>
    public static ValueConverter<List<T>?, string?> CreateEnumListConverter<T>()
        where T : struct, Enum
    {
        return new ValueConverter<List<T>?, string?>(
            v => v == null ? null : string.Join(',', v),
            v => string.IsNullOrEmpty(v) ?
                null :
                v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Enum.Parse<T>)
                    .ToList()
        );
    }

    /// <summary>
    /// Creates a value converter for converting between a list of GUIDs and their string representation.
    /// </summary>
    /// <returns>A ValueConverter that converts between List{Guid} and string representation.</returns>
    public static ValueConverter<ICollection<Guid>, string?> CreateGuidListConverter()
    {
        return new ValueConverter<ICollection<Guid>, string?>(
            v => string.Join(',', v),
            v => string.IsNullOrEmpty(v) ? new List<Guid>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList()
        );
    }

    /// <summary>
    /// Creates a value comparer for comparing lists of type T.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <returns>A ValueComparer that can compare two nullable lists of type T for equality and compute hash codes.</returns>
    public static ValueComparer<ICollection<T>?> CreateListComparer<T>()
    {
        return new ValueComparer<ICollection<T>?>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v == null ? 0 : v.GetHashCode())),
            c => c == null ? null : c.ToList()
        );
    }

    /// <summary>
    /// Creates a value comparer for a non-nullable <see cref="IReadOnlyList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A ValueComparer that compares by sequence equality and snapshots as a new list.</returns>
    [PublicAPI]
    public static ValueComparer<IReadOnlyList<T>> CreateReadOnlyListComparer<T>()
    {
        return new ValueComparer<IReadOnlyList<T>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v == null ? 0 : v.GetHashCode())),
            c => c.ToList());
    }
    
    /// <summary>    
    /// Creates a value comparer for comparing arrays of type T.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <returns>A ValueComparer that can compare two nullable arrays of type T for equality and compute hash codes.</returns>
    [UsedImplicitly]
    public static ValueComparer<T[]?> CreateArrayComparer<T>()
    {
        return new ValueComparer<T[]?>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && Enumerable.SequenceEqual(c1, c2)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v == null ? 0 : v.GetHashCode())),
            c => c == null ? null : c.ToArray()
        );
    }

    /// <summary>
    /// Creates a value converter for converting between an object of type T and its JSON string representation.
    /// </summary>
    /// <typeparam name="T">The type of the object to convert to/from JSON.</typeparam>
    /// <param name="options">Optional JsonSerializerOptions to customize serialization/deserialization.</param>
    /// <returns>A ValueConverter that converts between T and its JSON string representation.</returns>
    [UsedImplicitly]
    public static ValueConverter<T?, string?> CreateNullableJsonConverter<T>(JsonSerializerOptions? options = null)
    {
        return new ValueConverter<T?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, options),
            v => string.IsNullOrEmpty(v) ? default : JsonSerializer.Deserialize<T>(v, (JsonSerializerOptions?) null)
        );
    }

    /// <summary>
    /// Creates a value converter for converting between an object of type T and its JSON string representation.
    /// </summary>
    /// <typeparam name="T">The type of the object to convert to/from JSON.</typeparam>
    /// <param name="options">Optional JsonSerializerOptions to customize serialization/deserialization.</param>
    /// <returns>A ValueConverter that converts between T and its JSON string representation.</returns>
    public static ValueConverter<T, string?> CreateJsonConverter<T>(JsonSerializerOptions? options = null)
    {
        return new ValueConverter<T, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, options),
            v => string.IsNullOrEmpty(v) ? default! : JsonSerializer.Deserialize<T>(v, options)!
        );
    }
}