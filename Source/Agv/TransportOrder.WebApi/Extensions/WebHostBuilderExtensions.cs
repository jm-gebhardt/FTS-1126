using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Middleware;
using Gebhardt.Acc.TransportOrder.WebApi.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
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
    public static IWebHostBuilder AddTransportOrderWebApi(this IWebHostBuilder webHostBuilder)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();

        webHostBuilder.ConfigureServices((context, services) =>
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
                    delegate(string token, TokenValidationParameters _)
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
                            (ctx.HttpContext.WebSockets.IsWebSocketRequest || ctx.Request.Headers["Accept"] == "text/event-stream"))
                        {
                            ctx.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

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
                            new string[] { }
                        }
                    });
                    opt.AddOperationFilter(new SecurityRequirementsOperationFilter(true, JwtBearerDefaults.AuthenticationScheme));
                });
                options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
                options.SupportNonNullableReferenceTypes(); // Sets Nullable flags appropriately.
                options.UseAllOfToExtendReferenceSchemas(); // Allows $ref enums to be nullable
                options.UseAllOfForInheritance(); // Allows $ref objects to be nullable
                options.CustomSchemaIds(Swagger.CustomSchemaSelector.RemoveDtoSuffix);
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
                        new string[] {}
                    }
                });
                options.OperationFilter<SecurityRequirementsOperationFilter>(JwtBearerDefaults.AuthenticationScheme);
                IncludeXmlComments(options);
            });
            IdentityModelEventSource.ShowPII = true;

            services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                })
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