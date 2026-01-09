using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.Acc.Common.Filter.Api.Swagger;

/// <summary>
/// Schema filter that uses <see cref="SwaggerFilterableProperty"/> to add extensions to the annotated properties.
/// </summary>
[UsedImplicitly]
internal class SwaggerFilterableSchemaExtender(
    IOptions<JsonOptions>? mvcJsonOptions,
    IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>? httpJsonOptions
) : ISchemaFilter
{
    private JsonSerializerOptions? SerializerOptions => mvcJsonOptions?.Value.JsonSerializerOptions ?? httpJsonOptions?.Value.SerializerOptions;

    /// <inheritdoc cref="ISchemaFilter"/>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema openApiSchema)
        {
            if (openApiSchema.Properties == null || openApiSchema.Properties.Count == 0)
            {
                return;
            }

            foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                bool isFilterable = Attribute.IsDefined(property, typeof(SwaggerFilterableProperty));
                bool supportsWhereAggregation = Attribute.IsDefined(property, typeof(SwaggerSupportsWhereAggregationFilter));

                if (!isFilterable && !supportsWhereAggregation)
                {
                    continue;
                }

                string jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? SerializerOptions?.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;

                if (!openApiSchema.Properties.TryGetValue(jsonName, out var propSchema) && !openApiSchema.Properties.TryGetValue(property.Name, out propSchema))
                {
                    continue;
                }

                if (propSchema is OpenApiSchema propertySchema)
                {
                    if (isFilterable)
                    {
                        propertySchema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                        propertySchema.Extensions.Add("x-IsFilterableProperty", new JsonNodeExtension(true));
                        propertySchema.Extensions.Add("x-FilterablePropertyName", new JsonNodeExtension(property.Name));
                    }

                    if (supportsWhereAggregation)
                    {
                        propertySchema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                        propertySchema.Extensions.Add("x-SupportsWhereAggregationFilter", new JsonNodeExtension(true));
                        propertySchema.Extensions.Add("x-FilterablePropertyName", new JsonNodeExtension(property.Name));
                    }
                }
            }
        }
    }
}