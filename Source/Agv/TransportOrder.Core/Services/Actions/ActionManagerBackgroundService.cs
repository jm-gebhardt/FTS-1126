using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Performs the periodic trigger of the ActionManager logic
/// </summary>
public class ActionManagerBackgroundService : BackgroundService
{
    private const int PeriodicAssignmentTaskPeriodInMs = 1000;
    private readonly ActionManager _actionManager;
    private readonly ILogger _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="actionManager"></param>
    /// <param name="logger"></param>
    public ActionManagerBackgroundService(ActionManager actionManager, ILogger<ActionManagerBackgroundService> logger)
    {
        _actionManager = actionManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
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

            await Task.Delay(PeriodicAssignmentTaskPeriodInMs, stoppingToken);
        }
    }
}