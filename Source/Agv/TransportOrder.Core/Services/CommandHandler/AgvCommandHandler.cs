using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides a handler for agv related commands.
/// </summary>
[UsedImplicitly]
public class AgvCommandHandler :
    IConsumer<TryReactivateChargingAgv>
{
    private readonly ILogger<AgvCommandHandler> _logger;
    private readonly IActionManagerSemaphoreProvider _actionManagerSemaphoreProvider;
    private readonly IChargingService _chargingService;
    private readonly IAgvSagaCommunicationService _agvSagaCommunicationService;
    private readonly IActionGroupRegistry _actionGroupRegistry;

    /// <summary>
    /// Initializes a new <see cref="AgvCommandHandler"/> instance.
    /// </summary>
    public AgvCommandHandler(IActionManagerSemaphoreProvider actionManagerSemaphoreProvider,
        IChargingService chargingService, IAgvSagaCommunicationService agvSagaCommunicationService,
        IActionGroupRegistry actionGroupRegistry, ILogger<AgvCommandHandler> logger)
    {
        _logger = logger;
        _actionManagerSemaphoreProvider = actionManagerSemaphoreProvider;
        _chargingService = chargingService;
        _agvSagaCommunicationService = agvSagaCommunicationService;
        _actionGroupRegistry = actionGroupRegistry;
    }

    /// <summary>
    /// Handles the <see cref="TryReactivateChargingAgv"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryReactivateChargingAgv> context)
    {
        var cancellationToken = context.CancellationToken;
        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            List<IAgv> goodAgvs = await _chargingService.GetChargingAgvsWithGoodBatteryLevel();

            // preliminary logic: just go through all potential available Agvs and try to start a new action
            foreach (var agv in goodAgvs.OrderByDescending(a => a.GetRequiredAgvStatus().BatteryCharge))
            {
                if (await _actionGroupRegistry.AgvHasPendingActions(agv.CorrelationId))
                {
                    _logger.LogDebug("There is a new action for the charging Agv with CorrelationId: {AgvCorrelationId}. So stop charging by setting it to idle, so that it takes the new Action", agv.CorrelationId);
                    await _agvSagaCommunicationService.SetIdleAsync(agv.CorrelationId, cancellationToken);
                    // only do this for the Agv with the best battery level and quit
                    return;
                }

                _logger.LogDebug("No action needed for charging Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);
            }
        }, cancellationToken);
    }
}