using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.Statistics.WebApi.Controllers;
using Gebhardt.Acc.Statistics.WebApi.Middleware;
using Gebhardt.Acc.Statistics.WebApi.Swagger;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.Acc.Statistics.WebApi;

/// <summary>
/// Provides extension methods for <see cref="IWebHostBuilder"/>
/// </summary>
public static class WebHostBuilderExtension
{
    /// <summary>
    /// Health Endpoint name
    /// </summary>
    private const string HealthEndpoint = "/health";

    /// <summary>
    /// Extended health endpoint name with detailed information
    /// </summary>
    private const string HealthDetailedEndpoint = "/healthz";

    /// <summary>
    /// Add web api.
    /// </summary>
    /// <param name="webHostBuilder"></param>
    /// <returns></returns>
    public static IWebHostBuilder AddStatisticsWebApi(this IWebHostBuilder webHostBuilder)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();

        webHostBuilder.ConfigureServices((context, services) =>
        {
            services.ConfigureAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
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
                        (token, _) =>
                        {
                            var jwt = new JsonWebToken(token);
                            return jwt;
                        };
                });
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(new RestApiGroup());
                options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
                options.SchemaFilter<RequireNonNullableEnumPropertiesSchemaFilter>();
                options.SupportNonNullableReferenceTypes(); // Sets Nullable flags appropriately.
                options.UseAllOfToExtendReferenceSchemas(); // Allows $ref enums to be nullable
                options.UseAllOfForInheritance(); // Allows $ref objects to be nullable
                options.CustomSchemaIds(CustomSchemaSelector.RemoveDtoSuffix);
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
                        new List<string>()
                    }
                });
                options.OperationFilter<SecurityRequirementsOperationFilter>(JwtBearerDefaults.AuthenticationScheme);
                IncludeXmlComments(options);
            });
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
        var resolvedXmlDocs = XmlDocResolver.ResolveInheritedDocs(xmlDocs, assemblies);

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
    public static IApplicationBuilder UseStatisticsWebApi(this IApplicationBuilder app)
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
        app.UseSwaggerUI(options => { options.SwaggerEndpoint(new RestApiGroup()); });
        app.UseSwaggerUI();
        return app;
    }
}