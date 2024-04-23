using Gebhardt.Acc.Provisioning.Configuration;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
internal class AgvsSeeder : ISeeder
{
    private readonly AgvSeederFactory _agvSeederFactory;
    private readonly string[] _folderPaths;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc cref="AgvsSeeder"/>
    public AgvsSeeder(
        AgvSeederFactory agvSeederFactory,
        IOptions<ConfigOptions> configOptions,
        ILogger<AgvsSeeder> logger)
    {
        _agvSeederFactory = agvSeederFactory;
        string basePath = configOptions.Value.BasePath;

        string[] files = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories)
            .Select(f => f.Replace(basePath, string.Empty)).ToArray();

        logger.LogInformation("All files:");
        logger.LogInformation("{Files}", string.Join(Environment.NewLine, files));

        _folderPaths = Directory.GetDirectories(basePath);

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