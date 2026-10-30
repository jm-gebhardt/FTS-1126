using System.Reflection;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.Common.WebApi.Swagger;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Controllers;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Filters;

namespace Gebhardt.Acc.Vda5050Simulator.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSimulatorSwagger(this IServiceCollection services)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
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
            options.SupportNonNullableReferenceTypes();
            options.UseAllOfToExtendReferenceSchemas();
            options.UseAllOfForInheritance();
            options.CustomSchemaIds(WebApi.Swagger.CustomSchemaSelector.GetSchemaId);
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = JwtBearerDefaults.AuthenticationScheme
            });
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
        return services;
    }

    public static IApplicationBuilder UseSimulatorSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger(c =>
        {
            c.PreSerializeFilters.Add((swaggerDoc, request) =>
            {
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
