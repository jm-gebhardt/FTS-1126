using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Services;

/// <inheritdoc />
public sealed class AklAgvLimitGuard : IAklAgvLimitGuard
{
    private readonly IHostOrderRepository _hostOrderRepository;
    private readonly IStationRepository _stationRepository;
    private readonly ISystemStatusRepository _systemStatusRepository;
    private readonly ILogger<AklAgvLimitGuard> _logger;

    /// <inheritdoc cref="AklAgvLimitGuard" />
    public AklAgvLimitGuard(
        IHostOrderRepository hostOrderRepository,
        IStationRepository stationRepository,
        ISystemStatusRepository systemStatusRepository,
        ILogger<AklAgvLimitGuard> logger
    )
    {
        _hostOrderRepository = hostOrderRepository;
        _stationRepository = stationRepository;
        _systemStatusRepository = systemStatusRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsWithinAklAgvLimitAsync([NotNullWhen(true)] HostOrder? hostOrder)
    {
        if (hostOrder == null)
        {
            _logger.LogWarning("HostOrder is null, cannot check AKL AGV limit");
            return false;
        }

        var systemStatus = await _systemStatusRepository.GetStatus();
        if (systemStatus is not { UseAklAgvLimit: true })
        {
            _logger.LogInformation("HostOrder {CorrelationId} is within AKL AGV limit because system status does not use AKL AGV limit", hostOrder.CorrelationId);
            return true;
        }

        var stationGroup = await _stationRepository.GetStationGroup(hostOrder.Source, hostOrder.Target);
        if (stationGroup == null)
        {
            _logger.LogWarning("HostOrder {CorrelationId} is within AKL AGV limit because source {Source} and target {Target} are not part of a station group", hostOrder.CorrelationId, hostOrder.Source, hostOrder.Target);
            return true;
        }
        if (stationGroup.IsOccupied)
        {
            _logger.LogWarning("HostOrder {CorrelationId} is not within AKL AGV limit because station group of target {Target} is occupied", hostOrder.CorrelationId, hostOrder.Target);
            return false;
        }
        if (await _hostOrderRepository.HasHostOrdersForStationGroup(stationGroup.Sources, stationGroup.Targets, hostOrder.AssignedAgv))
        {
            _logger.LogWarning("HostOrder {CorrelationId} is not within AKL AGV limit because there are current host orders for station group of target {Target}", hostOrder.CorrelationId, hostOrder.Target);
            return false;
        }

        _logger.LogInformation("HostOrder {CorrelationId} is within AKL AGV limit for target {Target}", hostOrder.CorrelationId, hostOrder.Target);
        return true;
    }
}