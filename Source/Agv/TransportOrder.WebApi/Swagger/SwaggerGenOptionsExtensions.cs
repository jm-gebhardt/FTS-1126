using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.Acc.TransportOrder.WebApi.Swagger;

/// <summary>
/// SwaggerGenOptions
/// </summary>
public static class SwaggerGenOptionsExtensions
{
    /// <summary>
    /// Extension to set the SwaggerDoc for an ApiGroup
    /// </summary>
    /// <param name="swaggerGenOptions"></param>
    /// <param name="apiGroup"></param>
    public static void SwaggerDoc(
        this SwaggerGenOptions swaggerGenOptions,
        IApiGroup apiGroup)
    {
        swaggerGenOptions.SwaggerGeneratorOptions.SwaggerDocs.Add(apiGroup.Name, apiGroup.ApiInfo);
    }
}