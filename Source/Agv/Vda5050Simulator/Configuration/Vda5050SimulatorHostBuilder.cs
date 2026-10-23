using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Vda5050Simulator.DataAccess;
using Gebhardt.Acc.Vda5050Simulator.Repositories;
using Microsoft.EntityFrameworkCore;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.Common.WebApi.Constants;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Middleware;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Gebhardt.Acc.Vda5050Simulator.Messaging;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Hubs;
using Prometheus;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gebhardt.Acc.Vda5050Simulator.Configuration;

/// <summary>
/// Provides host building logic.
/// </summary>
public static class Vda5050SimulatorHostBuilder
{
    private const string AccessToken = "access_token";
    private const string HealthEndpoint = "/health";
    private const string HealthDetailedEndpoint = "/healthz";

    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return GebhardtStartup.CreateHostBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddScoped<ExceptionHandlerMiddleware>();
                services.AddControllers()
                    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Insert(0, new AnnotatedEnumConverterFactory()); });
                services.AddHealthChecks();

                var keycloak = context.Configuration.GetSection("KEYCLOAK");
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
                    {
                        o.MetadataAddress = $"{keycloak["URL"]!}/realms/{keycloak["REALM"]!}/.well-known/openid-configuration";
                        o.RequireHttpsMetadata = false;
                        o.Authority = $"{keycloak["URL"]!}/auth/realms/{keycloak["REALM"]!}";
                        o.TokenValidationParameters.ValidateIssuer = false;
                        o.TokenValidationParameters.ValidateAudience = false;
                        o.TokenValidationParameters.ValidateIssuerSigningKey = false;
                        o.TokenValidationParameters.SignatureValidator = (token, _) => new JsonWebToken(token);
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
                services.AddAuthorization();
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
                services.AddSimulatorSwagger();

                services.Configure<MqttOptions>(context.Configuration.GetSection("Mqtt"));
                services.Configure<SimulationOptions>(context.Configuration.GetSection("Simulation"));

                services.AddSingleton(TimeProvider.System);
                services.AddSingleton<ISimulationClock>(sp =>
                {
                    var simulation = context.Configuration.GetSection("Simulation").Get<SimulationOptions>() ?? new SimulationOptions();
                    return new SimulationClock(sp.GetRequiredService<TimeProvider>().GetUtcNow(), simulation.Speedup);
                });
                services.AddAgvPublishing();
                services.ConfigureServicesMassTransit(context.Configuration,
                    x => x.AddConsumer<EnsureSimulatedAgvTypeConfigurationConsumer>());
                services.AddSingleton<IFactsheetProvider, MassTransitFactsheetProvider>();

                services.AddSingleton<FleetHost>();
                services.AddHostedService(sp => sp.GetRequiredService<FleetHost>());
                services.AddDbContextFactory<Vda5050SimulatorDbContext>(builder =>
                {
                    string? connectionString = context.Configuration.GetConnectionString(Vda5050SimulatorDbContext.ConfigSectionName);
                    builder.UseSqlServer(connectionString, m =>
                    {
                        m.MigrationsHistoryTable(IDbContext<Vda5050SimulatorDbContext>.MigrationTableName);
                        m.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                    builder.AddInterceptors(new EfCommandMetricsInterceptor());
                });
                services.AddTransient<ISimulatedAgvTypeConfigurationRepository, EfSimulatedAgvTypeConfigurationRepository>();
                services.AddSingleton<SimulatedAgvTypeConfigurationService>();
                services.AddHostedService<AgvTypeConfigurationLoader>();
                services.AddHostedService<SimulationLoop>();
                services.AddHostedService<MqttGateway>();
            })
            .ConfigureWebHost(host =>
            {
                host.UseKestrel()
                    .ConfigureAppConfiguration((_, config) =>
                    {
                        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                        config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                    })
                    .Configure(app =>
                    {
                        app.UseMetricServer();
                        app.UseDefaultFiles();
                        app.UseStaticFiles();
                        app.UseRouting();
                        app.UseMiddleware<ExceptionHandlerMiddleware>();
                        app.UseCors();
                        app.UseWebSockets();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapHub<SimulatedAgvStateHub>(SimulatedAgvStateHub.UrlPath, o => { o.AllowStatefulReconnects = true; });
                            endpoints.MapHealthChecks(HealthEndpoint, new HealthCheckOptions { Predicate = _ => true });
                            endpoints.MapHealthChecks(HealthDetailedEndpoint, new HealthCheckOptions
                            {
                                Predicate = _ => true,
                                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                            });
                        });
                        app.UseSimulatorSwagger();
                        app.UseDeveloperExceptionPage();
                    });
            });
    }
}
