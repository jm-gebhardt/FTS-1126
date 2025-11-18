using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <summary>
/// EntityTypeBuilder extension 
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Configures all entity properties of T, except the one listed in filter.
    /// 
    /// Source <see href="https://stackoverflow.com/a/48278685" />
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="onlyInclude">If set, only true returning items will be configured</param>
    /// <param name="except">If set the provided property names won't be configured</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static List<PropertyBuilder> ConfigureAllProperties<T>(this EntityTypeBuilder<T> builder,
        Func<PropertyInfo, bool>? onlyInclude = null, IEnumerable<string>? except = null) where T : class
    {
        IEnumerable<PropertyInfo> properties = typeof(T)
            .GetProperties()
            .AsEnumerable();

        properties = properties
            .Where(x => (onlyInclude == null || onlyInclude(x)) &&
                        (except == null || !except.Contains(x.Name))
            );

        return properties
            .Select(x => builder.Property(x.PropertyType, x.Name))
            .ToList();
    }

    /// <summary>
    /// Serialize property as json
    /// </summary>
    /// <param name="propertyBuilder"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder) where T : class?
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        var converter = new ValueConverter<T, string>
        (
            v => JsonSerializer.Serialize(v, options),
            v => string.IsNullOrWhiteSpace(v) ? null! : JsonSerializer.Deserialize<T>(v, options)!
        );

        var comparer = new ValueComparer<T>
        (
            (l, r) => JsonSerializer.Serialize(l, options) == JsonSerializer.Serialize(r, options),
            v => v == null ? 0 : JsonSerializer.Serialize(v, options).GetHashCode(),
            v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, options), options)!
        );

        propertyBuilder.HasConversion(converter);
        propertyBuilder.Metadata.SetValueComparer(comparer);

        return propertyBuilder;
    }
}