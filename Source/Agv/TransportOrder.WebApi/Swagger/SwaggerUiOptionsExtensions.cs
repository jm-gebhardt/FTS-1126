using Microsoft.AspNetCore.Builder;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Gebhardt.Acc.TransportOrder.WebApi.Swagger;

/// <summary>
/// SwaggerUIOptions Extensions
/// </summary>
public static class SwaggerUiOptionsExtensions
{
    /// <summary>
    /// Extension to set the SwaggerEndpoint for an ApiGroup
    /// </summary>
    /// <param name="options"></param>
    /// <param name="apiGroup"></param>
    public static void SwaggerEndpoint(this SwaggerUIOptions options, IApiGroup apiGroup)
    {
        options.SwaggerEndpoint($"{apiGroup.Name}/swagger.json", apiGroup.Title);
    }
}