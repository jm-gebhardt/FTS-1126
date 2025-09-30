using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.Acc.Common.WebApi;

/// <summary>
/// Swagger Schema filter to make keys optional in schemas of Dictionary types with enum keys.
/// This is useful when generating clients, as all enum keys would be exported in the OpenAPI schema as required properties.
/// </summary>
[UsedImplicitly]
public class EnumDictionaryOptionalSchemaFilter : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsGenericType &&
            context.Type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var keyType = context.Type.GetGenericArguments()[0];

            if (keyType.IsEnum)
            {
                schema.Required.Clear();

                foreach (string key in keyType.GetEnumNames())
                {
                    if (schema.Properties.TryGetValue(key, out var property))
                    {
                        property.Nullable = false;
                    }
                }
            }
        }
    }
}