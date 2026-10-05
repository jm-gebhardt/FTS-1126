using Gebhardt.Acc.Provisioning.Configuration;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
public class TriggerLocationsSeeder : ISeeder
{
    private readonly string[] _folderPaths = [];
    private readonly IBus _bus;

    [UsedImplicitly]
    private readonly IOptions<ConfigOptions> _configOptions;

    private readonly ILogger<TriggerLocationSeeder> _triggerLocationsLogger;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc cref="TriggerLocationsSeeder"/>
    public TriggerLocationsSeeder(
        IOptions<ConfigOptions> configOptions,
        ILogger<TriggerLocationsSeeder> logger,
        ILogger<TriggerLocationSeeder> triggerLocationsLogger,
        IBus bus)
    {
         _bus = bus;
         _configOptions = configOptions;
         _triggerLocationsLogger = triggerLocationsLogger;
         string triggerBasePath = Path.Join(_configOptions.Value.BasePath, "TriggerLocations");

         if (Directory.Exists(triggerBasePath))
         {
             string[] files = Directory.GetFiles(triggerBasePath, "*", SearchOption.AllDirectories)
                 .Select(f => f.Replace(triggerBasePath, string.Empty)).ToArray();

             logger.LogInformation("All trigger location files:");
             logger.LogInformation("{Files}", string.Join(Environment.NewLine, files));

             _folderPaths = Directory.GetDirectories(triggerBasePath);
         }
         else
         {
             logger.LogInformation("Directory {TriggerBasePath} not found", triggerBasePath);
         }

         Name = $"Trigger location seeder of {_folderPaths.Length} trigger locations";
    }

    /// <inheritdoc />
    public async Task SeedAsync()
    {
        var tasks = _folderPaths.Select(f =>
            Task.Run(async () => await new TriggerLocationSeeder(_bus, f, _triggerLocationsLogger).SeedAsync()));
        await Task.WhenAll(tasks);
    }
}