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
/// Swagger Schema filter to mark all model properties as required unless they are nullable
/// 
/// <see href="https://stackoverflow.com/a/68987970" />
/// </summary>
[UsedImplicitly]
public class RequireNonNullablePropertiesSchemaFilter(
    IOptions<JsonOptions>? mvcJsonOptions,
    IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>? httpJsonOptions
) : ISchemaFilter
{
    private JsonSerializerOptions? SerializerOptions =>
        mvcJsonOptions?.Value.JsonSerializerOptions ?? httpJsonOptions?.Value.SerializerOptions;

    /// <summary>
    /// Add to model. Required all properties where Nullable is false.
    /// </summary>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema model)
        {
            if (model.Properties is null || model.Properties.Count == 0)
            {
                return;
            }

            model.Required ??= new HashSet<string>();

            var additionalRequiredProps = model.Properties
                .Where(x => x.Value.Type.HasValue && !x.Value.Type.Value.HasFlag(JsonSchemaType.Null) && !model.Required.Contains(x.Key))
                .Select(x => x.Key);
            foreach (string propKey in additionalRequiredProps)
            {
                model.Required.Add(propKey);
            }

            var nullabilityContext = new NullabilityInfoContext();
            foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyType = property.PropertyType;
                var underlyingType = Nullable.GetUnderlyingType(propertyType);

                // Skip Nullable<T> value types (e.g., int?, DateTime?)
                if (underlyingType != null)
                {
                    continue;
                }

                // Skip nullable reference types (e.g., ActionGroupDto?, GraphNodeDto?)
                var nullabilityInfo = nullabilityContext.Create(property);
                if (nullabilityInfo.WriteState == NullabilityState.Nullable ||
                    nullabilityInfo.ReadState == NullabilityState.Nullable)
                {
                    continue;
                }

                if (propertyType.IsEnum)
                {
                    continue;
                }
                string jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                                  ?? SerializerOptions?.PropertyNamingPolicy?.ConvertName(property.Name)
                                  ?? property.Name;

                if (!model.Properties.ContainsKey(jsonName) &&
                    !model.Properties.ContainsKey(property.Name))
                {
                    continue;
                }

                string schemaPropertyName = model.Properties.ContainsKey(jsonName) ? jsonName : property.Name;

                model.Required.Add(schemaPropertyName);
            }
        }
    }
}