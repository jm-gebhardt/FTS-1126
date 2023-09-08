using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for MoveAgvRequest. It sends the <see cref="QueueActionGroup"/> command to move the agv to the requested destination node
/// </summary>
[UsedImplicitly]
public class MoveAgvCommandHandler : IConsumer<MoveAgv>
{
    private readonly IAgvRegistry _agvRegistry;
    private readonly IRequestClient<QueueActionGroup> _queueActionGroupRequestClient;
    private readonly ILogger<MoveAgvCommandHandler> _logger;

    /// <inheritdoc cref="MoveAgvCommandHandler" />
    public MoveAgvCommandHandler(IAgvRegistry agvRegistry, IRequestClient<QueueActionGroup> queueActionGroupRequestClient, ILogger<MoveAgvCommandHandler> logger)
    {
        _agvRegistry = agvRegistry;
        _queueActionGroupRequestClient = queueActionGroupRequestClient;
        _logger = logger;
    }

    /// <inheritdoc cref="MoveAgvCommandHandler" />
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<MoveAgv> context)
    {
        (var correlationId, string targetNodeId, var creator) = context.Message;

        var agv = _agvRegistry.GetRequiredAgv(correlationId);
        var groupCorrelationId = Guid.NewGuid();
        _logger.LogInformation("{MessageType} for AgvCorrelationId: {AgvCorrelationId} to destinationNodeId:{DestinationNodeId} received. Sending QueueActionGroup {ActionGroupCorrelationId}", nameof(MoveAgv), context.Message.CorrelationId, context.Message.TargetNodeId, groupCorrelationId);

        var moveActionGroup = new ActionGroup(
            groupCorrelationId,
            $"Manual {ActionType.Move}",
            actions: new List<AgvAction> {new(Guid.NewGuid(), ActionType.Move, targetNodeId, groupCorrelationId)},
            assignedAgv: agv.CorrelationId,
            isManual: true,
            creator: creator
        );

        await _queueActionGroupRequestClient.GetResponse<Success>(new QueueActionGroup(moveActionGroup));

        await context.RespondAsync(new MoveAgvResult(moveActionGroup.CorrelationId));
    }
}