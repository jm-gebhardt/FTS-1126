using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Services;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Provides a background service to periodically check for zone status of agvs.
/// </summary>
public class ZoneRefreshBackgroundService : BackgroundService
{
    private readonly IZoneRefreshStateService _stateService;
    private readonly IBus _bus;
    private readonly ILogger<ZoneRefreshBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(5);

    /// <inheritdoc cref="ZoneRefreshBackgroundService"/>>
    public ZoneRefreshBackgroundService(
        IZoneRefreshStateService stateService,
        IBus bus,
        ILogger<ZoneRefreshBackgroundService> logger)
    {
        _stateService = stateService;
        _bus = bus;
        _logger = logger;
    }

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Zone refresh background service started");

        // Delay to ensure other services are ready
        await Task.Delay(_initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_stateService.ZoneRefreshRequired)
                {
                    await ExecuteZoneRefresh();
                }
                else
                {
                    _logger.LogDebug("Scheduler inactive - no offline AGVs in zones or no online AGVs available");
                }
            }
            catch (OperationCanceledException)
            {
                // Stop service
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in zone refresh background service");
            }
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Zone refresh background service stopped");
    }

    private async Task ExecuteZoneRefresh()
    {
        string? nextOnlineAgv = _stateService.GetNextOnlineAgv();
        if (nextOnlineAgv == null)
        {
            _logger.LogWarning("No online AGV available for zone refresh");
            return;
        }

        await _bus.Publish(new RequestAgvZones(nextOnlineAgv.ToGuid(), nextOnlineAgv));

        int offlineCount = _stateService.OfflineAgvsInZones.Count;
        _logger.LogDebug("Zone refresh requested from AGV {TargetAgv} for {OfflineCount} offline AGVs", nextOnlineAgv, offlineCount);
    }

    /// <inheritdoc cref="BackgroundService"/>>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Zone refresh background service is stopping");
        await base.StopAsync(cancellationToken);
    }
}