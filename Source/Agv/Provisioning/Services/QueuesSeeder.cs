using Gebhardt.Acc.Provisioning.Configuration;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
public class QueuesSeeder : ISeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBus _bus;
    private readonly IOptions<ConfigOptions> _configOptions;
    private readonly string[] _queueFolders;
    private readonly ILogger<QueuesSeeder> _logger;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc cref="QueuesSeeder"/>
    public QueuesSeeder(IOptions<ConfigOptions> configOptions,
        ILogger<QueuesSeeder> logger,
        IServiceProvider serviceProvider,
        IBus bus)
    {
        _serviceProvider = serviceProvider;
        _bus = bus;
        _logger = logger;
        _configOptions = configOptions;
        _queueFolders = GetPaths();
        Name = $"Queue seeder for {_queueFolders.Length} queues";
    }

    private string[] GetPaths()
    {
        string basePath = Path.Combine(_configOptions.Value.BasePath, "Queues");

        if (!Directory.Exists(basePath))
        {
            _logger.LogInformation("Directory {BasePath} not found.", basePath);
            return [];
        }
        string[] files = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories)
            .Select(f => f.Replace(basePath, string.Empty)).ToArray();

        _logger.LogInformation("Queue location files:");
        _logger.LogInformation("{Files}", string.Join(Environment.NewLine, files));

        return Directory.GetDirectories(basePath);
    }

    /// <inheritdoc />
    public async Task SeedAsync()
    {
        IEnumerable<Task> seedingTasks = _queueFolders.Select(f => Task.Run(async () =>
            await new QueueSeeder(_bus, f, _serviceProvider.GetRequiredService<ILogger<QueueSeeder>>()).SeedAsync()));
        await Task.WhenAll(seedingTasks);
    }
}