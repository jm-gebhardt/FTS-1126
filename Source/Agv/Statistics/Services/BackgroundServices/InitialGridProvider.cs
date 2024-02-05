using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.Statistics.Services.BackgroundServices;

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

    private async Task<bool> CreateGridsAsync(CancellationToken stoppingToken)
    {
        foreach (int step in new[] { 1, 5 })
        {
            await _addGridClient.GetResponse<Success>(new AddGrid($"{step}x{step}", step, step), stoppingToken);
        }
        return true;
    }

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InitialGridProvider started");
        bool gridCreated = false;
        ICollection<Grid> grids = await _db.Grids.GetAll();
        if (grids.Any())
        {
            // A grid was created previously.
            gridCreated = true;
        }
        while (!gridCreated && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                gridCreated = await CreateGridsAsync(stoppingToken);
            }
            catch (RequestTimeoutException)
            {
                _logger.LogWarning("Creating grid failed, trying again in next cycle.");
                await Task.Delay(ExecutionDelayMs, stoppingToken).WaitAsync(stoppingToken);
            }
        }
    }
}