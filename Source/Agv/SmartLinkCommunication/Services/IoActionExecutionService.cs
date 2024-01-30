using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Services;

/// <summary>
/// Service to execute assigned actions in SmartLink modules
/// </summary>
public class IoActionExecutionService : IIoActionExecutionService
{
    private readonly IBus _bus;

    /// <inheritdoc cref="IoActionExecutionService" />
    /// <param name="bus"></param>
    public IoActionExecutionService(IBus bus)
    {
        _bus = bus;
    }

    private async Task CreateActionGroup(IReadOnlyList<ActionDefinition> actionDefinitions)
    {
        var orderId = Guid.NewGuid();
        int sequence = 0;
        List<AgvAction> testActions = actionDefinitions.Select(a => new AgvAction(Guid.NewGuid(), a.Type, a.NodeId, orderId) { Sequence = sequence++ }).ToList();

        var actionGroup = new ActionGroup(
            orderId,
            orderId.ToString(),
            testActions,
            ActionGroupCreator.Simulation,
            DateTime.UtcNow);
        await _bus.Publish(new QueueActionGroup(actionGroup, true));
    }

    /// <inheritdoc />
    public async Task ExecuteActionAsync(IoActionConfig actionConfig)
    {
        switch (actionConfig.ActionType)
        {
            case ActionTypes.PublishQueueActionGroup:
                List<ActionDefinition> readOnlyList = actionConfig.Parameters.Cast<ActionDefinition>().ToList();
                await CreateActionGroup(readOnlyList);
                break;

            default:
            {
                throw new InvalidOperationException($"Unsupported action type: {actionConfig.ActionType}");
            }
        }
    }
}