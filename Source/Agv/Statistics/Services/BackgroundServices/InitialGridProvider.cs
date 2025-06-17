using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.BackgroundServices;

/// <summary>
/// Represents the background service that provides an initial grid.
/// </summary>
public class InitialGridProvider : BackgroundService
{
    private readonly IRequestClient<AddGrid> _addGridClient;
    private readonly ILogger<InitialGridProvider> _logger;
    private readonly IStatisticsDb _db;
    private const int ExecutionDelayMs = 1000;

    /// <inheritdoc cref="InitialGridProvider"/>>
    public InitialGridProvider(IRequestClient<AddGrid> addGridClient, IStatisticsDb db, ILogger<InitialGridProvider> logger)
    {
        _addGridClient = addGridClient;
        _logger = logger;
        _db = db;
    }

    private async Task<bool> CreateGridAsync(CancellationToken stoppingToken)
    {
        const int step = 1;
        await _addGridClient.GetResponse<Success>(new AddGrid($"{step}x{step}", step, step), stoppingToken);
        return true;
    }

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InitialGridProvider started");
        bool gridCreated = false;
        ICollection<Grid> grids = await _db.Grids.GetAll();
        if (grids.Count != 0)
        {
            // A grid was created previously.
            gridCreated = true;
        }
        while (!gridCreated && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                gridCreated = await CreateGridAsync(stoppingToken);
            }
            catch (RequestTimeoutException)
            {
                _logger.LogWarning("Creating grid failed, trying again in next cycle.");
                await Task.Delay(ExecutionDelayMs, stoppingToken).WaitAsync(stoppingToken);
            }
        }
    }
}