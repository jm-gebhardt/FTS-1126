using Gebhardt.Acc.Provisioning.Configuration;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
public class AgvsSeeder : ISeeder
{
    private readonly AgvSeederFactory _agvSeederFactory;
    private readonly string[] _folderPaths = [];

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc cref="AgvsSeeder"/>
    public AgvsSeeder(
        AgvSeederFactory agvSeederFactory,
        IOptions<ConfigOptions> configOptions,
        ILogger<AgvsSeeder> logger)
    {
        _agvSeederFactory = agvSeederFactory;
        string agvBasePath = Path.Join(configOptions.Value.BasePath, "Agvs");

        if (Directory.Exists(agvBasePath))
        {
            string[] files = Directory.GetFiles(agvBasePath, "*", SearchOption.AllDirectories)
                .Select(f => f.Replace(agvBasePath, string.Empty)).ToArray();

            logger.LogInformation("All agv files:");
            logger.LogInformation("{Files}", string.Join(Environment.NewLine, files));

            _folderPaths = Directory.GetDirectories(agvBasePath);
        }
        else
        {
            logger.LogInformation("Directory {AgvBasePath} not found.", agvBasePath);
        }

        Name = $"Agv seeder of {_folderPaths.Length} Agvs";
    }

    /// <inheritdoc />
    public async Task SeedAsync()
    {
        IEnumerable<Task> tasks = _folderPaths.Select(f =>
            Task.Run(async () => await _agvSeederFactory.Create(f).SeedAsync())
        );
        await Task.WhenAll(tasks);
    }
}