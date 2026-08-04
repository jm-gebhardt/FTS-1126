using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.Common.WebApi.Constants;
using Gebhardt.Acc.Common.WebApi.Hubs;
using Gebhardt.Acc.Common.WebApi.Swagger;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Controllers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Middleware;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Filters;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Configuration;

/// <summary>
/// Provides extension method for web host building.
/// </summary>
public static class WebHostBuilderExtension
{
    private const string AccessToken = "access_token";

    /// <summary>
    /// Health Endpoint name 
    /// </summary>
    private const string HealthEndpoint = "/health";

    /// <summary>
    /// Extended health endpoint name with detailed information
    /// </summary>
    private const string HealthDetailedEndpoint = "/healthz";

    extension(IWebHostBuilder webHostBuilder)
    {
        /// <summary>
        /// Configure the web api.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IWebHostBuilder AddSimulatorWebApi()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();

            webHostBuilder.ConfigureServices((context, services) =>
            {
                services.ConfigureAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
                services.AddScoped<ExceptionHandlerMiddleware>();
                services.AddControllers()
                    .AddApplicationPart(executingAssembly)
                    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Insert(0, new AnnotatedEnumConverterFactory()); });

                services.AddTransient<ISimulatedAgvStatePublisher, SimulatedAgvStatePublisher>();
                services.AddSingleton(typeof(IHubBroadcaster<,>), typeof(HubBroadcaster<,>));
                services.AddHub<SimulatedAgvStateHub>();
                services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));
                services.AddSingleton(typeof(IConcurrentDictionaryProvider<>), typeof(ConcurrentDictionaryProvider<>));
                services.AddSingleton(typeof(IKeyedSemaphoreProvider<>), typeof(KeyedSemaphoreProvider<>));
                services.AddTransient<IMessageFilter<Vda5050StateDto>, Vda5050StateKeyedFilter>();
                services.AddSingleton<ISignificanceChecker<Vda5050StateDto>, Vda5050StateSignificanceChecker>();
                services.AddSingleton(new Vda5050StateConfiguration
                {
                    MinInterval = TimeSpan.FromMilliseconds(250),
                    MaxInterval = TimeSpan.FromSeconds(10)
                });
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
                                new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme),
                                []
                            }
                        });
                        opt.AddOperationFilter(new SecurityRequirementsOperationFilter(true, JwtBearerDefaults.AuthenticationScheme));
                    });
                    options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
                    options.SchemaFilter<RequireNonNullableEnumPropertiesSchemaFilter>();
                    options.SchemaFilter<EnumDictionaryOptionalSchemaFilter>();
                    options.SupportNonNullableReferenceTypes(); // Sets Nullable flags appropriately.
                    options.UseAllOfToExtendReferenceSchemas(); // Allows $ref enums to be nullable
                    options.UseAllOfForInheritance(); // Allows $ref objects to be nullable

                    options.CustomSchemaIds(Swagger.CustomSchemaSelector.GetSchemaId);

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
                    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme),
                            []
                        }
                    });
                    options.OperationFilter<SecurityRequirementsOperationFilter>(JwtBearerDefaults.AuthenticationScheme);
                    options.IncludeXmlComments();
                });
                IdentityModelEventSource.ShowPII = true;
            });
            return webHostBuilder;
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Add web functionality to the application />
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IApplicationBuilder UseSimulatorWebApi()
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
                endpoints.MapHub<SimulatedAgvStateHub>(SimulatedAgvStateHub.UrlPath, o => { o.AllowStatefulReconnects = true; });
                endpoints.MapHealthChecks(HealthEndpoint, new HealthCheckOptions
                {
                    Predicate = _ => true
                });
                endpoints.MapHealthChecks(HealthDetailedEndpoint, new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, request) =>
                {
                    // If the request is coming from a proxy, set the server path to the proxy sub path.
                    if (request.Headers.TryGetValue("X-Forwarded-Prefix", out var serverPath))
                    {
                        swaggerDoc.Servers = new List<OpenApiServer>
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
}