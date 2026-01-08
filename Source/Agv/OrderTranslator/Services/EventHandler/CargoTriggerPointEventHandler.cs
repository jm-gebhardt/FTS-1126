using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.OrderTranslator.Services.EventHandler;

/// <summary>
/// Handles agvs passing the cargo trigger point
/// </summary>
[UsedImplicitly]
public class CargoTriggerPointEventHandler(
    ILogger<CargoTriggerPointEventHandler> logger, 
    IHostOrderRepository hostOrderRepository,
    IStationRepository stationRepository) : 
    IConsumer<AgvPassedCargoTriggerPoint>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedCargoTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedCargoTriggerPoint> context)
    {
        (var agv, Guid? actionGroupCorrelationId, var action, bool hasCargo) = context.Message;
        
        if (actionGroupCorrelationId == null)
        {
            logger.LogWarning("Skip reporting APL cargo state to conveyor: Agv {Agv} is not handling pick-drop action group ", agv);
            return;
        }
        if (action == null)
        {
            return;
        }
        if (action.Target.Node == null)
        {
            return;
        }
        var hostOrder = await hostOrderRepository.GetByIdAsync(actionGroupCorrelationId.Value);

        var targetStation = await stationRepository.GetStationByName(action.Target.Node.Name);
        if (targetStation is {NeedsCargoReporting: false})
        {
            logger.LogWarning("Target station {Target} does not need cargo reporting", targetStation.Name);
            return;
        }
        bool isDropping = action is { ActionType: ActionType.Drop };
        if (isDropping != hasCargo)
        {
            logger.LogWarning("Inconsistent information on APL cargo state for Agv {Agv}: message content {MessageContent} / host order {HostOrder}", agv, hasCargo, isDropping);
            return;
        }
        logger.LogInformation("Reporting cargo state of agv {Agv} running action group {ActionGroupCorrelationId} with cargo {HasCargo}", agv, hostOrder.CorrelationId, hasCargo);
        await context.Publish(new CargoReportingRequested(hostOrder.CorrelationId, hasCargo));
    }
}