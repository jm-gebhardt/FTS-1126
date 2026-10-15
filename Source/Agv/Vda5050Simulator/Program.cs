using Gebhardt.Acc.Vda5050Simulator.Configuration;
using Gebhardt.Acc.Vda5050Simulator.Messaging;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("Mqtt"));
builder.Services.Configure<SimulationOptions>(builder.Configuration.GetSection("Simulation"));
builder.Services.Configure<List<AgvOptions>>(builder.Configuration.GetSection("Agvs"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISimulationClock>(sp =>
{
    var simulation = builder.Configuration.GetSection("Simulation").Get<SimulationOptions>() ?? new SimulationOptions();
    return new SimulationClock(sp.GetRequiredService<TimeProvider>().GetUtcNow(), simulation.Speedup);
});
builder.Services.AddSingleton<MqttSendQueue>();
builder.Services.AddSingleton<IAgvPublisher>(sp => sp.GetRequiredService<MqttSendQueue>());
builder.Services.AddSingleton<FleetHost>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<FleetHost>());
builder.Services.AddHostedService<SimulationLoop>();
builder.Services.AddHostedService<MqttGateway>();

await builder.Build().RunAsync();
