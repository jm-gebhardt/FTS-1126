using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gebhardt.StoreWare.Agv.Common.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Performs the periodic trigger of the ActionManager logic
/// </summary>
public class ActionManagerBackgroundService : BackgroundService
{
    private const int PeriodicAssignmentTaskPeriodInMs = 10000;

    private readonly ILogger _logger;
    private readonly MassTransitBusEventConsumer _busEventConsumer;
    private readonly ActionManager _actionManager;


    /// <summary>
    /// Background service for cyclical tasks in action mananger.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="busEventConsumer"></param>
    /// <param name="actionManager"></param>
    public ActionManagerBackgroundService(ILogger<ActionManagerBackgroundService> logger,
                                          MassTransitBusEventConsumer busEventConsumer,
                                          ActionManager actionManager)
    {
        _logger = logger;
        _busEventConsumer = busEventConsumer;
        _actionManager = actionManager;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_busEventConsumer.BusStarted)
            {
                try
                {
                    var cancellationToken = new CancellationTokenSource(60 * 1000).Token;
                    await _actionManager.RequestAssignmentAsync(cancellationToken);
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, $"Failed to {nameof(ExecuteAsync)}");
                }
            }
            else
            {
                _logger.LogTrace("{Service} is waiting for bus initialization being finished", nameof(ActionManagerBackgroundService));
            }

            await Task.Delay(PeriodicAssignmentTaskPeriodInMs, stoppingToken);
        }
    }
}