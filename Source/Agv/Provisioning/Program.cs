using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using Gebhardt.Acc.Provisioning.Configuration;
using Gebhardt.Acc.Provisioning.Services;
using MassTransit;
using Gebhardt.Core.Hosting.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();

try
{
    var hostBuilder = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, conf) =>
        {
            conf.AddCoreConfiguration(context.HostingEnvironment.EnvironmentName);
        })
        .ConfigureLogging(conf => { conf.SetMinimumLevel(LogLevel.Information); })
        .ConfigureServices((context, services) =>
        {
            services.ConfigureServicesMassTransit(context.Configuration, configurator =>
            {
                configurator.AddRequestClient<AddOrUpdateFactsheet>(RequestTimeout.After(m: 2));
                configurator.AddRequestClient<UploadLif>(RequestTimeout.After(m: 3));
                configurator.AddRequestClient<EnsureSimulatedAgvTypeConfiguration>(RequestTimeout.After(m: 2));
            });
            services.AddOptions<MassTransitHostOptions>().Configure(options =>
            {
                options.WaitUntilStarted = true;
                options.StartTimeout = TimeSpan.FromSeconds(30);
            });
            services.Configure<ConfigOptions>(context.Configuration.GetSection(ConfigOptions.SectionName));
            services.AddSingleton(TimeProvider.System);

            services.AddScoped<ISeeder, GraphAndMapSeeder>();
            services.AddScoped<ISeeder, FactsheetSeeder>();
            services.AddScoped<ISeeder, LifSeeder>();
            services.AddScoped<ISeeder, SimulatorConfigSeeder>();
            services.AddScoped<ISeeder, TriggerLocationsSeeder>();
            services.AddScoped<ISeeder, AgvsSeeder>();
            services.AddScoped<ISeeder, QueuesSeeder>();
            services.AddScoped<ISeeder, NodesPropertiesSeeder>();
            services.AddSingleton<AgvSeederFactory>();
        });

    var host = hostBuilder.Build();
    await host.StartAsync();

    try
    {
        await SeedAsync(host);
    }
    finally
    {
        await host.StopAsync();
    }

    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}

static async Task SeedAsync(IHost host)
{
    var seeders = host.Services.GetRequiredService<IEnumerable<ISeeder>>().ToList();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting {SeederCount} seeders", seeders.Count);

    foreach (var seeder in seeders)
    {
        logger.LogInformation("Seeding {SeederName}", seeder.Name);
        await seeder.SeedAsync();
        logger.LogInformation("Seeding {SeederName} done", seeder.Name);
    }

    logger.LogInformation("───────────────────████");
    logger.LogInformation("───────────────────█████");
    logger.LogInformation("───────────────────██████");
    logger.LogInformation("───────────────────███████");
    logger.LogInformation("──────────────────████████");
    logger.LogInformation("──────────────────████████");
    logger.LogInformation("─────────────────█████████");
    logger.LogInformation("────────────────█████████");
    logger.LogInformation("───────────────█████████");
    logger.LogInformation("──────────────█████████");
    logger.LogInformation("──────────────██████████████████");
    logger.LogInformation("────────────█████████████████████");
    logger.LogInformation("───────────███████████████████████");
    logger.LogInformation("████████─██████████████████████████");
    logger.LogInformation("████████─███████████████████████████");
    logger.LogInformation("████████─████████████████████████████");
    logger.LogInformation("████████─████████████████████████████");
    logger.LogInformation("████████─████████████████████████████");
    logger.LogInformation("████████─████████████████████████████");
    logger.LogInformation("████████─███████████████████████████");
    logger.LogInformation("████████─██████████████████████████");
    logger.LogInformation("████████─█████████████████████████");
    logger.LogInformation("████████─████████████████████████");
    logger.LogInformation("████████─███████████████████████");
}