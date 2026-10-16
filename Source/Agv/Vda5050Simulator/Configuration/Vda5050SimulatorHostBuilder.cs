using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.WebApi.Constants;
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
    private const string HealthEndpoint = "/health";

    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return GebhardtStartup.CreateHostBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddControllers();
                services.AddHealthChecks();
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
                services.Configure<List<AgvOptions>>(context.Configuration.GetSection("Agvs"));

                services.AddSingleton(TimeProvider.System);
                services.AddSingleton<ISimulationClock>(sp =>
                {
                    var simulation = context.Configuration.GetSection("Simulation").Get<SimulationOptions>() ?? new SimulationOptions();
                    return new SimulationClock(sp.GetRequiredService<TimeProvider>().GetUtcNow(), simulation.Speedup);
                });
                services.AddAgvPublishing();

                services.AddSingleton<FleetHost>();
                services.AddHostedService(sp => sp.GetRequiredService<FleetHost>());
                services.AddHostedService<SimulationLoop>();
                services.AddHostedService<MqttGateway>();
            })
            .ConfigureWebHost(host =>
            {
                host.UseKestrel()
                    .Configure(app =>
                    {
                        app.UseMetricServer();
                        app.UseRouting();
                        app.UseWebSockets();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapHub<SimulatedAgvStateHub>(SimulatedAgvStateHub.UrlPath, o => { o.AllowStatefulReconnects = true; });
                            endpoints.MapHealthChecks(HealthEndpoint);
                        });
                        app.UseSimulatorSwagger();
                        app.UseDeveloperExceptionPage();
                    });
            });
    }
}
