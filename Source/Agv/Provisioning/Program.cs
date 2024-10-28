using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Provisioning.Configuration;
using Gebhardt.Acc.Provisioning.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();

try
{
    var hostBuilder = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(conf => { conf.SetMinimumLevel(LogLevel.Information); })
        .ConfigureServices((context, services) =>
        {
            services.ConfigureServicesMassTransit(context.Configuration);
            services.Configure<ConfigOptions>(context.Configuration.GetSection(ConfigOptions.SectionName));

            services.AddScoped<ISeeder, GraphAndMapSeeder>();
            services.AddScoped<ISeeder, TriggerLocationsSeeder>();
            services.AddScoped<ISeeder, AgvsSeeder>();
            services.AddScoped<ISeeder, QueuesSeeder>();
            services.AddSingleton<AgvSeederFactory>();
        });

    var host = hostBuilder.Build();

    List<ISeeder> seeders = host.Services.GetRequiredService<IEnumerable<ISeeder>>().ToList();
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

    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}