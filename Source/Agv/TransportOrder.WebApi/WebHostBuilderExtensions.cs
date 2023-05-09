using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.SchemaFilter;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi
{
    /// <summary>
    /// IWebHostBuilder extensions
    /// </summary>
    public static class WebHostBuilderExtensions
    {
        /// <summary>
        /// Configured the WebHostBuilder to used the TransportOrderWebApi <paramref name="webHostBuilder" />
        /// </summary>
        /// <param name="webHostBuilder"></param>
        public static IWebHostBuilder AddTransportOrderWebApi(this IWebHostBuilder webHostBuilder)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();

            webHostBuilder.ConfigureServices(services =>
            {
                services.AddScoped<ExceptionHandlerMiddleware>();
                services.AddMvc();
                services.AddControllers()
                    .AddApplicationPart(executingAssembly)
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });

                services.ConfigureEventLogSettings(executingAssembly);

                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "TransportOrder Monitoring API"
                    });
                    options.AddSignalRSwaggerGen();
                    options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
                    options.SupportNonNullableReferenceTypes(); // Sets Nullable flags appropriately.
                    options.UseAllOfToExtendReferenceSchemas(); // Allows $ref enums to be nullable
                    options.UseAllOfForInheritance(); // Allows $ref objects to be nullable
                    options.CustomSchemaIds(CustomSchemaSelector.RemoveDtoSuffix);

                    IncludeXmlComments(options);
                });
                services.AddSignalR();
            });

            return webHostBuilder;
        }

        private static void IncludeXmlComments(SwaggerGenOptions options)
        {
            var xmlDocs = new List<XDocument>();
            var assemblies = new List<Assembly>();

            // Load assemblies and their corresponding XML documentation files
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string path = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(path))
                {
                    xmlDocs.Add(XDocument.Load(path));
                    assemblies.Add(assembly);
                }
            }

            // Resolve <inheritdoc/> comments so they show up correctly in the API
            IEnumerable<XDocument> resolvedXmlDocs = XmlDocResolver.ResolveInheritedDocs(xmlDocs, assemblies);

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
        public static IApplicationBuilder UseTransportOrderWebApi(this IApplicationBuilder app)
        {
            app.UseDefaultFiles(); // Serve Default.htm/html, Index.htm/html by default, without having to explicitly qualify the URL.
            app.UseStaticFiles(); // Allow html, images, etc. in wwwroot directory to be served. (use UseWebRoot to change folder)
            app.UseRouting();
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            app.UseCors();
            app.UseWebSockets();
            app.UseAuthentication(); // verify user
            app.UseAuthorization(); // use security tokens

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<AgvHub>(AgvHub.UrlPath);
            });

            app.UseSwagger();
            app.UseSwaggerUI();
            return app;
        }
    }
}
