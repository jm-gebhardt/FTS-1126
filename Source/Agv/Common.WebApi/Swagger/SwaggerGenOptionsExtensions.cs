using System.Xml.XPath;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.Acc.Common.WebApi.Swagger;

/// <summary>
/// SwaggerGenOptions
/// </summary>
public static class SwaggerGenOptionsExtensions
{
    /// <param name="swaggerGenOptions"></param>
    extension(SwaggerGenOptions swaggerGenOptions)
    {
        /// <summary>
        /// Extension to set the SwaggerDoc for an ApiGroup
        /// </summary>
        /// <param name="apiGroup"></param>
        public void SwaggerDoc(IApiGroup apiGroup)
        {
            swaggerGenOptions.SwaggerGeneratorOptions.SwaggerDocs.Add(apiGroup.Name, apiGroup.ApiInfo);
        }

        /// <summary>
        /// Extension to include XML comments in Swagger. It resolves the XML documentation files and adds them to the Swagger configuration.
        /// </summary>
        public void IncludeXmlComments()
        {
            var resolvedXmlDocs = XmlDocResolver.GetResolvedXmlDocs();

            // Add resolved XML docs to Swagger
            foreach (var doc in resolvedXmlDocs)
            {
                swaggerGenOptions.IncludeXmlComments(() => new XPathDocument(doc.CreateReader()), true);
            }
        }
    }
}