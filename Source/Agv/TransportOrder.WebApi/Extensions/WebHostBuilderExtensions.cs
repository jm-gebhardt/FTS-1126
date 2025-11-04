using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Filter.Api;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.TransportOrder.WebApi.Constants;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.System;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Middleware;
using Gebhardt.Acc.TransportOrder.WebApi.Policies;
using Gebhardt.Acc.TransportOrder.WebApi.Swagger;
using Gebhardt.Acc.TransportOrder.WebApi.Swagger.Examples;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using AgvPositionSignificanceChecker = Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers.AgvPositionSignificanceChecker;

namespace Gebhardt.Acc.TransportOrder.WebApi.Extensions;

/// <summary>
/// IWebHostBuilder extensions
/// </summary>
public static class WebHostBuilderExtensions
{
    private const string AccessToken = "access_token";

    /// <summary>
    /// Configured the WebHostBuilder to use the TransportOrderWebApi <paramref name="webHostBuilder" />
    /// </summary>
    /// <param name="webHostBuilder"></param>
    /// <param name="isSimulationMode">If true, simulation endpoints will be available.</param>
    public static IWebHostBuilder AddTransportOrderWebApi(this IWebHostBuilder webHostBuilder, bool isSimulationMode = false)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();

        webHostBuilder.ConfigureServices((context, services) =>
        {
            services.AddAutoMapper(_ => { }, AppDomain.CurrentDomain.GetAssemblies());

            services.AddTransient<ITriggerPublisher, TriggerPublisher>();
            services.AddTransient<IActionGroupPublisher, ActionGroupPublisher>();
            services.AddTransient<IAgvPublisher, AgvPublisher>();
            services.AddTransient<ISystemPublisher, SystemPublisher>();
            services.AddTransient<IMapPublisher, MapPublisher>();
            services.AddTransient<IRestrictedAreaPublisher, RestrictedAreaPublisher>();
            services.AddTransient<IQueuePublisher, QueuePublisher>();

            services.AddTransient<IMessageFilter<AgvBatteryStatusDto>, AgvBatteryStatusChangedFilter>();
            services.AddTransient<IMessageFilter<AgvPositionStatusDto>, AgvPositionStatusChangedFilter>();

            services.AddSingleton<ISemaphoreProvider<AgvBatteryStatusChangedFilter>, SemaphoreProvider<AgvBatteryStatusChangedFilter>>();
            services.AddSingleton<ISemaphoreProvider<AgvPositionStatusChangedFilter>, SemaphoreProvider<AgvPositionStatusChangedFilter>>();

            services.AddSingleton<ISignificanceChecker<AgvBatteryStatusDto>, AgvBatteryStatusSignificanceChecker>();
            services.AddSingleton<ISignificanceChecker<AgvPositionDto>, AgvPositionSignificanceChecker>();
            services.AddSingleton(new AgvPositionConfiguration(0.5, Mathematics.ConvertToRadians(5), 1000));

            services.AddSingleton(typeof(IHubBroadcaster<,>), typeof(HubBroadcaster<,>));

            services.AddHub<AgvHub>();
            services.AddHub<SystemHub>();
            services.AddHub<ActionGroupHub>();
            services.AddHub<MapHub>();
            services.AddHub<TriggerHub>();
            services.AddHub<RestrictedAreaHub>();
            services.AddHub<QueueHub>();

            services.AddScoped<ExceptionHandlerMiddleware>();

            services.AddControllers(opts =>
                {
                    opts.Conventions.Add(new ExcludeControllersConvention(cm =>
                        !isSimulationMode && cm.ControllerType == typeof(SimulationController)));
                })
                .AddApplicationPart(typeof(SimulationController).Assembly)
                .AddFilterSerializer()
                .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

            var config = context.Configuration.GetSection("KEYCLOAK");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
                {
                    o.MetadataAddress = $"{config["URL"]!}/realms/{config["REALM"]!}/.well-known/openid-configuration";
                    o.RequireHttpsMetadata = false;
                    o.Authority = $"{config["URL"]!}/auth/realms/{config["REALM"]!}";
                    o.TokenValidationParameters.ValidateIssuer = false;
                    o.TokenValidationParameters.ValidateAudience = false;
                    o.TokenValidationParameters.ValidateIssuerSigningKey = false;
                    o.TokenValidationParameters.SignatureValidator =
                        (token, _) =>
                        {
                            var jwt = new JsonWebToken(token);
                            return jwt;
                        };
                    o.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            var accessToken = ctx.Request.Query[AccessToken];

                            if (!string.IsNullOrEmpty(accessToken) &&
                                (ctx.HttpContext.WebSockets.IsWebSocketRequest || ctx.Request.Headers.Accept == "text/event-stream"))
                            {
                                ctx.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(AuthorizationPolicies.Configure);

            services.ConfigureEventLogSettings(executingAssembly);

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(new RestApiGroup());
                options.SwaggerDoc(new HubApiGroup());
                options.AddSignalRSwaggerGen(opt =>
                {
                    opt.ScanAssemblies(executingAssembly);
                    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme }
                            },
                            []
                        }
                    });
                    opt.AddOperationFilter(new SecurityRequirementsOperationFilter(true, JwtBearerDefaults.AuthenticationScheme));
                });
                options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
                options.SchemaFilter<EnumDictionaryOptionalSchemaFilter>();
                options.SupportNonNullableReferenceTypes(); // Sets Nullable flags appropriately.
                options.UseAllOfToExtendReferenceSchemas(); // Allows $ref enums to be nullable
                options.UseAllOfForInheritance(); // Allows $ref objects to be nullable

                options.AddFilterExamples();

                options.CustomSchemaIds(Swagger.CustomSchemaSelector.RemoveDtoSuffix);

                options.AddFilterSwaggerExtensions();
                var securityScheme = new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = JwtBearerDefaults.AuthenticationScheme
                };
                options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme }
                        },
                        []
                    }
                });
                options.OperationFilter<SecurityRequirementsOperationFilter>(JwtBearerDefaults.AuthenticationScheme);
                IncludeXmlComments(options);
            });
            IdentityModelEventSource.ShowPII = true;

            services.AddSignalR(options =>
                {
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(WebApiConstants.ClientTimeoutSeconds);
                    options.KeepAliveInterval = TimeSpan.FromSeconds(WebApiConstants.KeepAliveIntervalSeconds);
                    options.EnableDetailedErrors = true;
                    options.StatefulReconnectBufferSize = WebApiConstants.StatefulReconnectBufferSize;
                })
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
        });

        return webHostBuilder;
    }

    private static void AddFilterExamples(this SwaggerGenOptions options)
    {
        options.OperationFilter<ActionGroupFilterExample>();
        options.OperationFilter<LogEntryFilterExample>();
    }

    private static void AddHub<THub>(this IServiceCollection services) where THub : Hub
    {
        services.AddSingleton<IAbortableHubLifetimeManager<THub>, AbortableHubLifetimeManager<THub>>();
        services.AddSingleton<HubLifetimeManager<THub>>(sp => (HubLifetimeManager<THub>) sp.GetRequiredService<IAbortableHubLifetimeManager<THub>>());
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
            endpoints.MapHub<SystemHub>(SystemHub.UrlPath, o => { o.AllowStatefulReconnects = true; });
            endpoints.MapHub<AgvHub>(AgvHub.UrlPath, o => { o.AllowStatefulReconnects = true; });
            endpoints.MapHub<ActionGroupHub>(ActionGroupHub.UrlPath, o => { o.AllowStatefulReconnects = true; });
            endpoints.MapHub<MapHub>(MapHub.UrlPath, o => { o.AllowStatefulReconnects = true; });
            endpoints.MapHub<TriggerHub>(TriggerHub.UrlPath, o => { o.AllowStatefulReconnects = true; });
            endpoints.MapHub<RestrictedAreaHub>(RestrictedAreaHub.UrlPath, o => { o.AllowStatefulReconnects = true; });
            endpoints.MapHub<QueueHub>(QueueHub.UrlPath, o => { o.AllowStatefulReconnects = true; });
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
            options.ShowExtensions();
        });
        return app;
    }
}