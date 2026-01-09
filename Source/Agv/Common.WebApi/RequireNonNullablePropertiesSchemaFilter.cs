using JetBrains.Annotations;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.Acc.Common.WebApi;

/// <summary>
/// Swagger Schema filter to mark all model properties as required unless they are nullable
/// 
/// <see href="https://stackoverflow.com/a/68987970" />
/// </summary>
[UsedImplicitly]
public class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Add to model. Required all properties where Nullable is false.
    /// </summary>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema model)
        {
            if (model.Properties is null)
            {
                return;
            }

            model.Required ??= new HashSet<string>();

            IEnumerable<string> additionalRequiredProps = model.Properties
                .Where(x => x.Value.Type.HasValue && !x.Value.Type.Value.HasFlag(JsonSchemaType.Null) && !model.Required.Contains(x.Key))
                .Select(x => x.Key);
            foreach (string propKey in additionalRequiredProps)
            {
                model.Required.Add(propKey);
            }
        }
    }
}