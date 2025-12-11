using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
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
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null || schema.Properties.Count == 0)
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

            if (!schema.Properties.TryGetValue(jsonName, out var propSchema) && !schema.Properties.TryGetValue(property.Name, out propSchema))
            {
                continue;
            }

            if (isFilterable)
            {
                propSchema.AddExtension("x-IsFilterableProperty", new OpenApiBoolean(true));
                propSchema.AddExtension("x-FilterablePropertyName", new OpenApiString(property.Name));
            }

            if (supportsWhereAggregation)
            {
                propSchema.AddExtension("x-SupportsWhereAggregationFilter", new OpenApiBoolean(true));
                propSchema.AddExtension("x-FilterablePropertyName", new OpenApiString(property.Name));
            }
        }
    }
}