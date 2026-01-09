using JetBrains.Annotations;
using Microsoft.OpenApi;
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
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema openApiSchema)
        {
            if (context.Type.IsGenericType &&
                context.Type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyType = context.Type.GetGenericArguments()[0];

                if (keyType.IsEnum)
                {
                    openApiSchema.Required ??= new HashSet<string>();
                    openApiSchema.Required.Clear();
                    if (openApiSchema.Properties is null)
                    {
                        return;
                    }
                    foreach (string key in keyType.GetEnumNames())
                    {
                        if (openApiSchema.Properties.TryGetValue(key, out var property) && property is OpenApiSchema { Type: not null } propertySchema)
                        {
                            propertySchema.Type = propertySchema.Type.Value & ~JsonSchemaType.Null;
                        }
                    }
                }
            }
        }
    }
}