using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.Common.Services.Significance;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.WebApi;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Middleware;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.SchemaFilter;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using AgvPositionSignificanceChecker = Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers.AgvPositionSignificanceChecker;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi;

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
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddTransient<IActionGroupPublisher, ActionGroupPublisher>();
            services.AddTransient<IAgvPublisher, AgvPublisher>();
            services.AddTransient<IMapPublisher, MapPublisher>();

            services.AddTransient<IMessageFilter<AgvBatteryStatusDto>, AgvBatteryStatusChangedFilter>();
            services.AddTransient<IMessageFilter<AgvPositionStatusDto>, AgvPositionStatusChangedFilter>();

            services.AddSingleton<ISemaphoreProvider<AgvBatteryStatusChangedFilter>, SemaphoreProvider<AgvBatteryStatusChangedFilter>>();
            services.AddSingleton<ISemaphoreProvider<AgvPositionStatusChangedFilter>, SemaphoreProvider<AgvPositionStatusChangedFilter>>();

            services.AddSingleton<ISignificanceChecker<AgvBatteryStatusDto>, AgvBatteryStatusSignificanceChecker>();
            services.AddSingleton<ISignificanceChecker<AgvPositionDto>, AgvPositionSignificanceChecker>();
            services.AddSingleton(new AgvPositionConfiguration(0.5, Mathematics.ConvertToRadians(5), 1000));
            
            services.AddScoped<ExceptionHandlerMiddleware>();
            services.AddMvc();
            services.AddControllers()
                .AddApplicationPart(executingAssembly)
                .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

            services.ConfigureEventLogSettings(executingAssembly);

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(new RestApiGroup());
                options.SwaggerDoc(new HubApiGroup());
                options.AddSignalRSwaggerGen(opt => { opt.ScanAssemblies(executingAssembly); });
                options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
                options.SupportNonNullableReferenceTypes(); // Sets Nullable flags appropriately.
                options.UseAllOfToExtendReferenceSchemas(); // Allows $ref enums to be nullable
                options.UseAllOfForInheritance(); // Allows $ref objects to be nullable
                options.CustomSchemaIds(CustomSchemaSelector.RemoveDtoSuffix);

                IncludeXmlComments(options);
            });
            services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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
            endpoints.MapHub<ActionGroupHub>(ActionGroupHub.UrlPath);
            endpoints.MapHub<MapHub>(MapHub.UrlPath);
        });

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
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(new RestApiGroup());
            options.SwaggerEndpoint(new HubApiGroup());
        });
        app.UseSwaggerUI();
        return app;
    }
}