using Gebhardt.Acc.Vda5050Simulator.Configuration;
using Gebhardt.Acc.Vda5050Simulator.Messaging;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSimulatorSwagger();

builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("Mqtt"));
builder.Services.Configure<SimulationOptions>(builder.Configuration.GetSection("Simulation"));
builder.Services.Configure<List<AgvOptions>>(builder.Configuration.GetSection("Agvs"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISimulationClock>(sp =>
{
    var simulation = builder.Configuration.GetSection("Simulation").Get<SimulationOptions>() ?? new SimulationOptions();
    return new SimulationClock(sp.GetRequiredService<TimeProvider>().GetUtcNow(), simulation.Speedup);
});
builder.Services.AddAgvPublishing();

builder.Services.AddSingleton<FleetHost>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<FleetHost>());
builder.Services.AddHostedService<SimulationLoop>();
builder.Services.AddHostedService<MqttGateway>();

var app = builder.Build();

app.UseSimulatorSwagger();
app.MapControllers();
app.MapHub<SimulatedAgvStateHub>(SimulatedAgvStateHub.UrlPath);

await app.RunAsync();
