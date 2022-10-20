using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.SchemaFilter;

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
            var assembly = Assembly.GetExecutingAssembly();

            webHostBuilder.ConfigureServices(services =>
            {
                services.AddMvc();

                services.AddControllers()
                    .AddApplicationPart(assembly)
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });

                services.Configure<Microsoft.Extensions.Logging.EventLog.EventLogSettings>(settings =>
                {
                    settings.SourceName = assembly.GetName().Name ?? "WebService";
                });
                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "TransportOrder Monitoring API"
                    });
                    options.AddSignalRSwaggerGen();

                    // Set the comments path for the Swagger JSON and UI.
                    string xmlFile = $"{assembly.GetName().Name}.xml";
                    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    options.IncludeXmlComments(xmlPath);

                    options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
                    options.SupportNonNullableReferenceTypes(); // Sets Nullable flags appropriately.
                    options.UseAllOfToExtendReferenceSchemas(); // Allows $ref enums to be nullable
                    options.UseAllOfForInheritance(); // Allows $ref objects to be nullable
                });
                services.AddSignalR();
            });

            return webHostBuilder;
        }

        /// <summary>
        /// Add web funtionality to the application />
        /// </summary>
        /// <param name="app"></param>
        // ReSharper disable UnusedMethodReturnValue.Global
        public static IApplicationBuilder UseTransportOrderWebApi(this IApplicationBuilder app)
        {
            app.UseDefaultFiles(); // Serve Default.htm/html, Index.htm/html by default, without having to explicitly qualify the URL.
            app.UseStaticFiles(); // Allow html, images, etc. in wwwroot directory to be served. (use UseWebRoot to change folder)
            app.UseRouting();
            app.UseCors();
            app.UseWebSockets();
            app.UseAuthentication(); // verify user
            app.UseAuthorization(); // use security tokens

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<OrderHub>(OrderHub.UrlPath);
                endpoints.MapHub<AgvHub>(AgvHub.UrlPath);
            });

            app.UseSwagger();
            app.UseSwaggerUI();

            return app;
        }
    }
}
