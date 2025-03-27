using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles agvs passing the cargo trigger point
/// </summary>
[UsedImplicitly]
public class CargoTriggerPointEventHandler(
    ILogger<CargoTriggerPointEventHandler> logger, 
    IPrismatCommunicationHostOrderRepository hostOrderRepository, 
    IConveyorCommunicator conveyorCommunicator, 
    ITransportOrderActionGroupRepository actionGroupRepository,
    KaeserGraphConfiguration configuration) : 
    IConsumer<AgvPassedCargoTriggerPoint>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedCargoTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedCargoTriggerPoint> context)
    {
        (var agv, Guid? actionGroupCorrelationId, bool hasCargo) = context.Message;
        
        if (actionGroupCorrelationId == null)
        {
            logger.LogWarning("Skip reporting APL cargo state to conveyor: Agv {Agv} is not handling pick-drop action group ", agv);
            return;
        }

        HostOrder hostOrder;
        try
        {
            hostOrder = await hostOrderRepository.GetByIdAsync(actionGroupCorrelationId.Value);
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning("Skip reporting APL cargo state to conveyor: Agv {Agv} is not handling pick-drop action group ", agv);
            return;
        }
        var actionGroup = await actionGroupRepository.GetActionGroupAsync(actionGroupCorrelationId.Value);
        var action = actionGroup!.Actions.FirstOrDefault(a => a.State == ActionState.Started);
        if (action == null)
        {
            logger.LogWarning("Agv without started action passed Cargo trigger point. Current action group is: ({ActionGroup})", actionGroup.CorrelationId);
            return;
        }
        bool isDropping = action is { ActionType: ActionType.Drop };
        
        switch (isDropping)
        {
            case true:
                if (!hostOrder.Actions.All(a => configuration.PrismatIncompletePickTargets.Contains(a.Node)))
                {
                    logger.LogWarning("Skip reporting APL cargo state to conveyor: Agv {Agv} with current action group {ActionGroupCorrelationId} is not heading towards 9060/9070", 
                        actionGroup.CorrelationId, agv);
                    return;
                }
                break;

            case false:
                if (!hostOrder.Actions.All(a => configuration.PrismatIncompleteDropTargets.Contains(a.Node)))
                {
                    logger.LogWarning("Skip reporting APL cargo state to conveyor: Agv {Agv} with current action group {ActionGroupCorrelationId} is not heading towards 9060/9070", 
                        actionGroup.CorrelationId, agv);
                    return;
                }
                break;
        }

        if (isDropping != hasCargo)
        {
            logger.LogWarning("Inconsistent information on APL cargo state for Agv {Agv}: message content {MessageContent} / host order {HostOrder}", agv, hasCargo, isDropping);
            return;
        }
        logger.LogInformation("Reporting cargo state of agv {Agv} running action group {ActionGroupCorrelationId} with cargo {HasCargo}", agv, actionGroup.CorrelationId, hasCargo);
        conveyorCommunicator.ReportAplCargoState(hasCargo);
    }
}