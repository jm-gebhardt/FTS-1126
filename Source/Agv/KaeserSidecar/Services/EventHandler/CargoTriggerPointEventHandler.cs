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
public class CargoTriggerPointEventHandler(ILogger<CargoTriggerPointEventHandler> logger, IPrismatCommunicationHostOrderRepository hostOrderRepository, IConveyorCommunicator conveyorCommunicator, KaeserGraphConfiguration configuration) : 
    IConsumer<AgvPassedCargoTriggerPoint>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedCargoTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedCargoTriggerPoint> context)
    {
        (var agv, Guid? actionGroup, bool hasCargo) = context.Message;
        
        if (actionGroup == null)
        {
            logger.LogWarning("Skip reporting APL cargo state to conveyor: Agv {Agv} is not handling pick-drop action group ", agv);
            return;
        }

        HostOrder hostOrder;
        try
        {
            hostOrder = await hostOrderRepository.GetByIdAsync(actionGroup.Value);
        }
        catch (Exception)
        {
            logger.LogWarning("Skip reporting APL cargo state to conveyor: Agv {Agv} is not handling pick-drop action group ", agv);
            return;
        }

        bool isPicking = hostOrder.Actions.All(a => configuration.PrismatIncompleteDropTargets.Contains(a.Node));
        bool isDropping = hostOrder.Actions.All(a => configuration.PrismatIncompletePickTargets.Contains(a.Node));
        if (!isDropping && !isPicking)
        {
            logger.LogWarning("Skip reporting APL cargo state to conveyor: Agv {Agv} is not handling pick-drop action group heading towards 9060/9070", agv);
            return;
        }
        if (isDropping != hasCargo)
        {
            logger.LogWarning("Inconsistent information on APL cargo state for Agv {Agv}: message content {MessageContent} / host order {HostOrder}", agv, hasCargo, isDropping);
        }

        conveyorCommunicator.ReportAplCargoState(hasCargo);
    }
}