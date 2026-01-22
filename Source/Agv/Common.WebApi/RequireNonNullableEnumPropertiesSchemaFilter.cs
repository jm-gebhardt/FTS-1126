using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.Acc.Common.WebApi;

/// <summary>
/// Swagger Schema filter to mark non-nullable enum properties as required.
/// <para>
/// This filter is needed because <see cref="RequireNonNullablePropertiesSchemaFilter"/> only checks
/// properties with a <c>Type</c> set. Enum properties use <c>$ref</c> references instead of inline types,
/// so they are skipped by that filter.
/// </para>
/// </summary>
[UsedImplicitly]
public class RequireNonNullableEnumPropertiesSchemaFilter(
    IOptions<JsonOptions>? mvcJsonOptions,
    IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>? httpJsonOptions
) : ISchemaFilter
{
    private JsonSerializerOptions? SerializerOptions =>
        mvcJsonOptions?.Value.JsonSerializerOptions ?? httpJsonOptions?.Value.SerializerOptions;

    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema openApiSchema)
        {
            return;
        }

        if (openApiSchema.Properties is null || openApiSchema.Properties.Count == 0)
        {
            return;
        }

        openApiSchema.Required ??= new HashSet<string>();

        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propertyType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType);

            bool isNullableEnum = underlyingType?.IsEnum == true;
            bool isNonNullableEnum = propertyType.IsEnum;

            if (!isNonNullableEnum && !isNullableEnum)
            {
                continue; 
            }

            if (!isNonNullableEnum)
            {
                continue;
            }

            string jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                              ?? SerializerOptions?.PropertyNamingPolicy?.ConvertName(property.Name)
                              ?? property.Name;

            if (!openApiSchema.Properties.ContainsKey(jsonName) &&
                !openApiSchema.Properties.ContainsKey(property.Name))
            {
                continue;
            }

            string schemaPropertyName = openApiSchema.Properties.ContainsKey(jsonName)
                ? jsonName
                : property.Name;

            openApiSchema.Required.Add(schemaPropertyName);
        }
    }
}
