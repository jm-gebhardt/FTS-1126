using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;
using Gebhardt.StoreWare.Agv.Common.WebApi;
using Gebhardt.StoreWare.Agv.DeviceCommunication.WebApi.Controllers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.WebApi.Swagger;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.WebApi;

/// <summary>
/// Provides extension methods for <see cref="IWebHostBuilder"/>
/// </summary>
public static class WebHostBuilderExtension
{
    /// <summary>
    /// Add web api.
    /// </summary>
    /// <param name="webHostBuilder"></param>
    /// <returns></returns>
    public static IWebHostBuilder AddDeviceCommunicationWebApi(this IWebHostBuilder webHostBuilder)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();

        webHostBuilder.ConfigureServices(services =>
        {
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddMvc();
            services.AddControllers()
                .AddApplicationPart(executingAssembly)
                .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(new RestApiGroup());
                options.SupportNonNullableReferenceTypes(); // Sets Nullable flags appropriately.
                options.UseAllOfToExtendReferenceSchemas(); // Allows $ref enums to be nullable
                options.UseAllOfForInheritance(); // Allows $ref objects to be nullable
                options.CustomSchemaIds(CustomSchemaSelector.RemoveDtoSuffix);

                IncludeXmlComments(options);
            });
        });
        return webHostBuilder;
    }

    private static void IncludeXmlComments(SwaggerGenOptions options)
    {
        IEnumerable<XDocument> resolvedXmlDocs = XmlDocResolver.GetResolvedXmlDocs();

        // Add resolved XML docs to Swagger
        foreach (var doc in resolvedXmlDocs)
        {
            options.IncludeXmlComments(() => new XPathDocument(doc.CreateReader()), true);
        }
    }

    /// <summary>
    /// Add web functionality to the application />
    /// </summary>
    /// <param name="app"></param>
    // ReSharper disable UnusedMethodReturnValue.Global
    public static IApplicationBuilder UseDeviceCommunicationWebApi(this IApplicationBuilder app)
    {
        app.UseDefaultFiles(); // Serve Default.htm/html, Index.htm/html by default, without having to explicitly qualify the URL.
        app.UseStaticFiles(); // Allow html, images, etc. in wwwroot directory to be served. (use UseWebRoot to change folder)
        app.UseRouting();
        app.UseCors();
        app.UseWebSockets();
        app.UseAuthentication(); // verify user
        app.UseAuthorization(); // use security tokens

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        app.UseSwagger(c =>
        {
            c.PreSerializeFilters.Add((swaggerDoc, request) =>
            {
                // If the request is coming from a proxy, set the server path to the proxy sub path.
                if (request.Headers.TryGetValue("X-Forwarded-Prefix", out var serverPath))
                {
                    swaggerDoc.Servers = new List<OpenApiServer>()
                    {
                        new() { Description = "Server behind Proxy", Url = serverPath }
                    };
                }
            });
        });
        app.UseSwaggerUI(options => { options.SwaggerEndpoint(new RestApiGroup()); });
        return app;
    }
}