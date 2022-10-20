using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.SchemaFilter
{
    /// <summary>
    /// Swagger Schema filter to mark all model properties as required unless they are nullable
    /// 
    /// <see href="https://stackoverflow.com/a/68987970" />
    /// </summary>
    [UsedImplicitly]
    public class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
    {
        /// <summary>
        /// Add to model.Required all properties where Nullable is false.
        /// </summary>
        public void Apply(OpenApiSchema model, SchemaFilterContext context)
        {
            IEnumerable<string> additionalRequiredProps = model.Properties
                .Where(x => !x.Value.Nullable && !model.Required.Contains(x.Key))
                .Select(x => x.Key);
            foreach (string? propKey in additionalRequiredProps)
            {
                model.Required.Add(propKey);
            }
        }
    }
}