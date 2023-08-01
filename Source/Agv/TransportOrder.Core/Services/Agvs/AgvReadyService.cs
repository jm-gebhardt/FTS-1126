using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <summary>
/// This service will check if Agv is ready
/// </summary>
public class AgvReadyService : IAgvReadyService
{
    private readonly IAgvGraphRegistry _agvGraphRegistry;
    private readonly ILogger<AgvReadyService> _logger;

    /// <inheritdoc cref="AgvReadyService" />
    /// <param name="agvGraphRegistry"></param>
    /// <param name="logger"></param>
    public AgvReadyService(IAgvGraphRegistry agvGraphRegistry, ILogger<AgvReadyService> logger)
    {
        _agvGraphRegistry = agvGraphRegistry;
        _logger = logger;
    }
    /// <inheritdoc />
    public async Task<bool> IsAgvCompleteAsync(IAgv agv, bool ignoreCharging = true, bool ignoreLocked = false)
    {
        bool isReady = agv is
        {
            AgvStatus: not null,
            AgvStatus.LastNodeId: not null,
            HasError: false
        };

        isReady &= (agv.CurrentState == nameof(AgvStateMachine.Idle) || (agv.CurrentState is nameof(AgvStateMachine.Charging) && ignoreCharging))
                   && (!agv.IsLocked || ignoreLocked);

        _logger.LogDebug("Agv {AgvId} / {CorrelationId} is ready: {IsReady}. {CurrentState}, {IgnoreCharging}, {IsLocked}, {IgnoreLocked}", agv.AgvId, agv.CorrelationId, isReady, agv.CurrentState, ignoreCharging, agv.IsLocked, ignoreLocked);

        if (!isReady)
        {
            return false;
        }

        // check graph last as this is expensive
        bool graphExists = await _agvGraphRegistry.ExistsAsync(agv.CorrelationId);
        return graphExists;
    }
}