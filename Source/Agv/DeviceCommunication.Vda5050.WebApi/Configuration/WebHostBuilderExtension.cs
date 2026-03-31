using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.Common.WebApi.Swagger;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Controllers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Middleware;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Policies;
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
using System.Reflection;
using System.Text.Json.Serialization;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Configuration;

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
        public IWebHostBuilder AddDeviceCommunicationVda5050WebApi()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();

            webHostBuilder.ConfigureServices((context, services) =>
            {
                services.ConfigureAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
                services.AddScoped<ExceptionHandlerMiddleware>();
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
        public IApplicationBuilder UseDeviceCommunicationVda5050WebApi()
        {
            app.UseDefaultFiles(); // Serve Default.htm/html, Index.htm/html by default, without having to explicitly qualify the URL.
            app.UseStaticFiles(); // Allow html, images, etc. in wwwroot directory to be served. (use UseWebRoot to change folder)
            app.UseRouting();
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            app.UseCors();
            app.UseAuthentication(); // verify user
            app.UseAuthorization(); // use security tokens

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks(HealthEndpoint, new HealthCheckOptions()
                {
                    Predicate = _ => true
                });
                endpoints.MapHealthChecks(HealthDetailedEndpoint, new HealthCheckOptions()
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
                options.ShowExtensions();
            });
            return app;
        }
    }
}