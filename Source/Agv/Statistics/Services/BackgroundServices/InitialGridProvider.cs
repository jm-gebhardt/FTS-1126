using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.BackgroundServices;

/// <summary>
/// Represents the background service that provides an initial grid.
/// </summary>
public class InitialGridProvider : BackgroundService
{
    private readonly ILogger<InitialGridProvider> _logger;
    private readonly IServiceProvider _serviceProvider;

    private const int ExecutionDelayMs = 1000;

    /// <inheritdoc cref="InitialGridProvider"/>>
    public InitialGridProvider(ILogger<InitialGridProvider> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private async Task<bool> CreateGridAsync(CancellationToken stoppingToken)
    {
        const int step = 1;

        // Create scope to get the scoped IRequestClient
        await using var scope = _serviceProvider.CreateAsyncScope();
        var addGridClient = scope.ServiceProvider.GetRequiredService<IRequestClient<AddGrid>>();
        await addGridClient.GetResponse<Success>(new AddGrid($"{step}x{step}", step, step), stoppingToken);
        return true;
    }

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InitialGridProvider started");
        bool gridCreated = false;

        await using (var scope = _serviceProvider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IStatisticsDb>();

            ICollection<Grid> grids = await db.Grids.GetAll();
            if (grids.Count != 0)
            {
                // A grid was created previously.
                gridCreated = true;
            }
        }

        while (!gridCreated && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                gridCreated = await CreateGridAsync(stoppingToken);
            }
            catch (RequestTimeoutException)
            {
                _logger.LogWarning("Creating grid failed, trying again in next cycle");
                await Task.Delay(ExecutionDelayMs, stoppingToken).WaitAsync(stoppingToken);
            }
        }
    }
}